using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

public partial class SecurityPage : ContentPage
{
    private readonly SecurityClient _security;
    private readonly SessionService _session;
    private TwoFactorSetup? _setup;
    private string _codesText = string.Empty;

    public SecurityPage(SecurityClient security, SessionService session)
    {
        InitializeComponent();
        _security = security;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!await _session.TryRestoreAsync())
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }
        await LoadStatusAsync();
    }

    private void ShowOnly(Border? card)
    {
        OffCard.IsVisible = card == OffCard;
        SetupCard.IsVisible = card == SetupCard;
        CodesCard.IsVisible = card == CodesCard;
        OnCard.IsVisible = card == OnCard;
    }

    private void SetBusy(bool busy)
    {
        Busy.IsVisible = Busy.IsRunning = busy;
        if (busy) ErrorLabel.IsVisible = false;
    }

    private void ShowError(string? message)
    {
        ErrorLabel.Text = message ?? "Something went wrong. Please try again.";
        ErrorLabel.IsVisible = true;
    }

    private async Task<bool> HandleExpiredAsync(int status)
    {
        if (status != 401) return false;
        await Shell.Current.GoToAsync("//login");
        return true;
    }

    private async Task LoadStatusAsync()
    {
        SetBusy(true);
        RetryButton.IsVisible = false;
        InfoLabel.IsVisible = false;

        var result = await _security.GetStatusAsync();
        SetBusy(false);

        if (!result.Ok || result.Data == null)
        {
            if (await HandleExpiredAsync(result.StatusCode)) return;
            ShowOnly(null);
            ShowError(result.Error);
            RetryButton.IsVisible = true;
            return;
        }

        if (result.Data.Enabled)
        {
            LeftLabel.Text = $"Recovery codes left: {result.Data.RecoveryCodesLeft}";
            PasswordEntry.Text = string.Empty;
            ShowOnly(OnCard);
        }
        else
        {
            ShowOnly(OffCard);
        }
    }

    private async void OnStartSetupClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        var result = await _security.StartSetupAsync();
        SetBusy(false);

        if (!result.Ok || result.Data == null)
        {
            if (await HandleExpiredAsync(result.StatusCode)) return;
            ShowError(result.Error);
            return;
        }

        _setup = result.Data;
        KeyLabel.Text = _setup.SharedKey;
        CodeEntry.Text = string.Empty;
        InfoLabel.IsVisible = false;
        ShowOnly(SetupCard);
    }

    private async void OnOpenAuthenticatorClicked(object? sender, EventArgs e)
    {
        if (_setup == null) return;
        try
        {
            // Authenticator apps on the same phone can open otpauth:// links directly
            await Launcher.Default.OpenAsync(new Uri(_setup.AuthenticatorUri));
        }
        catch (Exception)
        {
            ShowError("Could not open an authenticator app. Install one, then use the setup key below.");
        }
    }

    private async void OnCopyKeyClicked(object? sender, EventArgs e)
    {
        if (_setup == null) return;
        await Clipboard.Default.SetTextAsync(_setup.SharedKey.Replace(" ", string.Empty));
        InfoLabel.Text = "Key copied.";
        InfoLabel.IsVisible = true;
    }

    private async void OnEnableClicked(object? sender, EventArgs e)
    {
        var code = (CodeEntry.Text ?? string.Empty).Trim();
        if (code.Length < 6)
        {
            ShowError("Enter the 6-digit code shown in your authenticator app.");
            return;
        }

        SetBusy(true);
        var result = await _security.EnableAsync(code);
        SetBusy(false);

        if (!result.Ok || result.Data == null)
        {
            if (await HandleExpiredAsync(result.StatusCode)) return;
            ShowError(result.Error);
            return;
        }

        _codesText = string.Join(Environment.NewLine, result.Data.RecoveryCodes);
        CodesLabel.Text = _codesText;
        ShowOnly(CodesCard);
    }

    private void OnCancelSetupClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        InfoLabel.IsVisible = false;
        ShowOnly(OffCard);
    }

    private async void OnCopyCodesClicked(object? sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(_codesText);
        InfoLabel.Text = "Codes copied. Paste them somewhere safe.";
        InfoLabel.IsVisible = true;
    }

    private async void OnCodesDoneClicked(object? sender, EventArgs e)
    {
        _codesText = string.Empty;
        CodesLabel.Text = string.Empty;
        await LoadStatusAsync();
    }

    private async void OnDisableClicked(object? sender, EventArgs e)
    {
        var password = PasswordEntry.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Enter your password to switch this off.");
            return;
        }

        SetBusy(true);
        var result = await _security.DisableAsync(password);
        SetBusy(false);

        if (!result.Ok)
        {
            if (await HandleExpiredAsync(result.StatusCode)) return;
            ShowError(result.Error);
            return;
        }

        await LoadStatusAsync();
        InfoLabel.Text = "Two-factor sign-in is now off.";
        InfoLabel.IsVisible = true;
    }

    private async void OnRetryClicked(object? sender, EventArgs e) => await LoadStatusAsync();
    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//home");
}
