using ProductName.App.Wpf.Common.Enums;

namespace ProductName.App.Wpf.Features.Main.Navigation.Interface;

// Each screen lists only the function keys it supports.
public interface IFunctionKeyProvider
{
    IReadOnlyDictionary<FunctionKey, FunctionKeyDefinition> FunctionKeys { get; }
}
