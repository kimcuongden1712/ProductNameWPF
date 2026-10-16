using ProductName.AppCore.Model;

namespace ProductName.AppCore.Authentication.Interface;

public interface IAuthenticationApi
{
    Task<SignInResult> SignInAsync(string customerCode, string password, CancellationToken cancellationToken);

    Task<AccountProfile> GetCurrentAccountAsync(string accessToken, CancellationToken cancellationToken);

    Task SignOutAsync(string accessToken, CancellationToken cancellationToken);
}