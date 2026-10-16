using ProductName.AppCore.Model;

namespace ProductName.AppCore.Authentication
{
    public sealed record AuthenticationSession
    {
        public string AccessToken { get; init; }
        public AccountProfile? Account { get; init; }
        public DateTimeOffset? ExpiresAtUtc { get; init; }
        public AuthenticationSession(string accessToken, AccountProfile? account, DateTimeOffset? expiresAtUtc = null)
        {
            AccessToken = accessToken;
            Account = account;
            ExpiresAtUtc = expiresAtUtc;
        }
    }
}