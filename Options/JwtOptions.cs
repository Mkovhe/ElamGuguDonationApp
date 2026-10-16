namespace ElamIgugu.Web.Options;

/// <summary>JWT settings for the Android app's API. The Key must come from user-secrets or an Azure setting, never from git.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ElamIgugu";
    public string Audience { get; set; } = "ElamIguguApp";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
