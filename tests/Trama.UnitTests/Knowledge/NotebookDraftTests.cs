using Trama.Application.Knowledge;

namespace Trama.UnitTests.Knowledge;

public sealed class NotebookDraftTests
{
    [Fact]
    public void NewDraft_HasNoBlocksOrContent()
    {
        var draft = new NotebookDraft();

        Assert.Empty(draft.Blocks);
        Assert.False(draft.HasContent);
    }

    [Fact]
    public void Insert_MixesTextCodeAndImagesInWritingOrder()
    {
        var draft = new NotebookDraft();
        var text = new NotebookTextBlock(Guid.NewGuid(), "Conceito de regressão");
        var code = new NotebookCodeBlock(Guid.NewGuid(), "model.fit(x, y)\n", "python");
        var image = new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "Resultado do experimento");
        var conclusion = new NotebookTextBlock(Guid.NewGuid(), "Análise do resultado");

        draft.Insert(0, text);
        draft.Insert(1, image);
        draft.Insert(1, code);
        draft.Insert(3, conclusion);

        Assert.Equal(new NotebookBlock[] { text, code, image, conclusion }, draft.Blocks);
        Assert.True(draft.HasContent);
    }

    [Fact]
    public void Insert_PreservesLongTextUnicodeAndLineBreaks()
    {
        var content = string.Concat(Enumerable.Repeat("  Estudo 🧵: ação, hipótese e conclusão.\n\n", 5000));
        var draft = new NotebookDraft();

        draft.Insert(0, new NotebookTextBlock(Guid.NewGuid(), content));

        Assert.Equal(content, Assert.IsType<NotebookTextBlock>(Assert.Single(draft.Blocks)).Text);
    }

    [Fact]
    public void Insert_PreservesCodeIndentationLanguageAndSpecialCharacters()
    {
        const string code = "def predict(x):\r\n\treturn x < 2 and x > 0  # previsão\r\n";
        var draft = new NotebookDraft();

        draft.Insert(0, new NotebookCodeBlock(Guid.NewGuid(), code, "python"));

        var block = Assert.IsType<NotebookCodeBlock>(Assert.Single(draft.Blocks));
        Assert.Equal(code, block.Code);
        Assert.Equal("python", block.Language);
        Assert.True(draft.HasContent);
    }

    [Fact]
    public void Insert_ImageOnlyDraftHasContentAndKeepsManagedAttachmentReference()
    {
        var image = new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "  Print do estudo  ");
        var draft = new NotebookDraft();

        draft.Insert(0, image);

        Assert.Equal(image, Assert.IsType<NotebookImageBlock>(Assert.Single(draft.Blocks)));
        Assert.True(draft.HasContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhitespaceOnlyBlock_DoesNotCountAsContent(bool isCode)
    {
        var draft = new NotebookDraft();
        NotebookBlock block = isCode
            ? new NotebookCodeBlock(Guid.NewGuid(), " \n\t ", "csharp")
            : new NotebookTextBlock(Guid.NewGuid(), " \n\t ");

        draft.Insert(0, block);

        Assert.False(draft.HasContent);
    }

    [Fact]
    public void Update_ReplacesBlockContentWithoutChangingItsPositionOrIdentity()
    {
        var first = new NotebookTextBlock(Guid.NewGuid(), "Introdução");
        var original = new NotebookCodeBlock(Guid.NewGuid(), "print(1)", "python");
        var updated = original with { Code = "Console.WriteLine(2);\n", Language = "csharp" };
        var draft = new NotebookDraft();
        draft.Insert(0, first);
        draft.Insert(1, original);

        draft.Update(updated);

        Assert.Equal(new NotebookBlock[] { first, updated }, draft.Blocks);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    public void Move_ReordersBlocksWithoutLosingTheirContent(int source, int target)
    {
        NotebookBlock[] blocks =
        [
            new NotebookTextBlock(Guid.NewGuid(), "Explicação"),
            new NotebookCodeBlock(Guid.NewGuid(), "x = 42", "python"),
            new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "Print")
        ];
        var draft = new NotebookDraft();
        for (var index = 0; index < blocks.Length; index++)
        {
            draft.Insert(index, blocks[index]);
        }

        draft.Move(blocks[source].Id, target);

        var expected = blocks.ToList();
        expected.RemoveAt(source);
        expected.Insert(target, blocks[source]);
        Assert.Equal(expected, draft.Blocks);
    }

    [Fact]
    public void Remove_RemovesOnlyTheSelectedBlock()
    {
        var text = new NotebookTextBlock(Guid.NewGuid(), "Manter");
        var image = new NotebookImageBlock(Guid.NewGuid(), Guid.NewGuid(), "Remover da página");
        var draft = new NotebookDraft();
        draft.Insert(0, text);
        draft.Insert(1, image);

        draft.Remove(image.Id);

        Assert.Equal(text, Assert.Single(draft.Blocks));
    }

    [Fact]
    public void Insert_DuplicateIdentityIsRejectedWithoutChangingTheDraft()
    {
        var text = new NotebookTextBlock(Guid.NewGuid(), "Original");
        var draft = new NotebookDraft();
        draft.Insert(0, text);

        Assert.Throws<ArgumentException>(() => draft.Insert(1, text with { Text = "Duplicado" }));

        Assert.Equal(text, Assert.Single(draft.Blocks));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Insert_InvalidPositionIsRejectedWithoutAddingABlock(int index)
    {
        var draft = new NotebookDraft();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            draft.Insert(index, new NotebookTextBlock(Guid.NewGuid(), "Texto")));

        Assert.Empty(draft.Blocks);
    }

    [Fact]
    public void Insert_ImageWithoutAttachmentIsRejected()
    {
        var draft = new NotebookDraft();

        Assert.Throws<ArgumentException>(() =>
            draft.Insert(0, new NotebookImageBlock(Guid.NewGuid(), Guid.Empty, "Sem arquivo")));

        Assert.Empty(draft.Blocks);
    }
}
