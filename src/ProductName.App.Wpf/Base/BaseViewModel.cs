namespace ProductName.App.Wpf.Base
{
    /// <summary>
    /// Base class for ViewModels providing common MVVM functionality
    /// </summary>
    public abstract class BaseViewModel : BaseNotifyPropertyChanged
    {
        #region Fields

        #endregion

        #region Common Properties
        #endregion

        public BaseViewModel()
        {
        }

        #region Lifecycle Methods

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

        #endregion

        #region Overrides

        protected override void OnDisposing()
        {
            base.OnDisposing();
        }

        #endregion
    }
}
