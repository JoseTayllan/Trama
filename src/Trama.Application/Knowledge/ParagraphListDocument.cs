namespace Trama.Application.Knowledge;

public sealed record ParagraphListItem(int Offset, string Kind);

public sealed class ParagraphListDocument
{
    public ParagraphListDocument(string text, IReadOnlyList<ParagraphListItem>? lists = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        var starts = Starts(text).ToHashSet();
        var seen = new HashSet<int>();
        var items = (lists ?? Array.Empty<ParagraphListItem>()).ToArray();
        foreach (var item in items)
        {
            if (item is null || item.Kind is not ("bullet" or "numbered") ||
                !starts.Contains(item.Offset) || !seen.Add(item.Offset))
                throw new ArgumentException("Invalid paragraph list metadata.", nameof(lists));
        }
        Lists = items.OrderBy(item => item.Offset).ToArray();
    }

    public string Text { get; private set; }
    public IReadOnlyList<ParagraphListItem> Lists { get; private set; }

    public static IReadOnlyList<int> Starts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
            else if (text[i] is not ('\r' or '\n')) continue;
            starts.Add(i + 1);
        }
        return starts;
    }

    public IReadOnlyList<int> SelectedStarts(int offset, int length)
    {
        ValidateRange(offset, length);
        var starts = Starts(Text);
        var first = starts.Last(start => start <= offset);
        return starts.Where(start => start == first || (start > offset && start < offset + length)).ToArray();
    }

    public void SetKind(int offset, int length, string kind)
    {
        if (kind is not ("none" or "bullet" or "numbered")) throw new ArgumentException("Unsupported list kind.", nameof(kind));
        var selected = SelectedStarts(offset, length).ToHashSet();
        Lists = Lists.Where(item => !selected.Contains(item.Offset))
            .Concat(kind == "none" ? [] : selected.Select(start => new ParagraphListItem(start, kind)))
            .OrderBy(item => item.Offset).ToArray();
    }

    public void ReplaceText(int offset, int length, string replacement)
    {
        ValidateRange(offset, length);
        ArgumentNullException.ThrowIfNull(replacement);
        var inherited = Lists.FirstOrDefault(item => item.Offset == Starts(Text).Last(start => start <= offset))?.Kind;
        var removedEnd = offset + length;
        var delta = replacement.Length - length;
        var updated = string.Concat(Text.AsSpan(0, offset), replacement, Text.AsSpan(removedEnd));
        var starts = Starts(updated).ToHashSet();
        var mapped = new Dictionary<int, ParagraphListItem>();
        foreach (var item in Lists)
        {
            if (item.Offset > offset && item.Offset < removedEnd) continue;
            var position = item.Offset <= offset ? item.Offset : item.Offset + delta;
            if (starts.Contains(position)) mapped.TryAdd(position, item with { Offset = position });
        }
        if (inherited is not null)
            foreach (var start in starts.Where(start => start > offset && start <= offset + replacement.Length))
                mapped[start] = new ParagraphListItem(start, inherited);
        Text = updated;
        Lists = mapped.Values.OrderBy(item => item.Offset).ToArray();
    }

    private void ValidateRange(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > Text.Length || length > Text.Length - offset)
            throw new ArgumentOutOfRangeException(nameof(offset));
    }
}
