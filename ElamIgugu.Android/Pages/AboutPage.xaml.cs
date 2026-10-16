using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

public partial class AboutPage : ContentPage
{
    private readonly SessionService _session;

    public AboutPage(SessionService session)
    {
        InitializeComponent();
        _session = session;
    }

    // Tapping clothes, food, money or a step: signed-in donors go to their home screen, others choose to sign up or sign in.
    private async void OnDonateTapped(object? sender, TappedEventArgs e)
    {
        if (_session.IsLoggedIn || await _session.TryRestoreAsync())
        {
            await Shell.Current.GoToAsync("//home");
            return;
        }

        var choice = await DisplayActionSheet("Sign up or sign in to donate", "Cancel", null, "Sign up", "Sign in");
        if (choice == "Sign up")
            await Shell.Current.GoToAsync("register");
        else if (choice == "Sign in")
            await Shell.Current.GoToAsync("//login");
    }

    private async void OnWebinarsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("webinars");
}
