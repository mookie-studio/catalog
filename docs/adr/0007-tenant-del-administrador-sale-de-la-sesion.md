# Tres grupos de rutas y el tenant del Administrador sale de su sesión

La API se divide en `/api/public/{tenant}/...` (Visitante, anónimo), `/api/admin/...` (Administrador) y `/api/platform/...` (Superadministrador). En las rutas de administración el tenant nunca viaja en la URL ni en el cuerpo: se obtiene de la relación usuario-tenant de nuestra base de datos (ADR 0004) y alimenta el filtro global de EF Core (ADR 0003). Así un Administrador no puede apuntar a otro tenant manipulando una petición.

## Consequences

- Solo `/api/platform` puede operar sobre varios tenants, y es el único sitio donde se admite saltarse el filtro global.
- Convenciones comunes: errores en ProblemDetails, paginación por página y tamaño (máximo 50), fechas en UTC.
