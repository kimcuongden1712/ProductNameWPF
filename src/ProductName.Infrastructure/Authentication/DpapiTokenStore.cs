using System.Security.Cryptography;
using System.Text;
using ProductName.AppCore.Authentication;

namespace ProductName.Infrastructure.Authentication;

public sealed class DpapiTokenStore : ITokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ProductNameWPF.SessionToken.v1");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _tokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProductNameWPF",
        "session.bin");

    public async Task<string?> ReadAsync(CancellationToken cancellationToken)
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
                return Encoding.UTF8.GetString(tokenBytes);
            }
            catch (CryptographicException)
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

    public async Task SaveAsync(string accessToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_tokenPath)!;
            Directory.CreateDirectory(directory);

            var tokenBytes = Encoding.UTF8.GetBytes(accessToken);
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
