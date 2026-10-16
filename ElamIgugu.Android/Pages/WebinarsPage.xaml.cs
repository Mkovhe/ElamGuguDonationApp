using ElamIguguLeli.App.Models;
using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

public partial class WebinarsPage : ContentPage
{
    private readonly ApiClient _api;

    public WebinarsPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Busy.IsVisible = Busy.IsRunning = true;
        var result = await _api.GetWebinarsAsync();
        Busy.IsVisible = Busy.IsRunning = false;

        if (!result.Ok || result.Data == null)
        {
            WebinarList.ItemsSource = new List<ApiWebinar>();
            await DisplayAlert("Could not load webinars", result.Error ?? "Please try again.", "OK");
            return;
        }
        WebinarList.ItemsSource = result.Data;
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: int id })
        {
            var webinar = (WebinarList.ItemsSource as List<ApiWebinar>)?.FirstOrDefault(w => w.Id == id);
            var topic = Uri.EscapeDataString(webinar?.Topic ?? "this webinar");
            await Shell.Current.GoToAsync($"rsvp?id={id}&topic={topic}");
        }
    }
}
