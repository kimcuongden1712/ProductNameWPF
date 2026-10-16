using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;
using ProductName.Domain;
using ProductName.Infrastructure.Http;
using System.Net;
using System.Net.Http.Headers;

namespace ProductName.Infrastructure.Tests;

public sealed class ExecuteHttpClientTests
{
    [Test]
    public void AuthenticatedRequest_WithoutSession_IsRejectedBeforeSending()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var state = new AuthenticationSessionState();
        var store = new FakeTokenStore();
        var client = CreateClient(handler, state, store);

        Assert.ThrowsAsync<AuthenticationException>(() => client.GetAuthenticatedAsync(NewRequest()));
        Assert.That(handler.RequestCount, Is.Zero);
    }

    [Test]
    public async Task AuthenticatedRequest_AttachesSessionToken()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.That(request.Headers.Authorization, Is.EqualTo(new AuthenticationHeaderValue("Bearer", "valid-token")));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var state = new AuthenticationSessionState();
        state.Set(CreateSession("valid-token", DateTimeOffset.UtcNow.AddMinutes(5)));
        var client = CreateClient(handler, state, new FakeTokenStore());

        var result = await client.GetAuthenticatedAsync(NewRequest());

        Assert.That(result.IsError, Is.False);
        Assert.That(handler.RequestCount, Is.EqualTo(1));
    }

    [Test]
    public void ExpiredSession_IsClearedAndRequestIsNotSent()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var state = new AuthenticationSessionState();
        state.Set(CreateSession("expired-token", DateTimeOffset.UtcNow.AddMinutes(-1)));
        var store = new FakeTokenStore("expired-token");
        var client = CreateClient(handler, state, store);

        Assert.ThrowsAsync<AuthenticationException>(() => client.GetAuthenticatedAsync(NewRequest()));

        Assert.That(handler.RequestCount, Is.Zero);
        Assert.That(state.CurrentSession, Is.Null);
        Assert.That(store.Token, Is.Null);
    }

    [Test]
    public async Task UnauthorizedResponse_InvalidatesSessionAndStoredToken()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var state = new AuthenticationSessionState();
        state.Set(CreateSession("rejected-token", DateTimeOffset.UtcNow.AddMinutes(5)));
        var store = new FakeTokenStore("rejected-token");
        var client = CreateClient(handler, state, store);

        ApiResultModel result = await client.GetAuthenticatedAsync(NewRequest());

        Assert.That(result.IsError, Is.True);
        Assert.That(state.CurrentSession, Is.Null);
        Assert.That(store.Token, Is.Null);
    }

    private static ExecuteHttpClient CreateClient(HttpMessageHandler handler, IAuthenticationSessionState state, ITokenStore store) =>
        new(new HttpClient(handler), state, store);

    private static HttpRequestMessage NewRequest() => new(HttpMethod.Get, "https://example.test/me");

    private static AuthenticationSession CreateSession(string token, DateTimeOffset expiresAtUtc) =>
        new(token, new AccountProfile("operator", "Operator", "Company", Array.Empty<BranchProfile>()), expiresAtUtc);

    private sealed class FakeTokenStore(string? token = null) : ITokenStore
    {
        public string? Token { get; private set; } = token;
        public Task<(string? AccessToken, DateTimeOffset? ExpiresAtUtc)?> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<(string? AccessToken, DateTimeOffset? ExpiresAtUtc)?>(Token is null ? null : (Token, DateTimeOffset.UtcNow.AddMinutes(5)));
        public Task SaveAsync(string accessToken, DateTimeOffset? expiresAtUtc, CancellationToken cancellationToken) { Token = accessToken; return Task.CompletedTask; }
        public Task ClearAsync(CancellationToken cancellationToken) { Token = null; return Task.CompletedTask; }
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(respond(request));
        }
    }
}
