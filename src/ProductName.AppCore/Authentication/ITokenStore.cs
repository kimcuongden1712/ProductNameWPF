namespace ProductName.AppCore.Authentication;

public interface ITokenStore
{
    Task<string?> ReadAsync(CancellationToken cancellationToken);

    Task SaveAsync(string accessToken, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
