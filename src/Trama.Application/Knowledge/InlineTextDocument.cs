using System.Text.Json.Serialization;

namespace Trama.Application.Knowledge;

[Flags]
public enum InlineTextStyle
{
    None = 0,
    Bold = 1,
    Italic = 2
}

public sealed record TextStyleSpan(int Offset, int Length, InlineTextStyle Style)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FontSize { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Foreground { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Highlight { get; init; }
}

public sealed class InlineTextDocument
{
    public InlineTextDocument(string text, IReadOnlyList<TextStyleSpan>? styles = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        var copy = (styles ?? Array.Empty<TextStyleSpan>()).ToArray();
        var end = 0;
        foreach (var span in copy.OrderBy(span => span?.Offset))
        {
            ArgumentNullException.ThrowIfNull(span);
            ValidateRange(span.Offset, span.Length);
            if (span.Style != InlineTextStyle.None) ValidateStyle(span.Style);
            if (!HasFormatting(span)) throw new ArgumentException("Empty text formatting.", nameof(styles));
            ValidateAttributes(span);
            if (span.Length == 0 || span.Offset < end) throw new ArgumentException("Invalid style intervals.", nameof(styles));
            end = span.Offset + span.Length;
        }
        Styles = Canonical(copy);
    }

    public string Text { get; private set; }
    public IReadOnlyList<TextStyleSpan> Styles { get; private set; }
    public void SetStyle(int offset, int length, InlineTextStyle style, bool enabled)
    {
        ValidateRange(offset, length);
        ValidateStyle(style);
        ChangeFormatting(offset, length, span => span with { Style = enabled ? span.Style | style : span.Style & ~style });
    }

    public void SetFontSize(int offset, int length, int? size) =>
        ChangeFormatting(offset, length, span => span with { FontSize = size });

    public void SetForeground(int offset, int length, string? color) =>
        ChangeFormatting(offset, length, span => span with { Foreground = color });

    public void SetHighlight(int offset, int length, string? color) =>
        ChangeFormatting(offset, length, span => span with { Highlight = color });

    public void ClearFormatting(int offset, int length) =>
        ChangeFormatting(offset, length, span => new TextStyleSpan(span.Offset, span.Length, InlineTextStyle.None));

    private void ChangeFormatting(int offset, int length, Func<TextStyleSpan, TextStyleSpan> change)
    {
        ValidateRange(offset, length);
        if (length == 0) return;
        var boundaries = Styles.SelectMany(span => new[] { span.Offset, span.Offset + span.Length })
            .Concat([0, Text.Length, offset, offset + length]).Distinct().Order().ToArray();
        var updated = new List<TextStyleSpan>();
        var spanIndex = 0;
        for (var index = 0; index < boundaries.Length - 1; index++)
        {
            var start = boundaries[index];
            while (spanIndex < Styles.Count && Styles[spanIndex].Offset + Styles[spanIndex].Length <= start) spanIndex++;
            var current = spanIndex < Styles.Count && Styles[spanIndex].Offset <= start
                ? Styles[spanIndex] : new TextStyleSpan(start, boundaries[index + 1] - start, InlineTextStyle.None);
            var interval = current with { Offset = start, Length = boundaries[index + 1] - start };
            if (start >= offset && start < offset + length) interval = change(interval);
            ValidateAttributes(interval);
            if (HasFormatting(interval)) updated.Add(interval);
        }
        Styles = Canonical(updated);
    }

    public void ReplaceText(int offset, int length, string replacement)
    {
        ValidateRange(offset, length);
        ArgumentNullException.ThrowIfNull(replacement);
        var inherited = SpanAt(offset == Text.Length ? offset - 1 : offset);
        var removedEnd = offset + length;
        var delta = replacement.Length - length;
        var updated = new List<TextStyleSpan>();
        foreach (var span in Styles)
        {
            var end = span.Offset + span.Length;
            var leftEnd = Math.Min(end, offset);
            if (leftEnd > span.Offset) updated.Add(span with { Length = leftEnd - span.Offset });
            var rightStart = Math.Max(span.Offset, removedEnd);
            if (end > rightStart) updated.Add(span with { Offset = rightStart + delta, Length = end - rightStart });
        }
        if (replacement.Length != 0 && inherited is not null)
            updated.Add(inherited with { Offset = offset, Length = replacement.Length });
        Text = string.Concat(Text.AsSpan(0, offset), replacement, Text.AsSpan(removedEnd));
        Styles = Canonical(updated);
    }

    public InlineTextStyle StyleAt(int offset) => SpanAt(offset)?.Style ?? InlineTextStyle.None;

    private TextStyleSpan? SpanAt(int offset) => Styles.FirstOrDefault(span => offset >= span.Offset && offset < span.Offset + span.Length);

    private static bool HasFormatting(TextStyleSpan span) => span.Style != InlineTextStyle.None ||
        span.FontSize is not null || span.Foreground is not null || span.Highlight is not null;

    private static void ValidateAttributes(TextStyleSpan span)
    {
        if (span.FontSize is not (null or 12 or 14 or 16 or 18 or 24 or 32))
            throw new ArgumentException("Unsupported text size.", nameof(span));
        if (span.Foreground is not (null or "teal" or "blue" or "violet" or "amber" or "coral"))
            throw new ArgumentException("Unknown foreground color.", nameof(span));
        if (span.Highlight is not (null or "amber" or "green" or "blue" or "violet"))
            throw new ArgumentException("Unknown highlight color.", nameof(span));
    }

    private void ValidateRange(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > Text.Length || length > Text.Length - offset)
            throw new ArgumentOutOfRangeException(nameof(offset));
    }
    private static void ValidateStyle(InlineTextStyle style)
    {
        if (style == InlineTextStyle.None || (style & ~(InlineTextStyle.Bold | InlineTextStyle.Italic)) != 0)
            throw new ArgumentException("Unknown text style.", nameof(style));
    }
    private static IReadOnlyList<TextStyleSpan> Canonical(IEnumerable<TextStyleSpan> source)
    {
        var result = new List<TextStyleSpan>();
        foreach (var span in source.OrderBy(span => span.Offset))
        {
            if (result.Count > 0 && result[^1].Offset + result[^1].Length == span.Offset &&
                result[^1].Style == span.Style && result[^1].FontSize == span.FontSize &&
                result[^1].Foreground == span.Foreground && result[^1].Highlight == span.Highlight)
                result[^1] = result[^1] with { Length = result[^1].Length + span.Length };
            else result.Add(span);
        }
        return result.Count == 0 ? Array.Empty<TextStyleSpan>() : result.AsReadOnly();
    }
}
