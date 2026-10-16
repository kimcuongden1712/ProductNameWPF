namespace ProductName.Infrastructure.Authentication;

public sealed class ApiOptions
{
    public string BaseUrl { get; init; } = "https://localhost:7221/api/v1";

    public string SignInPath { get; init; } = "/auth/signin";

    public string CurrentAccountPath { get; init; } = "/auth/me";

    public string SignOutPath { get; init; } = "/logout";
}
