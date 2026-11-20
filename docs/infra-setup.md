# Infrastructure setup (step by step, portal only)

This guide sets up the production infrastructure for Catalog by clicking through each provider's web
portal. No CLI is needed. It ends with a first deploy and a checklist to verify it.
`docs/deployment.md` is the reference of every secret, variable and setting; this guide is the order
in which to create them.

Everything stays on free tiers (ADR 0005, ADR 0011). Azure needs a subscription, but the API runs on
App Service's Free (F1) plan.

| Piece | Provider | What it hosts |
| --- | --- | --- |
| Database and identity | Supabase | PostgreSQL, Supabase Auth (sign-in, invitations) |
| Images | Cloudflare R2 | Product images, served from a public URL |
| SPA | Cloudflare Pages | The React web app |
| API | Azure App Service (Free F1) | The .NET API, deployed as code |
| CI/CD | GitHub Actions | Builds, migrations and deploys on merge to `main` |

## 0. Before you start

You need:

- A Supabase account (https://supabase.com/dashboard).
- A Cloudflare account (https://dash.cloudflare.com). R2 asks for a payment method even on the free tier.
- An Azure subscription (https://portal.azure.com), for example "Azure free account" or "Pay-As-You-Go".
- Admin rights on the `mookie-studio/just-looking` GitHub repository and on the `mookie-studio` organization.

Keep a private note open (a password manager is best) and copy each value into it as you go. Every
value you need later is marked like this: **Save as `NAME`**. Never paste these values into an issue,
a PR or a commit.

Pick the names now so they match everywhere. This guide uses:

| Thing | Example name |
| --- | --- |
| Supabase project | `catalog` |
| R2 bucket | `catalog-images` |
| Pages project | `catalog-web` (gives `https://catalog-web.pages.dev`) |
| Azure resource group | `catalog-rg` |
| App Service web app | `catalog-api` (gives `https://catalog-api.azurewebsites.net`, or `https://catalog-api-<hash>.<region>-01.azurewebsites.net` when **Secure unique default hostname** is on) |
| App Service plan | `catalog-plan` (Linux, Free F1) |

The SPA domain (`https://<pages-project>.pages.dev`) and the API URL are needed by other steps, so the
order below matters: Supabase, R2, Pages, Azure, GitHub, then the first deploy.

## 1. Supabase (database and identity)

### 1.1 Create the project

1. Open https://supabase.com/dashboard and click **New project**.
2. Choose your organization, set **Project name** to `catalog`.
3. Click **Generate a password** for the database password, or type one with only letters and digits
   (characters such as `;` or `'` break .NET connection strings). **Save as `DB_PASSWORD`**.
4. Pick the **Region** closest to the Azure region you will use later (for example West EU (Ireland)
   with Azure "West Europe"). Plan: **Free**.
5. Click **Create new project** and wait until the project is ready (one or two minutes).

The project URL is `https://<project-ref>.supabase.co`, where `<project-ref>` is the id in the
browser's address bar (`/project/<project-ref>`). **Save as `SUPABASE_URL`** and `PROJECT_REF`.

### 1.2 Database connection string

GitHub Actions runners and Azure App Service reach the internet over IPv4, and Supabase's direct
database host is IPv6 only. Use the **Session pooler**, which works over IPv4.

1. Click **Connect** at the top of the project page.
2. In **Connection String**, set **Type** to `.NET` (or keep "URI" and convert it by hand) and
   **Method** to **Session pooler**.
3. Copy the string and replace `[YOUR-PASSWORD]` with `DB_PASSWORD`. It looks like:
   `Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<DB_PASSWORD>;SSL Mode=Require`
   (add `;SSL Mode=Require` if it is missing). **Save as `DATABASE_CONNECTION_STRING`**.

The same string is used by GitHub (to apply migrations) and by the API.

### 1.3 API keys

1. Go to **Project Settings** (gear icon) → **API Keys**.
2. In the **Publishable and secret API keys** tab, copy the **publishable key** (`sb_publishable_...`).
   **Save as `VITE_SUPABASE_ANON_KEY`**. It is public by design.
3. In the same tab, reveal and copy the **secret key** (`sb_secret_...`). **Save as
   `SUPABASE_SECRET_KEY`**. It bypasses all access rules: it only ever goes to the API, never to the
   browser or to GitHub variables. (The legacy `service_role` key also works until Supabase retires
   legacy keys at the end of 2026.)

### 1.4 Signing keys (JWT)

1. Go to **Project Settings** → **JWT Keys**.
2. Check that the current key is asymmetric (**ECC (P-256)** or **RSA**). New projects use this by
   default. If the project still uses only the **Legacy JWT secret**, click **Migrate JWT secret** and
   then **Rotate keys** so that a new asymmetric key becomes current.

The API then validates tokens with:

- `Auth__Issuer` = `https://<project-ref>.supabase.co/auth/v1`
- `Auth__JwksUrl` = `https://<project-ref>.supabase.co/auth/v1/.well-known/jwks.json`

Open the JWKS URL in a browser: it must return JSON with at least one key.

### 1.5 Authentication settings

1. Go to **Authentication** → **Sign In / Providers**.
   - Under **User Signups**, turn **Allow new users to sign up** **off**, then **Save**. Admins only get
     in through invitations.
   - Keep the **Email** provider enabled (it is needed to sign in with email and password).
2. Go to **Authentication** → **URL Configuration**. You will know the SPA domain after step 3; you
   can come back, or use the Pages name you picked now:
   - **Site URL**: `https://catalog-web.pages.dev`
   - **Redirect URLs** → **Add URL**: `https://catalog-web.pages.dev/accept-invite` → **Save URLs**.

### 1.6 Invitation emails

Supabase's built-in email sender only delivers to the email addresses of your Supabase organization's
team members, and only a few emails per hour. That is enough to try the invitation flow with your own
address. To invite anyone else, configure your own SMTP server:

1. Create an account with an SMTP provider that has a free tier (for example Resend or Brevo) and get
   its SMTP host, port, user and password.
2. Go to **Authentication** → **Emails** → **SMTP Settings**, turn on **Enable Custom SMTP**, fill in
   the sender email, sender name and the SMTP details, and **Save**.
3. In **Authentication** → **Rate Limits**, raise the email rate limit if you need more invitations
   per hour.

### 1.7 First Platform Admin

The Platform Admin is a Supabase user whose id is listed in the API settings.

1. Go to **Authentication** → **Users** → **Add user** → **Create new user**.
2. Enter your email and a password, keep **Auto Confirm User** checked, and click **Create user**.
3. Click the new user and copy its **User UID**. **Save as `PLATFORM_ADMIN_USER_ID`**.
4. Keep the password in a password manager. Supabase never shows it again, and the dashboard cannot
   set a new one on an existing user (its **Send password recovery** only helps if the email can
   receive mail). If you lose it, reset it in **SQL Editor** without changing the User UID (replace
   both values, then **Run**):

   ```sql
   update auth.users
   set encrypted_password = extensions.crypt('<new password>', extensions.gen_salt('bf'))
   where email = '<your email>';
   ```

   Do not delete the user and create it again: the new user gets a new User UID, and
   `Auth__PlatformAdminUserIds__0` (step 4.4) would have to change too.

Free Supabase projects pause after a week without activity. If the API's health check returns 503
after a quiet week, open the project in the dashboard and click **Restore project**.

## 2. Cloudflare R2 (product images)

### 2.1 Enable R2 and create the bucket

1. Open https://dash.cloudflare.com and choose your account.
2. In the sidebar, go to **R2 Object Storage**. The first time, follow **Purchase R2 Plan** / **Add R2
   subscription** (the free tier costs nothing, but a payment method is required).
3. Click **Create bucket**. **Bucket name**: `catalog-images`. **Location**: Automatic. **Default
   storage class**: Standard. Click **Create bucket**. **Save as `R2_BUCKET`**.

### 2.2 Public URL

1. Open the bucket → **Settings** tab → **Public Development URL** (r2.dev subdomain) → **Enable**,
   type `allow` to confirm, and click **Allow**.
2. Copy the URL shown, for example `https://pub-0123456789abcdef.r2.dev`. **Save as
   `R2_PUBLIC_URL`**. Image URLs are `R2_PUBLIC_URL/<key>`; the bucket name is not part of the path.

The r2.dev URL is rate limited and meant for development and demos. If you have a domain on
Cloudflare, use **Custom Domains** → **Add** in the same tab instead (for example
`images.example.com`) and use that as `R2_PUBLIC_URL`.

The browser never uploads to R2 directly (uploads go through the API), so the bucket needs no CORS
policy.

### 2.3 API token for the API

1. Go back to **R2 Object Storage** (overview). In **Account Details**, next to **API Tokens**, click
   **Manage**.
2. Click **Create Account API token** (or **Create User API token**).
3. **Token name**: `catalog-api`. **Permissions**: **Object Read & Write**. **Specify bucket(s)**:
   **Apply to specific buckets only** → `catalog-images`. **TTL**: Forever.
4. Click **Create Account API Token**. The next page shows the values only once:
   - **Access Key ID**: **Save as `R2_ACCESS_KEY`**.
   - **Secret Access Key**: **Save as `R2_SECRET_KEY`**.
   - The S3 endpoint `https://<account-id>.r2.cloudflarestorage.com` (without the bucket name):
     **Save as `R2_ENDPOINT`**.

## 3. Cloudflare Pages (SPA)

### 3.1 Create the Pages project

The deploy workflow uploads the built SPA with `wrangler pages deploy`, which needs an existing
"Direct Upload" project.

1. On your computer, create a folder `placeholder` with a file `index.html` containing `Catalog`.
2. In the Cloudflare dashboard, go to **Workers & Pages** → **Create application**. Choose the **Pages**
   tab (if the page only offers Workers, click **Looking to deploy Pages? Get started**).
3. Choose **Drag and drop your files** / **Upload assets**.
4. **Project name**: `catalog-web` → **Create project**. **Save as `CLOUDFLARE_PAGES_PROJECT`**.
5. Drag the `placeholder` folder in and click **Deploy site**.
6. Note the production domain shown, for example `https://catalog-web.pages.dev`. If the name was
   taken, Cloudflare adds a suffix: use the domain it shows. **Save as `SPA_URL`**. If it differs from
   what you entered in Supabase (step 1.5), update the Site URL and Redirect URL there.

Do not connect the project to Git: GitHub Actions deploys it.

### 3.2 Account id and API token for GitHub

1. **Account id**: on **Workers & Pages**, the right column shows **Account ID** (or open the account
   home, click the menu next to the account name and **Copy account ID**). **Save as
   `CLOUDFLARE_ACCOUNT_ID`**.
2. **API token**: click the profile icon (top right) → **My Profile** → **API Tokens** → **Create
   Token** → **Create Custom Token** → **Get started**.
   - **Token name**: `catalog-github-pages`.
   - **Permissions**: `Account` · `Cloudflare Pages` · `Edit`.
   - **Account Resources**: `Include` · your account.
   - Click **Continue to summary** → **Create Token** and copy it. **Save as `CLOUDFLARE_API_TOKEN`**.

## 4. Azure App Service (API)

The API runs on App Service's free plan as code, not as a container (ADR 0011).

### 4.1 Resource group

1. Open https://portal.azure.com, search for **Resource groups** → **Create**.
2. **Subscription**: yours. **Resource group**: `catalog-rg`. **Region**: for example West Europe.
3. **Review + create** → **Create**.

### 4.2 Web app

1. Search for **App Services** → **Create** → **Web App**.
2. **Basics**:
   - **Resource group**: `catalog-rg`.
   - **Name**: `catalog-api` (it must be unique in Azure; if it is taken, pick another and use it
     everywhere below). **Save as `AZURE_WEBAPP_NAME`**.
   - **Publish**: **Code**. **Runtime stack**: **.NET 10 (LTS)**. **Operating System**: **Linux**.
   - **Region**: the same as the resource group.
   - **Linux Plan**: **Create new** → `catalog-plan`. **Pricing plan**: **Free F1** (open **Explore
     pricing plans** if it is not listed and pick **Free F1** under Dev/Test).
3. **Deployment**: leave **Continuous deployment** disabled (GitHub Actions deploys) and **Basic
   authentication** disabled.
4. **Monitoring**: **Enable Application Insights**: No (it is not needed and can cost money).
5. **Review + create** → **Create**. Wait for the deployment, then **Go to resource**.
6. On **Overview**, copy **Default domain**, for example `catalog-api.azurewebsites.net` or `catalog-api-<hash>.westeurope-01.azurewebsites.net` (use exactly what the portal shows), and add
   `https://` in front. **Save as `VITE_API_URL`**. Opening it now shows App Service's placeholder page.

If the portal reports that `Microsoft.Web` is not registered, go to **Subscriptions** → your
subscription → **Settings** → **Resource providers**, select `Microsoft.Web` and click **Register**,
then retry.

### 4.3 General settings

1. Go to **Settings** → **Configuration** → **General settings**.
2. **HTTPS Only**: On. **Minimum inbound TLS version**: 1.2. **FTP state**: FTPS only or Disabled.
3. **Always on** is not available on the Free plan: the app unloads after about 20 idle minutes and
   the first request afterwards takes several seconds.
4. Click **Save**.

### 4.4 App settings

1. Go to **Settings** → **Environment variables** → **App settings** tab.
2. Add each row below with **+ Add** (note the double underscore `__` in the names), then click
   **Apply** at the bottom and confirm. The app restarts with the new settings.

| Name | Value |
| --- | --- |
| `ConnectionStrings__Catalog` | `DATABASE_CONNECTION_STRING` |
| `Auth__Issuer` | `https://<project-ref>.supabase.co/auth/v1` |
| `Auth__JwksUrl` | `https://<project-ref>.supabase.co/auth/v1/.well-known/jwks.json` |
| `Auth__PlatformAdminUserIds__0` | `PLATFORM_ADMIN_USER_ID` |
| `Supabase__Url` | `SUPABASE_URL` |
| `Supabase__ServiceRoleKey` | `SUPABASE_SECRET_KEY` |
| `Supabase__InviteRedirectUrl` | `SPA_URL/accept-invite` |
| `Storage__ServiceUrl` | `R2_ENDPOINT` |
| `Storage__Bucket` | `R2_BUCKET` |
| `Storage__AccessKey` | `R2_ACCESS_KEY` |
| `Storage__SecretKey` | `R2_SECRET_KEY` |
| `Storage__PublicBaseUrl` | `R2_PUBLIC_URL` |
| `Storage__Region` | `auto` |
| `Cors__AllowedOrigins__0` | `SPA_URL` (no trailing slash) |
| `PublicSite__BaseUrl` | `SPA_URL` (no trailing slash). Sitemap addresses are built from it; without it the Sitemap endpoints answer 404 |

App settings are encrypted at rest and never shown to the browser. The deploy workflow only replaces
the code, so these settings survive every deploy.

### 4.5 Identity for GitHub Actions (OIDC, no password)

GitHub signs in to Azure with a federated credential, so no Azure password is stored anywhere.

1. Search for **Microsoft Entra ID** → **App registrations** → **New registration**.
   - **Name**: `catalog-github-deploy`. **Supported account types**: Single tenant. No redirect URI.
   - Click **Register**.
2. On the app's **Overview**, copy:
   - **Application (client) ID**: **Save as `AZURE_CLIENT_ID`**.
   - **Directory (tenant) ID**: **Save as `AZURE_TENANT_ID`**.
3. Go to **Certificates & secrets** → **Federated credentials** tab → **Add credential**.
   - **Federated credential scenario**: GitHub Actions deploying Azure resources.
   - **Organization**: `mookie-studio`. **Repository**: `just-looking`.
   - **Entity type**: Environment. **GitHub environment name**: `production`.
   - **Name**: `catalog-production`. Click **Add**.
   - The credential's subject is `repo:mookie-studio/just-looking:environment:production`. GitHub
     puts the repository name in the token it sends to Azure, so if the repository is ever renamed
     (it was called `catalog` until 2026-11-19), edit this credential and change **Repository** to
     the new name, or deployments fail with `AADSTS70021: No matching federated identity record found`.
4. Give it access to the web app only: go to **App Services** → `catalog-api` → **Access control
   (IAM)** → **Add** → **Add role assignment**.
   - **Role**: search for **Website Contributor**, select it, **Next**.
   - **Members**: **Assign access to**: User, group, or service principal → **Select members** →
     search `catalog-github-deploy` → **Select** → **Review + assign** (twice).
5. **Subscription id**: search for **Subscriptions** and copy your subscription's **Subscription ID**.
   **Save as `AZURE_SUBSCRIPTION_ID`**.

### 4.6 Logs

- **Monitoring** → **Log stream** shows the API's console output live (start-up errors appear here).
- To keep logs for a while, go to **Monitoring** → **App Service logs**, set **Application logging**
  to **File System**, retention a few days, and **Save**.

## 5. GitHub (secrets and variables)

### 5.1 Environment `production` and its secrets

1. Open https://github.com/mookie-studio/just-looking → **Settings** → **Environments** → **New
   environment** → name `production` → **Configure environment**.
2. Optional: under **Deployment branches and tags**, choose **Selected branches and tags** and add
   `main`, so only `main` can deploy.
3. Under **Environment secrets**, click **Add environment secret** for each:

| Secret | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | `AZURE_CLIENT_ID` |
| `AZURE_TENANT_ID` | `AZURE_TENANT_ID` |
| `AZURE_SUBSCRIPTION_ID` | `AZURE_SUBSCRIPTION_ID` |
| `DATABASE_CONNECTION_STRING` | `DATABASE_CONNECTION_STRING` |
| `CLOUDFLARE_API_TOKEN` | `CLOUDFLARE_API_TOKEN` |
| `CLOUDFLARE_ACCOUNT_ID` | `CLOUDFLARE_ACCOUNT_ID` |

### 5.2 Repository variables

The deploy workflows check `AZURE_WEBAPP_NAME` and `CLOUDFLARE_PAGES_PROJECT` in `if:`
conditions, which only see repository variables, so create these as repository variables (not
environment variables).

1. Go to **Settings** → **Secrets and variables** → **Actions** → **Variables** tab → **New repository
   variable** for each:

| Variable | Value |
| --- | --- |
| `VITE_API_URL` | `VITE_API_URL` (no trailing slash) |
| `VITE_SUPABASE_URL` | `SUPABASE_URL` |
| `VITE_SUPABASE_ANON_KEY` | the publishable key |
| `CLOUDFLARE_PAGES_PROJECT` | `catalog-web` |
| `AZURE_WEBAPP_NAME` | `AZURE_WEBAPP_NAME` |

Setting `CLOUDFLARE_PAGES_PROJECT` and `AZURE_WEBAPP_NAME` turns the deploy workflows on: from
now on every merge to `main` that touches `web/**` or `api/**` deploys.

## 6. First deploy

The workflows run on merges to `main`, and can be started by hand.

### 6.1 API

1. Open the repository → **Actions** → **Deploy API** → **Run workflow** → branch `main` → **Run
   workflow**.
2. The run has three jobs: `build` (publish the API), `migrate` (apply EF Core migrations to Supabase)
   and `deploy` (upload the code to the web app, then wait until `/api/health` answers).
3. If `deploy` fails at sign-in, check the federated credential (environment `production`) and the
   Website Contributor role. If it fails waiting for health, open the web app's **Log stream**.

### 6.2 SPA

1. **Actions** → **Deploy web** → **Run workflow** → branch `main` → **Run workflow**.
2. When it is green, the Pages project's **Deployments** tab in Cloudflare shows the new production
   deployment.

## 7. Verify

Work through this list in order; each step depends on the previous one.

1. **API is up and reaches the database.** Open `VITE_API_URL/api/health`. Expect `{"status":"ok"}`.
   A 503 means the database is unreachable: check `ConnectionStrings__Catalog` (session pooler,
   password) and that the Supabase project is not paused. An application error page or no answer: open the web app's
   **Monitoring** → **Log stream** for start-up errors, and check the app settings of step 4.4. The
   first request after an idle period can take several seconds (Free plan).
2. **Migrations ran.** In Supabase, **Table Editor** lists the Catalog tables and
   `__EFMigrationsHistory`.
3. **SPA loads.** Open `SPA_URL`. Then open a deep link such as `SPA_URL/login` directly: it must load
   the app, not a 404.
4. **Platform Admin signs in.** Sign in at `SPA_URL/login` with the user from step 1.7, then open
   `SPA_URL/platform`. If the sign-in form says **Invalid login credentials**, Supabase rejected the
   email and password: the user exists but the password is different (or the email is misspelled).
   Check the user in **Authentication** → **Users**, and reset the password as in step 1.7 step 4.
   If you get in but `SPA_URL/platform` says you have no access, the User UID is not the value of
   `Auth__PlatformAdminUserIds__0`. If sign-in works but the API answers 401, compare `Auth__Issuer` and
   `Auth__JwksUrl` with the project ref. If the browser console shows a CORS error, check
   `Cors__AllowedOrigins__0` is exactly `SPA_URL`.
5. **Invitation works.** Create a Tenant with an Admin email you can read (see step 1.6). The invitation
   email arrives, its link opens `SPA_URL/accept-invite`, and the Admin can set a password. If the link
   goes to `localhost`, fix the Site URL and Redirect URLs in Supabase.
6. **Admin manages Products and images.** As that Admin, create a Product, upload an image and publish
   it. The image URL starts with `R2_PUBLIC_URL` and opens in the browser; the object also appears in
   the R2 bucket's **Objects** tab.
7. **Visitors see the catalog.** Open `SPA_URL/<tenant-slug>` in a private window: the published Product
   is listed and its page shows the image.

8. **Admin tools work.** Sign in as the Admin: the first page is the **Dashboard**. Publish two products
   in the same Category and open one in the private window; the other appears under **Related
   products**. After a minute, reload the Dashboard: the opened product appears under **Most viewed
   products** with 1 view.
9. **Search engines can read the catalog.** Open `VITE_API_URL/api/public-sitemap.xml`: it lists
   `VITE_API_URL/api/public/<tenant-slug>/sitemap.xml`, and opening that lists the catalog and each
   published product with addresses that start with `SPA_URL`. A 404 means `PublicSite__BaseUrl` is
   missing (step 4.4). Open `SPA_URL/robots.txt`: it disallows `/admin` and ends with the
   `Sitemap:` line pointing to the sitemap index; if that line is missing, `VITE_API_URL` was empty
   when the SPA was built (step 5.2). Optionally submit the sitemap index address in Google Search
   Console.

The demo catalogs (`forja-hardware`, `lino-apparel`, `folio-books`) and their demo Admins are not
created by the deploy. They are loaded by the Demo seeder, run by hand from your computer
([ADR 0014](adr/0014-demo-data-seeded-through-the-api.md)). Until it has run, those pages show
the "not found" page and the only account is the Platform Admin of step 1.7.

When all nine pass, the infrastructure is done. Later deploys need no clicks: merge to `main`.

## Costs and limits to keep in mind

- **Supabase Free**: 500 MB database, pauses after a week of inactivity, built-in email only to team
  members.
- **Cloudflare R2**: 10 GB storage and generous request quotas free each month; r2.dev is rate limited.
- **Cloudflare Pages**: free, 500 builds a month (deploys via Direct Upload count too).
- **Azure App Service Free (F1)**: 60 CPU minutes a day, 1 GB memory, 1 GB storage, no Always On
  (the app unloads when idle). If the daily CPU quota runs out, the API stops answering until the
  next day. Set a budget alert in **Cost Management** → **Budgets** anyway, in case the plan is ever
  changed to a paid tier.
