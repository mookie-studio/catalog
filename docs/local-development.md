# Local development

Everything runs locally in Docker: Supabase (PostgreSQL, authentication and an email inbox) through the
Supabase CLI, and the API, the SPA and S3-compatible image storage through `docker compose`.

## Requirements

- Docker
- [Supabase CLI](https://supabase.com/docs/guides/local-development/cli/getting-started)
- .NET 10 SDK with `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## 1. Start Supabase

```sh
supabase start
supabase status
```

`supabase/config.toml` disables public sign-up and enables the email testing inbox at
http://localhost:54324, where invitation emails arrive. Supabase Studio is at http://localhost:54323.

Copy `.env.example` to `.env` and fill in `SUPABASE_ANON_KEY`, `SUPABASE_SERVICE_ROLE_KEY` and
`SUPABASE_JWT_SECRET` from `supabase status`.

## 2. Apply the migrations

Migrations are never applied at start-up. Apply them to the local Supabase database:

```sh
cd api
ConnectionStrings__Catalog="Host=localhost;Port=54322;Database=postgres;Username=postgres;Password=postgres" \
  dotnet ef database update --project src/Catalog.Api
```

## 3. Create the first Platform Admin

1. In Supabase Studio, open **Authentication → Users → Add user**, and create a user with an email
   and password (tick "Auto confirm user").
2. Copy the user's id into `PLATFORM_ADMIN_USER_ID` in `.env`. The API reads it as
   `Auth:PlatformAdminUserIds`.

## 4. Start the API, the SPA and image storage

```sh
docker compose up --build
```

- SPA: http://localhost:5173 (sign in at `/login`, then go to `/platform`)
- API: http://localhost:8080/api/health
- Images: uploaded images are stored in the local S3-compatible store and served from
  http://localhost:9090/catalog-images/...

The SPA calls the API through the Vite dev server proxy (`/api`), so no CORS setup is needed locally.

## Running without Docker

- API: `dotnet run --project api/src/Catalog.Api` with the same settings as the `api` service in
  `docker-compose.yml` (as environment variables or user secrets).
- SPA: `cd web && cp .env.example .env.local && npm run dev`. Vite proxies `/api` to
  `http://localhost:5217` unless `API_PROXY_TARGET` says otherwise.
