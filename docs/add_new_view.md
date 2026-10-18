Để thêm một màn hình mới, mỗi màn hình mới cần thêm **ViewModel, View, DataTemplate, đăng ký điều hướng và một điểm gọi điều hướng**. Cấu hình Function Key nằm trong ViewModel màn hình đó; điều hướng và thanh phím vẫn dùng chung, ví dụ F2
## Các bước

1. **Tạo `F2ViewModel`** trong `Features/LogicBusinessFolder`.

   ViewModel kế thừa `BaseViewModel` và triển khai `IFunctionKeyProvider`. Nếu màn hình có dữ liệu chưa lưu, triển khai thêm `IUnsavedChangesScreen`.

2. **Định nghĩa Function Key của F2** trong ViewModel.

   Tạo command cho các thao tác được màn hình hỗ trợ, rồi khai báo chúng trong `FunctionKeys`. Ví dụ:

   ```csharp
   FunctionKeys = new Dictionary<FunctionKey, FunctionKeyDefinition>
   {
       [FunctionKey.F5] = new("D1", NavigateToD1Command),
       [FunctionKey.F12] = new("Refresh", RefreshCommand)
   };
   ```

   Phím không khai báo sẽ tự bị disable. Nếu command có `CanExecute`, cả nút trên thanh và phím vật lý sẽ tuân theo trạng thái đó.

3. **Tạo `F2View.xaml`** và code-behind tối thiểu chỉ gọi `InitializeComponent()`. Bind các control vào property và command của `F2ViewModel`, không đặt nghiệp vụ trong code-behind.

4. **Đăng ký DataTemplate** trong **MainView.xaml** để WPF hiển thị `F2View` khi `CurrentScreen` là `F2ViewModel`:

   ```xml
   <DataTemplate DataType="{x:Type screens:F2ViewModel}">
       <main:F2View />
   </DataTemplate>
   ```

5. **Đăng ký instance F2 trong `MainViewModel`** và navigation service:

   ```csharp
   public const string F2ScreenId = "F2";

   var f2 = new F2ViewModel(_navigation);
   _navigation.Register(F2ScreenId, f2);

   NavigateToF2Command = CreateNavigationCommand(F2ScreenId);
   ```

   ViewModel được tạo một lần trong phiên của cửa sổ rồi được tái sử dụng, nên trạng thái được giữ khi chuyển sang màn hình khác và quay lại.
   
   CreateNavigationCommand gọi navigation service. Service tìm màn hình theo ID, hỏi Save / Discard / Cancel nếu màn hình hiện tại có thay đổi chưa lưu, rồi chỉ khi được phép mới đổi CurrentScreen. Vì F2 được đăng ký một lần nên khi người dùng quay lại, dùng lại chính instance đó.

   Sau đó quyết định nút nào yêu cầu mở F2.

   Nếu mở từ SideView, thêm command vào ViewModel:
      ```csharp
   public ICommand NavigateToF2Command { get; }
   ```

   Nếu mở bằng Function Key từ một màn hình khác, màn hình sở hữu phím đó gọi lệnh điều hướng. Ví dụ D1 dùng F5 để mở F2:
```csharp
    NavigateToF2Command = new AsyncCommand(
        () => _navigation.NavigateToAsync(MainViewModel.F2ScreenId, CancellationToken.None),
        () => true,
        HandleCommandError);

    FunctionKeys = new Dictionary<FunctionKey, FunctionKeyDefinition>
    {
        [FunctionKey.F5] = new("F2", NavigateToF2Command)
    };
   ```