using Trama.Application.Knowledge;

namespace Trama.UnitTests.Knowledge;

public sealed class InlineTextDocumentTests
{
    [Fact]
    public void SetStyle_FormatsOnlySelectedRangeWithoutChangingText()
    {
        var document = new InlineTextDocument("Estudar C# 🧵\r\n");
        document.SetStyle(8, 2, InlineTextStyle.Bold, true);
        Assert.Equal("Estudar C# 🧵\r\n", document.Text);
        Assert.Equal(new TextStyleSpan(8, 2, InlineTextStyle.Bold), Assert.Single(document.Styles));
    }

    [Fact]
    public void SetStyle_OverlapsBoldAndItalicWithoutLosingEither()
    {
        var document = new InlineTextDocument("abcdef");
        document.SetStyle(0, 4, InlineTextStyle.Bold, true);
        document.SetStyle(2, 4, InlineTextStyle.Italic, true);
        Assert.Equal(new[]
        {
            new TextStyleSpan(0, 2, InlineTextStyle.Bold),
            new TextStyleSpan(2, 2, InlineTextStyle.Bold | InlineTextStyle.Italic),
            new TextStyleSpan(4, 2, InlineTextStyle.Italic)
        }, document.Styles);
    }

    [Fact]
    public void RemoveStyle_LeavesOtherStyleIntact()
    {
        var document = new InlineTextDocument("abcd", [new TextStyleSpan(0, 4, InlineTextStyle.Bold | InlineTextStyle.Italic)]);
        document.SetStyle(1, 2, InlineTextStyle.Bold, false);
        Assert.Equal(new[]
        {
            new TextStyleSpan(0, 1, InlineTextStyle.Bold | InlineTextStyle.Italic),
            new TextStyleSpan(1, 2, InlineTextStyle.Italic),
            new TextStyleSpan(3, 1, InlineTextStyle.Bold | InlineTextStyle.Italic)
        }, document.Styles);
    }

    [Fact]
    public void SetStyle_MergesAdjacentEqualRanges()
    {
        var document = new InlineTextDocument("abcdef");
        document.SetStyle(0, 3, InlineTextStyle.Bold, true);
        document.SetStyle(3, 3, InlineTextStyle.Bold, true);
        Assert.Equal(new TextStyleSpan(0, 6, InlineTextStyle.Bold), Assert.Single(document.Styles));
    }

    [Fact]
    public void InsertBefore_ShiftsStylesWithoutFormattingThePrefix()
    {
        var document = new InlineTextDocument("abc", [new TextStyleSpan(1, 2, InlineTextStyle.Bold)]);
        document.ReplaceText(0, 0, "XX");
        Assert.Equal("XXabc", document.Text);
        Assert.Equal(new TextStyleSpan(3, 2, InlineTextStyle.Bold), Assert.Single(document.Styles));
    }

    [Fact]
    public void InsertInside_InheritsActiveStyle()
    {
        var document = new InlineTextDocument("abcd", [new TextStyleSpan(0, 4, InlineTextStyle.Italic)]);
        document.ReplaceText(2, 0, "XY");
        Assert.Equal("abXYcd", document.Text);
        Assert.Equal(new TextStyleSpan(0, 6, InlineTextStyle.Italic), Assert.Single(document.Styles));
    }

    [Fact]
    public void ReplaceStyledSelection_InheritsStyleAtSelectionStart()
    {
        var document = new InlineTextDocument("abcdef", [new TextStyleSpan(1, 3, InlineTextStyle.Bold)]);
        document.ReplaceText(1, 2, "XYZ");
        Assert.Equal("aXYZdef", document.Text);
        Assert.Equal(new TextStyleSpan(1, 4, InlineTextStyle.Bold), Assert.Single(document.Styles));
    }

    [Fact]
    public void DeleteAcrossRanges_RemovesDeletedStylesAndMergesSurvivors()
    {
        var document = new InlineTextDocument("abcdef", [new TextStyleSpan(0, 2, InlineTextStyle.Bold), new TextStyleSpan(4, 2, InlineTextStyle.Bold)]);
        document.ReplaceText(1, 4, string.Empty);
        Assert.Equal("af", document.Text);
        Assert.Equal(new TextStyleSpan(0, 2, InlineTextStyle.Bold), Assert.Single(document.Styles));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(3, 2)]
    public void InvalidSelection_IsRejectedWithoutMutation(int offset, int length)
    {
        var document = new InlineTextDocument("abcd");
        Assert.ThrowsAny<ArgumentException>(() => document.SetStyle(offset, length, InlineTextStyle.Bold, true));
        Assert.Equal("abcd", document.Text);
        Assert.Empty(document.Styles);
    }

    [Fact]
    public void InvalidInitialRanges_AreRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => new InlineTextDocument("abcd",
            [new TextStyleSpan(0, 3, InlineTextStyle.Bold), new TextStyleSpan(2, 2, InlineTextStyle.Italic)]));
    }

    [Fact]
    public void UnknownStyle_IsRejectedWithoutMutation()
    {
        var document = new InlineTextDocument("abcd");
        Assert.ThrowsAny<ArgumentException>(() => document.SetStyle(0, 1, (InlineTextStyle)8, true));
        Assert.Empty(document.Styles);
    }

    [Fact]
    public void InitialStyles_AreCopiedToProtectTheDocument()
    {
        var source = new List<TextStyleSpan> { new(0, 2, InlineTextStyle.Bold) };
        var document = new InlineTextDocument("abcd", source);
        source.Clear();
        Assert.Equal(new TextStyleSpan(0, 2, InlineTextStyle.Bold), Assert.Single(document.Styles));
    }
}
