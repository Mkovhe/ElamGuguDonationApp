using ElamIguguLeli.App.Models;
using ElamIguguLeli.App.Services;
using Microsoft.Maui.Controls.Shapes;

namespace ElamIguguLeli.App.Pages;

public partial class DonatePage : ContentPage
{
    private const int MaxProofBytes = 2 * 1024 * 1024;   // 2 MB, same as the server
    private readonly ApiClient _api;
    private ApiNpoContact? _contact;
    private byte[]? _proofBytes;
    private string? _proofName;

    public DonatePage(ApiClient api, SessionService session)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_contact == null) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Busy.IsVisible = Busy.IsRunning = true;
        LoadErrorLabel.IsVisible = RetryButton.IsVisible = false;

        var result = await _api.GetContactAsync();

        Busy.IsVisible = Busy.IsRunning = false;
        if (!result.Ok || result.Data == null)
        {
            LoadErrorLabel.Text = result.Error ?? "Could not load our details.";
            LoadErrorLabel.IsVisible = RetryButton.IsVisible = true;
            return;
        }

        _contact = result.Data;
        BankLabel.Text = $"Bank: {_contact.BankName}\nAccount name: {_contact.BankAccountName}\nAccount number: {_contact.BankAccountNumber}\nReference: your full name";
        ContactLabel.Text = $"Drop-off: {_contact.DropOffAddress}";
        InstagramButton.Text = $"Instagram {_contact.InstagramHandle}";

        PhoneStack.Children.Clear();
        foreach (var phone in _contact.PhoneNumbers)
        {
            var number = phone;
            var button = new Button { Text = $"Call {number}", Style = (Style)Application.Current!.Resources["SecondaryButton"] };
            button.Clicked += async (_, _) => await CallAsync(number);
            PhoneStack.Children.Add(button);
        }

        TypePicker.ItemsSource = _contact.ItemTypes;
        BuildTypeCards();
        CityPicker.ItemsSource = _contact.Cities;
        BuildCityChips();
        FormStack.IsVisible = true;
    }

    private async void OnRetryClicked(object? sender, EventArgs e) => await LoadAsync();

    // One big tappable card per item type (clothes, food, money). Tapping a card selects it in the hidden picker.
    private void BuildTypeCards()
    {
        TypeCards.Children.Clear();
        if (_contact == null) return;

        var res = Application.Current!.Resources;
        for (var i = 0; i < _contact.ItemTypes.Count; i++)
        {
            var index = i;
            var name = _contact.ItemTypes[i];
            var icon = name == _contact.MoneyItemType ? "icon_money.svg"
                : name.Contains("food", StringComparison.OrdinalIgnoreCase) ? "icon_food.svg"
                : "icon_clothes.svg";

            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.Add(new Image { Source = icon, HeightRequest = 52, WidthRequest = 52 }, 0, 0);
            grid.Add(new Label
            {
                Text = name,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)res["BrandDark"],
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            var card = new Border
            {
                Style = (Style)res["CardBorder"],
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
                Content = grid
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => TypePicker.SelectedIndex = index;
            card.GestureRecognizers.Add(tap);
            TypeCards.Children.Add(card);
        }
    }

    // One tappable chip per city; tapping selects it in the hidden picker.
    private void BuildCityChips()
    {
        CityChips.Children.Clear();
        if (_contact == null) return;

        for (var i = 0; i < _contact.Cities.Count; i++)
        {
            var index = i;
            var chip = new Border
            {
                Style = (Style)Application.Current!.Resources["CardBorder"],
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(22) },
                Padding = new Thickness(18, 10),
                Margin = new Thickness(0, 0, 8, 8),
                Content = new Label
                {
                    Text = _contact.Cities[i],
                    FontAttributes = FontAttributes.Bold,
                    TextColor = (Color)Application.Current!.Resources["BrandDark"]
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => CityPicker.SelectedIndex = index;
            chip.GestureRecognizers.Add(tap);
            CityChips.Children.Add(chip);
        }
    }

    private void OnCityChanged(object? sender, EventArgs e)
    {
        for (var i = 0; i < CityChips.Children.Count; i++)
        {
            if (CityChips.Children[i] is not Border chip) continue;
            var on = i == CityPicker.SelectedIndex;
            chip.BackgroundColor = on ? Color.FromArgb("#93D768") : Colors.White;
            chip.Stroke = new SolidColorBrush(on ? Color.FromArgb("#237A3B") : Color.FromArgb("#D6EBBE"));
            chip.StrokeThickness = on ? 3 : 1;
        }
    }

    private void HighlightSelectedCard(int selected)
    {
        for (var i = 0; i < TypeCards.Children.Count; i++)
        {
            if (TypeCards.Children[i] is not Border card) continue;
            var on = i == selected;
            card.BackgroundColor = on ? Color.FromArgb("#E6F1E6") : Colors.White;
            card.Stroke = new SolidColorBrush(on ? Color.FromArgb("#237A3B") : Color.FromArgb("#D6EBBE"));
            card.StrokeThickness = on ? 3 : 1;
        }
    }

    private void OnTypeChanged(object? sender, EventArgs e)
    {
        var index = TypePicker.SelectedIndex;
        HighlightSelectedCard(index);
        var isMoney = IsMoneyIndex(index);
        var isItems = index >= 0 && !isMoney;

        MoneyStack.IsVisible = isMoney;
        ItemsStack.IsVisible = isItems;
        ContactBox.IsVisible = isItems;
        DetailsEntry.IsVisible = index >= 0;
        SaveButton.IsVisible = index >= 0;
        SaveMessageLabel.IsVisible = false;
    }

    private bool IsMoneyIndex(int index) =>
        index >= 0 && _contact != null && index < _contact.ItemTypes.Count
        && _contact.ItemTypes[index] == _contact.MoneyItemType;

    private async void OnChooseProofClicked(object? sender, EventArgs e)
    {
        try
        {
            // The phone opens its Files app and the donor picks the PDF from the bank. Photo upload is not offered.
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose your proof of payment (PDF)",
                FileTypes = FilePickerFileType.Pdf
            });
            if (file == null) return;

            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            if (ms.Length > MaxProofBytes)
            {
                _proofBytes = null;
                _proofName = null;
                ProofLabel.Text = "That file is larger than 2 MB. Please choose a smaller PDF.";
                return;
            }

            _proofBytes = ms.ToArray();
            _proofName = file.FileName;
            ProofLabel.Text = "Selected: " + file.FileName;
        }
        catch (Exception)
        {
            ProofLabel.Text = "Could not open that file. Please try another PDF.";
        }
    }

    private async void OnInstagramClicked(object? sender, EventArgs e)
    {
        if (_contact == null) return;
        try
        {
            await Launcher.Default.OpenAsync("https://instagram.com/" + _contact.InstagramHandle.TrimStart('@'));
        }
        catch (Exception)
        {
            await DisplayAlert("Instagram", $"Find us on Instagram: {_contact.InstagramHandle}", "OK");
        }
    }

    private async Task CallAsync(string number)
    {
        if (PhoneDialer.Default.IsSupported)
        {
            try { PhoneDialer.Default.Open(number); return; }
            catch (Exception) { /* fall through to the alert */ }
        }
        await DisplayAlert("Call us", $"Please call: {number}", "OK");
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var index = TypePicker.SelectedIndex;
        if (index < 0)
        {
            ShowSaveMessage("Please choose what you are donating.", isError: true);
            return;
        }

        int? amount = null;
        string? city = null;
        Stream? proof = null;

        var isMoney = IsMoneyIndex(index);
        if (isMoney)
        {
            if (!int.TryParse(AmountEntry.Text?.Trim(), out var rand) || rand <= 0)
            {
                ShowSaveMessage("Please enter the amount in whole rand.", isError: true);
                return;
            }
            if (_proofBytes == null)
            {
                ShowSaveMessage("Please choose your proof of payment (a PDF of 2 MB or less).", isError: true);
                return;
            }
            amount = rand;
            proof = new MemoryStream(_proofBytes);
        }
        else
        {
            city = CityPicker.SelectedItem as string;
            if (city == null)
            {
                ShowSaveMessage("Please choose a city.", isError: true);
                return;
            }
        }

        SaveButton.IsEnabled = false;
        var result = await _api.CreateIntentAsync(_contact!.ItemTypes[index], amount, city, DetailsEntry.Text?.Trim(), _proofName, proof);
        proof?.Dispose();
        SaveButton.IsEnabled = true;

        if (!result.Ok)
        {
            if (result.StatusCode == 401)
            {
                await DisplayAlert("Session expired", result.Error, "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }
            ShowSaveMessage(result.Error ?? "We could not save that. Please try again.", isError: true);
            return;
        }

        var reference = result.Data?.Reference;
        await DisplayAlert("Saved as Submitted",
            isMoney
                ? $"Thank you! Your donation {reference} is saved. An admin will mark it Received once your payment is confirmed."
                : $"Thank you! Your donation {reference} is saved. Please contact us to hand the items over.",
            "OK");

        // Reset the form and go back to the home screen, which reloads My donations.
        AmountEntry.Text = DetailsEntry.Text = string.Empty;
        _proofBytes = null;
        _proofName = null;
        ProofLabel.Text = string.Empty;
        TypePicker.SelectedIndex = -1;
        await Shell.Current.GoToAsync("..");
    }

    private void ShowSaveMessage(string message, bool isError)
    {
        SaveMessageLabel.Text = message;
        SaveMessageLabel.TextColor = isError
            ? (Color)Application.Current!.Resources["ErrorText"]
            : (Color)Application.Current!.Resources["Brand"];
        SaveMessageLabel.IsVisible = true;
    }
}
