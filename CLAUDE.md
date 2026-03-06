# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Run the application
```bash
dotnet run --project InitialAspireProject.AppHost
```
This starts all services via Aspire orchestration (APIs, Web, PostgreSQL, Redis, Dashboard).

### Build
```bash
dotnet build
```

### Run tests
```bash
dotnet test
# Single test
dotnet test --filter "FullyQualifiedName~TestName"
```

### Restore packages
```bash
dotnet restore
```

## Architecture

This is a **.NET 9 Aspire** microservices solution with 6 projects (Aspire 13.1.2):

- **AppHost** — Aspire orchestrator. Defines all services, infrastructure (PostgreSQL, Redis), and their dependencies. Start here to understand the system topology.
- **ApiIdentity** — Auth service with ASP.NET Core Identity + JWT. Manages users, roles, and token issuance. Endpoints: `POST /auth/register`, `POST /auth/login`, `GET /auth/profile`, `GET /auth/admin-only`.
- **ApiCore** — Business API (currently WeatherForecast). Protected by JWT. Uses its own `coredb` PostgreSQL database. Swagger/OpenAPI via Swashbuckle.
- **Web** — Blazor Server frontend. Authenticates via ASP.NET cookie session server-side; JWT is stored in `ISession` via `JwtAuthStateProvider`. Calls both APIs through typed HTTP clients.
- **ServiceDefaults** — Shared extension methods applied to all services: OpenTelemetry, service discovery, HTTP resilience.
- **Tests** — Unit + integration tests. Uses xunit.v3, bUnit, Moq, Bogus. CI excludes `WebTests` (Aspire integration tests).

### Authentication Flow
1. User POSTs credentials to `ApiIdentity /auth/login` → receives a JWT (1-hour expiry)
2. Web stores JWT in **ASP.NET Session** (`IHttpContextAccessor.HttpContext.Session`) via `JwtAuthStateProvider`
3. `JwtAuthStateProvider` parses JWT claims, enforces expiry client-side, and provides `AuthenticationState` to Blazor
4. `WeatherApiService` reads the token from session and attaches it as a Bearer header when calling `ApiCore`

### Service Communication
- Services use Aspire **service discovery** — URLs like `https+http://apiidentity` resolve at runtime
- `ServiceDefaults` configures `Microsoft.Extensions.Http.Resilience` for HTTP clients
- Web registers two typed HTTP clients: `apiidentity` and `apicore`

### Databases
- **identitydb** — PostgreSQL, used by `ApiIdentity` (ASP.NET Core Identity tables)
- **coredb** — PostgreSQL, used by `ApiCore` (business entities)
- Both databases run EF Core migrations automatically on startup

### Infrastructure (AppHost)
- **PostgreSQL** container with two databases; PgAdmin available in development; host port 5432 in dev
- **Redis** container used for output caching in the Web project
- Docker Compose environment configured via `AddDockerComposeEnvironment("compose")`
- All services expose `/health` HTTP health checks

### CI (GitHub Actions)
- Runs on push/PR to `main`
- Builds in Release configuration using `InitialAspireProject.slnx`
- Runs unit tests with Coverlet (excludes `WebTests` integration tests, AppHost, ServiceDefaults, Program.cs, Migrations)
- Coverage gate: **fail below 40%**, warn below 80%
- Posts coverage summary as a PR comment

### Test structure
- `Tests/ApiIdentity/` — AuthControllerTests, TokenServiceTests, SeederTests, ApplicationDbContextTests
- `Tests/ApiCore/` — WeatherForecastControllerTests, WeatherForecastServiceTests, WeatherForecastDomainTests
- `Tests/Web/` — JwtAuthStateProviderTests, WeatherApiServiceTests, LoginServiceTests, RegisterServiceTests, ThemeServiceTests, CounterTests (bUnit)
- `Tests/Builders/` — Test data builders using Bogus (ApplicationUser, LoginModel, RegisterModel, WeatherForecast)

### Seeded test credentials
- Email: `admin@localhost`
- Password: `Admin123$`
- Roles seeded: `Admin`, `User`
