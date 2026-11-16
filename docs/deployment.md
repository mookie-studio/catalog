# Deployment

Merging to `main` deploys (ADR 0005):

- `api/**` changes run `.github/workflows/deploy-api.yml`: build the API image and push it to GitHub
  Container Registry, apply EF Core migrations with a migrations bundle against the Supabase database,
  then update the Azure Container App to the new image. Migrations run before the new revision goes live.
- `web/**` changes run `.github/workflows/deploy-web.yml`: build the SPA with the production `VITE_*`
  variables and deploy it with `wrangler pages deploy`.

Both workflows can also be started by hand (`workflow_dispatch`). They use the GitHub environment
`production`; add the secrets and variables below to that environment. No secret is stored in the repository.
Until `AZURE_CONTAINER_APP_NAME` (API) or `CLOUDFLARE_PAGES_PROJECT` (SPA) is set, the workflows are skipped.

Variables used in `if:` conditions must be repository variables (or organization variables), because
environment variables are only visible to jobs that already run in that environment.

## API (Azure Container Apps)

### GitHub secrets

| Secret | What it is |
| --- | --- |
| `AZURE_CLIENT_ID` | Client id of the Entra ID app (or managed identity) with a federated credential for this repository's `production` environment (OIDC, no password). It needs the Contributor role on the Container App's resource group. |
| `AZURE_TENANT_ID` | Entra ID tenant id. |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription id. |
| `DATABASE_CONNECTION_STRING` | Npgsql connection string of the Supabase database, used to apply migrations (for example `Host=db.<project>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=...;SSL Mode=Require`). |

### GitHub variables

| Variable | What it is |
| --- | --- |
| `AZURE_CONTAINER_APP_NAME` | Name of the Container App. |
| `AZURE_RESOURCE_GROUP` | Its resource group. |

### Container App settings

Configure these once on the Container App (as secrets referenced by environment variables). The
workflow only changes the image.

| Environment variable | Value |
| --- | --- |
| `ConnectionStrings__Catalog` | Supabase database connection string (pooler recommended). |
| `Auth__Issuer` | `https://<project>.supabase.co/auth/v1` |
| `Auth__JwksUrl` | `https://<project>.supabase.co/auth/v1/.well-known/jwks.json` (or `Auth__JwtSecret` for a legacy HS256 secret) |
| `Auth__PlatformAdminUserIds__0` | Supabase user id of a Platform Admin (add `__1`, `__2`… for more). |
| `Supabase__Url` | `https://<project>.supabase.co` |
| `Supabase__ServiceRoleKey` | Supabase service role key. Never exposed to the browser. |
| `Supabase__InviteRedirectUrl` | `https://<spa-domain>/accept-invite` |
| `Storage__ServiceUrl` | `https://<account-id>.r2.cloudflarestorage.com` |
| `Storage__Bucket` | R2 bucket name. |
| `Storage__AccessKey` / `Storage__SecretKey` | R2 API token credentials. |
| `Storage__PublicBaseUrl` | Public URL of the bucket (custom domain or `r2.dev`). |
| `Storage__Region` | `auto` |
| `Cors__AllowedOrigins__0` | `https://<spa-domain>` |

The image runs as a non-root user and listens on port 8080 (set the ingress target port to 8080).
The registry is `ghcr.io/<owner>/<repo>/api`; if the package is private, give the Container App
registry credentials for GHCR.

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
| `VITE_API_URL` | Public URL of the API, for example `https://catalog-api.<region>.azurecontainerapps.io`. |
| `VITE_SUPABASE_URL` | `https://<project>.supabase.co` |
| `VITE_SUPABASE_ANON_KEY` | Supabase anon (publishable) key. It is public by design. |

### Deep links

The build has no top-level `404.html`, so Cloudflare Pages serves `index.html` for any path that is
not a file, and React Router handles deep links such as `/<tenantSlug>/p/<productSlug>` and
`/admin/products`. The workflow fails if a `404.html` ever appears in the build.

## Supabase

- Disable public sign-ups (Authentication → Providers → Email → "Allow new users to sign up" off).
- Add `https://<spa-domain>/accept-invite` to Authentication → URL Configuration → Redirect URLs, and
  set the Site URL to the SPA domain.
