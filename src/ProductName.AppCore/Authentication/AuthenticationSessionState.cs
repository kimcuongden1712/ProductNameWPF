using ProductName.AppCore.Authentication.Interface;

namespace ProductName.AppCore.Authentication;

public sealed class AuthenticationSessionState : IAuthenticationSessionState
{
    public AuthenticationSession? CurrentSession { get; private set; }

    public event EventHandler? SessionInvalidated;

    public void Set(AuthenticationSession session) => CurrentSession = session;

    public void Clear(bool invalidated = false)
    {
        CurrentSession = null;
        if (invalidated) SessionInvalidated?.Invoke(this, EventArgs.Empty);
    }
}