namespace ProductName.AppCore.Authentication;

public sealed record BranchProfile(string Id, string Name);

public sealed record AccountProfile(
    string OperatorId,
    string DisplayName,
    string CompanyName,
    IReadOnlyList<BranchProfile> Branches);

public sealed record AuthenticationSession(string AccessToken, AccountProfile Account);

public enum RestoreSessionStatus
{
    NoToken,
    Restored,
    InvalidToken
}

public enum AuthenticationFailure
{
    InvalidCredentials,
    InvalidToken,
    NetworkUnavailable,
    ServiceUnavailable,
    InvalidResponse,
    LocalStorage
}

public sealed class AuthenticationException : Exception
{
    public AuthenticationException(AuthenticationFailure failure, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public AuthenticationFailure Failure { get; }
}
