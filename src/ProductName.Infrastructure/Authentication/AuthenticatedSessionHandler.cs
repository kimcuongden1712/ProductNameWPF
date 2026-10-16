using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;
using System.Net;
using System.Net.Http.Headers;

namespace ProductName.Infrastructure.Authentication;

public sealed class AuthenticatedSessionHandler(IAuthenticationWorkflow authenticationWorkflow)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var session = authenticationWorkflow.CurrentSession;
        if (session is null)
        {
            throw new AuthenticationException(
                AuthenticationFailure.InvalidToken,
                "Sign in is required before calling this endpoint.");
        }

        if (session.ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await authenticationWorkflow.InvalidateSessionAsync(cancellationToken);
            throw new AuthenticationException(AuthenticationFailure.InvalidToken, "Your session expired. Please sign in again.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await authenticationWorkflow.InvalidateSessionAsync(cancellationToken);
            throw new AuthenticationException(
                AuthenticationFailure.InvalidToken,
                "Your session expired. Please sign in again.");
        }

        return response;
    }
}
