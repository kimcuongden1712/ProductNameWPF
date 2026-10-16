namespace ProductName.AppCore.Authentication.Interface;

public interface ITokenStore
{
    Task<(string? AccessToken, DateTimeOffset? ExpiresAtUtc)?> ReadAsync(CancellationToken cancellationToken);

    Task SaveAsync(string accessToken, DateTimeOffset? expiresAtUtc, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}