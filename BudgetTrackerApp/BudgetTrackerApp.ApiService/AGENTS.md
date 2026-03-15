# ApiService Guidance

- Prefer thin controller files under `Controllers/` over adding more endpoint mappings in `Program.cs`.
- Never add application endpoints as minimal APIs in `Program.cs`; use controllers under `Controllers/` instead.
- Keep controllers as simple as possible and delegate business logic to services.
- Prefer services to return and leverage `ServiceResponse<T>` for backend application/service-layer operations.
