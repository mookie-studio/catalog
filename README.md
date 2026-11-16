# Catalog

Plataforma generalista y multi-tenant de catálogos de productos: cada negocio (tenant) gestiona sus productos desde una parte de administración y sus visitantes los consultan desde un catálogo público. No vende: no hay precios, carrito, pedidos ni stock.

- **API**: .NET 10 con minimal APIs, organizada en vertical slices, EF Core sobre PostgreSQL.
- **Web**: SPA en React + Vite + TypeScript.

## Documentación

- [GLOSSARY.md](./GLOSSARY.md): el lenguaje del dominio. Úsalo en código y conversación.
- [docs/adr](./docs/adr): decisiones de arquitectura.
