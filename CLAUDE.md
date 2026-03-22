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

### Mobile (Flutter)
```bash
cd InitialAspireProject.Mobile
flutter pub get
dart run build_runner build --delete-conflicting-outputs  # code generation (freezed, json_serializable)
flutter analyze --no-fatal-infos
flutter test
flutter test --coverage  # generates coverage/lcov.info
```

## Architecture

This is a **.NET 9 Aspire** microservices solution with 8 projects (Aspire 13.1.2) plus a Flutter mobile app:

- **AppHost** — Aspire orchestrator. Defines all services, infrastructure (PostgreSQL, Redis), and their dependencies. Start here to understand the system topology.
- **Shared** — Class library with communication DTOs (`InitialAspireProject.Shared.Models`) and constants (`InitialAspireProject.Shared.Constants`) shared between ApiIdentity, ApiCore, Web, and Tests. Contains: `LoginModel`, `RegisterModel`, `ForgotPasswordModel`, `ResetPasswordModel`, `ConfirmEmailModel`, `LoginResponse`, `LoginErrorResponse`, `ErrorValidation`, `RolePermissionsDto`, `AssignPermissionModel`, `PermissionConstants`, `RefreshTokenRequest`, `RevokeTokenRequest`, `UpdateProfileModel`, `ChangePasswordModel`, `ProfileResponse`, `SessionConstants`.
- **ApiIdentity** — Auth service with ASP.NET Core Identity + JWT + refresh tokens. Manages users, roles, permissions, and token issuance. Endpoints: `POST /auth/register`, `POST /auth/login`, `GET /auth/profile`, `PUT /auth/profile`, `POST /auth/change-password`, `GET /auth/admin-only`, `POST /auth/forgot-password`, `POST /auth/reset-password`, `POST /auth/confirm-email`, `POST /auth/resend-activation`, `POST /auth/refresh`, `POST /auth/revoke`, `GET /permissions`, `GET /permissions/roles`, `GET /permissions/roles/{roleName}`, `POST /permissions/roles/{roleName}`, `DELETE /permissions/roles/{roleName}/{permission}`.
- **ApiCore** — Business API (currently WeatherForecast). Protected by JWT. Uses its own `coredb` PostgreSQL database. Swagger/OpenAPI via Swashbuckle.
- **Bff** — Backend-for-Frontend API gateway (`InitialAspireProject.Bff`). Proxies requests from the mobile app to ApiIdentity and ApiCore. Swagger UI with JWT auth, CORS for mobile, `Accept-Language` forwarding for localization. Controllers extend `BffControllerBase` (shared token extraction and response forwarding). Proxy services extend `BackendProxyService` (error handling, timeout → 504, connection failure → 503). BFF endpoints mirror the backend under `/api/auth/*`, `/api/profile/*`, `/api/weather`.
- **Web** — Blazor Server frontend. Authenticates via ASP.NET cookie session server-side; JWT is stored in `ISession` via `JwtAuthStateProvider`. Calls both APIs through typed HTTP clients.
- **ServiceDefaults** — Shared extension methods applied to all services: OpenTelemetry, service discovery, HTTP resilience, and localization (`AddLocalizationDefaults`, `UseLocalizationDefaults`).
- **Tests** — Unit + integration tests. Uses xunit.v3, bUnit, Moq, Bogus. CI excludes `WebTests` (Aspire integration tests).
- **Mobile** — Flutter/Dart cross-platform mobile app (`InitialAspireProject.Mobile`). Clean architecture (domain/data/presentation layers), Riverpod state management, Dio HTTP client with auth interceptor and automatic token refresh, GoRouter navigation with auth guards. Features: login, register, forgot/reset password, email confirmation, profile management, weather forecast. i18n via ARB files (pt-BR default, en, es).

### Authentication Flow
1. User POSTs credentials to `ApiIdentity /auth/login` → receives a JWT (15-min expiry) + refresh token (7-day expiry)
2. Web stores both tokens in **ASP.NET Session** (`SessionConstants.TokenKey`, `SessionConstants.RefreshTokenKey`) via `JwtAuthStateProvider`
3. `JwtAuthStateProvider` parses JWT claims, enforces expiry client-side, and provides `AuthenticationState` to Blazor
4. When JWT expires, `JwtAuthStateProvider` automatically calls `TokenRefreshService.TryRefreshAsync()` to obtain a new JWT + rotated refresh token via `POST /auth/refresh`
5. Authenticated Web services extend `AuthenticatedHttpService`, which attaches the Bearer token per-request and retries on 401 after auto-refresh
6. On logout, Web revokes the refresh token via `POST /auth/revoke` (best-effort) before clearing the session

### Service Communication
- Services use Aspire **service discovery** — URLs like `https+http://apiidentity` resolve at runtime
- `ServiceDefaults` configures `Microsoft.Extensions.Http.Resilience` for HTTP clients, and provides `AddPermissionPolicies()` for registering permission-based authorization policies
- Web registers typed HTTP clients: `LoginService`, `RegisterService`, `ForgotPasswordService`, `ResetPasswordService`, `ConfirmEmailService`, `PermissionService`, `ProfileService`, `TokenRefreshService` (all → `apiidentity`), and `WeatherApiService` (→ `apicore`)
- BFF registers `IIdentityProxyService` (→ `apiidentity`) and `ICoreProxyService` (→ `apicore`) via typed `HttpClient`; forwards `Accept-Language` and Bearer tokens
- Mobile → BFF → Backend: Mobile uses Dio HTTP client pointed at the BFF's external endpoint; BFF proxies to the internal services
- All HTTP request/response DTOs live in `InitialAspireProject.Shared.Models` — Web and BFF use these typed models (not anonymous objects) when calling APIs
- Mobile DTOs live in `Mobile/lib/features/*/data/models/` — request-only DTOs use `@JsonSerializable(createFactory: false)`, response-only DTOs use `@JsonSerializable(createToJson: false)`
- Web-only result types (`RegisterResult`, `ForgotPasswordResult`, `ResetPasswordResult`, `ConfirmEmailResult`) live in `Web/Services/ServiceModels.cs`
- `LoginResult` (with `Token`, `RefreshToken`, `ErrorCode`, `Success`, `IsEmailNotConfirmed`) is defined in `Web/Services/LoginService.cs`
- Web services use `BaseHttpService` (error handling, validation parsing) and `AuthenticatedHttpService` (auto Bearer token attachment, 401 retry with token refresh)

### Databases
- **identitydb** — PostgreSQL, used by `ApiIdentity` (ASP.NET Core Identity tables + `RefreshTokens` table)
- **coredb** — PostgreSQL, used by `ApiCore` (business entities)
- Both databases run EF Core migrations automatically on startup

### Infrastructure (AppHost)
- **PostgreSQL** container with two databases; PgAdmin available in development; host port 5432 in dev
- **Redis** container used for output caching in the Web project
- **Mailpit** container (`AddMailPit`) for local SMTP capture in development; web UI auto-opened by Aspire
- **BFF** project registered with external HTTP endpoints, service discovery references to both ApiIdentity and ApiCore, `WaitFor` dependencies
- Docker Compose environment configured via `AddDockerComposeEnvironment("compose")`
- All services expose `/health` HTTP health checks

### CI (GitHub Actions)
**.NET** (`ci.yml`):
- Runs on push/PR to `main`
- Builds in Release configuration using `InitialAspireProject.slnx`
- Runs unit tests with Coverlet (excludes `WebTests` integration tests, AppHost, ServiceDefaults, Program.cs, Migrations)
- Coverage gate: **fail below 40%**, warn below 80%
- Posts coverage summary as a PR comment

**Mobile** (`ci-mobile.yml`):
- Runs on push/PR to `main` (working directory: `InitialAspireProject.Mobile`)
- Steps: Flutter setup → `pub get` → `build_runner` code generation → `flutter analyze --no-fatal-infos` → `flutter test --coverage`
- Coverage converted via `lcov_cobertura`, same thresholds (fail below 40%, warn below 80%)
- Posts mobile coverage summary as a separate PR comment (`mobile-coverage` header)

### Test structure
- `Tests/ApiIdentity/` — AuthControllerTests, AuthControllerPasswordResetTests, SmtpEmailServiceTests, TokenServiceTests, SeederTests, ApplicationDbContextTests, PermissionControllerTests, RefreshTokenServiceTests
- `Tests/ApiCore/` — WeatherForecastControllerTests, WeatherForecastServiceTests, WeatherForecastDomainTests
- `Tests/Shared/` — PermissionConstantsTests
- `Tests/Web/` — Service tests: JwtAuthStateProviderTests, WeatherApiServiceTests, LoginServiceTests, RegisterServiceTests, ForgotPasswordServiceTests, ResetPasswordServiceTests, ConfirmEmailServiceTests, PermissionServiceTests, ProfileServiceTests, TokenRefreshServiceTests, ThemeServiceTests, WebMessagesTests. Page tests (bUnit): CounterTests, LoginPageTests, RegisterPageTests, ForgotPasswordPageTests, ResetPasswordPageTests, HomePageTests, WeatherPageTests, LogoutPageTests, SettingsPageTests, AdminPermissionsPageTests, ProfilePageTests. Integration: WebTests
- `Tests/Bff/` — AuthControllerTests, ProfileControllerTests, WeatherControllerTests, IdentityProxyServiceTests, CoreProxyServiceTests
- `Tests/Builders/` — Test data builders using Bogus (ApplicationUser, LoginModel, RegisterModel, ForgotPasswordModel, ResetPasswordModel, WeatherForecast)
- `Mobile/test/` — Flutter tests: `core/error/error_handler_test.dart`, `core/network/auth_interceptor_test.dart`, `core/storage/token_storage_test.dart`, `features/auth/data/repositories/auth_repository_impl_test.dart`, `features/profile/data/repositories/profile_repository_impl_test.dart`, `features/weather/data/repositories/weather_repository_impl_test.dart`. Uses `mocktail` for mocking.

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

### Refresh Token System
- `RefreshTokenService` generates cryptographically random 32-byte tokens, stores SHA256 hashes in `RefreshTokens` table (raw token never persisted)
- Each token belongs to a **family** (rotation chain); on use, the old token is revoked and a new one issued in the same family
- **Replay detection**: if a revoked token is reused, the entire token family is revoked (all sessions compromised by that chain)
- `RefreshToken` entity: `Id`, `TokenHash` (unique indexed), `UserId` (FK), `CreatedAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc`, `ReplacedByTokenHash`, `Family`, `DeviceInfo` (max 512 chars from User-Agent)
- Configuration: `RefreshToken:ExpiryDays` (default 7), `Jwt:AccessTokenExpiryMinutes` (default 15)
- Password change (`POST /auth/change-password`) and password reset (`POST /auth/reset-password`) revoke all user refresh tokens
- `POST /auth/refresh` error codes: `Expired`, `NotFound`, `ReplayDetected`
- `TokenRefreshService` (Web): calls `/auth/refresh`, updates session tokens; uses per-session semaphore to prevent concurrent refresh races

### Profile Management
- `GET /auth/profile` — returns `ProfileResponse` (Email, FullName, Roles)
- `PUT /auth/profile` — updates FullName via `UpdateProfileModel`
- `POST /auth/change-password` — validates current password, updates to new password, revokes all refresh tokens via `ChangePasswordModel`
- `ProfileService` (Web): typed HTTP client extending `AuthenticatedHttpService`
- `/profile` page (`Profile.razor`): edit name + change password forms with localized validation

### Claims/Permissions Authorization
- Permissions are stored as role claims in the existing `AspNetRoleClaims` table (no migration needed)
- `PermissionConstants` (`Shared/Constants/PermissionConstants.cs`): `CanViewSettings`, `CanManageUsers`, `CanViewReports`, `CanManagePermissions` with `ClaimType = "Permission"`
- `TokenService.CreateToken` accepts optional `IList<Claim>? permissionClaims` — these are included in the JWT
- `AuthController.Login` and `GoogleLogin` fetch role claims via `RoleManager.GetClaimsAsync`, deduplicate, and pass to `CreateToken`
- `Seeder.SeedPermissionsAsync`: Admin gets all permissions, User gets `CanViewReports` only (idempotent)
- `PermissionController` (`/permissions/*`): CRUD for managing role-permission assignments, protected by `CanManagePermissions` policy
- `ServiceDefaults.AddPermissionPolicies()`: registers `RequireClaim("Permission", permissionName)` policies for each permission — used by ApiIdentity, ApiCore, and Web
- `JwtAuthStateProvider.ParseClaimsFromJwt`: handles JSON array claims (multiple permissions serialized as a JSON array in JWT payload)
- Frontend: `NavMenu.razor` and `MainLayout.razor` use `<AuthorizeView Policy="CanViewSettings">` / `<AuthorizeView Policy="CanManagePermissions">` to show/hide Settings and Manage Permissions links
- Pages: `/settings` (`[Authorize(Policy = "CanViewSettings")]`), `/admin/permissions` (`[Authorize(Policy = "CanManagePermissions")]`), `/profile` (`[Authorize]`)
- `PermissionService` (Web): typed HTTP client for permission CRUD operations
- Token staleness: after admin changes permissions, users keep old JWT until it expires (15min) or is refreshed

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

**Mobile (Flutter):**
- ARB files: `Mobile/lib/l10n/app_pt.arb` (pt-BR default), `app_en.arb`, `app_es.arb`
- Generated localizations: `Mobile/lib/l10n/app_localizations.dart` (auto-generated by `flutter gen-l10n`)
- All screens use `AppLocalizations.of(context)!` for localized strings
- `LanguageInterceptor` in Dio automatically sends device locale as `Accept-Language` header to the BFF

### BFF (Backend-for-Frontend)
- `BffControllerBase` — abstract base with `GetBearerToken()`, `GetRequiredBearerToken()`, `GetAcceptLanguage()`, `ForwardResponse()`
- `BackendProxyService` — base proxy with `CreateForwardRequest()` (bearer + accept-language) and `SendAsync()` (error handling: `HttpRequestException` → 503, timeout → 504)
- `IdentityProxyService` / `CoreProxyService` — typed proxy services forwarding to backend APIs via Aspire service discovery
- Controllers: `AuthController` (`/api/auth/*`), `ProfileController` (`/api/profile/*`), `WeatherController` (`/api/weather`)
- `[Authorize]` on ProfileController and WeatherController at class level; Revoke endpoint on AuthController
- JWT validation uses same `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` config as ApiIdentity
- Swagger UI available at `/swagger` in development with Bearer auth support

### Mobile Architecture
- **Clean Architecture**: `features/{name}/domain/` (entities, repositories interfaces), `features/{name}/data/` (DTOs, repository implementations), `features/{name}/presentation/` (screens, widgets), `features/{name}/application/providers/` (Riverpod state management)
- **State Management**: Riverpod — `authStateProvider` (login/logout/token refresh), `weatherStateProvider`, `profileStateProvider`
- **Networking**: Dio HTTP client configured via `dioProvider` with 3 interceptors: `AuthInterceptor` (Bearer token attachment, 401 auto-refresh with Completer dedup), `LanguageInterceptor` (Accept-Language), `ErrorInterceptor` (logging)
- **Routing**: GoRouter via `routerProvider` — public routes (`/login`, `/register`, `/forgot-password`, `/reset-password`, `/confirm-email`), authenticated routes under `ShellRoute` with `AppScaffold` (`/weather`, `/profile`). Auth guard redirects unauthenticated users to `/login`
- **Token Storage**: `flutter_secure_storage` via `TokenStorage` class — stores access and refresh tokens securely
- **Error Handling**: `ErrorHandler.handle()` maps `DioException` types to sealed `Failure` classes (`NetworkFailure`, `UnauthorizedFailure`, `EmailNotConfirmedFailure`, `ValidationFailure`, `ServerFailure`, `UnknownFailure`). `Result<T>` type wraps success/failure
- **Code Generation**: `freezed` for immutable models (`.freezed.dart`), `json_serializable` for JSON (`.g.dart`). Run `dart run build_runner build --delete-conflicting-outputs` after changing models
- **Configuration**: `EnvConfig` reads `ENV` and `BASE_URL` from `--dart-define` compile-time constants. Default dev URL: `https://localhost:7040` (BFF). Staging/prod require `BASE_URL` to be set (assertion enforced)
