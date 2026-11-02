using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

[assembly: AvaloniaTestApplication(typeof(Trama.IntegrationTests.Foundation.DesktopTestApplication))]

namespace Trama.IntegrationTests.Foundation;

public static class DesktopTestApplication
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class DesktopShellTests
{
    [AvaloniaFact]
    public void Shell_OpensWithTheFoundationMessageAndEmbeddedLogo()
    {
        var viewModel = new MainWindowViewModel();
        var window = new MainWindow(viewModel);
        try
        {
            window.Show();

            Assert.Equal("Trama", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == viewModel.Heading);

            using var stream = AssetLoader.Open(new Uri("avares://Trama.Desktop/Assets/trama-logo.png"));
            using var bitmap = new Bitmap(stream);
            Assert.True(bitmap.PixelSize.Width > 0);
        }
        finally
        {
            window.Close();
        }
    }
}
