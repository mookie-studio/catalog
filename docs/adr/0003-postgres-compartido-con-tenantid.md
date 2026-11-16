# Una única base de datos PostgreSQL compartida, aislada por TenantId

Todos los tenants comparten una sola base de datos PostgreSQL. Cada tabla de datos de negocio lleva el TenantId, y EF Core aplica un filtro global de consulta por tenant para que ninguna consulta pueda saltárselo por descuido. Se eligió así por coste y sencillez operativa: una sola migración, una sola conexión y nada de aprovisionar recursos por tenant.

## Considered Options

- **Una base de datos por tenant**: aislamiento más fuerte, descartada por el coste de operar y migrar N bases de datos.
- **Un esquema por tenant**: un punto intermedio, descartado por la misma complejidad de migraciones.

## Consequences

- Una fuga entre tenants solo puede venir de saltarse el filtro global (`IgnoreQueryFilters`), así que su uso debe ser excepcional y revisado.
- La unicidad por tenant (SKU, Identificador del producto) se implementa con índices únicos compuestos (TenantId, campo).
