using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trama.Application.Agenda;
using Trama.Desktop;
using Trama.Desktop.ViewModels;
using Trama.Desktop.Views;

namespace Trama.IntegrationTests.Knowledge;

public sealed class AgendaRetrospectiveBottomLayoutTests
{
    [AvaloniaFact]
    public async Task WideLayout_PlacesPendingReviewAndReflectionSideBySide()
    {
        using var f = await Fixture.OpenAsync(1600); var pending = f.C<Control>("AgendaRetrospectiveOpenTasks"); var reflection = f.C<Control>("AgendaRetrospectiveReflection");
        var left = pending.TranslatePoint(default, f.View); var right = reflection.TranslatePoint(default, f.View); Assert.NotNull(left); Assert.NotNull(right);
        Assert.True(right.Value.X >= left.Value.X + pending.Bounds.Width, "Reflexão deve ficar à direita das pendências.");
        Assert.True(Math.Abs(right.Value.Y - left.Value.Y) <= 1, "Os cartões devem começar na mesma linha.");
        Assert.True(pending.Bounds.Width > reflection.Bounds.Width, "As pendências devem ter a maior coluna, como na referência.");
    }

    [AvaloniaTheory]
    [InlineData(760)]
    [InlineData(980)]
    public async Task CompactLayout_StacksTheCardsWithoutHorizontalOverflow(int width)
    {
        using var f = await Fixture.OpenAsync(width); var pending = f.C<Control>("AgendaRetrospectiveOpenTasks"); var reflection = f.C<Control>("AgendaRetrospectiveReflection");
        var left = pending.TranslatePoint(default, f.View); var right = reflection.TranslatePoint(default, f.View); Assert.NotNull(left); Assert.NotNull(right);
        Assert.True(right.Value.Y >= left.Value.Y + pending.Bounds.Height); Assert.True(Math.Abs(right.Value.X - left.Value.X) <= 1);
        var scroll = f.C<ScrollViewer>("AgendaRetrospectiveScroll"); Assert.True(scroll.Extent.Width <= scroll.Viewport.Width + 1);
    }

    [AvaloniaFact]
    public async Task ReflectionCard_ShowsBothQuestionsAndTheirVisualFields()
    {
        using var f = await Fixture.OpenAsync(1600); var reflection = f.C<Control>("AgendaRetrospectiveReflection"); var text = Text(reflection);
        Assert.Contains("Sua retrospectiva", text); Assert.Contains("O que funcionou bem?", text); Assert.Contains("O que você quer ajustar?", text);
        Assert.True(f.C<TextBox>("AgendaReflectionWorkedInput").IsEffectivelyVisible); Assert.True(f.C<TextBox>("AgendaReflectionAdjustInput").IsEffectivelyVisible);
        Assert.False(string.IsNullOrWhiteSpace(f.C<TextBox>("AgendaReflectionWorkedInput").Watermark?.ToString()));
        Assert.False(string.IsNullOrWhiteSpace(f.C<TextBox>("AgendaReflectionAdjustInput").Watermark?.ToString()));
    }

    [AvaloniaFact]
    public async Task ReflectionEditor_AcceptsTextAndEnablesSaveOnlyAfterEditing()
    {
        using var f = await Fixture.OpenAsync(1600);
        var worked = f.C<TextBox>("AgendaReflectionWorkedInput");
        var adjustments = f.C<TextBox>("AgendaReflectionAdjustInput");
        var save = f.C<Button>("AgendaReflectionSaveButton");
        Assert.False(worked.IsReadOnly);
        Assert.False(adjustments.IsReadOnly);
        Assert.False(save.IsEffectivelyEnabled);

        worked.Text = "Revisão semanal";
        Dispatcher.UIThread.RunJobs();

        Assert.True(save.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task ReflectionEditor_SavePersistsExactTextAndKeepsTheClosure()
    {
        using var f = await Fixture.OpenAsync(1600);
        var report = f.Agenda.Retrospective!.Report!;
        const string worked = "  Revisão concluída\nPróximo passo  ";
        const string adjustments = "  Reservar tempo para estudar  ";
        f.SetReflection(worked, adjustments);

        await f.SaveReflectionAsync();

        var saved = await f.Reflections.GetAsync(report.StartDate);
        Assert.NotNull(saved);
        Assert.Equal(worked, saved.WorkedWell);
        Assert.Equal(adjustments, saved.Adjustments);
        Assert.Same(report, f.Agenda.Retrospective.Report);
        Assert.Equal("Rascunho mantido", f.Agenda.Title);
        Assert.False(f.C<Button>("AgendaReflectionSaveButton").IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public async Task ReflectionEditor_LoadsSavedTextAndCanClearBothFields()
    {
        using var f = await Fixture.OpenAsync(1600, savedReflection: true);
        Assert.Equal("O que funcionou", f.C<TextBox>("AgendaReflectionWorkedInput").Text);
        Assert.Equal("O que ajustar", f.C<TextBox>("AgendaReflectionAdjustInput").Text);
        var week = f.Agenda.Retrospective!.Report!.StartDate;

        f.SetReflection(string.Empty, string.Empty);
        await f.SaveReflectionAsync();

        var saved = await f.Reflections.GetAsync(week);
        Assert.NotNull(saved);
        Assert.Empty(saved.WorkedWell);
        Assert.Empty(saved.Adjustments);
    }

    [AvaloniaTheory]
    [InlineData(UnsavedChangesChoice.Cancel)]
    [InlineData(UnsavedChangesChoice.Discard)]
    [InlineData(UnsavedChangesChoice.Save)]
    public async Task ReflectionEditor_ResolvesDraftBeforeChangingWeeks(UnsavedChangesChoice choice)
    {
        using var f = await Fixture.OpenAsync(1600);
        var week = f.Agenda.Retrospective!.Report!.StartDate;
        var destination = week.AddDays(-7);
        f.Prompt.Choice = choice;
        f.SetReflection("Rascunho da primeira semana", string.Empty);

        var moved = await f.Agenda.ShowRetrospectiveAsync(destination);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, f.Prompt.Calls);
        var saved = await f.Reflections.GetAsync(week);
        if (choice == UnsavedChangesChoice.Cancel)
        {
            Assert.False(moved);
            Assert.Equal(week, f.Agenda.Retrospective.Report!.StartDate);
            Assert.Equal("Rascunho da primeira semana", f.C<TextBox>("AgendaReflectionWorkedInput").Text);
            Assert.Null(saved);
            return;
        }

        Assert.True(moved);
        Assert.Equal(destination, f.Agenda.Retrospective.Report!.StartDate);
        Assert.True(string.IsNullOrEmpty(f.C<TextBox>("AgendaReflectionWorkedInput").Text));
        if (choice == UnsavedChangesChoice.Save)
            Assert.Equal("Rascunho da primeira semana", saved?.WorkedWell);
        else
            Assert.Null(saved);
        Assert.Null(await f.Reflections.GetAsync(destination));
    }

    [AvaloniaFact]
    public async Task ReflectionEditor_PreservesDraftWhenReturningFromDayView()
    {
        using var f = await Fixture.OpenAsync(1600);
        var week = f.Agenda.Retrospective!.Report!.StartDate;
        f.SetReflection("Reflexão em andamento", string.Empty);

        Assert.True(await f.Agenda.ShowDayAsync());
        Assert.True(await f.Agenda.ShowRetrospectiveAsync(week));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Reflexão em andamento", f.C<TextBox>("AgendaReflectionWorkedInput").Text);
        Assert.Equal(0, f.Prompt.Calls);
        Assert.Null(await f.Reflections.GetAsync(week));
    }

    [AvaloniaFact]
    public async Task ReflectionEditor_CancelCloseProtectsDraftEvenWithDayViewVisible()
    {
        using var f = await Fixture.OpenAsync(1600, taskDraft: false);
        var week = f.Agenda.Retrospective!.Report!.StartDate;
        f.SetReflection("Ainda estou escrevendo", string.Empty);
        Assert.True(await f.Agenda.ShowDayAsync());

        Assert.False(await f.Agenda.RequestCloseAsync());

        Assert.Equal(1, f.Prompt.Calls);
        Assert.True(await f.Agenda.ShowRetrospectiveAsync(week));
        Assert.Equal("Ainda estou escrevendo", f.C<TextBox>("AgendaReflectionWorkedInput").Text);
        Assert.Null(await f.Reflections.GetAsync(week));
    }

    [AvaloniaFact]
    public async Task ReflectionFields_HaveAccessibleQuestionNames()
    {
        using var f = await Fixture.OpenAsync(1600);
        Assert.Equal("O que funcionou bem?", AutomationProperties.GetName(f.C<TextBox>("AgendaReflectionWorkedInput")));
        Assert.Equal("O que você quer ajustar?", AutomationProperties.GetName(f.C<TextBox>("AgendaReflectionAdjustInput")));
        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(f.C<Button>("AgendaReflectionSaveButton"))));
    }

    [AvaloniaFact]
    public async Task PairedLayout_PreservesCapturedPendingTitlesAndStatusLabels()
    {
        using var f = await Fixture.OpenAsync(1600); Assert.True(f.C<Control>("AgendaRetrospectiveReflection").IsEffectivelyVisible);
        var rows = f.View.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("retrospective-open-task") && c.IsEffectivelyVisible).ToArray();
        Assert.Equal(4, rows.Length); Assert.All(rows, row => Assert.Contains(((AgendaRetrospectiveTask)row.DataContext!).Task.Title, Text(row)));
        Assert.Contains(rows, row => Text(row).Contains("○") && Text(row).Contains("Pendente"));
        Assert.Contains(rows, row => Text(row).Contains("◐") && Text(row).Contains("Em andamento"));
    }

    [AvaloniaTheory]
    [InlineData(760)]
    [InlineData(1600)]
    public async Task Scroll_ReachesBothCardsAndPreservesTheAgendaDraft(int width)
    {
        using var f = await Fixture.OpenAsync(width); var reflection = f.C<Control>("AgendaRetrospectiveReflection");
        var scroll = f.C<ScrollViewer>("AgendaRetrospectiveScroll"); scroll.Offset = new Vector(0, scroll.Extent.Height); f.Window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        var bottom = reflection.TranslatePoint(new Point(0, reflection.Bounds.Height), scroll); Assert.NotNull(bottom); Assert.True(bottom.Value.Y <= scroll.Viewport.Height + 2);
        Assert.Equal("Rascunho mantido", f.Agenda.Title); Assert.True(f.Agenda.HasUnsavedChanges); Assert.True(f.Agenda.IsRetrospectiveView);
    }

    private static string Text(Control root) => string.Join("\n", root.GetVisualDescendants().OfType<TextBlock>().Where(c => c.IsEffectivelyVisible).Select(c => c.Text));
    private sealed class DraftPrompt : IUnsavedChangesPrompt
    {
        public UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        public int Calls { get; private set; }

        public Task<UnsavedChangesChoice> ChooseAsync()
        {
            Calls++;
            return Task.FromResult(Choice);
        }
    }

    private sealed class Fixture : IDisposable
    {
        public NotebookTestWorkspace Root { get; } = new();
        public ServiceProvider Services { get; private set; } = null!;
        public Window Window { get; } = new() { Height = 850 };
        public AgendaWorkspaceView View { get; private set; } = null!;
        public AgendaViewModel Agenda { get; private set; } = null!;
        public DraftPrompt Prompt { get; } = new();
        public IAgendaReflectionActions Reflections => Services.GetRequiredService<IAgendaReflectionActions>();

        public static async Task<Fixture> OpenAsync(int width, bool savedReflection = false, bool taskDraft = true)
        {
            var f = new Fixture();
            try
            {
                f.Services = await DesktopComposition.CreateAsync(f.Root.DirectoryPath, unsavedChangesPrompt: f.Prompt); f.Agenda = f.Services.GetRequiredService<AgendaViewModel>();
                var day = f.Agenda.Today.AddDays(-7); var monday = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
                var actions = f.Services.GetRequiredService<IAgendaActions>();
                for (var i = 0; i < 4; i++)
                    Assert.Equal(AgendaTaskSaveStatus.Saved, (await actions.SaveAsync(new(Guid.NewGuid(), $"Pendência fictícia {i}", "Perfil de teste", monday,
                        Category: (AgendaTaskCategory)(i % 3), Status: i % 2 == 0 ? AgendaTaskStatus.Pending : AgendaTaskStatus.InProgress))).Status);
                if (savedReflection)
                {
                    await f.Services.GetRequiredService<IAgendaRetrospectiveActions>().GetOrCloseAsync(monday);
                    Assert.Equal(AgendaReflectionSaveStatus.Saved, (await f.Reflections.SaveAsync(monday, "O que funcionou", "O que ajustar")).Status);
                }

                await f.Agenda.InitializeAsync(f.Agenda.Today);
                if (taskDraft) f.Agenda.Title = "Rascunho mantido";
                Assert.True(await f.Agenda.ShowRetrospectiveAsync(monday)); f.View = new() { DataContext = f.Agenda };
                f.Window.Width = width; f.Window.Content = f.View; f.Window.Show(); Dispatcher.UIThread.RunJobs(); f.Window.UpdateLayout(); return f;
            }
            catch { f.Dispose(); throw; }
        }
        public T C<T>(string name) where T : Control { var value = View.GetVisualDescendants().OfType<T>().SingleOrDefault(c => c.Name == name); Assert.NotNull(value); return value; }
        public void SetReflection(string worked, string adjustments)
        {
            C<TextBox>("AgendaReflectionWorkedInput").Text = worked;
            C<TextBox>("AgendaReflectionAdjustInput").Text = adjustments;
            Dispatcher.UIThread.RunJobs();
        }

        public async Task SaveReflectionAsync()
        {
            var button = C<Button>("AgendaReflectionSaveButton");
            Assert.True(button.IsEffectivelyEnabled);
            if (button.Command is { } command)
            {
                Assert.True(command.CanExecute(button.CommandParameter));
                command.Execute(button.CommandParameter);
            }
            else
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            do
            {
                Dispatcher.UIThread.RunJobs();
                if (!Agenda.IsBusy && !Agenda.Retrospective!.IsBusy) return;
                await Task.Delay(10);
            }
            while (elapsed.Elapsed < TimeSpan.FromSeconds(5));

            Assert.Fail("O salvamento da reflexão não terminou.");
        }

        public void Dispose() { Window.Close(); Dispatcher.UIThread.RunJobs(); Services?.Dispose(); Root.Dispose(); }
    }
}
