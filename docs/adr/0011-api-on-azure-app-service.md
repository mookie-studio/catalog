# The API runs on Azure App Service (Free F1), deployed as code

This supersedes the API-hosting part of ADR 0005. Azure Container Apps is not free for this demo, so the .NET API runs on **Azure App Service, Linux, Free (F1) plan**, deployed as code: GitHub Actions runs `dotnet publish` and uploads the output with `azure/webapps-deploy`, after applying EF Core migrations with a migrations bundle. Everything else in ADR 0005 stays: Supabase (PostgreSQL and Authentication), Cloudflare R2 for images and Cloudflare Pages for the SPA.

## Considered Options

- **Azure Container Apps**: rejected because it is not free beyond the monthly grant.
- **App Service with a custom container**: rejected because custom containers need a paid plan.
- **Other hosts (Fly.io, Render)**: rejected; Mookie chose Azure.

## Consequences

- F1 gives 60 CPU minutes per day and 1 GB of memory, with no Always On: the app unloads after about 20 idle minutes and the first request afterwards is slow. Good enough for a demo, not for real traffic.
- No deployment slots: a deploy replaces the running app.
- App Service tells the API which port to listen on; the API must not hard-code a port. The Dockerfile (port 8080) stays only for docker compose and the E2E job.
- Native libraries must ship inside the publish output (for example SkiaSharp's `NoDependencies` Linux assets, ADR 0008), since the host image cannot be customised.
- Settings are App Service application settings, using `__` for nested keys (`ConnectionStrings__Catalog`).
