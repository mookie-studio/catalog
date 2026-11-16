# Attributes in JSONB and PostgreSQL full-text search

Each product's Attributes are stored in an indexed (GIN) JSONB column, and visitor search uses PostgreSQL's native full-text search with the Spanish configuration. This avoids an entity-attribute-value model with one row per attribute and an external search engine to run and keep in sync.

## Considered Options

- **Entity-attribute-value (EAV) tables**: more normalized, rejected for query and filter complexity.
- **External search engine (Elasticsearch, Meilisearch, Azure AI Search)**: rejected for phase 1; to be reconsidered if volume or relevance demand it.

## Consequences

- Consistency between JSONB values and Attribute Definitions is validated by the API, not the database.
