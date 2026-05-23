# Infrastructure Guidelines

## Local Development Stack
- **.NET Aspire** orchestrates all local services — use `AppHost` as the entry point
- **Cosmos DB**: Azure Cosmos DB Emulator via Aspire (`AddAzureCosmosDB(...).RunAsEmulator()`)
- **Event Hub**: Azurite emulator via Aspire (`AddAzureEventHubs(...).RunAsEmulator()`)
- **PostgreSQL**: Aspire container (`AddPostgres(...)`)

Never use real Azure resources for local development.

## Aspire AppHost conventions
- One resource name per logical service — names must be lowercase, no underscores
- Inject connection strings via Aspire references (`.WithReference(...)`) — never hardcode
- Use `RunAsEmulator()` for all Azure services locally
- Environment variables flow from AppHost to services automatically — don't duplicate them

## Configuration
- All connection strings and secrets come from environment or Aspire-injected config
- Never commit secrets or connection strings to source control
- Use `IOptions<T>` with `ValidateOnStart()` for typed config — fail fast at startup

## Cosmos DB (local)
- Emulator partition key rules still apply — always specify partition key on every operation
- Use `TenantId`/`PartnerId` as partition keys — never `PartitionKey.None`
- Container names match document type: `Endpoints`, `Alerts`, `Tenants`

## Event Hub (local)
- Azurite emulator supports basic send/receive — sufficient for local dev
- Consumer group names: service name (e.g. `fleetservice`)
- Checkpoint store: use Azurite blob storage via Aspire

## PostgreSQL (local)
- Aspire spins up a container automatically — no local install needed
- Apply migrations on startup (`app.Services.MigrateDbAsync()` or EF `MigrateAsync()`)
- Never use `sa`/`postgres` superuser credentials in app config — create a dedicated user

## Anti-patterns
- Hardcoded localhost ports → breaks when Aspire assigns dynamic ports
- `PartitionKey.None` on Cosmos queries → cross-partition scans, isolation breach
- Secrets in `appsettings.json` → use user secrets (`dotnet user-secrets`) instead
- Skipping `ValidateOnStart()` → config errors surface at runtime, not startup
