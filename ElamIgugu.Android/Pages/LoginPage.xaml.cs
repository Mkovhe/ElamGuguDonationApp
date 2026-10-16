using System.ComponentModel.DataAnnotations;
using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly SessionService _session;

    public LoginPage(ApiClient api, SessionService session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (await _session.TryRestoreAsync())
            await Shell.Current.GoToAsync("//home");
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (!new EmailAddressAttribute().IsValid(email))
        {
            ShowError("Please enter a valid email address.");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter your password.");
            return;
        }

        SetBusy(true);
        var result = await _api.LoginAsync(email, password, CodeEntry.IsVisible ? CodeEntry.Text : null);
        SetBusy(false);

        if (!result.Ok || result.Data == null)
        {
            ShowError(result.Error ?? "Could not sign in. Please try again.");
            return;
        }

        if (result.Data.RequiresTwoFactor)
        {
            CodeEntry.IsVisible = true;
            ShowError("Enter the 6-digit code from your authenticator app, then tap Sign in again.");
            return;
        }

        // Admins work on the website (donors list, review and Verify); this app is for donors.
        if (result.Data.Roles.Contains("Admin"))
        {
            ShowError("Administrator accounts use the website. Please sign in there.");
            return;
        }

        await _session.SaveAsync(result.Data);
        PasswordEntry.Text = string.Empty;
        CodeEntry.Text = string.Empty;
        CodeEntry.IsVisible = false;
        ErrorLabel.IsVisible = false;
        await Shell.Current.GoToAsync("//home");
    }

    private async void OnRegisterClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("register");
    private async void OnWebinarsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("webinars");

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        Busy.IsVisible = busy;
        Busy.IsRunning = busy;
        LoginButton.IsEnabled = !busy;
        if (busy) ErrorLabel.IsVisible = false;
    }
}
