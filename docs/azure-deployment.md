# Deploying RMD to Azure

RMD runs as one ASP.NET Core (Blazor Server) web app plus one SQL database. On Azure that is
**App Service** (Linux or Windows) + **Azure SQL Database**.

## 1. Resources

| Resource | Suggested setting |
|---|---|
| App Service plan | B1 or higher (Free/F1 has no *Always On* and limited WebSockets) |
| Web App | Runtime stack .NET 10 (LTS) |
| Azure SQL Database | Serverless General Purpose is cheap for a single user; allow Azure services through the firewall |

## 2. Web App → Configuration → General settings

These matter for Blazor Server, which keeps a live connection to the browser:

- **Web sockets: On** (required; without it the app falls back to slow long polling or shows a reconnect loop)
- **ARR affinity (session affinity): On** (keeps a browser on the same instance)
- **HTTPS only: On**, minimum TLS 1.2 (the login cookie is HTTPS-only)
- **Always on: On** (avoids a cold start after idle)
- **Health check path:** `/healthz` (returns 200 when the app can reach the database)

## 3. Connection string

Web App → Configuration → **Connection strings**, name `RmdDatabase`, type `SQLAzure`:

```
Server=tcp:<server>.database.windows.net,1433;Database=<database>;User ID=<user>;Password=<password>;Encrypt=True;
```

Or, with a managed identity on the Web App (no password stored):

```
Server=tcp:<server>.database.windows.net,1433;Database=<database>;Authentication=Active Directory Default;Encrypt=True;
```

(then run `CREATE USER [<web-app-name>] FROM EXTERNAL PROVIDER; ALTER ROLE db_owner ADD MEMBER [<web-app-name>];` in the database).

The value in `appsettings.json` (localhost) is for development only; the app logs a warning if a
non-development host still uses it.

## 4. Application settings

Web App → Configuration → **Application settings** (nested keys use `__`):

| Setting | Value | Why |
|---|---|---|
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` | Linux App Service: real client IP (rate limiting) and https scheme behind Azure's proxy. Windows App Service handles this itself. |
| `App__PublicBaseUrl` | `https://<app>.azurewebsites.net` | Base of password-reset links. Reset e-mails are not sent if this is missing in Production. |
| `AllowedHosts` | `<app>.azurewebsites.net` (plus any custom domain, `;`-separated) | Rejects requests for other host names. |
| `Email__Username` / `Email__Password` | Gmail address / Gmail **app password** | Password-reset e-mails. Optional: `Email__Host`, `Email__Port`. |
| `SeedAdmin__Email` / `SeedAdmin__Password` | your login | Only used when the database has no user yet. Remove after the first start. |
| `Database__MigrateOnStartup` | `true` | Creates/updates the tables on start. Needed for the first deploy to an empty database; safe to leave on. |
| `Backup__Directory` | Linux: `/home/data/rmd-backups` · Windows: `D:\home\data\rmd-backups` | Backups written before "Tøm database". Must be under `/home`: the app folder is read-only when deployed as a package and is replaced on every deploy. |

Not needed on App Service: `DataProtection__KeysDirectory` (App Service already persists the keys
that encrypt login cookies). Set it to a persistent folder when hosting in Docker or on a plain VM,
otherwise every restart logs you out.

## 5. Publish

From the repository root:

```bash
dotnet publish RMD.GUI/RMD.GUI.csproj -c Release -o publish
```

Then deploy the `publish` folder, for example with the Azure CLI:

```bash
cd publish && zip -r ../rmd.zip . && cd ..
az webapp deploy --resource-group <rg> --name <app> --src-path rmd.zip --type zip
```

(Visual Studio's *Publish* or a GitHub Actions workflow work just as well.)

## 6. Move your existing data

1. Locally: **Innstillinger → Eksport → Last ned JSON**.
2. On Azure: log in, **Innstillinger → Import**, choose the file, check the preview, **Importer**.

## 7. After the first start

- Remove `SeedAdmin__Password` from the application settings.
- Change the password via **Glemt passord** if the seed password was ever shared or committed.
- Swagger is only available in Development; the REST API requires login in every environment.

## Troubleshooting

| Symptom | Check |
|---|---|
| "Attempting to reconnect" loops, blank page after login | Web sockets and ARR affinity are On |
| Login redirects back to the login page | The site is opened over https (HTTPS only On) |
| Rate limit or lockout hits all users at once | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` on Linux |
| No password-reset e-mail | `App__PublicBaseUrl` and the `Email__*` settings; see Log stream |
| `/healthz` returns 503 | Connection string and SQL firewall |
