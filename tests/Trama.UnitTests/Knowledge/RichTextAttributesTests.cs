using System.Text.Json;
using Trama.Application.Knowledge;

namespace Trama.UnitTests.Knowledge;

public sealed class RichTextAttributesTests
{
    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(16)]
    [InlineData(18)]
    [InlineData(24)]
    [InlineData(32)]
    public void SupportedFontSize_RoundTripPreservesTheSelectedSize(int size)
    {
        var document = Document("abcdef", Span(1, 3, fontSize: size));

        AssertAttributes(Assert.Single(document.Styles), size, null, null);
        Assert.Equal("abcdef", document.Text);
    }

    [Fact]
    public void Foreground_RoundTripPreservesTheSemanticColor()
    {
        var document = Document("abcdef", Span(1, 3, foreground: "teal"));

        AssertAttributes(Assert.Single(document.Styles), null, "teal", null);
    }

    [Fact]
    public void Highlight_RoundTripPreservesTheSemanticBackground()
    {
        var document = Document("abcdef", Span(1, 3, highlight: "amber"));

        AssertAttributes(Assert.Single(document.Styles), null, null, "amber");
    }

    [Fact]
    public void CombinedAttributes_PreserveTextRangeAndBothBooleanStyles()
    {
        var document = Document("abcdef", Span(1, 3,
            InlineTextStyle.Bold | InlineTextStyle.Italic, 24, "blue", "amber"));

        var span = Assert.Single(document.Styles);
        Assert.Equal(1, span.Offset);
        Assert.Equal(3, span.Length);
        Assert.Equal(InlineTextStyle.Bold | InlineTextStyle.Italic, span.Style);
        AssertAttributes(span, 24, "blue", "amber");
        Assert.Equal("abcdef", document.Text);
    }

    [Fact]
    public void AdjacentDifferentForegrounds_AreNotMerged()
    {
        var document = Document("abcd", Span(0, 2, foreground: "teal"), Span(2, 2, foreground: "blue"));

        Assert.Equal(2, document.Styles.Count);
        AssertAttributes(document.Styles[0], null, "teal", null);
        AssertAttributes(document.Styles[1], null, "blue", null);
    }

    [Fact]
    public void AdjacentDifferentSizes_AreNotMerged()
    {
        var document = Document("abcd", Span(0, 2, fontSize: 16), Span(2, 2, fontSize: 24));

        Assert.Equal(2, document.Styles.Count);
        AssertAttributes(document.Styles[0], 16, null, null);
        AssertAttributes(document.Styles[1], 24, null, null);
    }

    [Fact]
    public void AdjacentIdenticalAttributes_MergeWithoutLosingMetadata()
    {
        var document = Document("abcd",
            Span(0, 2, fontSize: 18, foreground: "teal", highlight: "blue"),
            Span(2, 2, fontSize: 18, foreground: "teal", highlight: "blue"));

        var span = Assert.Single(document.Styles);
        Assert.Equal(0, span.Offset);
        Assert.Equal(4, span.Length);
        AssertAttributes(span, 18, "teal", "blue");
    }

    [Fact]
    public void AddingItalicToPartOfRange_PreservesEveryOtherAttribute()
    {
        var document = Document("abcdef", Span(0, 6, fontSize: 24, foreground: "teal", highlight: "amber"));

        document.SetStyle(2, 2, InlineTextStyle.Italic, true);

        Assert.Equal(3, document.Styles.Count);
        Assert.All(document.Styles, span => AssertAttributes(span, 24, "teal", "amber"));
        Assert.Equal(InlineTextStyle.Bold | InlineTextStyle.Italic, document.Styles[1].Style);
        Assert.Equal("abcdef", document.Text);
    }

    [Fact]
    public void RemovingLastBooleanStyle_RetainsSizeForegroundAndHighlight()
    {
        var document = Document("abcdef", Span(1, 3, fontSize: 24, foreground: "teal", highlight: "amber"));

        document.SetStyle(1, 3, InlineTextStyle.Bold, false);

        var span = Assert.Single(document.Styles);
        Assert.Equal(InlineTextStyle.None, span.Style);
        Assert.Equal(1, span.Offset);
        Assert.Equal(3, span.Length);
        AssertAttributes(span, 24, "teal", "amber");
    }

    [Fact]
    public void InsertInsideRange_InheritsAllAttributes()
    {
        var document = Document("abcd", Span(0, 4, fontSize: 18, foreground: "teal", highlight: "amber"));

        document.ReplaceText(2, 0, "XY");

        Assert.Equal("abXYcd", document.Text);
        var span = Assert.Single(document.Styles);
        Assert.Equal(6, span.Length);
        AssertAttributes(span, 18, "teal", "amber");
    }

    [Fact]
    public void InsertBeforeRange_ShiftsAttributesWithoutColoringThePrefix()
    {
        var document = Document("abcd", Span(1, 3, fontSize: 18, foreground: "blue"));

        document.ReplaceText(0, 0, "XY");

        Assert.Equal("XYabcd", document.Text);
        var span = Assert.Single(document.Styles);
        Assert.Equal(3, span.Offset);
        Assert.Equal(3, span.Length);
        AssertAttributes(span, 18, "blue", null);
    }

    [Fact]
    public void ReplaceSelection_InheritsAllAttributesAtTheSelectionStart()
    {
        var document = Document("abcdef", Span(1, 3, fontSize: 24, foreground: "teal", highlight: "amber"));

        document.ReplaceText(1, 2, "XYZ");

        Assert.Equal("aXYZdef", document.Text);
        var span = Assert.Single(document.Styles);
        Assert.Equal(1, span.Offset);
        Assert.Equal(4, span.Length);
        AssertAttributes(span, 24, "teal", "amber");
    }

    [Fact]
    public void DeleteAcrossRanges_PreservesAndMergesEqualSurvivors()
    {
        var document = Document("abcdef",
            Span(0, 2, foreground: "teal", highlight: "amber"),
            Span(4, 2, foreground: "teal", highlight: "amber"));

        document.ReplaceText(1, 4, string.Empty);

        Assert.Equal("af", document.Text);
        var span = Assert.Single(document.Styles);
        Assert.Equal(2, span.Length);
        AssertAttributes(span, null, "teal", "amber");
    }

    [Fact]
    public void Utf16Offsets_PreserveEmojiAndFollowingFormattingWhenInsertingAPrefix()
    {
        var document = Document("A🧵BC", Span(3, 2, foreground: "teal"));

        document.ReplaceText(0, 0, "Oi ");

        Assert.Equal("Oi A🧵BC", document.Text);
        var span = Assert.Single(document.Styles);
        Assert.Equal(6, span.Offset);
        Assert.Equal(2, span.Length);
        AssertAttributes(span, null, "teal", null);
    }

    [Fact]
    public void ColorWithoutBoldOrItalic_IsAValidFormattedRange()
    {
        var document = Document("abcdef", Span(1, 3, InlineTextStyle.None, foreground: "teal"));

        var span = Assert.Single(document.Styles);
        Assert.Equal(InlineTextStyle.None, span.Style);
        AssertAttributes(span, null, "teal", null);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(1000)]
    public void UnsupportedFontSize_IsRejectedInsteadOfSilentlyDiscarded(int size)
    {
        var source = Span(1, 3, fontSize: size);

        Assert.ThrowsAny<ArgumentException>(() => Document("abcdef", source));
    }

    [Fact]
    public void UnknownForeground_IsRejectedInsteadOfSilentlyDiscarded()
    {
        var source = Span(1, 3, foreground: "unknown-color");

        Assert.ThrowsAny<ArgumentException>(() => Document("abcdef", source));
    }

    [Fact]
    public void UnknownHighlight_IsRejectedInsteadOfSilentlyDiscarded()
    {
        var source = Span(1, 3, highlight: "unknown-color");

        Assert.ThrowsAny<ArgumentException>(() => Document("abcdef", source));
    }

    [Fact]
    public void ConstructingFromCallerArray_CopiesAndPreservesEnrichedIntervals()
    {
        var source = new[] { Span(1, 3, fontSize: 18, foreground: "teal", highlight: "blue") };
        var document = new InlineTextDocument("abcdef", source);

        source[0] = new TextStyleSpan(0, 1, InlineTextStyle.Italic);

        var span = Assert.Single(document.Styles);
        Assert.Equal(1, span.Offset);
        Assert.Equal(3, span.Length);
        AssertAttributes(span, 18, "teal", "blue");
    }

    [Fact]
    public void LegacyBoldItalic_StillLoadEditAndSerializeWithoutNewAttributes()
    {
        var spans = JsonSerializer.Deserialize<TextStyleSpan[]>(
            """[{"Offset":0,"Length":4,"Style":3}]""")!;
        var document = new InlineTextDocument("abcd", spans);

        document.ReplaceText(2, 0, "XY");

        Assert.Equal("abXYcd", document.Text);
        var span = Assert.Single(document.Styles);
        Assert.Equal(new TextStyleSpan(0, 6, InlineTextStyle.Bold | InlineTextStyle.Italic), span);
        AssertAttributes(span, null, null, null);
    }

    // Exercise the existing serialized interval boundary: no future production API,
    // reflection, implementation stubs, UI or personal database is needed for RED.
    private static TextStyleSpan Span(int offset, int length, InlineTextStyle style = InlineTextStyle.Bold,
        int? fontSize = null, string? foreground = null, string? highlight = null)
    {
        var serialized = JsonSerializer.Serialize(new
        {
            Offset = offset,
            Length = length,
            Style = style,
            FontSize = fontSize,
            Foreground = foreground,
            Highlight = highlight
        });
        return JsonSerializer.Deserialize<TextStyleSpan>(serialized)!;
    }

    private static InlineTextDocument Document(string text, params TextStyleSpan[] spans) => new(text, spans);

    private static void AssertAttributes(TextStyleSpan span, int? size, string? foreground, string? highlight)
    {
        var serialized = JsonSerializer.SerializeToElement(span);
        if (size is { } expectedSize)
        {
            Assert.True(serialized.TryGetProperty("FontSize", out var actual), "FontSize was silently discarded.");
            Assert.Equal(expectedSize, actual.GetInt32());
        }
        else
        {
            Assert.True(!serialized.TryGetProperty("FontSize", out var actual) || actual.ValueKind == JsonValueKind.Null);
        }
        AssertColor(serialized, "Foreground", foreground);
        AssertColor(serialized, "Highlight", highlight);
    }

    private static void AssertColor(JsonElement serialized, string property, string? expected)
    {
        if (expected is null)
        {
            Assert.True(!serialized.TryGetProperty(property, out var actual) || actual.ValueKind == JsonValueKind.Null);
            return;
        }
        Assert.True(serialized.TryGetProperty(property, out var color), $"{property} was silently discarded.");
        Assert.Equal(expected, color.GetString());
    }
}
