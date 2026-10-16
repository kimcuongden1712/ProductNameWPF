namespace ProductName.Domain;

public enum ApiErrorKind
{
    Http,
    Validation,
    Network,
    Timeout,
    Application
}

public sealed class ErrorResultModel
{
    public ApiErrorKind Kind { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? RawBody { get; init; }
}
