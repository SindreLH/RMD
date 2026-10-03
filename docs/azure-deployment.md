# Deploying RMD to Azure

A step-by-step guide for running RMD on **Azure App Service (Linux)** with **Azure SQL Database**,
following Azure's security recommendations for a small, single-user web app.

Commands use the Azure CLI (`az`, version 2.60 or later) in Bash. In PowerShell, set the variables with
`$RG = "rg-rmd"` and use backtick (`` ` ``) instead of `\` for line breaks.

## Contents

1. [Architecture and security principles](#1-architecture-and-security-principles)
2. [Variables and resource group](#2-variables-and-resource-group)
3. [Azure SQL Database (Entra ID only)](#3-azure-sql-database-entra-id-only)
4. [App Service](#4-app-service)
5. [Database user for the app (managed identity)](#5-database-user-for-the-app-managed-identity)
6. [Database schema (migrations)](#6-database-schema-migrations)
7. [Key Vault for secrets](#7-key-vault-for-secrets)
8. [Application settings and connection string](#8-application-settings-and-connection-string)
9. [Restrict database network access](#9-restrict-database-network-access)
10. [Deploy the code](#10-deploy-the-code)
11. [First start and moving your data](#11-first-start-and-moving-your-data)
12. [Custom domain (optional)](#12-custom-domain-optional)
13. [Monitoring, alerts and backups](#13-monitoring-alerts-and-backups)
14. [Optional: an extra sign-in gate](#14-optional-an-extra-sign-in-gate)
15. [Security checklist](#15-security-checklist)
16. [Troubleshooting](#16-troubleshooting)

---

## 1. Architecture and security principles

```
Browser ──HTTPS/WSS──▶ App Service (Linux, .NET 10, managed identity)
                          │  ├─ secrets via Key Vault references
                          │  └─ backups in /home/data
                          └──TLS, Entra token──▶ Azure SQL Database (Entra-only auth)
GitHub Actions ──OIDC (no stored secret)──▶ deploys to App Service
```

| Principle | How |
|---|---|
| **No passwords between Azure services** | The app reaches SQL with its *managed identity*; SQL accepts Entra ID logins only |
| **Secrets out of code and config files** | SMTP and seed passwords live in Key Vault, referenced from App Service settings |
| **Least privilege** | The app's database user can read and write data but not change the schema; migrations run with your own admin login |
| **Encrypted in transit** | HTTPS only, TLS 1.2+, FTP off; SQL connections are encrypted |
| **No long-lived deployment credentials** | GitHub Actions logs in with OIDC (federated credential); basic-auth publishing is disabled |
| **Small attack surface** | Swagger is Development-only; the API requires login; login has lockout and rate limiting; security headers and a CSP are set by the app |
| **Recoverable** | Azure SQL point-in-time restore, plus RMD's own JSON export and pre-clear backups |

**Rough monthly cost** (Norway East, 2026 list prices, check the pricing calculator): App Service B1 ≈ USD 13,
Azure SQL Basic ≈ USD 5 (or the free serverless offer), Key Vault and logs a few cents.

## 2. Variables and resource group

Names for App Service, SQL server and Key Vault must be globally unique; replace `<suffix>`.

```bash
az login
az account set --subscription "<subscription name or id>"

RG=rg-rmd
LOC=norwayeast
PLAN=plan-rmd
APP=rmd-<suffix>                 # becomes https://rmd-<suffix>.azurewebsites.net
SQL=sql-rmd-<suffix>
DB=rmd
KV=kv-rmd-<suffix>

az group create --name $RG --location $LOC
```

## 3. Azure SQL Database (Entra ID only)

Make yourself the server's Entra admin and switch off SQL (password) authentication entirely:

```bash
ME_ID=$(az ad signed-in-user show --query id -o tsv)
ME_UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

az sql server create --resource-group $RG --name $SQL --location $LOC \
  --enable-ad-only-auth \
  --external-admin-principal-type User \
  --external-admin-name "$ME_UPN" \
  --external-admin-sid $ME_ID \
  --minimal-tls-version 1.2
```

Create the database. Pick one:

```bash
# A) Basic tier: always on, about USD 5/month
az sql db create --resource-group $RG --server $SQL --name $DB \
  --service-objective Basic --backup-storage-redundancy Local

# B) Free serverless offer: pauses when idle (the first request after a pause takes ~1 minute)
az sql db create --resource-group $RG --server $SQL --name $DB \
  --edition GeneralPurpose --compute-model Serverless --family Gen5 --capacity 1 \
  --use-free-limit --free-limit-exhaustion-behavior AutoPause \
  --backup-storage-redundancy Local
```

Allow your own IP for now (needed to create the app user and run migrations); it is removed again in step 9:

```bash
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create --resource-group $RG --server $SQL \
  --name admin-temp --start-ip-address $MY_IP --end-ip-address $MY_IP
```

## 4. App Service

```bash
az appservice plan create --resource-group $RG --name $PLAN --is-linux --sku B1

az webapp create --resource-group $RG --plan $PLAN --name $APP --runtime "DOTNETCORE:10.0"

# System-assigned managed identity (used for SQL and Key Vault)
az webapp identity assign --resource-group $RG --name $APP
```

Settings Blazor Server needs, plus transport hardening:

```bash
az webapp update --resource-group $RG --name $APP --https-only true --client-affinity-enabled true

az webapp config set --resource-group $RG --name $APP \
  --web-sockets-enabled true \
  --always-on true \
  --min-tls-version 1.2 \
  --ftps-state Disabled \
  --http20-enabled true \
  --generic-configurations '{"healthCheckPath": "/healthz"}'
```

- **Web sockets** carry Blazor's live connection; without them the app shows "reconnecting" loops.
- **ARR affinity** (client affinity) keeps a browser on the same instance, which Blazor Server requires.
- **Health check** restarts an instance that stops answering `/healthz` (which also checks the database).

Turn off username/password publishing (FTP and Kudu basic auth); deployments use Entra ID instead:

```bash
for p in ftp scm; do
  az resource update --resource-group $RG --name $p --namespace Microsoft.Web \
    --resource-type basicPublishingCredentialsPolicies --parent sites/$APP \
    --set properties.allow=false
done
```

## 5. Database user for the app (managed identity)

Connect to the database as yourself (Entra ID). SQL Server Management Studio, VS Code's MSSQL extension, or the
new Go-based `sqlcmd` (`winget install sqlcmd`; the old ODBC `sqlcmd` uses `-G` instead of `--authentication-method`):

```bash
sqlcmd -S tcp:$SQL.database.windows.net,1433 -d $DB --authentication-method ActiveDirectoryDefault
```

Then create a contained user for the Web App's identity (its name is the Web App name) with data-only rights:

```sql
CREATE USER [rmd-<suffix>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [rmd-<suffix>];
ALTER ROLE db_datawriter ADD MEMBER [rmd-<suffix>];
```

The app can now read and write artists, songs and login data, but cannot create or drop tables.

## 6. Database schema (migrations)

Apply the schema with **your** admin login, not the app's. From the repository root:

```bash
dotnet tool restore

# Idempotent scripts: safe to run again, only apply what's missing
dotnet tool run dotnet-ef migrations script --idempotent --project RMD.Data --startup-project RMD.GUI \
  --context RMDContext -o rmd-schema.sql
dotnet tool run dotnet-ef migrations script --idempotent --project RMD.Data --startup-project RMD.GUI \
  --context AuthDbContext -o rmd-auth-schema.sql

sqlcmd -S tcp:$SQL.database.windows.net,1433 -d $DB --authentication-method ActiveDirectoryDefault -i rmd-schema.sql
sqlcmd -S tcp:$SQL.database.windows.net,1433 -d $DB --authentication-method ActiveDirectoryDefault -i rmd-auth-schema.sql
```

Repeat this before deploying a version that adds a migration.

> Simpler but less strict alternative: give the app user `db_owner` (`ALTER ROLE db_owner ADD MEMBER [rmd-<suffix>];`)
> and set `Database__MigrateOnStartup=true` in step 8. The app then migrates itself on start.

## 7. Key Vault for secrets

```bash
az keyvault create --resource-group $RG --name $KV --location $LOC --enable-rbac-authorization true

KV_ID=$(az keyvault show --name $KV --query id -o tsv)
APP_PRINCIPAL=$(az webapp identity show --resource-group $RG --name $APP --query principalId -o tsv)

# You may manage secrets; the app may only read them
az role assignment create --assignee $ME_ID --role "Key Vault Secrets Officer" --scope $KV_ID
az role assignment create --assignee $APP_PRINCIPAL --role "Key Vault Secrets User" --scope $KV_ID
```

(Role assignments can take a minute to apply.) Store the secrets. Key Vault names can't contain `:` or `_`, so use `-`:

```bash
az keyvault secret set --vault-name $KV --name Email-Password      --value "<gmail app password>"
az keyvault secret set --vault-name $KV --name SeedAdmin-Password  --value "<a long, unique password>"
```

Use a **Gmail app password** (Google account → Security → App passwords), never your real Gmail password.
Azure blocks outbound SMTP on port 25 only; RMD uses 587, which works.

## 8. Application settings and connection string

Connection string: passwordless, via the managed identity:

```bash
az webapp config connection-string set --resource-group $RG --name $APP --connection-string-type SQLAzure \
  --settings RmdDatabase="Server=tcp:$SQL.database.windows.net,1433;Database=$DB;Authentication=Active Directory Default;Encrypt=True;"
```

App settings (`__` separates nested keys; `@Microsoft.KeyVault(...)` values are resolved by App Service):

```bash
az webapp config appsettings set --resource-group $RG --name $APP --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  App__PublicBaseUrl="https://$APP.azurewebsites.net" \
  AllowedHosts="$APP.azurewebsites.net" \
  Backup__Directory=/home/data/rmd-backups \
  Database__MigrateOnStartup=false \
  Email__Username="<you>@gmail.com" \
  Email__Password="@Microsoft.KeyVault(VaultName=$KV;SecretName=Email-Password)" \
  SeedAdmin__Email="<you>@gmail.com" \
  SeedAdmin__Password="@Microsoft.KeyVault(VaultName=$KV;SecretName=SeedAdmin-Password)"
```

| Setting | Why |
|---|---|
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Behind Azure's front end: real client IP for rate limiting and lockout, correct `https` scheme |
| `App__PublicBaseUrl` | Base of password-reset links; never taken from the request's Host header |
| `AllowedHosts` | Rejects requests for other host names (add a custom domain with `;`) |
| `Backup__Directory` | "Tøm database" backups must live under `/home`; the app folder is read-only and replaced on every deploy |
| `SeedAdmin__*` | Creates the login user on the first start only (the database has no user yet) |

Not needed on App Service: `DataProtection__KeysDirectory`. App Service already persists the keys that
protect login cookies, in `/home`, shared across instances.

In the portal, *Environment variables* shows a green tick next to each Key Vault reference once it resolves.

## 9. Restrict database network access

Remove your temporary rule and only let the Web App in:

```bash
az sql server firewall-rule delete --resource-group $RG --server $SQL --name admin-temp

for ip in $(az webapp show --resource-group $RG --name $APP --query possibleOutboundIpAddresses -o tsv | tr ',' ' '); do
  az sql server firewall-rule create --resource-group $RG --server $SQL --name "app-${ip//./-}" \
    --start-ip-address $ip --end-ip-address $ip
done
```

Avoid the "Allow Azure services and resources to access this server" switch: it admits traffic from every
Azure customer, relying only on authentication. When you need database access yourself (migrations), add
your IP again temporarily as in step 3.

> Stricter option: VNet integration for the Web App plus a **private endpoint** for SQL, and public network access
> on the SQL server disabled. Adds about USD 7/month for the private endpoint.

## 10. Deploy the code

### Recommended: GitHub Actions with OIDC (no stored secrets)

One-time setup: an Entra app that GitHub may sign in as, limited to this Web App:

```bash
SUB_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
GH_APP_ID=$(az ad app create --display-name rmd-github-deploy --query appId -o tsv)
az ad sp create --id $GH_APP_ID

az ad app federated-credential create --id $GH_APP_ID --parameters '{
  "name": "rmd-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:SindreLH/RMD:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

az role assignment create --assignee $GH_APP_ID --role "Website Contributor" \
  --scope $(az webapp show --resource-group $RG --name $APP --query id -o tsv)

echo "AZURE_CLIENT_ID=$GH_APP_ID  AZURE_TENANT_ID=$TENANT_ID  AZURE_SUBSCRIPTION_ID=$SUB_ID"
```

In GitHub: *Settings → Secrets and variables → Actions → Variables*, add `AZURE_CLIENT_ID`,
`AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` and `AZURE_WEBAPP_NAME` (these are identifiers, not secrets).
Then add `.github/workflows/deploy.yml`:

```yaml
name: Build, test and deploy

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  id-token: write   # OIDC login to Azure
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - run: dotnet test --configuration Release

      - run: dotnet publish RMD.GUI/RMD.GUI.csproj --configuration Release --output publish

      - uses: azure/login@v2
        with:
          client-id: ${{ vars.AZURE_CLIENT_ID }}
          tenant-id: ${{ vars.AZURE_TENANT_ID }}
          subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}

      - uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ vars.AZURE_WEBAPP_NAME }}
          package: publish
```

Every push to `main` now runs the tests and deploys only if they pass. Adding *Required reviewers* to the
`production` environment in GitHub gives you a manual approval step before each deploy.

### Alternative: deploy from your PC

Uses your own Entra login, so it still works with basic auth disabled:

```bash
dotnet publish RMD.GUI/RMD.GUI.csproj --configuration Release --output publish
cd publish && zip -r ../rmd.zip . && cd ..
az webapp deploy --resource-group $RG --name $APP --src-path rmd.zip --type zip
```

## 11. First start and moving your data

1. Open `https://$APP.azurewebsites.net` and log in with the seed e-mail and password from Key Vault.
2. Remove the seed password; it has done its job:
   ```bash
   az webapp config appsettings delete --resource-group $RG --name $APP --setting-names SeedAdmin__Password
   az keyvault secret delete --vault-name $KV --name SeedAdmin-Password
   ```
3. Locally: **Innstillinger → Eksport → Last ned JSON**. On Azure: **Innstillinger → Import**, check the
   preview, then **Importer**.
4. Test **Glemt passord** once so you know reset e-mails arrive.

## 12. Custom domain (optional)

```bash
az webapp config hostname add --resource-group $RG --webapp-name $APP --hostname rmd.example.no
az webapp config ssl create --resource-group $RG --name $APP --hostname rmd.example.no    # free managed certificate
az webapp config ssl bind --resource-group $RG --name $APP --ssl-type SNI \
  --certificate-thumbprint $(az webapp config ssl list --resource-group $RG --query "[?subjectName=='rmd.example.no'].thumbprint" -o tsv)
```

Create the DNS records the portal shows (a `CNAME` and an `asuid` TXT record) first. Afterwards update
`App__PublicBaseUrl` and `AllowedHosts` to the new name.

## 13. Monitoring, alerts and backups

**Logs.** *App Service logs → Application logging (Filesystem)* and *Log stream* show the app's log output,
including failed logins, lockouts and import or clear operations. For history and alerts, enable
**Application Insights** on the Web App (portal → *Application Insights* → *Turn on*; no code change needed).

**Alerts** worth having:
- *Health check status* below 100 % (the app or database is down)
- *Http 5xx* above 0 over 15 minutes
- A **budget** on the resource group (*Cost Management → Budgets*) with an e-mail at 80 %

**Backups.**
- Azure SQL keeps **point-in-time restore** for 7 days automatically (*SQL database → Restore*).
  Increase it, or add long-term retention, under *Backups → Retention policies*.
- Take an RMD **JSON export** now and then and keep it outside Azure; it's small and can be imported anywhere.
- "Tøm database" always writes a backup to `/home/data/rmd-backups` first; download it from Innstillinger.

**Defender for Cloud** (free tier) lists configuration recommendations for the resource group; check it once
after setting up.

## 14. Optional: an extra sign-in gate

RMD has its own login with lockout and rate limiting. For a personal app you can also put **App Service
Authentication** in front, so nobody but you reaches the login page at all:

1. Web App → *Authentication* → *Add identity provider* → **Microsoft**, *Require authentication*,
   unauthenticated requests: *HTTP 302 redirect*.
2. In *Enterprise applications*, open the new app, set **Assignment required = Yes** and assign only yourself.
3. Exclude the health check path so App Service can still probe it:
   ```bash
   az webapp auth update --resource-group $RG --name $APP --excluded-paths "[\"/healthz\"]"
   ```

You then sign in with your Microsoft account first, then with RMD's own login.

## 15. Security checklist

- [ ] SQL server: Entra-only authentication, TLS 1.2, no "Allow Azure services", firewall limited to the app (or a private endpoint)
- [ ] App's database user has `db_datareader` and `db_datawriter` only; migrations run with your admin login
- [ ] Secrets only in Key Vault (RBAC mode); app identity has *Key Vault Secrets User* only
- [ ] HTTPS only, minimum TLS 1.2, FTP and basic-auth publishing disabled
- [ ] Web sockets on, ARR affinity on, Always on, health check `/healthz`
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (keeps Swagger and detailed errors off)
- [ ] `App__PublicBaseUrl` and `AllowedHosts` set to your real host name
- [ ] Deployment via GitHub OIDC, scoped to the Web App only; no publish profile or password stored anywhere
- [ ] Seed password removed after the first start; your password is long and unique, and not the one in old git history
- [ ] Alerts for health and 5xx, a budget alert, and a recent JSON export kept somewhere safe

## 16. Troubleshooting

| Symptom | Check |
|---|---|
| "Attempting to reconnect" loops, blank page after login | Web sockets and ARR affinity are on |
| Login sends you back to the login page | The site is opened over `https` |
| HTTP 500.30 at start-up, or `/healthz` returns 503 | Connection string, SQL firewall, and that the app's database user exists (step 5); see *Log stream* |
| `Login failed for user '<token-identified principal>'` | The `CREATE USER ... FROM EXTERNAL PROVIDER` name must equal the Web App name |
| Key Vault setting shows a red cross | The app identity has *Key Vault Secrets User* on the vault; the secret name matches |
| Lockout or rate limit hits too early | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so each client is counted by its own IP |
| No password-reset e-mail | `App__PublicBaseUrl`, `Email__Username`, the Key Vault reference for `Email__Password`, Gmail app password |
| `Invalid object name 'Artists'` | The schema isn't applied yet (step 6) |
| Slow first request in the morning | Serverless SQL resuming from auto-pause (option B in step 3), or *Always on* is off |
