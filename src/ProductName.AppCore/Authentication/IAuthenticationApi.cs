namespace ProductName.AppCore.Authentication;

public interface IAuthenticationApi
{
    Task<string> SignInAsync(string customerCode, string password, CancellationToken cancellationToken);

    Task<AccountProfile> GetCurrentAccountAsync(string accessToken, CancellationToken cancellationToken);

    Task SignOutAsync(string accessToken, CancellationToken cancellationToken);
}
