using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Main.Navigation;

/// <summary>Provides the label and command for one function key.</summary>
public sealed record FunctionKeyDefinition(string Label, ICommand Command);
