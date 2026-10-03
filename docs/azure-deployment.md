# Deploying RMD to Azure (portal guide)

A step-by-step guide for running RMD on **Azure App Service (Linux, Free F1)** with **Azure SQL Database
(free offer)**, done almost entirely in the Azure portal (<https://portal.azure.com>) and on github.com.

The only things you need your own PC for: exporting your data from the local RMD, and pulling one
generated file into your local copy. A command-line version of this guide is in git history (commit `a4808f6`).

> Portal menus move around now and then. If a label doesn't match exactly, use the search box at the top of
> the portal or the search field at the top of a resource's left-hand menu.

## Contents

1. [What you'll build](#1-what-youll-build)
2. [Before you start](#2-before-you-start)
3. [Resource group](#3-resource-group)
4. [Azure SQL Database](#4-azure-sql-database)
5. [Web App](#5-web-app)
6. [Web App platform settings](#6-web-app-platform-settings)
7. [Database user for the app](#7-database-user-for-the-app)
8. [Key Vault for secrets](#8-key-vault-for-secrets)
9. [Connection string and app settings](#9-connection-string-and-app-settings)
10. [Lock down database network access](#10-lock-down-database-network-access)
11. [Deploy from GitHub](#11-deploy-from-github)
12. [First start](#12-first-start)
13. [Tighten up after the first start](#13-tighten-up-after-the-first-start)
14. [Move your data](#14-move-your-data)
15. [Monitoring, cost and backups](#15-monitoring-cost-and-backups)
16. [Later: deploying updates](#16-later-deploying-updates)
17. [Optional: an extra sign-in gate](#17-optional-an-extra-sign-in-gate)
18. [Security checklist](#18-security-checklist)
19. [Troubleshooting](#19-troubleshooting)

---

## 1. What you'll build

```
Browser ──HTTPS──▶ Web App (Linux, .NET 10, Free F1, managed identity)
                      │  ├─ secrets via Key Vault references
                      │  └─ "Tøm database" backups in /home/data
                      └──encrypted, Entra token──▶ Azure SQL Database (Entra-only, free offer)
GitHub Actions ──passwordless login (OIDC)──▶ deploys to the Web App on every push to main
```

| Principle | How |
|---|---|
| **No passwords between Azure services** | The app reaches SQL with its *managed identity*; SQL accepts Microsoft Entra logins only |
| **Secrets out of code** | The SMTP and seed passwords live in Key Vault; the app settings only point to them |
| **Least privilege** | After the first start the app's database user can read and write data, but not change tables |
| **Encrypted in transit** | HTTPS only, TLS 1.2+, FTP off |
| **No stored deploy passwords** | GitHub signs in to Azure with a federated identity; basic-auth publishing is off |
| **Recoverable** | Azure SQL point-in-time restore, plus RMD's JSON export and pre-clear backups |

**Cost:** about zero. F1 and the SQL free offer are free; Key Vault costs a few cents a month.

**F1 limits** (fine for ad-hoc personal use): 60 CPU-minutes and about 165 MB outbound traffic per day,
1 GB storage, and the app sleeps after about 20 minutes idle. The first visit after a pause takes a few
seconds, or up to about a minute if the database has paused too. The login background is 2.5 MB, is only
loaded on larger screens, and static files are cached for a week.

## 2. Before you start

- An Azure subscription where you are **Owner** (a personal pay-as-you-go subscription is). Owner is needed
  to assign roles in steps 8 and 11.
- The code you want to run is merged into **`main`** on GitHub (`SindreLH/RMD`). Deployment builds from `main`.
- Write down three names. They must be globally unique, so add a suffix such as your initials and a number:

| What | Example | Becomes |
|---|---|---|
| Web App | `rmd-shl01` | `https://rmd-shl01.azurewebsites.net` |
| SQL server | `sql-rmd-shl01` | `sql-rmd-shl01.database.windows.net` |
| Key Vault | `kv-rmd-shl01` | (max 24 characters) |

The rest of this guide uses these example names. Replace them with yours everywhere.

**Region:** use the same region for everything. *Norway East* is closest; if F1 or the SQL free offer
isn't offered there, use *Sweden Central* or *West Europe*.

## 3. Resource group

A resource group holds everything, so you can see the total cost in one place and delete it all at once.

1. Search for **Resource groups** → **+ Create**.
2. **Resource group:** `rg-rmd`. **Region:** your region.
3. **Review + create** → **Create**.

## 4. Azure SQL Database

1. Search for **SQL databases** → **+ Create**.
2. If a banner says *Want to try Azure SQL Database for free?*, click **Apply offer**. (One free database
   per subscription.)
3. **Basics** tab:
   - **Resource group:** `rg-rmd`
   - **Database name:** `rmd`
   - **Server:** **Create new**
     - **Server name:** `sql-rmd-shl01`
     - **Location:** your region
     - **Authentication method:** **Use Microsoft Entra-only authentication**
     - **Set Microsoft Entra admin:** **Set admin** → select yourself → **Select**
     - **OK**
   - **Free database offer:** when the free limit is reached: **Auto-pause the database until next month**
   - **Backup storage redundancy:** **Locally-redundant** (the free offer may set this for you)
4. **Networking** tab:
   - **Connectivity method:** **Public endpoint**
   - **Allow Azure services and resources to access this server:** **No**
   - **Add current client IP address:** **Yes** (you need this for the query editor in step 7; it's
     removed again in step 13)
5. **Security** tab: leave **Microsoft Defender for SQL** off (it's paid). If **Minimum TLS version** is
   shown, check that it is **1.2**.
6. **Review + create** → **Create**. It takes a few minutes.

> **Why Entra-only?** There is no SQL username and password anywhere that could leak. You sign in with your
> Microsoft account, and the app signs in with its managed identity.

## 5. Web App

1. Search for **App Services** → **+ Create** → **Web App**.
2. **Basics** tab:
   - **Resource group:** `rg-rmd`
   - **Name:** `rmd-shl01` (if offered, turn *Secure unique default hostname* off so the address stays
     `rmd-shl01.azurewebsites.net`)
   - **Publish:** **Code**
   - **Runtime stack:** **.NET 10 (LTS)**
   - **Operating System:** **Linux**
   - **Region:** your region
   - **Linux Plan:** **Create new** → `plan-rmd`
   - **Pricing plan:** **Free F1** (if you don't see it, click *Explore pricing plans* → *Dev/Test* → *F1*)
3. **Database** tab: leave it off. You already made the database, and the wizard would create one with
   password authentication.
4. **Deployment** tab:
   - **Continuous deployment:** **Disable** for now (set up in step 11, once the app is configured)
   - **Basic authentication:** **Disable**
5. **Networking** tab: **Enable public access:** **On**.
6. **Monitoring + secure** tab: **Enable Application Insights:** **No** for now (see step 15).
7. **Review + create** → **Create**.

Then turn on the app's own identity, which it uses to reach SQL and Key Vault:

8. Open the Web App → **Settings → Identity** → **System assigned** tab → **Status: On** → **Save** → **Yes**.

## 6. Web App platform settings

Web App → **Settings → Configuration** → **General settings** tab:

| Setting | Value | Why |
|---|---|---|
| **SCM Basic Auth Publishing Credentials** | Off | No deploy passwords |
| **FTP Basic Auth Publishing Credentials** | Off | No deploy passwords |
| **FTP state** | **Disabled** | FTP is unneeded |
| **HTTP version** | **2.0** | Faster page loads |
| **Web sockets** | **On** | Blazor's live connection uses them. Without them you get "reconnecting" loops |
| **Always on** | Off (not available on F1) | On a paid plan (B1+), turn it on to avoid the wake-up delay |
| **Session affinity** | **On** | Blazor Server needs each browser to stay on the same instance |
| **HTTPS Only** | **On** | Redirects all http to https |
| **Minimum Inbound TLS Version** | **1.2** | |

Click **Save** → **Continue**.

**Health check** (if it's offered on your plan): Web App → **Monitoring → Health check** → **Enable**,
**Path:** `/healthz` → **Save**. `/healthz` also checks the database connection.

## 7. Database user for the app

The app signs in to SQL with its managed identity. Create a database user for it:

1. Open the **SQL database** `rmd` (not the server) → **Query editor (preview)**.
2. Click **Continue as \<you\>** (Microsoft Entra sign-in). If it complains about the firewall, click the
   offered **Allowlist IP** link and try again.
3. Paste this, with **your Web App's name** in the brackets, and click **Run**:

```sql
CREATE USER [rmd-shl01] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [rmd-shl01];
ALTER ROLE db_datawriter ADD MEMBER [rmd-shl01];

-- Temporary: lets the app create its tables on the first start. Removed again in step 13.
ALTER ROLE db_owner ADD MEMBER [rmd-shl01];
```

You should see *Query succeeded*. The user's name must exactly match the Web App's name.

## 8. Key Vault for secrets

### Create the vault

1. Search for **Key vaults** → **+ Create**.
2. **Basics:** resource group `rg-rmd`, name `kv-rmd-shl01`, your region, **Pricing tier: Standard**.
3. **Access configuration:** **Permission model: Azure role-based access control** (the default).
4. **Review + create** → **Create**.

### Give yourself and the app access

Open the vault → **Access control (IAM)** → **+ Add** → **Add role assignment**. Do this twice:

| Role | Assign access to | Member | Why |
|---|---|---|---|
| **Key Vault Secrets Officer** | User, group, or service principal | yourself | Lets you create secrets (being Owner doesn't) |
| **Key Vault Secrets User** | **Managed identity** → *App Service* | `rmd-shl01` | Lets the app read secrets, and nothing more |

For each: pick the role → **Next** → **+ Select members** → pick the member → **Select** →
**Review + assign**. Wait a minute for it to take effect.

### Store the secrets

Vault → **Objects → Secrets** → **+ Generate/Import**, once for each:

| Name | Secret value |
|---|---|
| `SeedAdmin-Password` | The password you'll log in to RMD with. Long and unique, not one used before. At least 8 characters with upper and lower case, a digit and a symbol |
| `Email-Password` | A **Gmail app password** (Google account → *Security* → *App passwords*), never your real Gmail password |

Key Vault names can't contain `:` or `_`, so the names use `-`.

## 9. Connection string and app settings

Web App → **Settings → Environment variables**.

### Connection string

**Connection strings** tab → **+ Add**:

- **Name:** `RmdDatabase`
- **Value:** (change the server name)
  ```
  Server=tcp:sql-rmd-shl01.database.windows.net,1433;Database=rmd;Authentication=Active Directory Default;Encrypt=True;
  ```
- **Type:** **SQLAzure**
- **Apply**, then **Apply** → **Confirm** at the bottom of the page

There's no password in it: *Active Directory Default* makes the app use its managed identity.

### App settings

**App settings** tab → **Advanced edit**. Keep whatever is already in the list and add these entries
inside the `[ ... ]`, separated by commas. Replace the names and your e-mail:

```json
{ "name": "ASPNETCORE_ENVIRONMENT", "value": "Production", "slotSetting": false },
{ "name": "ASPNETCORE_FORWARDEDHEADERS_ENABLED", "value": "true", "slotSetting": false },
{ "name": "App__PublicBaseUrl", "value": "https://rmd-shl01.azurewebsites.net", "slotSetting": false },
{ "name": "AllowedHosts", "value": "rmd-shl01.azurewebsites.net", "slotSetting": false },
{ "name": "Backup__Directory", "value": "/home/data/rmd-backups", "slotSetting": false },
{ "name": "Database__MigrateOnStartup", "value": "true", "slotSetting": false },
{ "name": "Email__Username", "value": "you@gmail.com", "slotSetting": false },
{ "name": "Email__Password", "value": "@Microsoft.KeyVault(VaultName=kv-rmd-shl01;SecretName=Email-Password)", "slotSetting": false },
{ "name": "SeedAdmin__Email", "value": "you@gmail.com", "slotSetting": false },
{ "name": "SeedAdmin__Password", "value": "@Microsoft.KeyVault(VaultName=kv-rmd-shl01;SecretName=SeedAdmin-Password)", "slotSetting": false }
```

**OK** → **Apply** → **Confirm**.

| Setting | Why |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` keeps Swagger and detailed error pages off |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Behind Azure's front end: the real client IP for lockout and rate limiting, and the correct `https` scheme |
| `App__PublicBaseUrl` | Base of password-reset links; never taken from the request |
| `AllowedHosts` | Rejects requests for other host names |
| `Backup__Directory` | "Tøm database" backups must be under `/home`; the app folder is replaced on every deploy |
| `Database__MigrateOnStartup` | Creates the tables on the first start. Set to `false` in step 13 |
| `SeedAdmin__*` | Creates your login user on the first start, when there is none |
| `@Microsoft.KeyVault(...)` | App Service reads the secret from Key Vault; the value never appears in the settings |

After saving, the two Key Vault settings get a **green tick** in the *Source* column within a minute or so.
A red cross means step 8's role assignment or the secret name is wrong.

`DataProtection__KeysDirectory` isn't needed: App Service already keeps the keys that protect login cookies.

## 10. Lock down database network access

Only the Web App (and you, when needed) may reach the database.

1. Web App → **Settings → Properties** → copy the list under **Additional Outbound IP Addresses** (it
   includes all the others).
2. SQL **server** `sql-rmd-shl01` → **Security → Networking** → **Public access** tab:
   - **Public network access:** **Selected networks**
   - **Firewall rules:** one rule per IP from step 1, with the same start and end IP, named `app-1`,
     `app-2`, …
   - Keep your own client IP rule until step 13.
   - **Exceptions:** leave **Allow Azure services and resources to access this server** **unchecked**. It
     would let in traffic from every Azure customer and rely on authentication alone.
   - **Save**

> Stricter (and about USD 7/month more): a private endpoint for SQL plus VNet integration on the Web App,
> with public access disabled. VNet integration isn't available on F1.

## 11. Deploy from GitHub

The portal sets up a GitHub Actions workflow and a passwordless (OIDC) sign-in for it.

1. Web App → **Deployment → Deployment Center** → **Settings** tab.
2. **Source:** **GitHub** → **Authorize** and sign in to GitHub if asked.
3. **Organization:** `SindreLH`, **Repository:** `RMD`, **Branch:** `main`.
4. **Runtime stack / version:** **.NET**, **10**.
5. **Authentication type:** **User-assigned identity** (the passwordless option; it creates the identity
   and its federated credential for you). Leave **Subscription** and **Identity** as suggested.
6. **Preview file** if you're curious, then **Save**.

The portal commits a workflow file (`.github/workflows/main_rmd-shl01.yml`) to `main`, and the first
deploy starts. Follow it on GitHub under **Actions**, or in the portal under **Deployment Center → Logs**.

### Fix up the workflow file

The generated workflow builds the whole solution and doesn't run the tests. On github.com, open the
workflow file → **Edit** (pencil), and in the **build** job:

1. Replace the `dotnet build` step with a test step, so a failing test stops the deploy:
   ```yaml
         - name: Test
           run: dotnet test --configuration Release
   ```
2. In the `dotnet publish` step, publish just the web project. Keep the `-o` path that's already there,
   for example:
   ```yaml
           run: dotnet publish RMD.GUI/RMD.GUI.csproj -c Release -o "${{env.DOTNET_ROOT}}/myapp"
   ```
3. **Commit changes** directly to `main`. This starts a new deploy.

Then run `git pull` in your local copy so it has the workflow file, and your next push doesn't conflict.

Optional: on GitHub, **Settings → Environments → Production → Required reviewers** (add yourself) to
approve each deploy before it goes out.

## 12. First start

1. When the GitHub Action is green, open `https://rmd-shl01.azurewebsites.net`. The first load after a
   deploy can take 20–60 seconds.
2. Log in with `SeedAdmin__Email` and the `SeedAdmin-Password` secret.

If it doesn't start, see Web App → **Monitoring → Log stream** and [Troubleshooting](#19-troubleshooting).

## 13. Tighten up after the first start

The first start created the tables and your user. Now take away what was only needed for that:

1. **Database rights.** SQL database → **Query editor** → run:
   ```sql
   ALTER ROLE db_owner DROP MEMBER [rmd-shl01];
   ```
   The app keeps `db_datareader` and `db_datawriter`.
2. **App settings.** Web App → **Environment variables** → **App settings**:
   - Set `Database__MigrateOnStartup` to `false`.
   - Delete `SeedAdmin__Password` (trash icon).
   - **Apply** → **Confirm**. The app restarts.
3. **Seed secret.** Key Vault → **Secrets** → `SeedAdmin-Password` → **Delete**.
4. **Firewall.** Delete your own client IP rule (SQL server → **Security → Networking**). Re-add it
   whenever you need the query editor; it offers the one-click **Allowlist IP**.
5. **Password reset.** Log out, use **Glemt passord** once, and check that the e-mail arrives.

## 14. Move your data

1. Locally: start RMD → **Innstillinger → Eksport → Last ned JSON**.
2. On Azure: **Innstillinger → Import** → choose the file → check the preview → **Importer**.
   Import only appends and skips duplicates, so running it twice is harmless.

## 15. Monitoring, cost and backups

**Logs.** Web App → **Monitoring → App Service logs** → **Application logging: File System** → **Save**.
Then **Log stream** shows live output, including failed logins, lockouts, imports and database clears.
For log history and alerts, turn on **Application Insights** (Web App → **Monitoring → Application
Insights** → **Turn on**). Its first 5 GB a month are free, far more than RMD produces.

**Budget alert** (the safety net if something stops being free): search for **Cost Management** →
**Budgets** → **+ Add** → scope `rg-rmd`, a small monthly amount (e.g. 50 NOK), alert at 80 % to your e-mail.

**Alerts** (optional): Web App → **Monitoring → Alerts** → **+ Create → Alert rule**, for example
*Http Server Errors* greater than 0 over 15 minutes.

**F1 quota.** Web App → **App Service plan** → **Quotas** shows the daily CPU and data use. If you run into
the limits, upgrade under **App Service plan → Scale up** → **Basic B1** (about USD 13/month), then turn on
**Always on** (step 6).

**Backups.**
- Azure SQL keeps **point-in-time restore** for 7 days: SQL database → **Restore**.
- Take an RMD **JSON export** now and then and keep it outside Azure.
- "Tøm database" always writes a backup to `/home/data/rmd-backups` first; download it from Innstillinger.

**Defender for Cloud.** Search for it once and look at the free *Recommendations* for `rg-rmd`.

## 16. Later: deploying updates

- **Code changes:** merge into `main`. GitHub Actions tests and deploys automatically.
- **Changes with a new database migration** (a new file under `RMD.Data/Migrations`). Before merging:
  1. Query editor: `ALTER ROLE db_owner ADD MEMBER [rmd-shl01];`
  2. App settings: `Database__MigrateOnStartup` = `true`.
  3. Merge, wait for the deploy, and open the app once so it starts and migrates.
  4. Undo 1 and 2: `ALTER ROLE db_owner DROP MEMBER [rmd-shl01];`, and the setting back to `false`.

  Simpler but less strict: leave `db_owner` and `MigrateOnStartup=true` in place permanently, so every
  deploy migrates itself. The cost is that a bug or a break-in in the app could alter or drop tables.

## 17. Optional: an extra sign-in gate

RMD has its own login with lockout and rate limiting. For a personal app you can also put Microsoft's
sign-in in front, so nobody but you even reaches RMD's login page:

1. Web App → **Settings → Authentication** → **Add identity provider** → **Microsoft**.
   - **Restrict access:** **Require authentication**
   - **Unauthenticated requests:** **HTTP 302 Found redirect**
   - **Add**
2. Search for **Enterprise applications** → open the app just created (named after the Web App) →
   **Properties** → **Assignment required?** **Yes** → **Save** → **Users and groups** → **+ Add
   user/group** → yourself → **Assign**.

You then sign in with your Microsoft account first, then with RMD's own login.

## 18. Security checklist

- [ ] SQL server: Entra-only authentication, no "Allow Azure services", firewall limited to the Web App's IPs
- [ ] App's database user: `db_datareader` and `db_datawriter` only (no `db_owner`) after the first start
- [ ] Secrets only in Key Vault (RBAC); the app has *Key Vault Secrets User* only
- [ ] HTTPS only, TLS 1.2, FTP disabled, SCM and FTP basic auth off
- [ ] Web sockets on, session affinity on, health check `/healthz` if available
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `App__PublicBaseUrl` and `AllowedHosts` set to your real host name
- [ ] Deployment through GitHub Actions with a user-assigned identity; no publish profile anywhere
- [ ] `SeedAdmin__Password` setting and secret removed; your password is long, unique, and not the one in old git history
- [ ] A budget alert, and a recent JSON export kept somewhere safe

## 19. Troubleshooting

| Symptom | Check |
|---|---|
| GitHub Action fails at the Azure login step | Deployment Center was saved with *User-assigned identity*, and you are Owner on the subscription |
| GitHub Action fails at build or test | Read the failing step's log; the workflow must use .NET 10 |
| "Attempting to reconnect" loops, blank page after login | Web sockets and session affinity are **On** (step 6) |
| Login sends you back to the login page | The site is opened over `https` |
| HTTP 500.30, or `/healthz` returns 503 | *Log stream*. Usually the connection string, the SQL firewall (step 10), or the database user (step 7) |
| `Login failed for user '<token-identified principal>'` | The name in `CREATE USER [...]` must equal the Web App name exactly |
| `Invalid object name 'Artists'` | Tables not created: `db_owner` and `MigrateOnStartup=true` must both be in place on the first start (steps 7, 9). Restart the app after fixing |
| Key Vault setting has a red cross | The app has *Key Vault Secrets User* on the vault (step 8); the secret name is spelled the same |
| Can't log in on the first start | `SeedAdmin__Password` has a green tick, and the password meets the rules (8+ characters, upper and lower case, digit, symbol). If the user wasn't created, *Log stream* says why |
| No password-reset e-mail | `App__PublicBaseUrl`, `Email__Username`, the green tick on `Email__Password`, and that it's a Gmail **app** password |
| Lockout or rate limit hits too early | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` |
| Query editor: "Cannot open server ... client IP" | Click **Allowlist IP**, or add your IP under SQL server → Networking |
| Slow first request | Normal on F1 after 20 idle minutes, and when the free SQL database has auto-paused |
| App stops responding late in the day | F1's daily CPU or data quota is used up (step 15, *Quotas*). It resets daily, or scale up to B1 |
