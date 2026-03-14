# ApiService Guidance

- Prefer thin controller files under `Controllers/` over adding more endpoint mappings in `Program.cs`.
- Keep controllers as simple as possible and delegate business logic to services.
- Prefer services to return and leverage `ServiceResponse<T>` for backend application/service-layer operations.
