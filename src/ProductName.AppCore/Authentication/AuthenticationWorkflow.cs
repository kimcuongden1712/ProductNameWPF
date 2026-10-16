using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;

namespace ProductName.AppCore.Authentication;

public sealed class AuthenticationWorkflow(
    IAuthenticationApi authenticationApi,
    ITokenStore tokenStore) : IAuthenticationWorkflow
{
    #region fields
    private string? _pendingProfileToken;
    #endregion

    #region properties events
    public AuthenticationSession? CurrentSession { get; private set; }
    public event EventHandler? SessionInvalidated;
    public bool HasPendingProfileLoad => !string.IsNullOrWhiteSpace(_pendingProfileToken);
    #endregion

    #region Signin
    public async Task<AuthenticationSession> SignInAsync(string customerCode, string password, CancellationToken cancellationToken)
    {
        CurrentSession = null;
        _pendingProfileToken = null;

        var accessToken = await authenticationApi.SignInAsync(customerCode.Trim(), password, cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new AuthenticationException(AuthenticationFailure.InvalidResponse, "The sign-in response did not contain a valid access token.");
        }

        return await LoadAndCommitProfileAsync(accessToken, cancellationToken);
    }

    private async Task<AuthenticationSession> LoadAndCommitProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        AccountProfile account;
        try
        {
            account = await authenticationApi.GetCurrentAccountAsync(accessToken, cancellationToken);
        }
        catch (AuthenticationException ex) when (
            ex.Failure is AuthenticationFailure.NetworkUnavailable or AuthenticationFailure.ServiceUnavailable)
        {
            _pendingProfileToken = accessToken;
            throw;
        }
        catch
        {
            _pendingProfileToken = null;
            throw;
        }

        try
        {
            await tokenStore.SaveAsync(accessToken, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _pendingProfileToken = null;
            throw new AuthenticationException(
                AuthenticationFailure.LocalStorage,
                "The account was verified, but the session could not be saved on this device.",
                ex);
        }

        var session = new AuthenticationSession(accessToken, account);
        CurrentSession = session;
        _pendingProfileToken = null;
        return session;
    }
    #endregion


    public async Task<RestoreSessionStatus> TryRestoreSessionAsync(CancellationToken cancellationToken)
    {
        CurrentSession = null;
        _pendingProfileToken = null;

        var accessToken = await tokenStore.ReadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RestoreSessionStatus.NoToken;
        }

        try
        {
            var account = await authenticationApi.GetCurrentAccountAsync(accessToken, cancellationToken);
            CurrentSession = new AuthenticationSession(accessToken, account);
            return RestoreSessionStatus.Restored;
        }
        catch (AuthenticationException ex) when (ex.Failure == AuthenticationFailure.InvalidToken)
        {
            await tokenStore.ClearAsync(cancellationToken);
            return RestoreSessionStatus.InvalidToken;
        }
    }

    public async Task<AuthenticationSession> RetryProfileLoadAsync(CancellationToken cancellationToken)
    {
        var accessToken = _pendingProfileToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("There is no pending profile request to retry.");
        }

        return await LoadAndCommitProfileAsync(accessToken, cancellationToken);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var accessToken = CurrentSession?.AccessToken ?? _pendingProfileToken;
        CurrentSession = null;
        _pendingProfileToken = null;

        await tokenStore.ClearAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            await authenticationApi.SignOutAsync(accessToken, cancellationToken);
        }
    }

    public async Task InvalidateSessionAsync(CancellationToken cancellationToken)
    {
        CurrentSession = null;
        _pendingProfileToken = null;
        try
        {
            await tokenStore.ClearAsync(cancellationToken);
        }
        finally
        {
            SessionInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }
}
