# Deployment

To create the infrastructure for the first time, follow the portal walkthrough in
[infra-setup.md](infra-setup.md). This page is the reference of what each workflow needs.

Merging to `main` deploys (ADR 0005, ADR 0011):

- `api/**` changes run `.github/workflows/deploy-api.yml`: publish the API (`dotnet publish`), apply
  EF Core migrations with a migrations bundle against the Supabase database, then deploy the published
  output to the Azure App Service web app and wait until `/api/health` answers. Migrations run before
  the new version goes live.
- `web/**` changes run `.github/workflows/deploy-web.yml`: build the SPA with the production `VITE_*`
  variables and deploy it with `wrangler pages deploy`.

The API workflows use the .NET SDK pinned in `api/global.json` (10.0, latest feature band). GitHub
runners also ship newer SDKs (for example 11.0), and without the pin `dotnet` would pick the newest one.
Before building the migrations bundle, the `migrate` job runs `dotnet restore -r linux-x64` on the API
project, because `dotnet ef` reads the project metadata (which needs `obj/project.assets.json`) before it
builds; without it the job fails with `NETSDK1004`.

Both workflows can also be started by hand (`workflow_dispatch`). They use the GitHub environment
`production`; add the secrets and variables below to that environment. No secret is stored in the repository.
Until `AZURE_WEBAPP_NAME` (API) or `CLOUDFLARE_PAGES_PROJECT` (SPA) is set, the workflows are skipped.

Variables used in `if:` conditions must be repository variables (or organization variables), because
environment variables are only visible to jobs that already run in that environment.

## API (Azure App Service)

### GitHub secrets

| Secret | What it is |
| --- | --- |
| `AZURE_CLIENT_ID` | Client id of the Entra ID app (or managed identity) with a federated credential for this repository's `production` environment (OIDC, no password; subject `repo:mookie-studio/just-looking:environment:production`, which must change if the repository is renamed). It needs the Website Contributor role on the web app. |
| `AZURE_TENANT_ID` | Entra ID tenant id. |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription id. |
| `DATABASE_CONNECTION_STRING` | Npgsql connection string of the Supabase database, used to apply migrations through the session pooler, because GitHub runners have no IPv6 (for example `Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project>;Password=...;SSL Mode=Require`). |

### GitHub variables

| Variable | What it is |
| --- | --- |
| `AZURE_WEBAPP_NAME` | Name of the App Service web app (for example `catalog-api`). The API URL is the web app's **Default domain** (Overview in the portal). Apps created with a unique default hostname get `https://<name>-<hash>.<region>-01.azurewebsites.net`, not `https://<name>.azurewebsites.net`; the workflow checks `/api/health` on the URL that `azure/webapps-deploy` reports, so it works with both. |

### Web app settings

Configure these once on the web app (**Settings → Environment variables → App settings**). The
workflow only replaces the code, so they survive every deploy.

| App setting | Value |
| --- | --- |
| `ConnectionStrings__Catalog` | Supabase database connection string (session pooler; the direct host is IPv6 only). |
| `Auth__Issuer` | `https://<project>.supabase.co/auth/v1` |
| `Auth__JwksUrl` | `https://<project>.supabase.co/auth/v1/.well-known/jwks.json` (or `Auth__JwtSecret` for a legacy HS256 secret) |
| `Auth__PlatformAdminUserIds__0` | Supabase user id of a Platform Admin (add `__1`, `__2`… for more). |
| `Supabase__Url` | `https://<project>.supabase.co` |
| `Supabase__ServiceRoleKey` | Supabase secret key (`sb_secret_...`) or legacy `service_role` key. Never exposed to the browser. |
| `Supabase__InviteRedirectUrl` | `https://<spa-domain>/accept-invite` |
| `Storage__ServiceUrl` | `https://<account-id>.r2.cloudflarestorage.com` |
| `Storage__Bucket` | R2 bucket name. |
| `Storage__AccessKey` / `Storage__SecretKey` | R2 API token credentials. |
| `Storage__PublicBaseUrl` | Public URL of the bucket (custom domain or `r2.dev`). |
| `Storage__Region` | `auto` |
| `Cors__AllowedOrigins__0` | `https://<spa-domain>` |
| `PublicSite__BaseUrl` | `https://<spa-domain>` (Sitemap addresses; without it the Sitemap endpoints answer 404) |

App Service tells the API which port to listen on; no port setting is needed.

The API trusts `X-Forwarded-For` and `X-Forwarded-Proto` from any proxy (the host's front ends have no
fixed addresses), so it sees the Visitor's IP for the Inquiry rate limit. Optional settings:
`RateLimits__Inquiries__PermitLimit` (default 5) and `RateLimits__Inquiries__WindowSeconds` (default 600) for Inquiries,
and `RateLimits__ProductViews__PermitLimit` (default 30) and `RateLimits__ProductViews__WindowSeconds` (default 60)
for Product views.

## SPA (Cloudflare Pages)

### GitHub secrets

| Secret | What it is |
| --- | --- |
| `CLOUDFLARE_API_TOKEN` | API token with the "Cloudflare Pages: Edit" permission. |
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare account id. |

### GitHub variables

| Variable | What it is |
| --- | --- |
| `CLOUDFLARE_PAGES_PROJECT` | Name of the Pages project (create it once with "Direct Upload"). |
| `VITE_API_URL` | Public URL of the API, for example `https://catalog-api-abc123.westeurope-01.azurewebsites.net` (the web app's **Default domain**). The build also writes it into `robots.txt` as the address of the sitemap index (`<API>/api/public-sitemap.xml`); submitting that address to Google Search Console is optional. |
| `VITE_SUPABASE_URL` | `https://<project>.supabase.co` |
| `VITE_SUPABASE_ANON_KEY` | Supabase publishable key (`sb_publishable_...`) or legacy anon key. It is public by design. |

### Deep links

The build has no top-level `404.html`, so Cloudflare Pages serves `index.html` for any path that is
not a file, and React Router handles deep links such as `/<tenantSlug>/p/<productSlug>` and
`/admin/products`. The workflow fails if a `404.html` ever appears in the build.

## Demo data

The pipeline never loads or resets demo data. The Demo seeder (`api/tools/Catalog.DemoSeeder`) is built and
tested by the API CI and run twice by the end-to-end job against the local stack, but it is only run against
production by a person, from their computer ([demo data](demo.md)). It is not part of the deployed API.

## Supabase

- Disable public sign-ups (Authentication → Providers → Email → "Allow new users to sign up" off).
- Add `https://<spa-domain>/accept-invite` to Authentication → URL Configuration → Redirect URLs, and
  set the Site URL to the SPA domain.
