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
