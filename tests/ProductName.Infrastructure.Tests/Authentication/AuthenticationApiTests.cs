using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ProductName.AppCore.Authentication;
using ProductName.Infrastructure.Authentication;

namespace ProductName.Infrastructure.Tests.Authentication;

public sealed class AuthenticationApiTests
{
    [Test]
    public async Task Sign_in_posts_expected_fields_and_reads_access_token()
    {
        HttpMethod? sentMethod = null;
        string? sentBody = null;
        var handler = new StubHandler(async (request, cancellationToken) =>
        {
            sentMethod = request.Method;
            sentBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(HttpStatusCode.OK, """{"access_token":"token-123"}""");
        });
        using var client = CreateClient(handler);
        var api = new AuthenticationApi(client, CreateOptions());

        var token = await api.SignInAsync("customer-7", "secret", CancellationToken.None);

        Assert.That(sentMethod, Is.EqualTo(HttpMethod.Post));
        using var body = JsonDocument.Parse(sentBody!);
        Assert.That(body.RootElement.GetProperty("customerCode").GetString(), Is.EqualTo("customer-7"));
        Assert.That(body.RootElement.GetProperty("password").GetString(), Is.EqualTo("secret"));
        Assert.That(token, Is.EqualTo("token-123"));
    }

    [Test]
    public void Sign_in_response_without_token_is_rejected()
    {
        var handler = new StubHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, """{"access_token":""}""")));
        using var client = CreateClient(handler);
        var api = new AuthenticationApi(client, CreateOptions());

        var exception = Assert.ThrowsAsync<AuthenticationException>(
            async () => await api.SignInAsync("customer-7", "secret", CancellationToken.None));

        Assert.That(exception!.Failure, Is.EqualTo(AuthenticationFailure.InvalidResponse));
    }

    [Test]
    public async Task Get_me_sends_bearer_token_and_maps_profile_and_branches()
    {
        AuthenticationHeaderValue? authorization = null;
        var handler = new StubHandler((request, _) =>
        {
            authorization = request.Headers.Authorization;
            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "operator_id": "operator-4",
                  "display_name": "A User",
                  "company_name": "A Company",
                  "branches": [{"id": "branch-1", "name": "North"}]
                }
                """));
        });
        using var client = CreateClient(handler);
        var api = new AuthenticationApi(client, CreateOptions());

        var account = await api.GetCurrentAccountAsync("session-token", CancellationToken.None);

        Assert.That(authorization?.Scheme, Is.EqualTo("Bearer"));
        Assert.That(authorization?.Parameter, Is.EqualTo("session-token"));
        Assert.That(account.OperatorId, Is.EqualTo("operator-4"));
        Assert.That(account.CompanyName, Is.EqualTo("A Company"));
        Assert.That(account.Branches, Has.Count.EqualTo(1));
        Assert.That(account.Branches[0].Name, Is.EqualTo("North"));
    }

    [Test]
    public void Unauthorized_me_response_is_reported_as_invalid_token()
    {
        var handler = new StubHandler((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.Unauthorized, """{"message":"expired"}""")));
        using var client = CreateClient(handler);
        var api = new AuthenticationApi(client, CreateOptions());

        var exception = Assert.ThrowsAsync<AuthenticationException>(
            async () => await api.GetCurrentAccountAsync("expired", CancellationToken.None));

        Assert.That(exception!.Failure, Is.EqualTo(AuthenticationFailure.InvalidToken));
    }

    [Test]
    public async Task Authenticated_client_clears_session_after_unauthorized_response()
    {
        var tokenStore = new FakeTokenStore();
        var authApi = new FakeAuthenticationApi();
        var workflow = new AuthenticationWorkflow(authApi, tokenStore);
        await workflow.SignInAsync("customer-1", "secret", CancellationToken.None);

        var sentAuthorization = (AuthenticationHeaderValue?)null;
        var innerHandler = new StubHandler((request, _) =>
        {
            sentAuthorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        var sessionHandler = new AuthenticatedSessionHandler(workflow)
        {
            InnerHandler = innerHandler
        };
        using var client = new HttpClient(sessionHandler);
        var invalidated = false;
        workflow.SessionInvalidated += (_, _) => invalidated = true;

        var exception = Assert.ThrowsAsync<AuthenticationException>(
            async () => await client.GetAsync("https://api.example.invalid/protected"));

        Assert.That(exception!.Failure, Is.EqualTo(AuthenticationFailure.InvalidToken));
        Assert.That(sentAuthorization?.Scheme, Is.EqualTo("Bearer"));
        Assert.That(sentAuthorization?.Parameter, Is.EqualTo("test-token"));
        Assert.That(workflow.CurrentSession, Is.Null);
        Assert.That(tokenStore.Token, Is.Null);
        Assert.That(invalidated, Is.True);
    }

    private static ApiOptions CreateOptions() => new()
    {
        BaseUrl = "https://api.example.invalid/",
        SignInPath = "/signin",
        CurrentAccountPath = "/me"
    };

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://api.example.invalid/") };

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }

    private sealed class FakeTokenStore : ITokenStore
    {
        public string? Token { get; private set; }

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
        public Task<string> SignInAsync(
            string customerCode,
            string password,
            CancellationToken cancellationToken) =>
            Task.FromResult("test-token");

        public Task<AccountProfile> GetCurrentAccountAsync(
            string accessToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AccountProfile("op-1", "Operator", "Company", []));

        public Task SignOutAsync(string accessToken, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
