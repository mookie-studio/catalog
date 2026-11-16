# Catalog is multi-tenant from phase 1

A single Catalog installation hosts several businesses (tenants) isolated from each other, instead of deploying one instance per business. It is decided up front because adding tenant isolation later means touching every table, query and endpoint. The concrete isolation strategy and tenant resolution are recorded in ADRs 0003 and 0007.

## Considered Options

- **One catalog per deployment (single-tenant)**: simpler, rejected because the product must serve several businesses from one platform.
