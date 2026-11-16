# Infraestructura gratuita para la demo: Supabase, Cloudflare R2, Azure Container Apps y Cloudflare Pages

Para mantener la demo a coste cero, PostgreSQL vive en el proyecto de Supabase (el mismo que la autenticación), las imágenes en Cloudflare R2 servidas desde un dominio público de Cloudflare, la API .NET en Azure Container Apps con escalado a cero, y la SPA en Cloudflare Pages. Cada pieza se eligió por su plan gratuito y por no atarnos: la API accede a PostgreSQL con EF Core (no con el cliente de Supabase) y a R2 con su API compatible con S3, detrás de una interfaz propia de almacenamiento.

## Considered Options

- **Supabase Storage o Azure Blob para imágenes**: descartados por cuota gratuita menor (1 GB) o temporal (12 meses).
- **Azure App Service F1 para la API**: descartado por sus límites de CPU diarios.

## Consequences

- Con escalado a cero, la primera petición tras un rato sin tráfico tarda unos segundos.
- Las imágenes suben a través de la API (no con URL firmada): JPG, PNG o WebP, máximo 2 MB por imagen y 5 imágenes por producto, sin miniaturas en la fase 1.
- En local todo corre en Docker (docker compose): PostgreSQL y autenticación de Supabase, la API, la SPA y un almacenamiento compatible con S3 en lugar de R2, sin depender de servicios en la nube.
- CI con GitHub Actions en cada pull request; despliegue al fusionar en `main`, con las migraciones de EF Core aplicadas desde el pipeline y no al arrancar la API.
