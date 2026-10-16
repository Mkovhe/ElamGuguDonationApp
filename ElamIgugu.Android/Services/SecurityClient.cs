using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ElamIguguLeli.App.Services;

public class TwoFactorStatus
{
    public bool Enabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
}

public class TwoFactorSetup
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
}

public class RecoveryCodesResult
{
    public List<string> RecoveryCodes { get; set; } = new();
}

public class SecurityMessage
{
    public string? Message { get; set; }
}

/// <summary>Calls the API's /api/security endpoints (two-factor on/off). Never throws for network or server problems.</summary>
public class SecurityClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly SessionService _session;

    public SecurityClient(SessionService session)
    {
        _session = session;
        _http = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl),
            Timeout = TimeSpan.FromSeconds(40)
        };
    }

    public Task<ApiResult<TwoFactorStatus>> GetStatusAsync() =>
        SendAsync<TwoFactorStatus>(HttpMethod.Get, "api/security/two-factor");

    public Task<ApiResult<TwoFactorSetup>> StartSetupAsync() =>
        SendAsync<TwoFactorSetup>(HttpMethod.Post, "api/security/two-factor/setup");

    public Task<ApiResult<RecoveryCodesResult>> EnableAsync(string code) =>
        SendAsync<RecoveryCodesResult>(HttpMethod.Post, "api/security/two-factor/enable", new { code });

    public Task<ApiResult<SecurityMessage>> DisableAsync(string password) =>
        SendAsync<SecurityMessage>(HttpMethod.Post, "api/security/two-factor/disable", new { password });

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body != null)
                request.Content = JsonContent.Create(body, body.GetType(), null, Json);
            if (_session.Token is { } token)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(Json);
                return data != null
                    ? ApiResult<T>.Success(data)
                    : ApiResult<T>.Fail("The server sent an empty reply. Please try again.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _session.Clear();
                return ApiResult<T>.Fail("Your session has expired. Please sign in again.", 401);
            }

            return ApiResult<T>.Fail(await ReadErrorAsync(response), (int)response.StatusCode);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Fail("The server took too long to respond. Check your internet connection and try again.");
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Fail("Could not reach the server. Check your internet connection and try again.");
        }
        catch (JsonException)
        {
            return ApiResult<T>.Fail("The server sent an unexpected reply. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString()!;
        }
        catch (Exception)
        {
            // fall through to the generic message
        }

        return response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
            ? "Too many attempts. Please wait a minute and try again."
            : "Something went wrong. Please try again.";
    }
}
