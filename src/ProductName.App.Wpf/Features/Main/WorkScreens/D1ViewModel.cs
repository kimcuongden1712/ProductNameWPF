using ProductName.App.Wpf.Base;
using ProductName.App.Wpf.Common.Constants;
using ProductName.App.Wpf.Common.Enums;
using ProductName.App.Wpf.Features.Main.Navigation;
using ProductName.App.Wpf.Features.Main.Navigation.Interface;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Main.WorkScreens;

public sealed class D1ViewModel : BaseViewModel, IFunctionKeyProvider, IUnsavedChangesScreen
{
    #region Fields
    private readonly MainRegionNavigationService _navigation;
    private string _customerCode = string.Empty;
    private string _selectedCategory = "Dispatch";
    private bool _isPriority;
    private DateTime? _dispatchDate = DateTime.Today;
    private string _notes = string.Empty;
    private string _selectedRegion = "North";
    private bool _isStandardMode = true;
    private bool _isExpeditedMode;
    private bool _isAutoDispatch = true;
    private D1FormSnapshot _savedForm;
    private string _status = "D1 is ready.";

    public string ScreenTitle => "D1 — Dispatch";

    public IReadOnlyList<string> Categories { get; } = ["Dispatch", "Pickup", "Delivery"];
    public IReadOnlyList<string> Operators { get; } = ["Operator A", "Operator B", "Operator C"];
    public IReadOnlyDictionary<FunctionKey, FunctionKeyDefinition> FunctionKeys { get; }
    #endregion
    #region Properties
    public string CustomerCode
    {
        get => _customerCode;
        set => SetFormProperty(ref _customerCode, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set => SetFormProperty(ref _selectedCategory, value);
    }

    public bool IsPriority
    {
        get => _isPriority;
        set => SetFormProperty(ref _isPriority, value);
    }

    public DateTime? DispatchDate
    {
        get => _dispatchDate;
        set => SetFormProperty(ref _dispatchDate, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetFormProperty(ref _notes, value);
    }

    public string SelectedRegion
    {
        get => _selectedRegion;
        private set => SetFormProperty(ref _selectedRegion, value);
    }

    public string RegionMenuHeader => $"Region: {SelectedRegion}";

    public bool IsStandardMode
    {
        get => _isStandardMode;
        set
        {
            if (!value)
                return;

            SetFormProperty(ref _isStandardMode, true);
            SetFormProperty(ref _isExpeditedMode, false, nameof(IsExpeditedMode));
        }
    }

    public bool IsExpeditedMode
    {
        get => _isExpeditedMode;
        set
        {
            if (!value)
                return;

            SetFormProperty(ref _isExpeditedMode, true);
            SetFormProperty(ref _isStandardMode, false, nameof(IsStandardMode));
        }
    }

    public bool IsAutoDispatch
    {
        get => _isAutoDispatch;
        set => SetFormProperty(ref _isAutoDispatch, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    // Compare all form fields so every input participates in the leave-screen prompt.
    public bool HasUnsavedChanges => CaptureForm() != _savedForm;
    #endregion
    #region Commands & events
    public ICommand ShowDetailsCommand { get; }
    public ICommand NavigateToD2Command { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectRegionCommand { get; }
    #endregion

    public D1ViewModel(MainRegionNavigationService navigation)
    {
        _navigation = navigation;

        ShowDetailsCommand = new RelayCommand(_ => Status = "F1 action executed on D1.");
        NavigateToD2Command = new AsyncCommand(
            () => _navigation.NavigateToAsync(AppConstants.D2ScreenId, CancellationToken.None),
            () => true,
            HandleCommandError);
        RefreshCommand = new RelayCommand(_ => Status = "D1 data refreshed.");
        SelectRegionCommand = new RelayCommand(parameter =>
        {
            if (parameter is string region)
            {
                SelectedRegion = region;
                OnPropertyChanged(nameof(RegionMenuHeader));
            }
        });

        // D1 declares its own available keys and actions.
        FunctionKeys = new Dictionary<FunctionKey, FunctionKeyDefinition>
        {
            [FunctionKey.F1] = new("Details", ShowDetailsCommand),
            [FunctionKey.F2] = new("Check", ShowDetailsCommand),//Sample
            [FunctionKey.F5] = new("D2", NavigateToD2Command),
            [FunctionKey.F12] = new("Refresh", RefreshCommand)
        };

        _savedForm = CaptureForm();
    }
    #region methods
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _savedForm = CaptureForm();
        OnPropertyChanged(nameof(HasUnsavedChanges));
        Status = "D1 changes saved for this session.";
        return Task.CompletedTask;
    }

    public void DiscardChanges()
    {
        CustomerCode = _savedForm.CustomerCode;
        SelectedCategory = _savedForm.Category;
        IsPriority = _savedForm.IsPriority;
        DispatchDate = _savedForm.DispatchDate;
        Notes = _savedForm.Notes;
        SelectedRegion = _savedForm.Region;
        IsStandardMode = _savedForm.IsStandardMode;
        IsExpeditedMode = _savedForm.IsExpeditedMode;
        IsAutoDispatch = _savedForm.IsAutoDispatch;
        OnPropertyChanged(nameof(RegionMenuHeader));
        Status = "D1 changes discarded.";
    }

    private bool SetFormProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
            return false;

        OnPropertyChanged(nameof(HasUnsavedChanges));
        return true;
    }

    // A single snapshot makes save and discard cover every form field.
    private D1FormSnapshot CaptureForm() => new(
        CustomerCode,
        SelectedCategory,
        IsPriority,
        DispatchDate,
        Notes,
        SelectedRegion,
        IsStandardMode,
        IsExpeditedMode,
        IsAutoDispatch);

    private void HandleCommandError(Exception exception) => Status = exception.Message;
    #endregion
    #region Nested types
    private sealed record D1FormSnapshot(
        string CustomerCode,
        string Category,
        bool IsPriority,
        DateTime? DispatchDate,
        string Notes,
        string Region,
        bool IsStandardMode,
        bool IsExpeditedMode,
        bool IsAutoDispatch);
    #endregion
}
