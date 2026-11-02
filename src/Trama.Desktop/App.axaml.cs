using System.Data.Common;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;
using Trama.Infrastructure.Persistence;

namespace Trama.Desktop;

public sealed partial class App : Avalonia.Application
{
    private ServiceProvider? _services;
    private readonly CancellationTokenSource _shutdown = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            var window = new MainWindow(viewModel);
            desktop.MainWindow = window;
            window.Opened += async (_, _) =>
            {
                try
                {
                    var directory = DataDirectoryOptions.Resolve(desktop.Args ?? []);
                    var services = await DesktopComposition.CreateAsync(directory, enableAutoSave: true, _shutdown.Token,
                        new WindowUnsavedChangesPrompt(window));
                    if (_shutdown.IsCancellationRequested)
                    {
                        services.Dispose();
                        return;
                    }

                    _services = services;
                    var library = services.GetRequiredService<LibraryViewModel>();
                    await library.InitializeAsync();
                    if (_shutdown.IsCancellationRequested) return;
                    viewModel.Library = library;
                    viewModel.Backup = services.GetRequiredService<BackupViewModel>();
                    viewModel.DataDirectory = directory;
                    viewModel.Status = "Pronto · armazenamento local";
                }
                catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
                {
                    // The window was closed during initialization.
                }
                catch (ArgumentException)
                {
                    viewModel.Status = "Perfil inválido. Use --data-directory seguido do caminho absoluto da biblioteca.";
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DbException)
                {
                    viewModel.Status = "Não foi possível preparar o banco. Verifique as permissões e reabra o Trama.";
                }
            };
            desktop.Exit += (_, _) =>
            {
                _shutdown.Cancel();
                _services?.Dispose();
                _shutdown.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
