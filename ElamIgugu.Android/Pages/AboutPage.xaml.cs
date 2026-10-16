namespace ElamIguguLeli.App.Pages;

public partial class AboutPage : ContentPage
{
    public AboutPage() => InitializeComponent();

    private async void OnWebinarsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("webinars");
}
