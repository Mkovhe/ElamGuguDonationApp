# Android app + API (added to the website)

## What was added (nothing of the existing website was removed)
- `Api/` – JSON endpoints for the app: `api/auth/register`, `api/auth/login`, `api/npo/contact`,
  `api/donor/donations` (GET + POST, donor only), `api/webinars` (GET) and `api/webinars/{id}/rsvp`.
  They reuse the same Identity users, roles, validation rules and tables as the website.
- `Services/TokenService.cs`, `Options/JwtOptions.cs` – the login token the app sends.
- `Program.cs` – JWT bearer sign-in (the website still uses cookies) and a rate limit on sign-in/sign-up.
- `ElamIgugu.Web.csproj` – JwtBearer package; Android folder excluded from the website build.
- `appsettings.json` – empty `Jwt` section.
- `ElamIgugu.Android/` – the .NET MAUI Android app (open `ElamIgugu.Android.csproj` in Visual Studio 2022).
- No new tables, so **no migration is needed**.

## One-time setup (the website will not start without this)
Local: right-click the web project > Manage User Secrets, add
`{ "Jwt": { "Key": "<random text, 32+ characters>" } }`
Azure: App Service > Environment variables > add `Jwt__Key` with a different long random value.
Never put the key in git.

## Android app
1. Set `ApiConfig.BaseUrl` in `ElamIgugu.Android/Services/ApiConfig.cs` to your deployed https address.
2. Visual Studio 2022 with the ".NET Multi-platform App UI development" workload, pick an Android emulator, run.
