# NewTemplateProject

A standalone WPF style-resource starter and visual catalogue targeting `net10.0-windows`. It is not added to the main solution.

## Build and run

```powershell
dotnet build .\ProductName.SampleTemplate.csproj
dotnet run --project .\ProductName.SampleTemplate.csproj
```

## Resource layout

- `Resources/Tokens/Colors.xaml`: source palette colors.
- `Resources/Tokens/Typography.xaml`: font family and type sizes.
- `Resources/Tokens/Dimensions.xaml`: spacing, corner radius, and control dimensions.
- `Resources/Themes/Light.xaml`: semantic brushes for window, popup, list, selection, and function-key roles.
- `Resources/Styles/CommonStyles.xaml`: shared control base, text hierarchy, label variants, and showcase card/code styles.
- `Resources/Styles/ButtonStyles.xaml`: base, primary, secondary, size variants, and function-key button.
- `Resources/Styles/InputStyles.xaml`: text box sizes, check box, radio button, toggle button, ComboBox, and DatePicker.
- `Resources/Styles/ListStyles.xaml`: ListBox, item containers, and operational status colors.
- `Resources/Styles/DataGridStyles.xaml`: stock WPF DataGrid, column header, row, and cell styles.
- `Resources/Styles/ControlStyles.xaml`: tooltip and scrollbar styles.
- `Resources/Styles/CustomScrollViewerStyles.xaml`: executable WPF ScrollViewer and scrollbar templates.
- `Resources/Styles/FunctionKeyStyles.xaml`: function-key label/button composition.
- `Resources/Styles/WindowStyles.xaml`: main and dialog window styles.
- `Resources/ThirdParty/Source/OptionalInfragisticsStyles.xaml`: optional Infragistics control styles, copied as content and not compiled or loaded by this app.
- `SampleTemplateView.xaml`: visual catalogue with interactive controls, a populated WPF DataGrid, a dialog sample, and style usage examples.

`App.xaml` merges dictionaries in dependency order: tokens, theme brushes, shared styles, then control-specific styles. Add new dictionaries there and keep shared keys unique.

## Adding or changing a style

1. Add or update a token in the appropriate `Tokens` dictionary.
2. If it is a theme-dependent color, expose it as a semantic brush in `Themes/Light.xaml`.
3. Reference tokens and brushes from a keyed style in the relevant `Styles` dictionary.
4. Apply the style explicitly with `StaticResource` in a view, or derive a variant with `BasedOn`.
5. Build and inspect the corresponding section in `SampleTemplateView`; check normal, hover, pressed, focused, selected, and disabled states.

This starter uses `StaticResource` because it has one fixed theme. If runtime theme switching is introduced, use `DynamicResource` for theme-dependent values and replace the active theme dictionary as a separately tested feature.

## Optional Infragistics styles

`Resources/ThirdParty/Source/OptionalInfragisticsStyles.xaml` contains styles for `XamDataGrid`, `DataRecordCellArea`, `LabelPresenter`, `CellValuePresenter`, and `XamDateTimeEditor`. The sample page shows how to reference the style keys. The dictionary is intentionally not compiled or merged into this application because:

- It requires Infragistics WPF 2020.2 assemblies (`InfragisticsWPF4.DataPresenter.v20.2`, `InfragisticsWPF4.Controls.Editors.XamDateTimeInput.v20.2`, `InfragisticsWPF4.Editors.v20.2`, and `InfragisticsWPF4.v20.2`), which are not installed or present in this workspace.
- These are licensed, vendor-specific assemblies; the project does not bundle or redistribute them.
- The source application targets .NET Framework 4.8, while this showcase targets `net10.0-windows`. Compatibility with this target must be confirmed before integration.

To activate these resources, use a compatible Infragistics WPF version under the applicable license, align the application's target framework with the version's supported frameworks, add the required references, then compile and merge the dictionary after its dependency dictionaries and theme resources.

`Resources/Styles/CustomScrollViewerStyles.xaml` is a runnable WPF-only adaptation of the custom scrollbar/ScrollViewer template. It is merged by `App.xaml` and does not depend on Infragistics. The template includes the WPF scroll content presenter part and bindings required to operate as a ScrollViewer.
