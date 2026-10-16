using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;

namespace ProductName.AppCore.Authentication;

public sealed class AuthenticationWorkflow(
    IAuthenticationApi authenticationApi,
    ITokenStore tokenStore) : IAuthenticationWorkflow
{
    #region fields

    private string? _pendingProfileToken;

    #endregion fields

    #region properties events

    public AuthenticationSession? CurrentSession { get; private set; }

    public event EventHandler? SessionInvalidated;

    public bool HasPendingProfileLoad => !string.IsNullOrWhiteSpace(_pendingProfileToken);

    #endregion properties events

    #region Signin

    public async Task<AuthenticationSession> SignInAsync(string customerCode, string password, CancellationToken cancellationToken)
    {
        CurrentSession = null;
        _pendingProfileToken = null;

        var result = await authenticationApi.SignInAsync(customerCode.Trim(), password, cancellationToken);
        var accessToken = result.AccessToken;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new AuthenticationException(AuthenticationFailure.InvalidResponse, "The sign-in response did not contain a valid access token.");
        }

        return await LoadAndCommitProfileAsync(accessToken, result.ExpiresAtUtc, cancellationToken);
    }

    private async Task<AuthenticationSession> LoadAndCommitProfileAsync(string accessToken, DateTimeOffset? expiresAtUtc, CancellationToken cancellationToken)
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

        var session = new AuthenticationSession(accessToken, account, expiresAtUtc);
        CurrentSession = session;
        _pendingProfileToken = null;
        return session;
    }

    #endregion Signin

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
        catch (AuthenticationException ex) when (ex.Failure is AuthenticationFailure.NetworkUnavailable or AuthenticationFailure.ServiceUnavailable)
        {
            _pendingProfileToken = accessToken;
            return RestoreSessionStatus.Unavailable;
        }
    }

    public async Task<AuthenticationSession> RetryProfileLoadAsync(CancellationToken cancellationToken)
    {
        var accessToken = _pendingProfileToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("There is no pending profile request to retry.");
        }

        return await LoadAndCommitProfileAsync(accessToken, null, cancellationToken);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var accessToken = CurrentSession?.AccessToken ?? _pendingProfileToken;
        CurrentSession = null;
        _pendingProfileToken = null;

        await tokenStore.ClearAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            try { await authenticationApi.SignOutAsync(accessToken, cancellationToken); }
            catch { /* Local sign-out succeeds even when the service is unavailable. */ }
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