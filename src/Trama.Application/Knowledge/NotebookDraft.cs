namespace Trama.Application.Knowledge;

public abstract record NotebookBlock(Guid Id);

public sealed record NotebookTextBlock(Guid Id, string Text) : NotebookBlock(Id)
{
    private IReadOnlyList<ParagraphListItem> _lists = Array.Empty<ParagraphListItem>();
    public IReadOnlyList<TextStyleSpan> Styles { get; init; } = Array.Empty<TextStyleSpan>();
    public IReadOnlyList<ParagraphListItem> Lists { get => _lists; init => _lists = value ?? Array.Empty<ParagraphListItem>(); }
}

public sealed record NotebookCodeBlock(Guid Id, string Code, string Language) : NotebookBlock(Id);

public sealed record NotebookImageBlock(Guid Id, Guid AttachmentId, string Caption) : NotebookBlock(Id);

public sealed class NotebookDraft
{
    private readonly List<NotebookBlock> _blocks = [];
    private readonly IReadOnlyList<NotebookBlock> _blocksView;

    public NotebookDraft()
    {
        _blocksView = _blocks.AsReadOnly();
    }

    public IReadOnlyList<NotebookBlock> Blocks => _blocksView;

    public bool HasContent => _blocks.Any(block => block switch
    {
        NotebookTextBlock text => !string.IsNullOrWhiteSpace(text.Text),
        NotebookCodeBlock code => !string.IsNullOrWhiteSpace(code.Code),
        NotebookImageBlock image => image.AttachmentId != Guid.Empty,
        _ => false
    });

    public void Insert(int index, NotebookBlock block)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _blocks.Count);
        ValidateBlock(block);
        if (_blocks.Any(existing => existing.Id == block.Id))
        {
            throw new ArgumentException("A block with this identity already exists.", nameof(block));
        }

        _blocks.Insert(index, block);
    }

    public void Update(NotebookBlock block)
    {
        ValidateBlock(block);
        var index = FindIndex(block.Id);
        _blocks[index] = block;
    }

    public void Move(Guid blockId, int targetIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(targetIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(targetIndex, _blocks.Count);
        var sourceIndex = FindIndex(blockId);
        var block = _blocks[sourceIndex];
        _blocks.RemoveAt(sourceIndex);
        _blocks.Insert(targetIndex, block);
    }

    public void Remove(Guid blockId)
    {
        _blocks.RemoveAt(FindIndex(blockId));
    }

    private int FindIndex(Guid blockId)
    {
        var index = _blocks.FindIndex(block => block.Id == blockId);
        if (index < 0)
        {
            throw new ArgumentException("The block does not exist in this draft.", nameof(blockId));
        }

        return index;
    }

    private static void ValidateBlock(NotebookBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (block is NotebookTextBlock text) _ = new ParagraphListDocument(text.Text, text.Lists);
        if (block.Id == Guid.Empty)
        {
            throw new ArgumentException("A block must have an identity.", nameof(block));
        }

        if (block is NotebookImageBlock { AttachmentId: var attachmentId } && attachmentId == Guid.Empty)
        {
            throw new ArgumentException("An image must reference an attachment.", nameof(block));
        }
    }
}
