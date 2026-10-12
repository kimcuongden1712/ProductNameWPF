using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProductName.App.Wpf.Base
{
    /// <summary>
    /// Base class providing INotifyPropertyChanged implementation
    /// with helper methods for property change notification
    /// </summary>
    public abstract class BaseNotifyPropertyChanged : INotifyPropertyChanged, IDisposable
    {
        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed (auto-filled by CallerMemberName)</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises PropertyChanged events for multiple properties
        /// </summary>
        /// <param name="propertyNames">Names of the properties that changed</param>
        protected virtual void OnPropertiesChanged(params string[] propertyNames)
        {
            if (propertyNames == null) return;

            foreach (var propertyName in propertyNames)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Raises PropertyChanged event with custom PropertyChangedEventArgs
        /// </summary>
        /// <param name="e">PropertyChangedEventArgs containing property information</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        #endregion

        #region Property Setting Helper Methods

        /// <summary>
        /// Sets the property value and raises PropertyChanged if the value has changed
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value to set</param>
        /// <param name="propertyName">Name of the property (auto-filled by CallerMemberName)</param>
        /// <returns>True if the property value was changed; otherwise, false</returns>
        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets the property value and raises PropertyChanged if the value has changed,
        /// with a callback action executed after the property is set
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value to set</param>
        /// <param name="onChanged">Action to execute after the property is changed</param>
        /// <param name="propertyName">Name of the property (auto-filled by CallerMemberName)</param>
        /// <returns>True if the property value was changed; otherwise, false</returns>
        protected virtual bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets the property value and raises PropertyChanged if the value has changed,
        /// with a callback action that receives the old and new values
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value to set</param>
        /// <param name="onChanged">Action to execute with old and new values after the property is changed</param>
        /// <param name="propertyName">Name of the property (auto-filled by CallerMemberName)</param>
        /// <returns>True if the property value was changed; otherwise, false</returns>
        protected virtual bool SetProperty<T>(ref T field, T value, Action<T, T> onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            T oldValue = field;
            field = value;
            onChanged?.Invoke(oldValue, value);
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets the property value and raises PropertyChanged for multiple properties
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value to set</param>
        /// <param name="propertyNames">Names of all properties to notify about changes</param>
        /// <returns>True if the property value was changed; otherwise, false</returns>
        protected virtual bool SetProperty<T>(ref T field, T value, params string[] propertyNames)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertiesChanged(propertyNames);
            return true;
        }

        /// <summary>
        /// Sets the property value with validation and raises PropertyChanged if the value has changed
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value to set</param>
        /// <param name="validator">Function to validate the new value</param>
        /// <param name="propertyName">Name of the property (auto-filled by CallerMemberName)</param>
        /// <returns>True if the property value was changed; otherwise, false</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        protected virtual bool SetProperty<T>(ref T field, T value, Func<T, bool> validator, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (validator != null && !validator(value))
                throw new ArgumentException($"Invalid value for property {propertyName}: {value}");

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if PropertyChanged event has any subscribers
        /// </summary>
        protected bool HasPropertyChangedSubscribers => PropertyChanged != null;

        /// <summary>
        /// Gets the number of PropertyChanged event subscribers
        /// </summary>
        protected int PropertyChangedSubscriberCount => PropertyChanged?.GetInvocationList().Length ?? 0;

        /// <summary>
        /// Temporarily suspends PropertyChanged notifications
        /// </summary>
        /// <returns>IDisposable that resumes notifications when disposed</returns>
        protected virtual IDisposable SuspendNotifications()
        {
            return new NotificationSuspender(this);
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        /// <summary>
        /// Releases all resources used by the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clear event handlers to prevent memory leaks
                    PropertyChanged = null;
                    OnDisposing();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Override this method in derived classes for custom disposal logic
        /// </summary>
        protected virtual void OnDisposing()
        {
            // Override in derived classes for custom disposal logic
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~BaseNotifyPropertyChanged()
        {
            Dispose(false);
        }

        /// <summary>
        /// Throws ObjectDisposedException if the object has been disposed
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Helper class to temporarily suspend property change notifications
        /// </summary>
        private class NotificationSuspender : IDisposable
        {
            private readonly BaseNotifyPropertyChanged _owner;
            private readonly PropertyChangedEventHandler? _originalHandler;

            public NotificationSuspender(BaseNotifyPropertyChanged owner)
            {
                _owner = owner;
                _originalHandler = _owner.PropertyChanged;
                _owner.PropertyChanged = null;
            }

            public void Dispose()
            {
                _owner.PropertyChanged = _originalHandler;
            }
        }

        #endregion
    }
}