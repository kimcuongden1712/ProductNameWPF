namespace ProductName.AppCore.Model
{
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
}
