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

This is a **.NET 9 Aspire** microservices solution with 7 projects (Aspire 13.1.2):

- **AppHost** — Aspire orchestrator. Defines all services, infrastructure (PostgreSQL, Redis), and their dependencies. Start here to understand the system topology.
- **Shared** — Class library with communication DTOs (`InitialAspireProject.Shared.Models`) shared between ApiIdentity, Web, and Tests. Contains: `LoginModel`, `RegisterModel`, `ForgotPasswordModel`, `ResetPasswordModel`, `ConfirmEmailModel`, `LoginResponse`, `LoginErrorResponse`, `ErrorValidation`.
- **ApiIdentity** — Auth service with ASP.NET Core Identity + JWT. Manages users, roles, and token issuance. Endpoints: `POST /auth/register`, `POST /auth/login`, `GET /auth/profile`, `GET /auth/admin-only`, `POST /auth/forgot-password`, `POST /auth/reset-password`, `POST /auth/confirm-email`, `POST /auth/resend-activation`.
- **ApiCore** — Business API (currently WeatherForecast). Protected by JWT. Uses its own `coredb` PostgreSQL database. Swagger/OpenAPI via Swashbuckle.
- **Web** — Blazor Server frontend. Authenticates via ASP.NET cookie session server-side; JWT is stored in `ISession` via `JwtAuthStateProvider`. Calls both APIs through typed HTTP clients.
- **ServiceDefaults** — Shared extension methods applied to all services: OpenTelemetry, service discovery, HTTP resilience, and localization (`AddLocalizationDefaults`, `UseLocalizationDefaults`).
- **Tests** — Unit + integration tests. Uses xunit.v3, bUnit, Moq, Bogus. CI excludes `WebTests` (Aspire integration tests).

### Authentication Flow
1. User POSTs credentials to `ApiIdentity /auth/login` → receives a JWT (1-hour expiry)
2. Web stores JWT in **ASP.NET Session** (`IHttpContextAccessor.HttpContext.Session`) via `JwtAuthStateProvider`
3. `JwtAuthStateProvider` parses JWT claims, enforces expiry client-side, and provides `AuthenticationState` to Blazor
4. `WeatherApiService` reads the token from session and attaches it as a Bearer header when calling `ApiCore`

### Service Communication
- Services use Aspire **service discovery** — URLs like `https+http://apiidentity` resolve at runtime
- `ServiceDefaults` configures `Microsoft.Extensions.Http.Resilience` for HTTP clients
- Web registers five typed HTTP clients: `LoginService`, `RegisterService`, `ForgotPasswordService`, `ResetPasswordService`, `ConfirmEmailService` (all → `apiidentity`), and `WeatherApiService` (→ `apicore`)
- All HTTP request/response DTOs live in `InitialAspireProject.Shared.Models` — Web services use these typed models (not anonymous objects) when calling APIs
- Web-only result types (`RegisterResult`, `ForgotPasswordResult`, `ResetPasswordResult`, `ConfirmEmailResult`) live in `Web/Services/ServiceModels.cs`
- `LoginResult` (with `Token`, `ErrorCode`, `Success`, `IsEmailNotConfirmed`) is defined in `Web/Services/LoginService.cs`

### Databases
- **identitydb** — PostgreSQL, used by `ApiIdentity` (ASP.NET Core Identity tables)
- **coredb** — PostgreSQL, used by `ApiCore` (business entities)
- Both databases run EF Core migrations automatically on startup

### Infrastructure (AppHost)
- **PostgreSQL** container with two databases; PgAdmin available in development; host port 5432 in dev
- **Redis** container used for output caching in the Web project
- **Mailpit** container (`AddMailPit`) for local SMTP capture in development; web UI auto-opened by Aspire
- Docker Compose environment configured via `AddDockerComposeEnvironment("compose")`
- All services expose `/health` HTTP health checks

### CI (GitHub Actions)
- Runs on push/PR to `main`
- Builds in Release configuration using `InitialAspireProject.slnx`
- Runs unit tests with Coverlet (excludes `WebTests` integration tests, AppHost, ServiceDefaults, Program.cs, Migrations)
- Coverage gate: **fail below 40%**, warn below 80%
- Posts coverage summary as a PR comment

### Test structure
- `Tests/ApiIdentity/` — AuthControllerTests, AuthControllerPasswordResetTests, SmtpEmailServiceTests, TokenServiceTests, SeederTests, ApplicationDbContextTests
- `Tests/ApiCore/` — WeatherForecastControllerTests, WeatherForecastServiceTests, WeatherForecastDomainTests
- `Tests/Web/` — Service tests: JwtAuthStateProviderTests, WeatherApiServiceTests, LoginServiceTests, RegisterServiceTests, ForgotPasswordServiceTests, ResetPasswordServiceTests, ConfirmEmailServiceTests, ThemeServiceTests, WebMessagesTests. Page tests (bUnit): CounterTests, LoginPageTests, RegisterPageTests, ForgotPasswordPageTests, ResetPasswordPageTests, HomePageTests, WeatherPageTests, LogoutPageTests. Integration: WebTests
- `Tests/Builders/` — Test data builders using Bogus (ApplicationUser, LoginModel, RegisterModel, ForgotPasswordModel, ResetPasswordModel, WeatherForecast)

### Password Reset Flow
1. User submits email to `POST /auth/forgot-password` — always returns a generic success message (anti-enumeration)
2. ApiIdentity generates an ASP.NET Core Identity token and emails a reset link via `SmtpEmailService` (MailKit)
3. Link format: `{App:BaseUrl}/reset-password?email=...&token=...`
4. User submits new password to `POST /auth/reset-password` — validates token via Identity and updates password
- `SmtpEmailService` uses `SecureSocketOptions.Auto` (STARTTLS on port 587, SSL on port 465)
- In development, emails are captured by Mailpit (no real email sent)
- In production, configure `SMTP_HOST`, `SMTP_PORT`, `SMTP_USE_SSL`, `SMTP_USERNAME`, `SMTP_PASSWORD`, and `APP_BASE_URL` in `.env`

### Email Activation Flow
1. User registers via `POST /auth/register` — account is created with `EmailConfirmed = false`
2. ApiIdentity generates an email confirmation token via `UserManager.GenerateEmailConfirmationTokenAsync` and sends an activation email via `SmtpEmailService.SendActivationEmailAsync`
3. Link format: `{App:BaseUrl}/confirm-email?email=...&token=...`
4. User clicks the link → `ConfirmEmail.razor` page automatically calls `POST /auth/confirm-email` with email + token
5. `AuthController.ConfirmEmail` validates the token via `UserManager.ConfirmEmailAsync` and activates the account
6. Login is blocked for unconfirmed emails — returns `{ code: "EmailNotConfirmed", message: "..." }`
7. Login page detects this and shows a warning with a "Resend activation link" button
8. `POST /auth/resend-activation` generates a new token and re-sends the activation email (anti-enumeration: always returns generic message)
- Seeded admin user has `EmailConfirmed = true` (unaffected)
- In development, activation emails are captured by Mailpit

### Seeded test credentials
- Email: `admin@localhost`
- Password: `Admin123$`
- Roles seeded: `Admin`, `User`

### Globalization / i18n
Supported cultures: **pt-BR** (default), **en**, **es**.

**Backend (ApiIdentity):**
- Resource files: `ApiIdentity/Resources/AuthMessages.resx` (pt-BR), `AuthMessages.en.resx`, `AuthMessages.es.resx`
- `AuthController` and `SmtpEmailService` inject `IStringLocalizer<AuthMessages>` — all response strings and email content are localized
- Culture resolved from `Accept-Language` header via `UseRequestLocalization` middleware

**Frontend (Web):**
- Resource files: `Web/Resources/WebMessages.resx` (pt-BR), `WebMessages.en.resx`, `WebMessages.es.resx`
- `WebMessages.cs` is NOT an empty marker class — it exposes `public static string` properties backed by `ResourceManager`; these are required by `DataAnnotations` `ErrorMessageResourceType` for localized `ValidationMessage` output
- `_Imports.razor` injects `IStringLocalizer<WebMessages> L` globally — all Razor components use `L["Key"]` without individual `@inject` declarations
- `DataAnnotations` attributes on form models use `ErrorMessageResourceType = typeof(WebMessages), ErrorMessageResourceName = "Key"` (not `ErrorMessage = "..."`) so `ValidationMessage` components respect the active culture
- Culture stored in a 1-year cookie (`.AspNetCore.Culture`) via `CookieRequestCultureProvider` registered as priority 0 in `Web/Program.cs`
- `/set-culture?culture=en&redirectUri=/path` minimal API endpoint writes the cookie and redirects; links use `data-enhance-nav="false"` to force a full page load so the new circuit picks up the cookie
- Language switcher dropdown in `NavMenu.razor` (sidebar footer)
