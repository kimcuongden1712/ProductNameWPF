using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;

namespace ProductName.AppCore.Tests;

public sealed class AuthenticationWorkflowTests
{
    [Test]
    public async Task Restore_WhenServiceUnavailable_KeepsTokenAndDoesNotCreateSession()
    {
        var store = new FakeTokenStore(("saved-token", (DateTimeOffset?)null));
        var api = new FakeAuthenticationApi { ProfileError = new AuthenticationException(AuthenticationFailure.NetworkUnavailable, "offline") };
        var workflow = new AuthenticationWorkflow(api, store);

        var result = await workflow.TryRestoreSessionAsync(CancellationToken.None);

        Assert.That(result, Is.EqualTo(RestoreSessionStatus.Unavailable));
        Assert.That(store.Token, Is.EqualTo("saved-token"));
        Assert.That(workflow.CurrentSession, Is.Null);
        Assert.That(workflow.HasPendingProfileLoad, Is.True);
    }

    [Test]
    public async Task Restore_WhenTokenInvalid_ClearsToken()
    {
        var store = new FakeTokenStore(("expired", (DateTimeOffset?)null));
        var api = new FakeAuthenticationApi { ProfileError = new AuthenticationException(AuthenticationFailure.InvalidToken, "expired") };
        var workflow = new AuthenticationWorkflow(api, store);

        var result = await workflow.TryRestoreSessionAsync(CancellationToken.None);

        Assert.That(result, Is.EqualTo(RestoreSessionStatus.InvalidToken));
        Assert.That(store.Token, Is.Null);
    }

    [Test]
    public async Task SignIn_CommitsTokenOnlyAfterProfileLoads()
    {
        var store = new FakeTokenStore();
        var api = new FakeAuthenticationApi { ProfileError = new AuthenticationException(AuthenticationFailure.ServiceUnavailable, "busy") };
        var workflow = new AuthenticationWorkflow(api, store);

        Assert.ThrowsAsync<AuthenticationException>(() => workflow.SignInAsync(" C1 ", "secret", CancellationToken.None));

        Assert.That(store.Token, Is.Null);
        Assert.That(workflow.CurrentSession, Is.Null);
        Assert.That(workflow.HasPendingProfileLoad, Is.True);
    }

    private sealed class FakeTokenStore((string? AccessToken, DateTimeOffset? ExpiresAtUtc)? token = null) : ITokenStore
    {
        public string? Token { get; private set; } = token?.AccessToken;

        public Task<(string? AccessToken, DateTimeOffset? ExpiresAtUtc)?> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(token);

        public Task SaveAsync(string accessToken, DateTimeOffset? expiresAtUtc, CancellationToken cancellationToken)
        { Token = accessToken; return Task.CompletedTask; }

        public Task ClearAsync(CancellationToken cancellationToken)
        { Token = null; return Task.CompletedTask; }
    }

    private sealed class FakeAuthenticationApi : IAuthenticationApi
    {
        public AuthenticationException? ProfileError { get; init; }

        public Task<SignInResult> SignInAsync(string customerCode, string password, CancellationToken cancellationToken) => Task.FromResult(new SignInResult("new-token", DateTimeOffset.UtcNow.AddHours(1)));

        public Task<AccountProfile> GetCurrentAccountAsync(string accessToken, CancellationToken cancellationToken) => ProfileError is null
            ? Task.FromResult(new AccountProfile("op", "Operator", "Company", Array.Empty<BranchProfile>()))
            : Task.FromException<AccountProfile>(ProfileError);

        public Task SignOutAsync(string accessToken, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}