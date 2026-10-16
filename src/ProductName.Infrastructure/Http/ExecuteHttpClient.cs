using ProductName.Domain;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ProductName.Infrastructure.Http;

public sealed class ExecuteHttpClient(HttpClient httpClient)
{
    #region request don't have access token
    public Task<ApiResultModel> GetDataListAsync(HttpRequestMessage request, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        request.Method = HttpMethod.Get;
        return SendAsync(request, accessToken, cancellationToken);
    }
    public Task<ApiResultModel> PostDataListAsync(HttpRequestMessage request, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        request.Method = HttpMethod.Post;
        return SendAsync(request, accessToken, cancellationToken);
    }
    public Task<ApiResultModel> PutDataListAsync(HttpRequestMessage request, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        request.Method = HttpMethod.Put;
        return SendAsync(request, accessToken, cancellationToken);
    }
    public Task<ApiResultModel> DeleteDataAsync(HttpRequestMessage request, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        request.Method = HttpMethod.Delete;
        return SendAsync(request, string.Empty, cancellationToken);
    }
    #endregion


    private async Task<ApiResultModel> SendAsync(HttpRequestMessage request, string? accessToken = null, CancellationToken cancellationToken = default)
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

