using ProductName.Domain;
using System.Text.Json;

namespace ProductName.Infrastructure.Http
{
    static class ApiResultExtensions
    {
        public static ApiResultModel WithError(this ApiResultModel result, ErrorResultModel error) => new()
        {
            StatusCode = result.StatusCode,
            IsSuccessStatusCode = result.IsSuccessStatusCode,
            IsError = true,
            ResponseBody = result.ResponseBody,
            Error = error,
            FieldErrors = error.Kind == ApiErrorKind.Validation ? ExtractFields(result.ResponseBody) : new Dictionary<string, string[]>()
        };
        private static IReadOnlyDictionary<string, string[]> ExtractFields(string? body)
        {
            try { using var doc = JsonDocument.Parse(body ?? "{}"); if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object) return errors.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ValueKind == JsonValueKind.Array ? x.Value.EnumerateArray().Select(v => v.ToString()).ToArray() : new[] { x.Value.ToString() }); }
            catch { }
            return new Dictionary<string, string[]>();
        }
    }
}
