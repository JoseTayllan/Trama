using System.Text;

namespace Trama.Application.Knowledge;

public static class PageTags
{
    public static string Normalize(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        return tag.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    public static string[] Normalize(IReadOnlyList<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return tags.Select(Normalize).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }
}
