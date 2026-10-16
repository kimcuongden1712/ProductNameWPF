using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;
using ProductName.Domain;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ProductName.Infrastructure.Http;

public sealed class ExecuteHttpClient(
    HttpClient httpClient,
    IAuthenticationSessionState sessionState,
    ITokenStore tokenStore)
{
    public Task<ApiResultModel> GetAnonymousAsync(HttpRequestMessage request, HttpMethod method, CancellationToken cancellationToken = default)
    {
        request.Method = method;
        return SendAsync(request, null, invalidateOnUnauthorized: false, cancellationToken);
    }

    /// <summary>
    /// Used by session workflows to validate a stored or newly issued token with /me or call API without check ExpiresAtUtc
    /// </summary>
    /// <param name="request">The HTTP request message to send. Must not be null.</param>
    /// <param name="method">The HTTP method to use for the request, such as GET or POST.</param>
    /// <param name="accessToken">The access token to include in the request for authentication. Cannot be null or whitespace.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an ApiResultModel with the response
    /// data.</returns>
    public Task<ApiResultModel> SendWithTokenAsync(HttpRequestMessage request, HttpMethod method, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        request.Method = method;
        return SendAsync(request, accessToken, invalidateOnUnauthorized: false, cancellationToken);
    }

    public Task<ApiResultModel> GetAuthenticatedAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(request, HttpMethod.Get, cancellationToken);

    public Task<ApiResultModel> PostAuthenticatedAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(request, HttpMethod.Post, cancellationToken);

    public Task<ApiResultModel> PutAuthenticatedAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(request, HttpMethod.Put, cancellationToken);

    public Task<ApiResultModel> DeleteAuthenticatedAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(request, HttpMethod.Delete, cancellationToken);

    private async Task<ApiResultModel> SendAuthenticatedAsync(HttpRequestMessage request, HttpMethod method, CancellationToken cancellationToken)
    {
        request.Method = method;
        var session = sessionState.CurrentSession;
        if (session is null)
        {
            throw new AuthenticationException(AuthenticationFailure.InvalidToken, "Sign in is required before calling this endpoint.");
        }

        if (session.ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await InvalidateSessionAsync(cancellationToken);
            throw new AuthenticationException(AuthenticationFailure.InvalidToken, "Your session expired. Please sign in again.");
        }

        return await SendAsync(request, session.AccessToken, invalidateOnUnauthorized: true, cancellationToken);
    }

    private async Task InvalidateSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await tokenStore.ClearAsync(cancellationToken);
        }
        finally
        {
            sessionState.Clear(invalidated: true);
        }
    }

    private async Task<ApiResultModel> SendAsync(HttpRequestMessage request, string? accessToken, bool invalidateOnUnauthorized, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = response.Content is null ? null : await response.Content.ReadAsStringAsync(cancellationToken);
            var result = new ApiResultModel { StatusCode = response.StatusCode, IsSuccessStatusCode = response.IsSuccessStatusCode, ResponseBody = body };
            if (response.StatusCode == HttpStatusCode.Unauthorized && invalidateOnUnauthorized)
            {
                await InvalidateSessionAsync(cancellationToken);
            }
            var normalized = TryNormalizeError(body, (int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                return result.WithError(normalized ?? new ErrorResultModel
                {
                    Kind = response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity
                    ? ApiErrorKind.Validation
                    : ApiErrorKind.Http,
                    Message = $"The server returned an error (HTTP {(int)response.StatusCode}).",
                    RawBody = body
                });
            }
            if (normalized is not null) return result.WithError(normalized);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiResultModel
            {
                IsError = true,
                Error = new ErrorResultModel
                {
                    Kind = ApiErrorKind.Timeout,
                    Message = "The server request timed out."
                }
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResultModel
            {
                IsError = true,
                Error = new ErrorResultModel
                {
                    Kind = ApiErrorKind.Network,
                    Message = "The server could not be reached."
                }
            };
        }
    }

    private static ErrorResultModel? TryNormalizeError(string? body, int status)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var fields = new Dictionary<string, string[]>();
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in errors.EnumerateObject())
                    fields[field.Name] = field.Value.ValueKind == JsonValueKind.Array ? field.Value.EnumerateArray().Select(x => x.ToString()).ToArray() : [field.Value.ToString()];
            }
            var details = fields.SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}")).ToArray();
            string? message = null;
            if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String) message = msg.GetString();
            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String) message = detail.GetString();
            if (details.Length > 0) message = string.IsNullOrWhiteSpace(message) ? string.Join(Environment.NewLine, details) : message + Environment.NewLine + string.Join(Environment.NewLine, details);
            string? code = null;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("message", out var em) && string.IsNullOrWhiteSpace(message)) message = em.GetString();
                    if (error.TryGetProperty("code", out var ec)) code = ec.ToString();
                }
                else if (error.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(message)) message = error.GetString();
            }
            var envelopeFailure = root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False;
            if (!envelopeFailure && fields.Count == 0 && string.IsNullOrWhiteSpace(message)) return null;
            return new ErrorResultModel { Kind = fields.Count > 0 || status is 400 or 422 ? ApiErrorKind.Validation : ApiErrorKind.Application, Message = message ?? "The API reported an error.", RawBody = body, Code = code };
        }
        catch (JsonException) { return null; }
    }
}

