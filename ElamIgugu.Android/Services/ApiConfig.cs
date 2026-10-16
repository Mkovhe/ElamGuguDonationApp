namespace ElamIguguLeli.App.Services;

public static class ApiConfig
{
    /// <summary>
    /// Base address of the deployed website (ElamIgugu.Web) (must end with a slash), e.g. https://elam-igugu-leli.azurewebsites.net/
    /// Use the deployed https address. Android emulators cannot reach a laptop's self-signed https://localhost certificate.
    /// </summary>
    public const string BaseUrl = "https://elamiguguweb2026-cuhpgyhscfajf7ek.southafricanorth-01.azurewebsites.net/";
}
