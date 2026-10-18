using ProductName.App.Wpf.Base;
using ProductName.App.Wpf.Common.Constants;
using ProductName.App.Wpf.Common.Enums;
using ProductName.App.Wpf.Features.Main.Navigation;
using ProductName.App.Wpf.Features.Main.Navigation.Interface;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Main.WorkScreens;

public sealed class D2ViewModel : BaseViewModel, IFunctionKeyProvider, IUnsavedChangesScreen
{
    #region Fields

    private readonly MainRegionNavigationService _navigation;
    private string _notes = string.Empty;
    private string _savedNotes = string.Empty;
    private string _status = "D2 is ready.";

    public string ScreenTitle => "D2 — Keep vehicles";

    #endregion Fields

    #region properties

    public string Notes
    {
        get => _notes;
        set
        {
            if (SetProperty(ref _notes, value))
                OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool HasUnsavedChanges => !string.Equals(Notes, _savedNotes, StringComparison.Ordinal);

    // D2 has its own function-key set, separate from D1.
    public IReadOnlyDictionary<FunctionKey, FunctionKeyDefinition> FunctionKeys { get; }

    #endregion properties

    #region commands & events

    public ICommand NavigateToD1Command { get; }
    public ICommand RefreshCommand { get; }

    #endregion commands & events

    public D2ViewModel(MainRegionNavigationService navigation)
    {
        _navigation = navigation;

        NavigateToD1Command = new AsyncCommand(
            () => _navigation.NavigateToAsync(AppConstants.D1ScreenId, CancellationToken.None),
            () => true,
            HandleCommandError);
        RefreshCommand = new RelayCommand(_ => Status = "D2 data refreshed.");

        FunctionKeys = new Dictionary<FunctionKey, FunctionKeyDefinition>
        {
            [FunctionKey.F5] = new("D1", NavigateToD1Command),
            [FunctionKey.F12] = new("Refresh", RefreshCommand)
        };
    }

    #region methods

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _savedNotes = Notes;
        OnPropertyChanged(nameof(HasUnsavedChanges));
        Status = "D2 changes saved for this session.";
        return Task.CompletedTask;
    }

    public void DiscardChanges()
    {
        Notes = _savedNotes;
        Status = "D2 changes discarded.";
    }

    private void HandleCommandError(Exception exception) => Status = exception.Message;

    #endregion methods
}