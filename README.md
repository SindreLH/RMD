# Background

One of my favorite hobbies in my spare time is DJing, and I am especially motivated to spread the music featured in my mixing sessions to as many people as possible via YouTube. 
This has resulted in an extensive personal music library and it has become a challenge to keep track of it all.
Rather than creating a simple spread sheet, I have decided to utilize my skills in .NET to create a solution for myself that is both scalable and always available. 

RMD is a personal tool under development designed to help me keep track of all the individual tracks in my collection and most importantly if each track has been featured on my YouTube channel or not, thus ensuring completely fresh mix sessions every time. 

# Features

- **Dashboard:** totals, latest additions, additions per month, genre distribution, wanted tracks and a list of music resources
- **Archive:** searchable, filterable artist and song lists; artist profiles with their tracks and links
- **Register:** artists, and songs with multiple artists and remixers; duplicate detection (same title, artists, remixers and mix type)
- **Settings:** five colour themes, JSON export, JSON import with preview (appends, skips duplicates), and a password-protected "clear database" that saves a backup first
- **Responsive:** sidebar on desktop, drawer menu and card lists on phones
- **Single-user login:** ASP.NET Core Identity with lockout, rate limiting and password reset by e-mail

# Tech stack

.NET 10 · Blazor Server · EF Core 10 · SQL Server / Azure SQL · ASP.NET Core Identity · Bootstrap 5.3 · ApexCharts · xUnit

# Build and test

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Developer/Express or LocalDB) on `localhost` with Windows authentication, or change `ConnectionStrings:RmdDatabase` in `RMD.GUI/appsettings.json`
- The EF Core tool: `dotnet tool install -g dotnet-ef` (or `dotnet tool update -g dotnet-ef`)

## First run

```bash
# Create the database (both schemas: music data and login)
dotnet ef database update --project RMD.Data --startup-project RMD.GUI --context RMDContext
dotnet ef database update --project RMD.Data --startup-project RMD.GUI --context AuthDbContext

# The login user is created on first start from these secrets (never commit them)
dotnet user-secrets set "SeedAdmin:Email" "you@example.com" --project RMD.GUI
dotnet user-secrets set "SeedAdmin:Password" "<a strong password>" --project RMD.GUI

# Optional: password-reset e-mails through Gmail (use an app password)
dotnet user-secrets set "Email:Username" "you@gmail.com" --project RMD.GUI
dotnet user-secrets set "Email:Password" "<gmail app password>" --project RMD.GUI

dotnet run --project RMD.GUI
```

Open https://localhost:7023 and log in. Swagger (Development only) is at `/swagger`; the API requires login.

## Tests

```bash
dotnet test
```

The tests use an in-memory SQLite database, so they need no SQL Server.

## Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:RmdDatabase` | SQL Server connection |
| `SeedAdmin:Email`, `SeedAdmin:Password` | Creates the login user when none exists |
| `Email:Username`, `Email:Password` (`Email:Host`, `Email:Port`) | SMTP for password-reset e-mails |
| `App:PublicBaseUrl` | Base URL in reset links (required outside Development) |
| `Backup:Directory` | Where "Tøm database" writes backups (default `App_Data/backups`) |
| `Database:MigrateOnStartup` | Apply migrations when the app starts |
| `DataProtection:KeysDirectory` | Persist login-cookie keys (Docker/VM hosting) |

# Project structure

| Project | Contents |
|---|---|
| `RMD.Data` | Entities, DTOs, EF Core contexts (`RMDContext`, `AuthDbContext`) and migrations |
| `RMD.Business` | Services: artists, songs, dashboard statistics, import/export, e-mail |
| `RMD.GUI` | Blazor Server app: pages, components, controllers (`/auth`, `/api`), themes and layout CSS |
| `RMD.Tests` | xUnit tests for the services and helpers |

Theme colours live in `RMD.GUI/wwwroot/css/themes.css` (one block per theme, registered in
`Infrastructure/ThemeCatalog.cs`); layout primitives in `layout.css` and shared components in `components.css`.

# Deployment

See [docs/azure-deployment.md](docs/azure-deployment.md) for Azure App Service + Azure SQL.
