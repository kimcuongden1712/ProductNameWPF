namespace ProductName.App.Wpf.Base
{
    /// <summary>
    /// Base class for ViewModels providing common MVVM functionality
    /// </summary>
    public abstract class BaseViewModel : BaseNotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class.
        /// </summary>
        public BaseViewModel()
        {
        }

        /// <summary>
        /// Called when ViewModel is initialized (override in derived classes)
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// Called when ViewModel is loaded (override in derived classes)
        /// </summary>
        protected virtual void OnLoaded()
        {
        }

        /// <summary>
        /// The OnDisposing
        /// </summary>
        protected override void OnDisposing()
        {
            base.OnDisposing();
        }
    }
}