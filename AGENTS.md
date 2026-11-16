# Agent guide

Read this before working on any issue. Issues are small (size S) and meant to be implemented one at a time.

## Before you start

1. Read `GLOSSARY.md`. Use its terms in code, tests, issues and PRs. Do not invent synonyms.
2. Read the ADRs in `docs/adr` that the issue mentions.
3. Check the issue's "Blocked by" list. Every blocking issue must be closed first.
4. All text in this repository (code, comments, docs, commits, issues, PRs) is in English.

## Repository layout

```
api/
  Catalog.slnx
  src/Catalog.Api/            # the only API project
  tests/Catalog.Api.Tests/    # HTTP-level integration tests
web/                          # React SPA (Vite + TypeScript)
docker-compose.yml            # local environment
.github/workflows/            # CI/CD
```

## API conventions (.NET 10, minimal APIs)

- **Vertical slices.** One file per use case under `Features/<Area>/<UseCase>.cs` (for example `Features/Admin/Products/CreateProduct.cs`). A slice holds its request/response records, its endpoint mapping and its logic. No service layers, repositories or MediatR.
- **Route groups** (ADR 0007): `/api/public/{tenantSlug}`, `/api/admin`, `/api/platform`. Each slice maps itself onto its group.
- **Data:** a single `CatalogDbContext` with entities in `Data/`. Tenant-scoped entities implement `ITenantScoped` (a `TenantId` property) and get a global query filter driven by `ICurrentTenant` (ADR 0003). Never call `IgnoreQueryFilters` outside `/api/platform`.
- **Ids** are `Guid` created with `Guid.CreateVersion7()`. **Dates** are `DateTimeOffset` in UTC.
- **Errors** are ProblemDetails: `TypedResults.ValidationProblem` for invalid input (400), `TypedResults.NotFound` for missing or other-tenant resources (404, never 403, so nothing leaks), `TypedResults.Conflict` for uniqueness violations (409).
- **Pagination:** `page` (1-based) and `pageSize` (default 20, max 50) query parameters; responses are `{ items, page, pageSize, totalCount }`.
- **Code names:** `Tenant`, `AdminMembership`, `Product`, `ProductStatus` (`Draft`, `Published`, `Archived`), `AttributeDefinition`, `AttributeType`, `AttributeOption`, `Category`, `ProductImage`.
- **Migrations:** add an EF Core migration in the same PR as the schema change. Never apply migrations at start-up.

## Testing conventions

- Test through HTTP only: call endpoints with the test `HttpClient` and assert on responses (and on later responses). Do not test internal classes or query the database directly in assertions.
- Tests use a real PostgreSQL via Testcontainers. If the environment variable `CATALOG_TEST_DB` is set, use that connection string instead (for environments without Docker).
- Only two fakes exist: the identity provider (tests mint their own JWTs and use a fake invitation sender) and image storage (in-memory).
- Every admin endpoint gets a tenant-isolation test: an admin of tenant A must get 404 for tenant B's resources.

## Web conventions

- TypeScript, React Router, TanStack Query, Tailwind + shadcn/ui.
- Call the API only through the generated client in `web/src/api`; never hand-write request types.
- After changing an endpoint, run `dotnet build` in `api/` (it rewrites `api/openapi/catalog.json`), then `npm run api:generate` in `web/` (it rewrites `src/api/schema.ts`). Commit both files; CI fails when either is out of date.
- Admin and platform areas are lazy-loaded routes; public pages must not import admin code.
- Component tests with Vitest + Testing Library only where there is real logic.

## Definition of done

- `dotnet build` and `dotnet test` pass in `api/`; `npm run build`, `npm run lint` and `npm test` pass in `web/` (for the parts that exist).
- The issue's acceptance criteria are covered by tests.
- The PR description references the issue (`Closes #N`).
