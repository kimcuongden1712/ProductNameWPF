namespace ProductName.AppCore.Model
{
    public enum RestoreSessionStatus
    {
        NoToken,
        Restored,
        InvalidToken,
        Unavailable
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
