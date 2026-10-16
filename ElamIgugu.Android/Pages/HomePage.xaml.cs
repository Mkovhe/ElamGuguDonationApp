using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiClient _api;
    private readonly SessionService _session;

    public HomePage(ApiClient api, SessionService session)
    {
        InitializeComponent();
        _api = api;
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
        var first = (_session.FullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        WelcomeLabel.Text = $"Welcome, {first}";
        await LoadDonationsAsync();
    }

    private async Task LoadDonationsAsync()
    {
        Busy.IsVisible = Busy.IsRunning = true;
        StateLabel.IsVisible = RetryButton.IsVisible = false;

        var result = await _api.GetMyIntentsAsync();

        Busy.IsVisible = Busy.IsRunning = false;
        if (!result.Ok || result.Data == null)
        {
            if (result.StatusCode == 401)
            {
                await Shell.Current.GoToAsync("//login");
                return;
            }
            StateLabel.Text = result.Error ?? "Could not load your donations.";
            StateLabel.IsVisible = RetryButton.IsVisible = true;
            BindableLayout.SetItemsSource(DonationsList, null);
            return;
        }

        BindableLayout.SetItemsSource(DonationsList, result.Data);
        if (result.Data.Count == 0)
        {
            StateLabel.Text = "You have not donated yet. Tap Donate / Drop Off to start.";
            StateLabel.IsVisible = true;
        }
    }

    private async void OnRetryClicked(object? sender, EventArgs e) => await LoadDonationsAsync();
    private async void OnDonateClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("donate");
    private async void OnWebinarsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("webinars");

    private async void OnDonateTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("donate");
    private async void OnWebinarsTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("webinars");
    private async void OnAboutTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("about");

    private async void OnSecurityClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("security");

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _session.Clear();
        await Shell.Current.GoToAsync("//login");
    }
}
