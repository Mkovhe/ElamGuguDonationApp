using ElamIguguLeli.App.Pages;

namespace ElamIguguLeli.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("donate", typeof(DonatePage));
        Routing.RegisterRoute("about", typeof(AboutPage));
        Routing.RegisterRoute("webinars", typeof(WebinarsPage));
        Routing.RegisterRoute("rsvp", typeof(RsvpPage));
        Routing.RegisterRoute("security", typeof(SecurityPage));
    }
}
