using System.Text.RegularExpressions;
using ElamIguguLeli.App.Services;

namespace ElamIguguLeli.App.Pages;

[QueryProperty(nameof(WebinarIdText), "id")]
[QueryProperty(nameof(Topic), "topic")]
public partial class RsvpPage : ContentPage
{
    private static readonly Regex PhoneRegex = new(@"^(\+27|0)[1-9]\d{8}$");
    private readonly ApiClient _api;
    private string _webinarIdText = string.Empty;
    private string _topic = string.Empty;

    public RsvpPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    public string WebinarIdText
    {
        get => _webinarIdText;
        set => _webinarIdText = value ?? string.Empty;
    }

    public string Topic
    {
        get => _topic;
        set
        {
            _topic = Uri.UnescapeDataString(value ?? string.Empty);
            TopicLabel.Text = _topic;
        }
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var phone = new string((PhoneEntry.Text ?? string.Empty).Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (!int.TryParse(_webinarIdText, out var webinarId))
        {
            ShowMessage("Something went wrong. Please go back and choose the webinar again.", isError: true);
            return;
        }
        if (name.Length < 2) { ShowMessage("Please enter your name.", isError: true); return; }
        if (!PhoneRegex.IsMatch(phone)) { ShowMessage("Enter a valid South African phone number, e.g. 082 123 4567.", isError: true); return; }

        SubmitButton.IsEnabled = false;
        Busy.IsVisible = Busy.IsRunning = true;
        var result = await _api.RsvpAsync(webinarId, name, phone);
        Busy.IsVisible = Busy.IsRunning = false;
        SubmitButton.IsEnabled = true;

        if (!result.Ok)
        {
            ShowMessage(result.Error ?? "We could not save your registration. Please try again.", isError: true);
            return;
        }

        ShowMessage($"You are registered, {name}! Thank you.", isError: false);
        SubmitButton.IsEnabled = false;
    }

    private void ShowMessage(string message, bool isError)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor = isError
            ? (Color)Application.Current!.Resources["ErrorText"]
            : (Color)Application.Current!.Resources["Brand"];
        MessageLabel.IsVisible = true;
    }
}
