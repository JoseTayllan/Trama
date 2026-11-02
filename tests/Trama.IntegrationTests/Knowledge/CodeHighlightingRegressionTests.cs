using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class CodeHighlightingRegressionTests
{
    [AvaloniaFact]
    public void CSharp_MethodCalls_AreDistinctFromControlKeywords()
    {
        WithEditor("c#", "if (ready) Save();", editor =>
            Assert.NotEqual(ColorAt(editor, "if"), ColorAt(editor, "Save")));
    }

    [AvaloniaFact]
    public void Python_FunctionCalls_AreDistinctFromControlKeywords()
    {
        WithEditor("python", "if ready: save()", editor =>
            Assert.NotEqual(ColorAt(editor, "if"), ColorAt(editor, "save")));
    }

    [AvaloniaFact]
    public void CSharp_PrimitiveTypes_AreDistinctFromControlKeywords()
    {
        WithEditor(" C# ", "if (ready) { int total = 42; }", editor =>
            Assert.NotEqual(ColorAt(editor, "if"), ColorAt(editor, "int")));
    }

    [AvaloniaFact]
    public void CSharp_StringsNumbersCommentsAndKeywords_KeepDistinctColors()
    {
        WithEditor("csharp", "if (ready) Save(\"hello\", 42); // comment", editor =>
        {
            var colors = new[] { ColorAt(editor, "if"), ColorAt(editor, "hello"), ColorAt(editor, "42"), ColorAt(editor, "comment") };
            Assert.Equal(4, colors.Distinct().Count());
        });
    }

    [AvaloniaFact]
    public void ChangingKeywordPalette_DoesNotRecolorFunctionCalls()
    {
        WithEditor("c#", "if (ready) Save();", editor =>
        {
            var functionColor = ColorAt(editor, "Save");
            var keywordBrush = Assert.IsType<SolidColorBrush>(editor.KeywordBrush);
            keywordBrush.Color = Color.Parse("#FF77AA");

            Assert.Equal(keywordBrush.Color, ColorAt(editor, "if"));
            Assert.Equal(functionColor, ColorAt(editor, "Save"));
        });
    }

    [AvaloniaFact]
    public void UnsupportedLanguage_PreservesPlainTextWithoutHighlighting()
    {
        const string text = "if (ready) Save(\"hello\", 42);";
        WithEditor("plaintext", text, editor =>
        {
            Assert.Null(editor.SyntaxHighlighting);
            Assert.Equal(text, editor.Text);
            Assert.Equal(text, editor.CodeText);
        });
    }

    private static void WithEditor(string language, string text, Action<NotebookCodeEditor> verify)
    {
        var editor = new NotebookCodeEditor
        {
            Foreground = Brushes.White,
            KeywordBrush = new SolidColorBrush(Color.Parse("#C5B3FF")),
            StringBrush = new SolidColorBrush(Color.Parse("#B5DE9E")),
            NumberBrush = new SolidColorBrush(Color.Parse("#EAB685")),
            CommentBrush = new SolidColorBrush(Color.Parse("#92A6BF")),
            Language = language,
            CodeText = text
        };
        var window = new Window { Content = editor, Width = 800, Height = 300 };
        try
        {
            window.Show();
            verify(editor);
        }
        finally
        {
            window.Close();
        }
    }

    private static Color ColorAt(NotebookCodeEditor editor, string token)
    {
        var offset = editor.Text.IndexOf(token, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Token not found: {token}");
        Assert.NotNull(editor.SyntaxHighlighting);
        using var highlighter = new DocumentHighlighter(editor.Document, editor.SyntaxHighlighting);
        var line = highlighter.HighlightLine(1);
        var section = line.Sections.LastOrDefault(item => item.Offset <= offset && offset < item.Offset + item.Length && item.Color.Foreground is not null);
        return section?.Color.Foreground.GetColor(null!) ?? Assert.IsAssignableFrom<ISolidColorBrush>(editor.Foreground).Color;
    }
}
