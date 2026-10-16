using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using ProductName.AppCore.Model;
using ProductName.Domain;
using ProductName.Infrastructure.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductName.Infrastructure.Authentication;

public sealed class AuthenticationApi(ExecuteHttpClient httpClient, ApiOptions options) : IAuthenticationApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SignInResult> SignInAsync(string customerCode, string password, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage
        {
            RequestUri = new Uri(options.BaseUrl + options.SignInPath),
            Content = JsonContent.Create(new SignInRequest(customerCode, password), options: JsonOptions)
        };

        ApiResultModel response = await httpClient.GetAnonymousAsync(request, HttpMethod.Post, cancellationToken);
        if (response.IsError)
        {
            var failure = response.Error?.Kind switch
            {
                ApiErrorKind.Network or ApiErrorKind.Timeout
                    => AuthenticationFailure.NetworkUnavailable,

                ApiErrorKind.Validation
                    => AuthenticationFailure.InvalidCredentials,

                _ when response.StatusCode == HttpStatusCode.Unauthorized
                    => AuthenticationFailure.InvalidCredentials,

                _ => AuthenticationFailure.ServiceUnavailable
            };

            throw new AuthenticationException(
                failure,
                response.Error?.Message
                    ?? $"Sign-in failed (HTTP {(int)response.StatusCode}).");
        }

        SignInResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<SignInResponse>(
                response.ResponseBody ?? string.Empty,
                JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new AuthenticationException(
                AuthenticationFailure.InvalidResponse,
                "The sign-in response had an unexpected format.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(result?.AccessToken))
        {
            throw new AuthenticationException(
                AuthenticationFailure.InvalidResponse,
                "The sign-in response did not include a valid access token.");
        }
        return new SignInResult(result.AccessToken, result.ExpiresAtUtc);
    }

    public async Task<AccountProfile> GetCurrentAccountAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage
        {
            RequestUri = new Uri(options.BaseUrl + options.CurrentAccountPath),
        };
        ApiResultModel response = await httpClient.SendWithTokenAsync(request, HttpMethod.Get, accessToken, cancellationToken);
        if (response.IsError)
        {
            var failure = response.StatusCode == HttpStatusCode.Unauthorized
                ? AuthenticationFailure.InvalidToken
                : response.Error?.Kind is ApiErrorKind.Network or ApiErrorKind.Timeout
                    ? AuthenticationFailure.NetworkUnavailable
                    : AuthenticationFailure.ServiceUnavailable;
            throw new AuthenticationException(failure, response.Error?.Message ?? "Could not load the account profile.");
        }

        AccountProfile? result;
        try
        {
            result = JsonSerializer.Deserialize<AccountProfile>(response.ResponseBody ?? string.Empty, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new AuthenticationException(AuthenticationFailure.InvalidResponse, "The sign-in response had an unexpected format.", ex);
        }

        return result ?? throw new AuthenticationException(AuthenticationFailure.InvalidResponse, "The account response was empty or invalid.");
    }

    public async Task SignOutAsync(string accessToken, CancellationToken cancellationToken)
    {
        // SampleWPFAPI has no logout/revocation endpoint; local token removal ends this device's session.
        await Task.CompletedTask;
    }

    #region request/response models /api/v1/auth/signin
    private sealed record SignInRequest
    {
        [JsonPropertyName("customerCode")]
        public string CustomerCode { get; init; }

        [JsonPropertyName("password")]
        public string Password { get; init; }

        public SignInRequest(string customerCode, string password) =>
            (CustomerCode, Password) = (customerCode, password);
    }

    private sealed record SignInResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expiresAtUtc")]
        public DateTimeOffset? ExpiresAtUtc { get; init; }

        [JsonPropertyName("tokenType")]
        public string? tokenType { get; init; }

    }
    #endregion
    private sealed class CurrentAccountResponse
    {
        [JsonPropertyName("operatorId")]
        public string? OperatorId { get; init; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; init; }

        [JsonPropertyName("branches")]
        public List<BranchResponse>? Branches { get; init; }
    }

    private sealed class BranchResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
