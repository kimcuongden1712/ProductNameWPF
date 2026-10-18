using ProductName.App.Wpf.Common.Enums;
using ProductName.App.Wpf.Features.Main.Navigation;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Main;

// Represents one fixed slot in the bottom function-key bar.
public sealed class FunctionKeySlotViewModel(FunctionKey key, FunctionKeyDefinition? definition)
{
    public FunctionKey Key { get; } = key;
    public string KeyLabel => Key.ToString();
    public string Label { get; } = definition?.Label ?? string.Empty;
    public ICommand Command { get; } = definition?.Command ?? DisabledCommand.Instance;

    private sealed class DisabledCommand : ICommand
    {
        public static DisabledCommand Instance { get; } = new();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => false;
        public void Execute(object? parameter) { }
    }
}
