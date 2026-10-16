namespace ProductName.AppCore.Authentication;

public sealed record SignInResult(string AccessToken, DateTimeOffset? ExpiresAtUtc);