using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trama.Application.Knowledge;

namespace Trama.Desktop.ViewModels;

public enum NotebookBlockKind
{
    Text,
    Code,
    Image
}

public sealed class NotebookBlockEditor : ObservableObject
{
    private string _text = string.Empty;
    private string _language = "plaintext";
    private IReadOnlyList<TextStyleSpan> _styles = Array.Empty<TextStyleSpan>();
    private IReadOnlyList<ParagraphListItem> _lists = Array.Empty<ParagraphListItem>();

    public Guid Id { get; init; }
    public NotebookBlockKind Kind { get; init; }
    public string Text { get => _text; set => SetProperty(ref _text, value ?? string.Empty); }
    public string Language { get => _language; set => SetProperty(ref _language, value ?? "plaintext"); }
    public Guid? AttachmentId { get; init; }
    public IReadOnlyList<TextStyleSpan> Styles { get => _styles; set => SetProperty(ref _styles, value); }
    public IReadOnlyList<ParagraphListItem> Lists { get => _lists; set => SetProperty(ref _lists, value); }
    public bool IsText => Kind == NotebookBlockKind.Text;
    public bool IsCode => Kind == NotebookBlockKind.Code;
    public bool IsImage => Kind == NotebookBlockKind.Image;
}

public sealed partial class NotebookEditorViewModel : ObservableObject, IDisposable
{
    private readonly ObservableCollection<NotebookBlockEditor> _blocks = [];
    private readonly INotebookActions _actions;
    private string _title = string.Empty;
    private Guid _pageId = Guid.NewGuid();
    private bool _isBusy;
    private string _feedback = "As alterações são salvas quando você clica em Salvar.";
    private NotebookSaveStatus? _lastSaveStatus;
    private long _revision;
    private long _savedRevision;
    private bool _restoring = true;

    public NotebookEditorViewModel(INotebookActions actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        _actions = actions;
        Blocks = new ReadOnlyObservableCollection<NotebookBlockEditor>(_blocks);
        AddTextCommand = new RelayCommand(() => AddBlock(NotebookBlockKind.Text), () => !IsBusy);
        AddCodeCommand = new RelayCommand(() => AddBlock(NotebookBlockKind.Code), () => !IsBusy);
        RemoveBlockCommand = new RelayCommand<Guid>(RemoveBlock, _ => !IsBusy);
        MoveBlockUpCommand = new RelayCommand<Guid>(id => MoveBlock(id, -1), _ => !IsBusy);
        MoveBlockDownCommand = new RelayCommand<Guid>(id => MoveBlock(id, 1), _ => !IsBusy);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        AddBlock(NotebookBlockKind.Text);
        _restoring = false;
    }

    public NotebookEditorViewModel(INotebookActions actions, TimeProvider autoSaveClock) : this(actions)
    {
        ArgumentNullException.ThrowIfNull(autoSaveClock);
        _autoSaveClock = autoSaveClock;
        Feedback = "Salvamento automático após 2 segundos de pausa. Você também pode usar Salvar página.";
    }

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value ?? string.Empty)) MarkEdited();
        }
    }
    public Guid PageId { get => _pageId; private set => SetProperty(ref _pageId, value); }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                AddTextCommand.NotifyCanExecuteChanged();
                AddCodeCommand.NotifyCanExecuteChanged();
                RemoveBlockCommand.NotifyCanExecuteChanged();
                MoveBlockUpCommand.NotifyCanExecuteChanged();
                MoveBlockDownCommand.NotifyCanExecuteChanged();
                SaveCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanEditContent));
                if (!value && !_isAutomaticSave) TrySaveWhenDue();
            }
        }
    }

    public string Feedback { get => _feedback; private set => SetProperty(ref _feedback, value); }
    public NotebookSaveStatus? LastSaveStatus { get => _lastSaveStatus; private set => SetProperty(ref _lastSaveStatus, value); }
    public ReadOnlyObservableCollection<NotebookBlockEditor> Blocks { get; }
    public bool HasUnsavedChanges => _revision != _savedRevision;

    public IRelayCommand AddTextCommand { get; }
    public IRelayCommand AddCodeCommand { get; }
    public IRelayCommand<Guid> RemoveBlockCommand { get; }
    public IRelayCommand<Guid> MoveBlockUpCommand { get; }
    public IRelayCommand<Guid> MoveBlockDownCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    private void AddBlock(NotebookBlockKind kind)
    {
        if (!IsBusy)
        {
            var block = new NotebookBlockEditor
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                Language = kind == NotebookBlockKind.Code ? "csharp" : "plaintext"
            };
            block.PropertyChanged += BlockEdited;
            _blocks.Add(block);
            MarkEdited();
        }
    }

    private void RemoveBlock(Guid id)
    {
        if (!IsBusy && _blocks.FirstOrDefault(block => block.Id == id) is { } block)
        {
            _blocks.Remove(block);
            block.PropertyChanged -= BlockEdited;
            MarkEdited();
        }
    }

    private void MoveBlock(Guid id, int offset)
    {
        if (IsBusy)
        {
            return;
        }

        var block = _blocks.FirstOrDefault(block => block.Id == id);
        var index = block is null ? -1 : _blocks.IndexOf(block);
        var target = index + offset;
        if (index >= 0 && target >= 0 && target < _blocks.Count)
        {
            _blocks.Move(index, target);
            MarkEdited();
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy || _disposed)
        {
            return;
        }

        CancelScheduledAutoSave();
        IsBusy = true;
        LastSaveStatus = null;
        Feedback = "Salvando página…";
        try
        {
            var revision = _revision;
            var snapshot = new NotebookSnapshot(PageId, Title, _blocks.Select(ToSnapshot).ToArray());
            var result = await _actions.SaveAsync(snapshot);
            if (result.Status == NotebookSaveStatus.Saved)
            {
                _savedRevision = revision;
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
            LastSaveStatus = result.Status;
            Feedback = result.Status switch
            {
                NotebookSaveStatus.Saved => HasUnsavedChanges
                    ? "Versão anterior salva. As alterações mais recentes ainda estão pendentes."
                    : "Página salva neste computador. Pode continuar escrevendo.",
                NotebookSaveStatus.ValidationFailed => "Escreva um título ou adicione conteúdo à página.",
                _ => "Não foi possível salvar. Seu conteúdo foi mantido."
            };
        }
        catch (OperationCanceledException)
        {
            Feedback = "Salvamento cancelado. Seu conteúdo foi mantido.";
        }
        catch (Exception)
        {
            Feedback = "Não foi possível salvar. Seu conteúdo foi mantido.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportImageAsync(Stream source, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Feedback = "Importando imagem…";
        try
        {
            var attachment = await _actions.ImportImageAsync(source, cancellationToken);
            var block = new NotebookBlockEditor
            {
                Id = Guid.NewGuid(),
                Kind = NotebookBlockKind.Image,
                AttachmentId = attachment.Id
            };
            block.PropertyChanged += BlockEdited;
            _blocks.Add(block);
            MarkEdited();
            Feedback = IsAutoSaveEnabled
                ? "Imagem adicionada. A página será salva automaticamente após a pausa."
                : "Imagem adicionada. Salve a página para registrar suas alterações.";
        }
        catch (OperationCanceledException)
        {
            Feedback = "Importação cancelada. Sua página foi mantida.";
        }
        catch (InvalidDataException)
        {
            Feedback = "Escolha uma imagem PNG ou JPEG de até 10 MiB.";
        }
        catch (Exception)
        {
            Feedback = "Não foi possível importar a imagem. Sua página foi mantida.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task<Stream?> OpenImageAsync(Guid id, CancellationToken cancellationToken = default) =>
        _actions.OpenImageAsync(id, cancellationToken);

    public void ReportImageSelectionFailure() => Feedback = "Não foi possível abrir a imagem selecionada. Sua página foi mantida.";

    public async Task LoadAsync(Guid id, CancellationToken cancellationToken = default) =>
        await TryLoadAsync(id, cancellationToken);

    public async Task<bool> TryLoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        Feedback = "Abrindo página…";
        try
        {
            var page = await _actions.LoadAsync(id, cancellationToken);
            if (page is null)
            {
                Feedback = "Página não encontrada. Seu conteúdo atual foi mantido.";
                return false;
            }

            RestoreSnapshot(page);

            LastSaveStatus = null;
            Feedback = IsAutoSaveEnabled
                ? "Página aberta. As próximas alterações serão salvas automaticamente."
                : "Página aberta. Salve quando terminar de editar.";
            return true;
        }
        catch (OperationCanceledException)
        {
            Feedback = "Abertura cancelada. Seu conteúdo atual foi mantido.";
        }
        catch (Exception)
        {
            Feedback = "Não foi possível abrir a página. Seu conteúdo atual foi mantido.";
        }
        finally
        {
            IsBusy = false;
        }

        return false;
    }

    internal void RestoreSnapshot(NotebookSnapshot page)
    {
        var blocks = page.Blocks.Select(ToEditor).ToArray();
        CancelScheduledAutoSave();
        _restoring = true;
        try
        {
            foreach (var block in _blocks) block.PropertyChanged -= BlockEdited;
            _blocks.Clear();
            foreach (var block in blocks)
            {
                block.PropertyChanged += BlockEdited;
                _blocks.Add(block);
            }

            PageId = page.Id;
            Title = page.Title;
            _revision = 0;
            _savedRevision = 0;
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
        finally
        {
            _restoring = false;
        }

    }

    private void BlockEdited(object? sender, PropertyChangedEventArgs e) => MarkEdited();

    private void MarkEdited()
    {
        if (!_restoring)
        {
            _revision++;
            OnPropertyChanged(nameof(HasUnsavedChanges));
            ScheduleAutoSave();
        }
    }

    private static NotebookBlock ToSnapshot(NotebookBlockEditor block) => block.Kind switch
    {
        NotebookBlockKind.Text => new NotebookTextBlock(block.Id, block.Text) { Styles = block.Styles.ToArray(), Lists = block.Lists.ToArray() },
        NotebookBlockKind.Code => new NotebookCodeBlock(block.Id, block.Text, block.Language),
        NotebookBlockKind.Image when block.AttachmentId is { } id => new NotebookImageBlock(block.Id, id, block.Text),
        _ => throw new InvalidOperationException("The block type is not supported.")
    };

    private static NotebookBlockEditor ToEditor(NotebookBlock block) => block switch
    {
        NotebookTextBlock text => new() { Id = text.Id, Kind = NotebookBlockKind.Text, Text = text.Text, Styles = text.Styles, Lists = text.Lists },
        NotebookCodeBlock code => new() { Id = code.Id, Kind = NotebookBlockKind.Code, Text = code.Code, Language = code.Language },
        NotebookImageBlock image => new() { Id = image.Id, Kind = NotebookBlockKind.Image, AttachmentId = image.AttachmentId, Text = image.Caption },
        _ => throw new InvalidOperationException("The block type is not supported.")
    };
}
