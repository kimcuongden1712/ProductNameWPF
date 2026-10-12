namespace ProductName.Infrastructure.Authentication;

public sealed class ApiOptions
{
    public string BaseUrl { get; init; } = "";

    public string SignInPath { get; init; } = "/signin";

    public string CurrentAccountPath { get; init; } = "/me";

    public string SignOutPath { get; init; } = "";
}
