# Catalog es multi-tenant desde la fase 1

Una misma instancia de Catalog aloja varios negocios (tenants) aislados entre sí, en lugar de desplegar una instancia por negocio. Se decide desde el principio porque añadir aislamiento por tenant a posteriori obliga a tocar cada tabla, consulta y endpoint. La estrategia de aislamiento concreta (base de datos compartida o por tenant) y cómo se identifica el tenant se decidirán en ADRs posteriores.

## Considered Options

- **Un catálogo por despliegue (single-tenant)**: más simple, descartado porque el producto debe servir a varios negocios desde una sola plataforma.
