using System.Windows.Input;

namespace ProductName.App.Wpf.Base;

/// <summary>
/// A command that relays its functionality to other objects by invoking delegates. The default return value for the CanExecute method is 'true'.
/// Using for synchronous commands that do not require async/await, complete quick, such as UI navigation, simple validation, clear content etc.
/// </summary>
/// <param name="execute"></param>
/// <param name="canExecute"></param>
public sealed class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
