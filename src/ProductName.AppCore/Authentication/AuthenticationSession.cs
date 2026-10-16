using ProductName.AppCore.Model;

namespace ProductName.AppCore.Authentication
{
    public sealed record AuthenticationSession
    {
        public string AccessToken = string.Empty;
        public AccountProfile? Account;
        public AuthenticationSession(string accessToken, AccountProfile? account)
        {
            AccessToken = accessToken;
            Account = account;
        }
    }
}
