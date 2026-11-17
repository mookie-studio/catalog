# Catalog

A general-purpose, multi-tenant product catalog platform: each business (tenant) manages its products from an admin area, and its visitors browse them in a public catalog. It does not sell: no prices, cart, orders or stock.

- **API**: .NET 10 with minimal APIs, organized in vertical slices, EF Core on PostgreSQL.
- **Web**: React + Vite + TypeScript SPA.

## Documentation

- [GLOSSARY.md](./GLOSSARY.md): the domain language. Use it in code and conversation.
- [docs/adr](./docs/adr): architecture decisions.
- [docs/local-development.md](./docs/local-development.md): running Catalog locally, with .NET Aspire (recommended, [ADR 0010](./docs/adr/0010-aspire-for-local-orchestration.md)) or docker compose.
- [docs/deployment.md](./docs/deployment.md): how CI deploys the API and the SPA.
