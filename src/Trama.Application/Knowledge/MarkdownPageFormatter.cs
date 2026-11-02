using System.Text;
using System.Text.RegularExpressions;

namespace Trama.Application.Knowledge;

public static class MarkdownPageFormatter
{
    public static string Format(NotebookSnapshot page, IReadOnlyDictionary<Guid, string> images)
    {
        var title = string.IsNullOrWhiteSpace(page.Title) ? "Sem título" : Regex.Replace(page.Title.Trim(), @"\s+", " ");
        var output = new StringBuilder("# " + Escape(title) + "\n\n");
        foreach (var block in page.Blocks)
        {
            switch (block)
            {
                case NotebookTextBlock text:
                    output.Append(FormatText(text));
                    break;
                case NotebookCodeBlock code:
                    var longest = Regex.Matches(code.Code, "`+").Select(match => match.Length).DefaultIfEmpty(0).Max();
                    var fence = new string('`', Math.Max(3, longest + 1));
                    var language = Regex.IsMatch(code.Language, @"\A[a-zA-Z0-9_+-]{1,32}\z") ? code.Language : "text";
                    output.Append(fence).Append(language).Append('\n').Append(code.Code.Replace("\r\n", "\n", StringComparison.Ordinal));
                    if (!code.Code.EndsWith('\n')) output.Append('\n');
                    output.Append(fence);
                    break;
                case NotebookImageBlock image:
                    output.Append("![").Append(Escape(Regex.Replace(image.Caption, @"\s+", " ")))
                        .Append("](").Append(images[image.AttachmentId]).Append(')');
                    break;
                default:
                    throw new ArgumentException("Unsupported export block.", nameof(page));
            }
            output.Append("\n\n");
        }
        return output.ToString();
    }

    private static string FormatText(NotebookTextBlock block)
    {
        var output = new StringBuilder();
        var starts = ParagraphListDocument.Starts(block.Text);
        var lists = block.Lists.ToDictionary(item => item.Offset, item => item.Kind);
        var number = 0;
        string? previous = null;
        for (var i = 0; i < starts.Count; i++)
        {
            var start = starts[i];
            var end = i + 1 < starts.Count ? starts[i + 1] : block.Text.Length;
            while (end > start && block.Text[end - 1] is '\r' or '\n') end--;
            var kind = lists.GetValueOrDefault(start);
            if (i != 0) output.Append(kind is not null && kind == previous ? "\n" : "\n\n");
            if (kind == "numbered") output.Append(previous == kind ? ++number : number = 1).Append(". ");
            else { number = 0; if (kind == "bullet") output.Append("- "); }
            output.Append(FormatRange(block, start, end));
            previous = kind;
        }
        return output.ToString();
    }

    private static string FormatRange(NotebookTextBlock block, int start, int end)
    {
        var output = new StringBuilder();
        var boundaries = block.Styles.SelectMany(span => new[] { span.Offset, span.Offset + span.Length })
            .Where(offset => offset > start && offset < end).Prepend(start).Append(end).Distinct().Order().ToArray();
        for (var i = 0; i < boundaries.Length - 1; i++)
        {
            var from = boundaries[i]; var to = boundaries[i + 1];
            var style = block.Styles.FirstOrDefault(span => span.Offset <= from && span.Offset + span.Length > from)?.Style ?? InlineTextStyle.None;
            // Visual attributes do not split emphasis in portable Markdown.
            while (i + 1 < boundaries.Length - 1)
            {
                var next = boundaries[i + 1];
                var nextStyle = block.Styles.FirstOrDefault(span => span.Offset <= next && span.Offset + span.Length > next)?.Style ?? InlineTextStyle.None;
                if (nextStyle != style) break;
                i++;
                to = boundaries[i + 1];
            }
            var raw = block.Text[from..to];
            var core = raw.Trim();
            if (core.Length == 0) { output.Append(raw); continue; }
            var left = raw.Length - raw.TrimStart().Length;
            var right = raw.Length - raw.TrimEnd().Length;
            var marker = style switch { InlineTextStyle.Bold => "**", InlineTextStyle.Italic => "*", InlineTextStyle.Bold | InlineTextStyle.Italic => "***", _ => "" };
            output.Append(raw.AsSpan(0, left)).Append(marker).Append(Escape(core)).Append(marker)
                .Append(raw.AsSpan(raw.Length - right, right));
        }
        return output.ToString();
    }

    private static string Escape(string text)
    {
        var escaped = new StringBuilder();
        foreach (var character in text)
        {
            if ("\\`*_{}[]<>()!#|~+-.".Contains(character)) escaped.Append('\\');
            escaped.Append(character);
        }
        return escaped.ToString();
    }
}
