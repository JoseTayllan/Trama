using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public interface IClipboardImageSource
{
    // A non-null stream is owned by the caller; null preserves native text paste.
    Task<Stream?> OpenImageAsync(CancellationToken cancellationToken = default);
}

public sealed record EditorImageInput(string Name, Func<CancellationToken, Task<Stream>> OpenReadAsync);

public enum ImagePasteStatus
{
    Imported,
    NoImage,
    Failed,
    Busy
}

public sealed partial class NotebookEditorViewModel
{
    public async Task<ImagePasteStatus> PasteImageAsync(IClipboardImageSource source)
    {
        if (IsBusy) return ImagePasteStatus.Busy;
        IsBusy = true;
        try
        {
            await using var stream = await source.OpenImageAsync();
            if (stream is null) { Feedback = "Nenhuma imagem na área de transferência. A colagem de texto continua disponível."; return ImagePasteStatus.NoImage; }
            await AppendImageAsync(stream);
            Feedback = IsAutoSaveEnabled ? "Print adicionado. A página será salva automaticamente após a pausa." : "Print adicionado. Salve a página para registrar as alterações.";
            return ImagePasteStatus.Imported;
        }
        catch (Exception)
        {
            Feedback = "Não foi possível colar a imagem. Use PNG/JPEG de até 10 MiB. Sua página foi mantida.";
            return ImagePasteStatus.Failed;
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> ImportDroppedImagesAsync(IReadOnlyList<EditorImageInput> inputs)
    {
        if (IsBusy) return false;
        IsBusy = true;
        var imported = 0;
        var rejected = 0;
        try
        {
            foreach (var input in inputs)
            {
                try
                {
                    await using var stream = await input.OpenReadAsync(CancellationToken.None);
                    await AppendImageAsync(stream);
                    imported++;
                }
                catch (Exception) { rejected++; }
            }
            Feedback = $"{imported} imagem(ns) adicionada(s); {rejected} item(ns) rejeitado(s). " +
                (IsAutoSaveEnabled ? "A página será salva automaticamente após a pausa." : "Salve para registrar as alterações.");
            return imported > 0;
        }
        finally { IsBusy = false; }
    }

    private async Task AppendImageAsync(Stream source)
    {
        var attachment = await _actions.ImportImageAsync(source);
        var block = new NotebookBlockEditor { Id = Guid.NewGuid(), Kind = NotebookBlockKind.Image, AttachmentId = attachment.Id };
        block.PropertyChanged += BlockEdited;
        _blocks.Add(block);
        MarkEdited();
    }

    public bool FormatTextSelection(Guid blockId, int offset, int length, InlineTextStyle style)
    {
        if (!CanEditContent || length == 0 || _blocks.FirstOrDefault(block => block.Id == blockId && block.IsText) is not { } block) return false;
        try
        {
            var document = new InlineTextDocument(block.Text, block.Styles);
            var selected = block.Styles.Where(span => span.Offset < offset + length && span.Offset + span.Length > offset).ToArray();
            var covered = selected.Where(span => (span.Style & style) == style)
                .Sum(span => Math.Min(offset + length, span.Offset + span.Length) - Math.Max(offset, span.Offset));
            document.SetStyle(offset, length, style, covered != length);
            block.Styles = document.Styles;
            return true;
        }
        catch (ArgumentException) { Feedback = "Selecione um trecho válido para formatar."; return false; }
    }

    public bool FormatTextSize(Guid blockId, int offset, int length, int? size) =>
        ChangeTextFormatting(blockId, offset, length, document => document.SetFontSize(offset, length, size));

    public bool FormatTextForeground(Guid blockId, int offset, int length, string? color) =>
        ChangeTextFormatting(blockId, offset, length, document => document.SetForeground(offset, length, color));

    public bool FormatTextHighlight(Guid blockId, int offset, int length, string? color) =>
        ChangeTextFormatting(blockId, offset, length, document => document.SetHighlight(offset, length, color));

    public bool ClearTextFormatting(Guid blockId, int offset, int length) =>
        ChangeTextFormatting(blockId, offset, length, document => document.ClearFormatting(offset, length));

    private bool ChangeTextFormatting(Guid blockId, int offset, int length, Action<InlineTextDocument> change)
    {
        if (!CanEditContent || length == 0 || _blocks.FirstOrDefault(block => block.Id == blockId && block.IsText) is not { } block) return false;
        try
        {
            var document = new InlineTextDocument(block.Text, block.Styles);
            change(document);
            if (block.Styles.SequenceEqual(document.Styles)) return false;
            block.Styles = document.Styles;
            return true;
        }
        catch (ArgumentException) { Feedback = "Selecione um trecho válido para formatar."; return false; }
    }

    public bool EditText(Guid blockId, int offset, int length, string replacement)
    {
        if (!CanEditContent || _blocks.FirstOrDefault(block => block.Id == blockId && block.IsText) is not { } block) return false;
        try
        {
            var document = new InlineTextDocument(block.Text, block.Styles);
            var lists = new ParagraphListDocument(block.Text, block.Lists);
            document.ReplaceText(offset, length, replacement);
            lists.ReplaceText(offset, length, replacement);
            block.Text = document.Text;
            block.Styles = document.Styles;
            block.Lists = lists.Lists;
            return true;
        }
        catch (ArgumentException) { Feedback = "Não foi possível aplicar esta edição. Seu conteúdo foi mantido."; return false; }
    }

    public bool FormatParagraphList(Guid blockId, int offset, int length, string kind)
    {
        if (!CanEditContent || _blocks.FirstOrDefault(block => block.Id == blockId && block.IsText) is not { } block) return false;
        try
        {
            var document = new ParagraphListDocument(block.Text, block.Lists);
            document.SetKind(offset, length, kind);
            if (block.Lists.SequenceEqual(document.Lists)) return false;
            block.Lists = document.Lists;
            return true;
        }
        catch (ArgumentException) { Feedback = "Selecione um parágrafo válido para formatar."; return false; }
    }
}
