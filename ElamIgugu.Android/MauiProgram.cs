using ElamIguguLeli.App.Pages;
using ElamIguguLeli.App.Services;
using Microsoft.Extensions.Logging;

namespace ElamIguguLeli.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<ApiClient>();

        builder.Services.AddTransient<LandingPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();
        
        builder.Services.AddTransient<DonatePage>();
        builder.Services.AddTransient<WebinarsPage>();
        builder.Services.AddTransient<RsvpPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
