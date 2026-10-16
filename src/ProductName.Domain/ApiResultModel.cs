using System.Net;

namespace ProductName.Domain;

public sealed class ApiResultModel
{
    public HttpStatusCode StatusCode { get; init; }
    public bool IsSuccessStatusCode { get; init; }
    public bool IsError { get; init; }
    public string? ResponseBody { get; init; }
    public ErrorResultModel? Error { get; init; }
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();
}
