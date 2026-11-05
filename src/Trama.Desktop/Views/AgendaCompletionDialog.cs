using Avalonia.Controls;
using Avalonia.Interactivity;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;

namespace Trama.Desktop.Views;

public sealed partial class AgendaCompletionDialog : Window
{
    public AgendaCompletionDialog() => InitializeComponent();

    public AgendaCompletionDialog(AgendaCompletionDialogViewModel viewModel) : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        DataContext = viewModel;
    }

    private void CompleteClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaCompletionDialogViewModel { CanCompleteSelected: true } vm)
            Close(vm.CreateDecision(AgendaCompletionChoice.CompleteSelected));
    }
    private void KeepOpenClicked(object? sender, RoutedEventArgs e) => Close(new AgendaCompletionDecision(AgendaCompletionChoice.KeepOpen, []));
    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(new AgendaCompletionDecision(AgendaCompletionChoice.Cancel, []));
}

public sealed class WindowAgendaCompletionPrompt : IAgendaCompletionPrompt
{
    private readonly Window _owner;

    public WindowAgendaCompletionPrompt(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    public async Task<AgendaCompletionDecision> ChooseAsync(IReadOnlyList<AgendaTaskSnapshot> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!_owner.IsVisible) return new(AgendaCompletionChoice.Cancel, []);
        return await new AgendaCompletionDialog(new AgendaCompletionDialogViewModel(candidates))
            .ShowDialog<AgendaCompletionDecision?>(_owner) ?? new(AgendaCompletionChoice.Cancel, []);
    }
}
