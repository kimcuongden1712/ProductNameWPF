namespace ProductName.AppCore.Authentication;

public interface IAuthenticationWorkflow
{
    event EventHandler? SessionInvalidated;

    AuthenticationSession? CurrentSession { get; }

    bool HasPendingProfileLoad { get; }

    Task<RestoreSessionStatus> TryRestoreSessionAsync(CancellationToken cancellationToken);

    Task<AuthenticationSession> SignInAsync(
        string customerCode,
        string password,
        CancellationToken cancellationToken);

    Task<AuthenticationSession> RetryProfileLoadAsync(CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);

    Task InvalidateSessionAsync(CancellationToken cancellationToken);
}
