using System.Net;
using System.Net.Http.Headers;
using ProductName.AppCore.Authentication;

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
