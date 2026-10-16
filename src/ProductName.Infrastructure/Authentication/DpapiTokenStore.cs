using ProductName.AppCore.Authentication.Interface;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProductName.Infrastructure.Authentication;

public sealed class DpapiTokenStore : ITokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ProductNameWPF.SessionToken.v1");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _tokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProductNameWPF",
        "session.bin");

    private sealed record TokenStore(string AccessToken, DateTimeOffset? ExpiresAtUtc);

    public async Task<(string? AccessToken, DateTimeOffset? ExpiresAtUtc)?> ReadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_tokenPath))
            {
                return null;
            }

            var protectedBytes = await File.ReadAllBytesAsync(_tokenPath, cancellationToken);
            try
            {
                var tokenBytes = ProtectedData.Unprotect(
                    protectedBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser);

                var payload = Encoding.UTF8.GetString(tokenBytes);
                // Read tokens written by older builds, which protected the token string directly.
                if (!payload.TrimStart().StartsWith('{'))
                {
                    return string.IsNullOrWhiteSpace(payload) ? null : (payload, null);
                }

                var token = JsonSerializer.Deserialize<TokenStore>(payload);
                if (token is null)
                {
                    File.Delete(_tokenPath);
                    return null;
                }

                return (token.AccessToken, token.ExpiresAtUtc);
            }
            catch (CryptographicException)
            {
                File.Delete(_tokenPath);
                return null;
            }
            catch (JsonException)
            {
                File.Delete(_tokenPath);
                return null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(string accessToken, DateTimeOffset? expiresAtUtc, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_tokenPath)!;
            Directory.CreateDirectory(directory);

            var token = new TokenStore(accessToken, expiresAtUtc);
            var json = JsonSerializer.Serialize(token);
            var tokenBytes = Encoding.UTF8.GetBytes(json);
            var protectedBytes = ProtectedData.Protect(
                tokenBytes,
                Entropy,
                DataProtectionScope.CurrentUser);
            var temporaryPath = _tokenPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken);
                File.Move(temporaryPath, _tokenPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_tokenPath))
            {
                File.Delete(_tokenPath);
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
