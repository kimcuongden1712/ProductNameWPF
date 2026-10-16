namespace ProductName.App.Wpf.Features.Login
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Defines the <see cref="LoginView" />
    /// </summary>
    public partial class LoginView : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginView"/> class.
        /// </summary>
        /// <param name="vm">The vm<see cref="LoginViewModel"/></param>
        public LoginView(LoginViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        /// <summary>
        /// The PasswordInput_OnPasswordChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void PasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox box && DataContext is LoginViewModel vm) vm.Password = box.Password;
        }
    }
}
