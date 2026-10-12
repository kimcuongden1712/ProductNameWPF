using ProductName.AppCore.Authentication;

namespace ProductName.AppCore.Tests.Authentication;

public sealed class AuthenticationWorkflowTests
{
    private static readonly AccountProfile Account = new(
        "operator-1",
        "Test Operator",
        "Test Company",
        [new BranchProfile("branch-1", "Main Branch")]);

    [Test]
    public async Task Restore_without_saved_token_returns_no_token()
    {
        var api = new FakeAuthenticationApi();
        var store = new FakeTokenStore();
        var workflow = new AuthenticationWorkflow(api, store);

        var result = await workflow.TryRestoreSessionAsync(CancellationToken.None);

        Assert.That(result, Is.EqualTo(RestoreSessionStatus.NoToken));
        Assert.That(api.GetAccountCalls, Is.Zero);
        Assert.That(workflow.CurrentSession, Is.Null);
    }

    [Test]
    public async Task Restore_with_invalid_token_clears_saved_token()
    {
        var api = new FakeAuthenticationApi
        {
            GetAccount = (_, _) => throw new AuthenticationException(
                AuthenticationFailure.InvalidToken,
                "Invalid token")
        };
        var store = new FakeTokenStore { Token = "expired-token" };
        var workflow = new AuthenticationWorkflow(api, store);

        var result = await workflow.TryRestoreSessionAsync(CancellationToken.None);

        Assert.That(result, Is.EqualTo(RestoreSessionStatus.InvalidToken));
        Assert.That(store.Token, Is.Null);
        Assert.That(workflow.CurrentSession, Is.Null);
    }

    [Test]
    public async Task Restore_when_service_is_unavailable_preserves_saved_token()
    {
        var api = new FakeAuthenticationApi
        {
            GetAccount = (_, _) => throw new AuthenticationException(
                AuthenticationFailure.NetworkUnavailable,
                "Network unavailable")
        };
        var store = new FakeTokenStore { Token = "still-possibly-valid" };
        var workflow = new AuthenticationWorkflow(api, store);

        Assert.ThrowsAsync<AuthenticationException>(
            async () => await workflow.TryRestoreSessionAsync(CancellationToken.None));

        Assert.That(store.Token, Is.EqualTo("still-possibly-valid"));
        Assert.That(workflow.CurrentSession, Is.Null);
    }

    [Test]
    public async Task Sign_in_saves_token_only_after_profile_load_succeeds()
    {
        var api = new FakeAuthenticationApi
        {
            SignIn = (_, _, _) => Task.FromResult("new-token"),
            GetAccount = (_, _) => Task.FromResult(Account)
        };
        var store = new FakeTokenStore();
        var workflow = new AuthenticationWorkflow(api, store);

        var session = await workflow.SignInAsync(" customer-1 ", "secret", CancellationToken.None);

        Assert.That(session.AccessToken, Is.EqualTo("new-token"));
        Assert.That(api.LastCustomerCode, Is.EqualTo("customer-1"));
        Assert.That(api.LastPassword, Is.EqualTo("secret"));
        Assert.That(store.Token, Is.EqualTo("new-token"));
        Assert.That(workflow.CurrentSession, Is.SameAs(session));
    }

    [Test]
    public async Task Sign_in_network_failure_loading_profile_can_retry_without_signing_in_again()
    {
        var api = new FakeAuthenticationApi
        {
            SignIn = (_, _, _) => Task.FromResult("pending-token"),
            GetAccount = (_, call) => call == 1
                ? throw new AuthenticationException(AuthenticationFailure.NetworkUnavailable, "Offline")
                : Task.FromResult(Account)
        };
        var store = new FakeTokenStore();
        var workflow = new AuthenticationWorkflow(api, store);

        Assert.ThrowsAsync<AuthenticationException>(
            async () => await workflow.SignInAsync("customer-1", "secret", CancellationToken.None));
        Assert.That(workflow.CurrentSession, Is.Null);
        Assert.That(workflow.HasPendingProfileLoad, Is.True);
        Assert.That(store.Token, Is.Null);

        var session = await workflow.RetryProfileLoadAsync(CancellationToken.None);

        Assert.That(api.SignInCalls, Is.EqualTo(1));
        Assert.That(api.GetAccountCalls, Is.EqualTo(2));
        Assert.That(store.Token, Is.EqualTo("pending-token"));
        Assert.That(workflow.CurrentSession, Is.SameAs(session));
    }

    [Test]
    public async Task Sign_out_clears_local_token_even_when_remote_sign_out_fails()
    {
        var api = new FakeAuthenticationApi
        {
            SignIn = (_, _, _) => Task.FromResult("token"),
            GetAccount = (_, _) => Task.FromResult(Account),
            SignOut = (_, _) => throw new AuthenticationException(
                AuthenticationFailure.NetworkUnavailable,
                "Offline")
        };
        var store = new FakeTokenStore();
        var workflow = new AuthenticationWorkflow(api, store);
        await workflow.SignInAsync("customer-1", "secret", CancellationToken.None);

        Assert.ThrowsAsync<AuthenticationException>(
            async () => await workflow.SignOutAsync(CancellationToken.None));

        Assert.That(store.Token, Is.Null);
        Assert.That(workflow.CurrentSession, Is.Null);
    }

    [Test]
    public async Task Invalidate_session_clears_local_state_and_notifies_subscribers()
    {
        var api = new FakeAuthenticationApi
        {
            SignIn = (_, _, _) => Task.FromResult("token"),
            GetAccount = (_, _) => Task.FromResult(Account)
        };
        var store = new FakeTokenStore();
        var workflow = new AuthenticationWorkflow(api, store);
        await workflow.SignInAsync("customer-1", "secret", CancellationToken.None);

        var notified = false;
        workflow.SessionInvalidated += (_, _) => notified = true;
        await workflow.InvalidateSessionAsync(CancellationToken.None);

        Assert.That(store.Token, Is.Null);
        Assert.That(workflow.CurrentSession, Is.Null);
        Assert.That(notified, Is.True);
    }

    [Test]
    public void Sign_in_with_empty_credentials_does_not_call_api()
    {
        var api = new FakeAuthenticationApi();
        var workflow = new AuthenticationWorkflow(api, new FakeTokenStore());

        Assert.ThrowsAsync<ArgumentException>(
            async () => await workflow.SignInAsync(" ", "", CancellationToken.None));

        Assert.That(api.SignInCalls, Is.Zero);
    }

    private sealed class FakeTokenStore : ITokenStore
    {
        public string? Token { get; set; }

        public Task<string?> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Token);

        public Task SaveAsync(string accessToken, CancellationToken cancellationToken)
        {
            Token = accessToken;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            Token = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuthenticationApi : IAuthenticationApi
    {
        public int SignInCalls { get; private set; }

        public int GetAccountCalls { get; private set; }

        public string? LastCustomerCode { get; private set; }

        public string? LastPassword { get; private set; }

        public Func<string, string, CancellationToken, Task<string>> SignIn { get; init; } =
            (_, _, _) => Task.FromResult("token");

        public Func<string, int, Task<AccountProfile>> GetAccount { get; init; } =
            (_, _) => Task.FromResult(Account);

        public Func<string, CancellationToken, Task> SignOut { get; init; } =
            (_, _) => Task.CompletedTask;

        public Task<string> SignInAsync(
            string customerCode,
            string password,
            CancellationToken cancellationToken)
        {
            SignInCalls++;
            LastCustomerCode = customerCode;
            LastPassword = password;
            return SignIn(customerCode, password, cancellationToken);
        }

        public Task<AccountProfile> GetCurrentAccountAsync(
            string accessToken,
            CancellationToken cancellationToken)
        {
            GetAccountCalls++;
            return GetAccount(accessToken, GetAccountCalls);
        }

        public Task SignOutAsync(string accessToken, CancellationToken cancellationToken) =>
            SignOut(accessToken, cancellationToken);
    }
}
