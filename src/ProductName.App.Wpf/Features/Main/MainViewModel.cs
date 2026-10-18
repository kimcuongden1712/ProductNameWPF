using ProductName.App.Wpf.Base;
using ProductName.App.Wpf.Common.Constants;
using ProductName.App.Wpf.Common.Enums;
using ProductName.App.Wpf.Features.Main.Navigation;
using ProductName.App.Wpf.Features.Main.Navigation.Interface;
using ProductName.App.Wpf.Features.Main.WorkScreens;
using ProductName.AppCore.Authentication.Interface;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Main;

public sealed class MainViewModel : BaseViewModel
{
    #region fields
    // The shell owns screen navigation and forwards function keys to the current screen.
    private readonly MainRegionNavigationService _navigation;
    private readonly Dictionary<FunctionKey, RelayCommand> _functionKeyCommands;
    private readonly List<ICommand> _activeFunctionKeyCommands = [];
    private IReadOnlyList<FunctionKeySlotViewModel> _functionKeySlots = [];
    private BaseViewModel? _currentScreen;
    private string _navigationMessage = string.Empty;
    #endregion
    #region properties
    public string Header { get; }
    public BaseViewModel? CurrentScreen
    {
        get => _currentScreen;
        private set => SetProperty(ref _currentScreen, value);
    }

    public IReadOnlyList<FunctionKeySlotViewModel> FunctionKeySlots
    {
        get => _functionKeySlots;
        private set => SetProperty(ref _functionKeySlots, value);
    }

    public string NavigationMessage
    {
        get => _navigationMessage;
        private set => SetProperty(ref _navigationMessage, value);
    }
    #endregion
    #region commands & events
    public ICommand NavigateToD1Command { get; }
    public ICommand NavigateToD2Command { get; }
    public ICommand F1Command => _functionKeyCommands[FunctionKey.F1];
    public ICommand F2Command => _functionKeyCommands[FunctionKey.F2];
    public ICommand F3Command => _functionKeyCommands[FunctionKey.F3];
    public ICommand F4Command => _functionKeyCommands[FunctionKey.F4];
    public ICommand F5Command => _functionKeyCommands[FunctionKey.F5];
    public ICommand F6Command => _functionKeyCommands[FunctionKey.F6];
    public ICommand F7Command => _functionKeyCommands[FunctionKey.F7];
    public ICommand F8Command => _functionKeyCommands[FunctionKey.F8];
    public ICommand F9Command => _functionKeyCommands[FunctionKey.F9];
    public ICommand F10Command => _functionKeyCommands[FunctionKey.F10];
    public ICommand F11Command => _functionKeyCommands[FunctionKey.F11];
    public ICommand F12Command => _functionKeyCommands[FunctionKey.F12];
    #endregion
    public MainViewModel(IAuthenticationWorkflow workflow, IUnsavedChangesPrompt unsavedChangesPrompt)
    {
        Header = $"{workflow.CurrentSession?.Account?.CompanyName} — {workflow.CurrentSession?.Account?.DisplayName}";

        _navigation = new MainRegionNavigationService(unsavedChangesPrompt);
        var d1 = new D1ViewModel(_navigation);
        var d2 = new D2ViewModel(_navigation);
        _navigation.Register(AppConstants.D1ScreenId, d1);
        _navigation.Register(AppConstants.D2ScreenId, d2);
        _navigation.CurrentScreenChanged += OnCurrentScreenChanged;

        _functionKeyCommands = Enum.GetValues<FunctionKey>().ToDictionary(
            key => key,
            key => new RelayCommand(_ => ExecuteFunctionKey(key), _ => CanExecuteFunctionKey(key)));

        NavigateToD1Command = CreateNavigationCommand(AppConstants.D1ScreenId);
        NavigateToD2Command = CreateNavigationCommand(AppConstants.D2ScreenId);
        _navigation.ActivateInitialScreen(AppConstants.D1ScreenId);
    }

    #region methods
    private ICommand CreateNavigationCommand(string screenId) => new AsyncCommand(
        () => NavigateAsync(screenId),
        () => true,
        exception => NavigationMessage = $"Navigation failed: {exception.Message}");

    private async Task NavigateAsync(string screenId)
    {
        NavigationMessage = string.Empty;
        await _navigation.NavigateToAsync(screenId, CancellationToken.None);
    }

    private void OnCurrentScreenChanged(object? sender, EventArgs eventArgs)
    {
        var screen = _navigation.CurrentScreen!;
        foreach (var command in _activeFunctionKeyCommands)
            command.CanExecuteChanged -= OnActiveFunctionKeyCanExecuteChanged;
        _activeFunctionKeyCommands.Clear();

        // Refresh the key bar whenever the displayed screen changes.
        CurrentScreen = screen;
        FunctionKeySlots = CreateFunctionKeySlots(screen);
        NavigationMessage = string.Empty;

        if (screen is IFunctionKeyProvider provider)
        {
            foreach (var command in provider.FunctionKeys.Values.Select(definition => definition.Command).Distinct())
            {
                command.CanExecuteChanged += OnActiveFunctionKeyCanExecuteChanged;
                _activeFunctionKeyCommands.Add(command);
            }
        }

        foreach (var command in _functionKeyCommands.Values)
            command.NotifyCanExecuteChanged();
    }

    private void OnActiveFunctionKeyCanExecuteChanged(object? sender, EventArgs eventArgs)
    {
        foreach (var command in _functionKeyCommands.Values)
            command.NotifyCanExecuteChanged();
    }

    private static IReadOnlyList<FunctionKeySlotViewModel> CreateFunctionKeySlots(BaseViewModel screen)
    {
        var definitions = (screen as IFunctionKeyProvider)?.FunctionKeys
            ?? new Dictionary<FunctionKey, FunctionKeyDefinition>();

        return Enum.GetValues<FunctionKey>()
            .Select(key => new FunctionKeySlotViewModel(
                key,
                definitions.TryGetValue(key, out var definition) ? definition : null))
            .ToArray();
    }

    private bool CanExecuteFunctionKey(FunctionKey key)
    {
        if (CurrentScreen is not IFunctionKeyProvider provider ||
            !provider.FunctionKeys.TryGetValue(key, out var definition))
            return false;

        return definition.Command.CanExecute(null);
    }

    private void ExecuteFunctionKey(FunctionKey key)
    {
        if (CurrentScreen is IFunctionKeyProvider provider &&
            provider.FunctionKeys.TryGetValue(key, out var definition) &&
            definition.Command.CanExecute(null))
        {
            definition.Command.Execute(null);
        }
    }
    #endregion
}
