# Atributos en JSONB y búsqueda con el full-text de PostgreSQL

Los Atributos de cada producto se guardan en una columna JSONB indexada (GIN), y la búsqueda del Visitante usa la búsqueda de texto nativa de PostgreSQL en español. Se evita así un modelo entidad-atributo-valor con una fila por atributo y un motor de búsqueda externo que habría que mantener y sincronizar.

## Considered Options

- **Tablas entidad-atributo-valor (EAV)**: más normalizado, descartado por la complejidad de consultas y filtros.
- **Motor de búsqueda externo (Elasticsearch, Meilisearch, Azure AI Search)**: descartado en fase 1; se reconsidera si el volumen o la relevancia lo exigen.

## Consequences

- La coherencia entre los valores en JSONB y las Definiciones de atributo la valida la API, no la base de datos.
