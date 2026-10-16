using System.Windows.Input;

namespace ProductName.App.Wpf.Base;

/// <summary>
/// Used for asynchronous commands that may take some time to complete, such as network requests or database operations. 
/// It prevents multiple executions of the command while it is already running and provides error handling.
/// </summary>
/// <param name="execute"></param>
/// <param name="canExecute"></param>
/// <param name="onError"></param>
public sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute, Action<Exception> onError) : ICommand
{
    private bool _isRunning;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isRunning && canExecute();

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isRunning = true;
        NotifyCanExecuteChanged();
        try
        {
            await execute();
        }
        catch (Exception ex)
        {
            onError(ex);
        }
        finally
        {
            _isRunning = false;
            NotifyCanExecuteChanged();
        }
    }

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
