using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

public partial class LandingPage : ContentPage
{
    private readonly SessionService _session;

    public LandingPage(SessionService session)
    {
        InitializeComponent();
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // A donor who is still signed in goes straight to their home screen.
        if (await _session.TryRestoreAsync())
            await Shell.Current.GoToAsync("//home");
    }

    private async void OnSignUpClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("register");
    private async void OnSignInClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//login");
    private async void OnWebinarsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("webinars");
}
