## Getting Started
The WPF application is the executable entry point of the product. 
It contains the application startup and shutdown logic, the main window, WPF views, view models, navigation, and application-wide UI resources.
This project composes the other layers at startup, configures dependency injection, and selects the initial screen. It should not contain core business rules or directly implement database and device access.

Typical contents: App.xaml, App.xaml.cs, windows, views, view models, navigation, and UI resources.

## Responsibility
WPF, Windows, View, ViewModel, Start application, and handle the main window.

## Dependencies
AppCore; Infrastructure

## NOTE
App.Wpf là composition root: nó biết các implementation cụ thể cần ghép với abstraction khi khởi động. 
Application không tham chiếu Infrastructure; nếu không, chi tiết công nghệ sẽ lan vào use case. 
Với App.Wpf, hạn chế tham chiếu trực tiếp Domain: để Application trả kết quả phù hợp cho UI thường giúp các tầng ít gắn chặt với nhau hơn.

## Screen flow

`MainWindow` is the shell and displays the current feature through a `DataTemplate`:

`LoginView` → `MenuView` (`A_1_View`) → `DispatchView` (`B_View`).

Each screen has its own ViewModel. `DispatchView` keeps its header, side bar, and main workspace in separate regions. The header contains `B_1View` and `B_2View`; the side region switches between `C_3View` and the dispatch menu; the main region switches between `D_1View`, `D_2View`, `E_1View`, and `F_1View`. Its initial content is `C_3View` and `D_1View`.

`MainView` is retained as a reference screen and is not part of the normal sign-in navigation flow.
