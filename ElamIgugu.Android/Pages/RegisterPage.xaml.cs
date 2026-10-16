using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

public partial class RegisterPage : ContentPage
{
    private static readonly Regex PhoneRegex = new(@"^(\+27|0)[1-9]\d{8}$");
    private readonly ApiClient _api;
    private readonly SessionService _session;

    public RegisterPage(ApiClient api, SessionService session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var phone = new string((PhoneEntry.Text ?? string.Empty).Where(c => char.IsDigit(c) || c == '+').ToArray());
        var password = PasswordEntry.Text ?? string.Empty;

        if (name.Length < 2) { ShowError("Please enter your full name."); return; }
        if (!new EmailAddressAttribute().IsValid(email)) { ShowError("Please enter a valid email address."); return; }
        if (!PhoneRegex.IsMatch(phone)) { ShowError("Enter a valid South African phone number, e.g. 082 123 4567."); return; }
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
        { ShowError("Your password needs 8+ characters with an uppercase letter, a lowercase letter and a number."); return; }
        if (password != ConfirmEntry.Text) { ShowError("The passwords do not match."); return; }

        if (!ConsentBox.IsChecked) { ShowError("Please agree to the privacy (POPIA) notice to create an account."); return; }

        SetBusy(true);
        var result = await _api.RegisterAsync(name, email, phone, password, ConsentBox.IsChecked);
        SetBusy(false);

        if (!result.Ok || result.Data == null)
        {
            ShowError(result.Error ?? "Could not create your account. Please try again.");
            return;
        }

        await _session.SaveAsync(result.Data);
        await Shell.Current.GoToAsync("//home");
    }

    private async void OnSignInClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//login");

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        Busy.IsVisible = busy;
        Busy.IsRunning = busy;
        RegisterButton.IsEnabled = !busy;
        if (busy) ErrorLabel.IsVisible = false;
    }
}
