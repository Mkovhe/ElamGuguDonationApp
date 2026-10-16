using ElamIguguLeli.App.Models;

namespace ElamIguguLeli.App.Services;

/// <summary>Keeps the login token in the device's secure storage (Keychain / Android Keystore / Windows credential locker).</summary>
public class SessionService
{
    private const string TokenKey = "elam_token";
    private const string ExpiryKey = "elam_token_expiry";
    private const string NameKey = "elam_full_name";

    public string? Token { get; private set; }
    public string? FullName { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public async Task SaveAsync(ApiAuthResponse auth)
    {
        Token = auth.Token;
        FullName = auth.FullName;
        try
        {
            await SecureStorage.Default.SetAsync(TokenKey, auth.Token ?? string.Empty);
            await SecureStorage.Default.SetAsync(ExpiryKey, auth.ExpiresAtUtc?.ToString("O") ?? string.Empty);
            await SecureStorage.Default.SetAsync(NameKey, auth.FullName ?? string.Empty);
        }
        catch (Exception)
        {
            // Secure storage can be unavailable on some devices/simulators. The user simply has to log in again next time.
        }
    }

    public async Task<bool> TryRestoreAsync()
    {
        if (IsLoggedIn) return true;
        try
        {
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            var expiry = await SecureStorage.Default.GetAsync(ExpiryKey);
            if (string.IsNullOrEmpty(token) || !DateTime.TryParse(expiry, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt))
                return false;
            if (expiresAt <= DateTime.UtcNow.AddMinutes(1))
            {
                Clear();
                return false;
            }

            Token = token;
            FullName = await SecureStorage.Default.GetAsync(NameKey);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Clear()
    {
        Token = null;
        FullName = null;
        try
        {
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(ExpiryKey);
            SecureStorage.Default.Remove(NameKey);
        }
        catch (Exception)
        {
            // ignore
        }
    }
}
