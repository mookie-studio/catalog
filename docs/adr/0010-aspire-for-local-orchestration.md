# .NET Aspire orchestrates local development only

.NET Aspire is used for **local development and observability only**. An AppHost (`api/src/Catalog.AppHost`) starts PostgreSQL, the S3-compatible image storage, the API and the SPA, and its dashboard shows their logs, traces and metrics. The API uses Aspire service defaults (`api/src/Catalog.ServiceDefaults`): OpenTelemetry, which exports only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, and the `/health` and `/alive` endpoints.

Deployment stays host-agnostic and outside Aspire: GitHub Actions publishes the API and deploys it to Azure App Service (ADR 0011), the SPA still goes to Cloudflare Pages, and migrations are still applied by the pipeline. There is no `azd` and no Aspire-generated infrastructure. Supabase Authentication keeps running locally through the Supabase CLI; its URL and keys are passed to the AppHost as parameters. Tests never start the AppHost, because cloud agent sessions have no Docker.

## Considered Options

- **Aspire with `azd` deploy**: rejected; it targets paid Azure services (Container Apps) and would tie deployment to Aspire.
- **docker compose only**: kept as the alternative local path and for the E2E job.

## Consequences

- Developers need Docker, the Supabase CLI and the .NET 10 SDK; no Aspire workload is needed (the AppHost SDK comes from NuGet).
- The AppHost must build without Docker so CI can build the whole solution.
- Running migrations locally is an explicit action in the dashboard; the API never applies them at start-up.
- `/health` (database included) and `/alive` reveal only a status word, so they are mapped in every environment and any host's probes can use them.
