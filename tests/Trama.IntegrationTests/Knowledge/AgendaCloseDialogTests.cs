using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Trama.Application.Agenda;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaCloseDialogTests
{
    [Fact]
    public void MissingCandidates_AreRejected() => Assert.Throws<ArgumentNullException>(() => new AgendaCompletionDialogViewModel(null!));

    [Fact]
    public void Candidates_StartUnselectedWithCompletionDisabled()
    {
        var task = Example(); var vm = new AgendaCompletionDialogViewModel([task]);
        Assert.Equal(task, Assert.Single(vm.Items).Task); Assert.False(vm.Items[0].IsSelected);
        Assert.Equal(0, vm.SelectedCount); Assert.False(vm.CanCompleteSelected);
    }

    [Fact]
    public void CheckingAnItem_UpdatesCountAndCompletionAvailability()
    {
        var vm = new AgendaCompletionDialogViewModel([Example(), Example()]);
        vm.Items[1].IsSelected = true; Assert.Equal(1, vm.SelectedCount); Assert.True(vm.CanCompleteSelected);
        vm.Items[1].IsSelected = false; Assert.Equal(0, vm.SelectedCount); Assert.False(vm.CanCompleteSelected);
    }

    [Fact]
    public void SelectingAllAndClearing_OnlyChangesTheSelection()
    {
        var first = Example(); var second = Example(); var vm = new AgendaCompletionDialogViewModel([first, second]);
        vm.SelectAllCommand.Execute(null); Assert.Equal(2, vm.SelectedCount); Assert.All(vm.Items, item => Assert.True(item.IsSelected));
        vm.ClearSelectionCommand.Execute(null); Assert.Equal(0, vm.SelectedCount); Assert.All(vm.Items, item => Assert.False(item.IsSelected));
        Assert.Equal(new[] { first, second }, vm.Items.Select(item => item.Task));
    }

    [Fact]
    public void CompleteDecision_CopiesOnlyTheSelectedIdentities()
    {
        var first = Example(); var second = Example(); var vm = new AgendaCompletionDialogViewModel([first, second]);
        vm.Items[1].IsSelected = true;
        var decision = vm.CreateDecision(AgendaCompletionChoice.CompleteSelected);
        vm.Items[1].IsSelected = false; vm.Items[0].IsSelected = true;
        Assert.Equal(AgendaCompletionChoice.CompleteSelected, decision.Choice); Assert.Equal(new[] { second.Id }, decision.SelectedTaskIds);
    }

    [Fact]
    public void CompletingWithoutSelection_IsRejected()
    {
        var vm = new AgendaCompletionDialogViewModel([Example()]);
        Assert.Throws<InvalidOperationException>(() => vm.CreateDecision(AgendaCompletionChoice.CompleteSelected));
    }

    [Theory]
    [InlineData(AgendaCompletionChoice.Cancel)]
    [InlineData(AgendaCompletionChoice.KeepOpen)]
    public void CancelAndKeepOpen_DoNotReturnTasksToComplete(AgendaCompletionChoice choice)
    {
        var vm = new AgendaCompletionDialogViewModel([Example()]); vm.SelectAllCommand.Execute(null);
        var decision = vm.CreateDecision(choice); Assert.Equal(choice, decision.Choice); Assert.Empty(decision.SelectedTaskIds);
    }

    [AvaloniaFact]
    public Task NativeCompleteButton_ReturnsOnlyTheCheckedTask() => NativeDecisionAsync(AgendaCompletionChoice.CompleteSelected);

    [AvaloniaFact]
    public Task NativeKeepOpenButton_DoesNotReturnACompletionSelection() => NativeDecisionAsync(AgendaCompletionChoice.KeepOpen);

    [AvaloniaFact]
    public Task NativeCancelButton_DoesNotReturnACompletionSelection() => NativeDecisionAsync(AgendaCompletionChoice.Cancel);

    [AvaloniaFact]
    public async Task ClosingTheDialogByItsTitleBar_IsCancellation()
    {
        var owner = new Window { Width = 1000, Height = 700 }; owner.Show();
        try
        {
            var pending = new WindowAgendaCompletionPrompt(owner).ChooseAsync([Example()]); Dispatcher.UIThread.RunJobs();
            var dialog = Assert.IsType<AgendaCompletionDialog>(Assert.Single(owner.OwnedWindows));
            dialog.Close(); var decision = await pending;
            Assert.Equal(AgendaCompletionChoice.Cancel, decision.Choice); Assert.Empty(decision.SelectedTaskIds);
        }
        finally { foreach (var child in owner.OwnedWindows.ToArray()) child.Close(); owner.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaFact]
    public async Task CompactDialog_KeepsTheThreeActionsVisibleAndCancelAsTheDefault()
    {
        var owner = new Window { Width = 1000, Height = 700 }; owner.Show();
        var dialog = new AgendaCompletionDialog(new AgendaCompletionDialogViewModel([Example()]));
        try
        {
            var pending = dialog.ShowDialog<AgendaCompletionDecision?>(owner); dialog.Width = 560; dialog.Height = 420; Dispatcher.UIThread.RunJobs(); dialog.UpdateLayout();
            foreach (var name in new[] { "CompleteSelectedButton", "KeepOpenButton", "CancelCompletionButton" })
            {
                var button = dialog.FindControl<Button>(name); Assert.NotNull(button); Assert.True(button.IsEffectivelyVisible);
                var point = button.TranslatePoint(default, dialog)!.Value;
                Assert.True(button.Bounds.Width > 0 && button.Bounds.Height > 0);
                Assert.InRange(point.X, 0, dialog.Bounds.Width - button.Bounds.Width + 1); Assert.InRange(point.Y, 0, dialog.Bounds.Height - button.Bounds.Height + 1);
            }
            Assert.True(dialog.FindControl<Button>("CancelCompletionButton")!.IsCancel);
            Assert.True(dialog.FindControl<Button>("CancelCompletionButton")!.IsDefault);
            dialog.Close(); await pending;
        }
        finally { dialog.Close(); owner.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    private static async Task NativeDecisionAsync(AgendaCompletionChoice choice)
    {
        var owner = new Window { Width = 1000, Height = 700 }; owner.Show();
        var task = Example(); var dialog = new AgendaCompletionDialog(new AgendaCompletionDialogViewModel([task]));
        try
        {
            var pending = dialog.ShowDialog<AgendaCompletionDecision?>(owner); Dispatcher.UIThread.RunJobs(); dialog.UpdateLayout();
            var complete = dialog.FindControl<Button>("CompleteSelectedButton"); Assert.NotNull(complete); Assert.False(complete.IsEffectivelyEnabled);
            var checkbox = Assert.Single(dialog.GetVisualDescendants().OfType<CheckBox>(), item => item.Classes.Contains("completion-task")); checkbox.IsChecked = true; Dispatcher.UIThread.RunJobs();
            Assert.True(complete.IsEffectivelyEnabled);
            var button = choice switch { AgendaCompletionChoice.CompleteSelected => complete, AgendaCompletionChoice.KeepOpen => dialog.FindControl<Button>("KeepOpenButton"), _ => dialog.FindControl<Button>("CancelCompletionButton") };
            Assert.NotNull(button); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var decision = Assert.IsType<AgendaCompletionDecision>(await pending);
            Assert.Equal(choice, decision.Choice);
            Assert.Equal(choice == AgendaCompletionChoice.CompleteSelected ? new[] { task.Id } : Array.Empty<Guid>(), decision.SelectedTaskIds);
        }
        finally { dialog.Close(); owner.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    private static AgendaTaskSnapshot Example() => new(Guid.NewGuid(), "Tarefa fictícia", "Descrição para identificar a tarefa", new DateOnly(2026, 11, 4));
}
