using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace Trama.Desktop.Views;

public sealed class NotebookCodeEditor : TextEditor
{
    public static readonly StyledProperty<string> CodeTextProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, string>(nameof(CodeText), string.Empty,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<string> LanguageProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, string>(nameof(Language), "plaintext");
    public static readonly StyledProperty<ISolidColorBrush> KeywordBrushProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, ISolidColorBrush>(nameof(KeywordBrush), Brushes.White);
    public static readonly StyledProperty<ISolidColorBrush> StringBrushProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, ISolidColorBrush>(nameof(StringBrush), Brushes.White);
    public static readonly StyledProperty<ISolidColorBrush> NumberBrushProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, ISolidColorBrush>(nameof(NumberBrush), Brushes.White);
    public static readonly StyledProperty<ISolidColorBrush> CommentBrushProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, ISolidColorBrush>(nameof(CommentBrush), Brushes.White);
    public static readonly StyledProperty<ISolidColorBrush> FunctionBrushProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, ISolidColorBrush>(nameof(FunctionBrush), Brushes.LightGoldenrodYellow);
    public static readonly StyledProperty<ISolidColorBrush> TypeBrushProperty =
        AvaloniaProperty.Register<NotebookCodeEditor, ISolidColorBrush>(nameof(TypeBrush), Brushes.LightSkyBlue);

    private bool _updating;
    private bool _observingBrushes;
    private readonly HashSet<AvaloniaObject> _observedBrushes = [];

    public NotebookCodeEditor()
    {
        ShowLineNumbers = true;
        FontFamily = new FontFamily("Cascadia Code,Consolas,monospace");
        FontSize = 14;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        TextChanged += (_, _) =>
        {
            if (!_updating)
            {
                SetCurrentValue(CodeTextProperty, Text);
            }

            ResizeToContent();
        };
        ResizeToContent();
    }

    protected override Type StyleKeyOverride => typeof(TextEditor);
    public string CodeText { get => GetValue(CodeTextProperty); set => SetValue(CodeTextProperty, value); }
    public string Language { get => GetValue(LanguageProperty); set => SetValue(LanguageProperty, value); }
    public ISolidColorBrush KeywordBrush { get => GetValue(KeywordBrushProperty); set => SetValue(KeywordBrushProperty, value); }
    public ISolidColorBrush StringBrush { get => GetValue(StringBrushProperty); set => SetValue(StringBrushProperty, value); }
    public ISolidColorBrush NumberBrush { get => GetValue(NumberBrushProperty); set => SetValue(NumberBrushProperty, value); }
    public ISolidColorBrush CommentBrush { get => GetValue(CommentBrushProperty); set => SetValue(CommentBrushProperty, value); }
    public ISolidColorBrush FunctionBrush { get => GetValue(FunctionBrushProperty); set => SetValue(FunctionBrushProperty, value); }
    public ISolidColorBrush TypeBrush { get => GetValue(TypeBrushProperty); set => SetValue(TypeBrushProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CodeTextProperty && Text != CodeText)
        {
            _updating = true;
            try
            {
                Text = CodeText ?? string.Empty;
            }
            finally
            {
                _updating = false;
            }

            ResizeToContent();
        }

        if (change.Property == LanguageProperty || change.Property == KeywordBrushProperty ||
            change.Property == StringBrushProperty || change.Property == NumberBrushProperty || change.Property == CommentBrushProperty ||
            change.Property == FunctionBrushProperty || change.Property == TypeBrushProperty)
        {
            if (_observingBrushes)
            {
                ObserveBrushes(true);
            }

            ApplyHighlighting();
        }
    }

    private void ResizeToContent() => Height = Math.Max(108, (Document?.LineCount ?? 1) * 22 + 24);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _observingBrushes = true;
        ObserveBrushes(true);
        ApplyHighlighting();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ObserveBrushes(false);
        _observingBrushes = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void ObserveBrushes(bool subscribe)
    {
        foreach (var brush in _observedBrushes)
        {
            brush.PropertyChanged -= SyntaxBrushChanged;
        }

        _observedBrushes.Clear();
        if (!subscribe)
        {
            return;
        }

        foreach (var brush in new[] { KeywordBrush, StringBrush, NumberBrush, CommentBrush, FunctionBrush, TypeBrush }.OfType<AvaloniaObject>().Distinct())
        {
            _observedBrushes.Add(brush);
            brush.PropertyChanged += SyntaxBrushChanged;
        }
    }

    private void SyntaxBrushChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => ApplyHighlighting();

    private void ApplyHighlighting()
    {
        var resource = Language?.Trim().ToLowerInvariant() switch
        {
            "csharp" or "c#" => "CSharp-Mode.xshd",
            "python" or "py" => "Python-Mode.xshd",
            "javascript" or "js" => "JavaScript-Mode.xshd",
            "json" => "Json.xshd",
            "sql" => "TSQL-Mode.xshd",
            _ => null
        };
        if (resource is null)
        {
            SyntaxHighlighting = null;
            return;
        }

        var assembly = typeof(TextEditor).Assembly;
        var name = assembly.GetManifestResourceNames().Single(item => item.EndsWith("." + resource, StringComparison.Ordinal));
        using var source = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("The highlighting resource is missing.");
        var definition = XDocument.Load(source);
        foreach (var color in definition.Descendants().Where(element => element.Name.LocalName == "Color"))
        {
            var colorName = ((string?)color.Attribute("name") ?? string.Empty).ToLowerInvariant();
            var brush = colorName.Contains("comment", StringComparison.Ordinal) ? CommentBrush
                : colorName.Contains("string", StringComparison.Ordinal) || colorName.Contains("char", StringComparison.Ordinal) ? StringBrush
                : colorName.Contains("number", StringComparison.Ordinal) || colorName.Contains("digit", StringComparison.Ordinal) ? NumberBrush
                : colorName.Contains("method", StringComparison.Ordinal) || colorName.Contains("function", StringComparison.Ordinal) ? FunctionBrush
                : colorName.Contains("type", StringComparison.Ordinal) ? TypeBrush
                : KeywordBrush;
            color.SetAttributeValue("foreground", brush.Color.ToString());
        }

        using var reader = definition.CreateReader();
        SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
