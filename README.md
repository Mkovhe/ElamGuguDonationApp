# Elam Igugu Leli Donation System

Group 19, IIE, module XADAD7112 (Work-Integrated Learning). A donation platform for the NPO **Elam Igugu Leli**: a website for donors and admins, plus an Android app for donors.

## What is in this repo

| Part | Folder | What it does |
|---|---|---|
| Website | root (`Controllers`, `Views`, `Models`, `Data`, `Migrations`) | ASP.NET Core 8 MVC with Identity. Donors register, submit donations and RSVP to webinars. Admins manage donations and webinars. |
| API | `Api`, `Services`, `Options` | JSON endpoints under `/api` secured with JWT tokens. The Android app uses these. |
| Android app | `ElamIgugu.Android` | .NET MAUI app (Android only) for donors. It talks to the API and never touches the database directly. |

## How it fits together

```
Android app  --HTTPS + JWT-->  API (in the website project)  -->  Azure SQL
Browser      --HTTPS + cookie-> Website (MVC)                 -->  Azure SQL
```

Both clients use the same rules and the same database, so a donation made in the app shows up on the website.

- Donation statuses: `Submitted`, then `Received`.
- Donation references look like `EIL-0001`.
- Money donations are whole rand and need a PDF proof of payment (max 2 MB).
- Admin accounts use the website. The app rejects admin logins.

## API endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/auth/register` | Create a donor account |
| POST | `/api/auth/login` | Sign in, returns a JWT |
| GET | `/api/npo/contact` | NPO contact details, item types, cities |
| GET | `/api/donor/donations` | The signed-in donor's donations |
| POST | `/api/donor/donations` | Submit a donation (multipart) |
| GET | `/api/webinars` | Upcoming webinars |
| POST | `/api/webinars/{id}/rsvp` | RSVP to a webinar |

More detail is in `ANDROID_AND_API.md`.

## Running the website locally

Requirements: Visual Studio 2022 or later (or the .NET 8 SDK) and SQL Server LocalDB, which installs with Visual Studio.

1. Clone the repo and open `ElamIgugu.Web.slnx` in Visual Studio.
2. Press Run (https profile). That is all.

In Development the site uses a built-in test JWT key and the LocalDB database from `appsettings.json`. The database is created and migrated automatically on first start.

Optional: to create an admin account for the website, set these before running:

```
dotnet user-secrets set "Seed:AdminEmail" "<admin email>"
dotnet user-secrets set "Seed:AdminPassword" "<strong password>"
```

In Production (Azure) the test key is NOT used. The site refuses to start unless `Jwt__Key` is set.

## Running on Azure

Set these under **App Service > Environment variables**:

- `Jwt__Key` (32+ characters)
- `DefaultConnection` as a connection string of type **SQLServer**
- Optional: `Seed__AdminEmail` and `Seed__AdminPassword`

## Running the Android app

Requirements: Visual Studio 2026 with the .NET MAUI workload and an Android emulator.

1. Open `ElamIgugu.Android/Services/ApiConfig.cs` and set `BaseUrl` to your website address, ending in `/`.
2. Select an Android emulator in the toolbar and press Run.

## Security notes

- Secrets (JWT key, database password) are never stored in the repo. They live in user-secrets locally and in Azure settings online.
- Passwords need 8+ characters with upper case, lower case and a digit. Accounts lock after repeated failed logins.
- API sign-in and registration are rate limited.

## Team

IIE Group 19. Website by a teammate. API and Android app by Mukovhe Rambau.
