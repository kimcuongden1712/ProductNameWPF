namespace ProductName.AppCore.Authentication.Interface;

public interface IAuthenticationWorkflow
{
    event EventHandler? SessionInvalidated;

    AuthenticationSession? CurrentSession { get; }

    bool HasPendingProfileLoad { get; }

    Task<AuthenticationSession> SignInAsync(
        string customerCode,
        string password,
        CancellationToken cancellationToken);

    //Task<RestoreSessionStatus> TryRestoreSessionAsync(CancellationToken cancellationToken);

    //Task<AuthenticationSession> RetryProfileLoadAsync(CancellationToken cancellationToken);

    //Task SignOutAsync(CancellationToken cancellationToken);

    //Task InvalidateSessionAsync(CancellationToken cancellationToken);
}
