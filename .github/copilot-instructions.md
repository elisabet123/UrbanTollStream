# Copilot instructions for UrbanTollStream

Purpose
- Short, actionable guidance to help Copilot-based assistants work effectively in this repository.
- Incorporates key parts of existing docs under .github/instructions/ .

Repository snapshot (detected files used to create this guide)
- .github/instructions/csharp.md — C# code, style, testing and architecture notes
- .github/instructions/infrastructure.md — Aspire/AppHost, local emulators, Cosmos/Postgres/EventHubs guidance


1) Build, test, and lint commands (concrete .NET guidance)
- Build solution: dotnet build
- Run full test suite (solution or test project): dotnet test
- Run a specific test project: dotnet test src/YourProject.Tests/YourProject.Tests.csproj
- Run a single test by fully-qualified name:
  dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
- Run a single test by partial name or display name:
  dotnet test --filter "DisplayName~'partial test name'"
- If tests require Aspire emulators or containers, start the AppHost/emulator runner first (see infra docs) then run dotnet test against the test project.
- Code formatting / basic fix: dotnet format

When CI/workflow files are added, prefer the exact commands used there.

2) High-level architecture (what to look for)
- Technology: .NET services using Aspire (AppHost) with Clean Architecture + CQRS patterns.
- Entrypoint: AppHost (Aspire) orchestrates local infra and service startup. AppHost.cs is the main file to understand for local development flow.
- Local infra: Cosmos DB (emulator), Event Hubs (Azurite via Aspire), PostgreSQL (Aspire container). Aspire wiring calls like AddAzureCosmosDB(...).RunAsEmulator() are authoritative for local behavior.
- Data and boundaries: Cosmos containers named by document type (Endpoints, Alerts, Tenants). Use TenantId/PartnerId as partition keys.
- Messaging: Event Hub consumer group names are service names; checkpoint store uses Azurite blobs.

3) Key repository-specific conventions
- Aspire/AppHost conventions:
  - Use AppHost as single orchestration entrypoint.
  - Use RunAsEmulator() for local Azure resources; do not point to live Azure in local configs.
  - Resource names: lowercase, no underscores.
  - Inject connection strings via Aspire references (.WithReference(...)), never hardcode secrets.
- Configuration:
  - Prefer IOptions<T> with ValidateOnStart() to fail fast on invalid config.
  - Keep secrets out of source control; use user-secrets for local sensitive values.
- Database/Cosmos rules:
  - Always specify partition keys. Avoid PartitionKey.None.
  - Container names mirror document types.
- Testing:
  - xUnit v3, Shouldly, NSubstitute conventions: tests have DisplayName in Given/When/Then style.
  - Mock only external/platform dependencies; prefer real in-memory/local emulators for infra.
- DI and code style:
  - Internal by default; public when required for DI/hosting contracts.
  - Async methods end with Async. One type per file; filename matches type.

4) How Copilot assistants should start
- Open these files first: .github/copilot-instructions.md, README.md, then .github/instructions/*.md, then project manifests (Directory.Build.props, *.csproj, global.json) and Program/Main files.
- If CI workflows exist, use them for exact build/test commands.
- For local infra-aware tests, prefer running Aspire/AppHost/emulator steps before tests.

5) Maintenance & updates
- When adding new languages/tooling, add a short section under "Build, test, and lint commands" with exact commands and how to run a single test.
- Keep this file minimal and factual: exact commands and repository-specific conventions only.
