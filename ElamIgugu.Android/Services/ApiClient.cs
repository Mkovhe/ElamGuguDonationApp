using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ElamIguguLeli.App.Models;

namespace ElamIguguLeli.App.Services;

/// <summary>Talks to the website's REST API (the /api/... endpoints). Never throws for network/server problems: returns a friendly ApiResult instead.</summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly SessionService _session;

    public ApiClient(SessionService session)
    {
        _session = session;
        _http = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl),
            Timeout = TimeSpan.FromSeconds(40)
        };
    }

    public Task<ApiResult<ApiAuthResponse>> LoginAsync(string email, string password, string? twoFactorCode) =>
        SendAsync<ApiAuthResponse>(HttpMethod.Post, "api/auth/login",
            new { email, password, twoFactorCode });

    public Task<ApiResult<ApiAuthResponse>> RegisterAsync(string fullName, string email, string contactNumber, string password, bool consent) =>
        SendAsync<ApiAuthResponse>(HttpMethod.Post, "api/auth/register",
            new { fullName, contactNumber, email, password, consent });

    public Task<ApiResult<ApiNpoContact>> GetContactAsync() =>
        SendAsync<ApiNpoContact>(HttpMethod.Get, "api/npo/contact");

    public Task<ApiResult<List<ApiWebinar>>> GetWebinarsAsync() =>
        SendAsync<List<ApiWebinar>>(HttpMethod.Get, "api/webinars");

    public Task<ApiResult<MessageResponse>> RsvpAsync(int webinarId, string name, string phoneNumber) =>
        SendAsync<MessageResponse>(HttpMethod.Post, $"api/webinars/{webinarId}/rsvp", new { attendeeName = name, contactNumber = phoneNumber });

    /// <summary>Sends a donation as multipart/form-data so a money donation can include its proof-of-payment PDF.</summary>
    public async Task<ApiResult<ApiIntent>> CreateIntentAsync(
        string itemType, int? amountRand, string? city, string? description, string? proofFileName, Stream? proof)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(itemType), "ItemType");
        if (amountRand.HasValue) form.Add(new StringContent(amountRand.Value.ToString()), "Amount");
        if (!string.IsNullOrWhiteSpace(city)) form.Add(new StringContent(city), "City");
        if (!string.IsNullOrWhiteSpace(description)) form.Add(new StringContent(description), "Description");
        if (proof != null)
        {
            var file = new StreamContent(proof);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(file, "Proof", string.IsNullOrWhiteSpace(proofFileName) ? "proof.pdf" : proofFileName);
        }
        return await SendAsync<ApiIntent>(HttpMethod.Post, "api/donor/donations", content: form, requiresAuth: true);
    }

    public Task<ApiResult<List<ApiIntent>>> GetMyIntentsAsync() =>
        SendAsync<List<ApiIntent>>(HttpMethod.Get, "api/donor/donations", requiresAuth: true);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, bool requiresAuth = false, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (content != null)
                request.Content = content;
            else if (body != null)
                request.Content = JsonContent.Create(body, body.GetType(), null, Json);
            if (requiresAuth && _session.Token is { } token)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(Json);
                return data != null
                    ? ApiResult<T>.Success(data)
                    : ApiResult<T>.Fail("The server sent an empty reply. Please try again.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && requiresAuth)
            {
                _session.Clear();
                return ApiResult<T>.Fail("Your session has expired. Please log in again.", 401);
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

    /// <summary>Turns ASP.NET Core problem-details JSON into one readable sentence.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var sb = new StringBuilder();
                foreach (var field in errors.EnumerateObject())
                    foreach (var message in field.Value.EnumerateArray())
                        sb.Append(message.GetString()).Append(' ');
                if (sb.Length > 0) return sb.ToString().Trim();
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
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
