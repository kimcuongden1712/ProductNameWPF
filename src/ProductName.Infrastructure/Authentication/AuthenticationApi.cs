using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProductName.AppCore.Authentication;

namespace ProductName.Infrastructure.Authentication;

public sealed class AuthenticationApi(HttpClient httpClient, ApiOptions options) : IAuthenticationApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> SignInAsync(
        string customerCode,
        string password,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.SignInPath)
        {
            Content = JsonContent.Create(new SignInRequest(customerCode, password), options: JsonOptions)
        };

        using var response = await SendAsync(request, AuthenticationFailure.InvalidCredentials, cancellationToken);
        var result = await ReadJsonAsync<SignInResponse>(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(result.AccessToken))
        {
            throw InvalidResponse("The sign-in response did not include access_token.");
        }

        return result.AccessToken;
    }

    public async Task<AccountProfile> GetCurrentAccountAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, options.CurrentAccountPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await SendAsync(request, AuthenticationFailure.InvalidToken, cancellationToken);
        var result = await ReadJsonAsync<CurrentAccountResponse>(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(result.OperatorId))
        {
            throw InvalidResponse("The /me response did not include operator_id.");
        }

        var branches = (result.Branches ?? [])
            .Where(branch => !string.IsNullOrWhiteSpace(branch.Id))
            .Select(branch => new BranchProfile(branch.Id!, branch.Name ?? string.Empty))
            .ToArray();

        return new AccountProfile(
            result.OperatorId,
            result.DisplayName ?? string.Empty,
            result.CompanyName ?? string.Empty,
            branches);
    }

    public async Task SignOutAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.SignOutPath))
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.SignOutPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await SendAsync(request, AuthenticationFailure.InvalidToken, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        AuthenticationFailure unauthorizedFailure,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuthenticationException(
                AuthenticationFailure.NetworkUnavailable,
                "The server did not respond before the request timed out.");
        }
        catch (HttpRequestException ex)
        {
            throw new AuthenticationException(
                AuthenticationFailure.NetworkUnavailable,
                "The server could not be reached. Check the network connection and try again.",
                ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var failure = response.StatusCode == HttpStatusCode.Unauthorized
            ? unauthorizedFailure
            : (int)response.StatusCode >= 500
                ? AuthenticationFailure.ServiceUnavailable
                : AuthenticationFailure.ServiceUnavailable;

        var message = failure switch
        {
            AuthenticationFailure.InvalidCredentials => "The customer ID or password is incorrect.",
            AuthenticationFailure.InvalidToken => "The saved or current session is no longer valid.",
            _ => $"The server returned an error (HTTP {(int)response.StatusCode})."
        };

        response.Dispose();
        throw new AuthenticationException(failure, message);
    }

    private static async Task<T> ReadJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return value ?? throw InvalidResponse("The server returned an empty response.");
        }
        catch (JsonException ex)
        {
            throw new AuthenticationException(
                AuthenticationFailure.InvalidResponse,
                "The server response had an unexpected format.",
                ex);
        }
    }

    private static AuthenticationException InvalidResponse(string message) =>
        new(AuthenticationFailure.InvalidResponse, message);

    private sealed record SignInRequest(
        [property: JsonPropertyName("customerCode")] string CustomerCode,
        [property: JsonPropertyName("password")] string Password);

    private sealed record SignInResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);

    private sealed class CurrentAccountResponse
    {
        [JsonPropertyName("operator_id")]
        public string? OperatorId { get; init; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("company_name")]
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
