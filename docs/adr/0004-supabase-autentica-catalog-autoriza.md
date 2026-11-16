# Supabase Authentication autentica; Catalog autoriza

Para la demo, la identidad de Superadministradores y Administradores la gestiona Supabase Authentication: login, contraseñas e invitaciones por email, con el registro público desactivado. La API solo valida el token de Supabase. Qué tenant y qué rol tiene cada persona lo guarda nuestra propia base de datos, en una tabla que relaciona el id de usuario de Supabase con su tenant y su rol. Así cambiar de proveedor de identidad solo afecta a la validación del token y al flujo de invitación.

## Considered Options

- **ASP.NET Core Identity**: sin dependencias externas, descartado para la demo por el trabajo de construir login, recuperación de contraseña e invitaciones.
- **Roles y tenant como claims dentro de Supabase**: descartado porque ata la autorización al proveedor.

## Consequences

- El alta de un administrador la hace nuestra API llamando a la invitación de Supabase con la clave de servicio, que nunca llega al front.
- No se usan las políticas RLS de Supabase: el aislamiento por tenant lo garantiza la API (ADR 0003).
