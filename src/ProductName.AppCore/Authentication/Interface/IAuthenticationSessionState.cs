namespace ProductName.AppCore.Authentication.Interface;

/// <summary>Shared in-memory session state used by workflows and authenticated API transport.</summary>
public interface IAuthenticationSessionState
{
    AuthenticationSession? CurrentSession { get; }

    event EventHandler? SessionInvalidated;

    void Set(AuthenticationSession session);

    void Clear(bool invalidated = false);
}