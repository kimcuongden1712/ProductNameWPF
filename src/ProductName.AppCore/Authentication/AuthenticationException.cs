using ProductName.AppCore.Model;

namespace ProductName.AppCore.Authentication
{
    public sealed class AuthenticationException : Exception
    {
        public AuthenticationException(AuthenticationFailure failure, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            Failure = failure;
        }

        public AuthenticationFailure Failure { get; }
    }
}
