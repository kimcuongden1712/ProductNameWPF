using ProductName.App.Wpf.Features.Main;
using ProductName.App.Wpf.Features.Main.Navigation;
using ProductName.App.Wpf.Features.Main.Navigation.Interface;
using ProductName.App.Wpf.Features.Main.WorkScreens;
using ProductName.App.Wpf.Features.Dialogs;
using ProductName.App.Wpf.Common.Enums;
using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;

namespace ProductName.App.Wpf.Tests;

// Covers screen reuse, function-key routing, and unsaved-change decisions.
public sealed class MainViewModelTests
{
    [Test]
    public async Task FunctionKeysFollowCurrentScreenAndScreenStateIsReused()
    {
        var viewModel = CreateViewModel();
        var originalD1 = (D1ViewModel)viewModel.CurrentScreen!;
        originalD1.Notes = "Keep this note";
        await originalD1.SaveChangesAsync(CancellationToken.None);

        Assert.That(viewModel.F1Command.CanExecute(null), Is.True);
        Assert.That(viewModel.F3Command.CanExecute(null), Is.False);
        Assert.That(viewModel.FunctionKeySlots, Has.Count.EqualTo(12));
        Assert.That(viewModel.FunctionKeySlots.Single(slot => slot.Key == FunctionKey.F1).Label, Is.EqualTo("Details"));

        viewModel.F1Command.Execute(null);
        Assert.That(originalD1.Status, Is.EqualTo("F1 action executed on D1."));

        viewModel.F5Command.Execute(null);
        await WaitUntilAsync(() => viewModel.CurrentScreen is D2ViewModel);

        Assert.That(viewModel.F1Command.CanExecute(null), Is.False);
        Assert.That(viewModel.F5Command.CanExecute(null), Is.True);
        Assert.That(viewModel.F12Command.CanExecute(null), Is.True);

        viewModel.NavigateToD1Command.Execute(null);
        await WaitUntilAsync(() => ReferenceEquals(viewModel.CurrentScreen, originalD1));

        Assert.That(originalD1.Notes, Is.EqualTo("Keep this note"));
    }

    [Test]
    public async Task NavigationCancelKeepsCurrentScreenAndUnsavedChanges()
    {
        var prompt = new FakeUnsavedChangesPrompt(UnsavedChangesDecision.Cancel);
        var viewModel = CreateViewModel(prompt);
        var d1 = (D1ViewModel)viewModel.CurrentScreen!;
        d1.Notes = "Unsaved";

        viewModel.NavigateToD2Command.Execute(null);
        await Task.Delay(50);

        Assert.That(viewModel.CurrentScreen, Is.SameAs(d1));
        Assert.That(d1.Notes, Is.EqualTo("Unsaved"));
        Assert.That(prompt.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task D1FormTracksAndDiscardsChangesAcrossItsInputs()
    {
        var d1 = (D1ViewModel)CreateViewModel().CurrentScreen!;
        d1.CustomerCode = "CUS-01";
        d1.SelectedCategory = "Pickup";
        d1.IsPriority = true;
        d1.DispatchDate = new DateTime(2026, 10, 20);
        d1.SelectRegionCommand.Execute("Central");
        d1.IsExpeditedMode = true;
        d1.IsAutoDispatch = false;
        d1.Notes = "Fragile";

        Assert.That(d1.HasUnsavedChanges, Is.True);

        await d1.SaveChangesAsync(CancellationToken.None);
        Assert.That(d1.HasUnsavedChanges, Is.False);

        d1.CustomerCode = "CUS-02";
        d1.DiscardChanges();

        Assert.That(d1.CustomerCode, Is.EqualTo("CUS-01"));
        Assert.That(d1.SelectedCategory, Is.EqualTo("Pickup"));
        Assert.That(d1.SelectedRegion, Is.EqualTo("Central"));
        Assert.That(d1.IsExpeditedMode, Is.True);
        Assert.That(d1.IsAutoDispatch, Is.False);
        Assert.That(d1.HasUnsavedChanges, Is.False);
    }

    [TestCase(nameof(UnsavedChangesDialogViewModel.SaveCommand), UnsavedChangesDecision.Save)]
    [TestCase(nameof(UnsavedChangesDialogViewModel.DiscardCommand), UnsavedChangesDecision.Discard)]
    [TestCase(nameof(UnsavedChangesDialogViewModel.CancelCommand), UnsavedChangesDecision.Cancel)]
    public void UnsavedChangesDialogCommandsReturnTheSelectedDecision(string commandName, UnsavedChangesDecision expected)
    {
        var viewModel = new UnsavedChangesDialogViewModel();
        switch (commandName)
        {
            case nameof(UnsavedChangesDialogViewModel.SaveCommand):
                viewModel.SaveCommand.Execute(null);
                break;
            case nameof(UnsavedChangesDialogViewModel.DiscardCommand):
                viewModel.DiscardCommand.Execute(null);
                break;
            default:
                viewModel.CancelCommand.Execute(null);
                break;
        }

        Assert.That(viewModel.Decision, Is.EqualTo(expected));
    }

    private static MainViewModel CreateViewModel(FakeUnsavedChangesPrompt? prompt = null) =>
        new(new FakeAuthenticationWorkflow(), prompt ?? new FakeUnsavedChangesPrompt(UnsavedChangesDecision.Discard));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < timeout)
            await Task.Delay(10);

        Assert.That(condition(), Is.True, "The expected screen transition did not complete.");
    }

    private sealed class FakeUnsavedChangesPrompt(UnsavedChangesDecision decision) : IUnsavedChangesPrompt
    {
        public int CallCount { get; private set; }

        public Task<UnsavedChangesDecision> AskAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(decision);
        }
    }

    private sealed class FakeAuthenticationWorkflow : IAuthenticationWorkflow
    {
        public event EventHandler? SessionInvalidated { add { } remove { } }
        public AuthenticationSession? CurrentSession => null;
        public bool HasPendingProfileLoad => false;
        public Task<AuthenticationSession> SignInAsync(string customerCode, string password, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RestoreSessionStatus> TryRestoreSessionAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SignOutAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task InvalidateSessionAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
