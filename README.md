# Initial Aspire Project

Um starter de microsserviços .NET 9 Aspire pronto para produção, com autenticação, autorização, redefinição de senha, globalização e observabilidade — inclui gateway BFF e app mobile Flutter — pronto para clonar e estender.

## Arquitetura

Oito projetos .NET orquestrados pelo Aspire 13.1.2, mais um app mobile Flutter:

| Projeto | Função |
|---|---|
| **AppHost** | Orquestrador do Aspire — define todos os serviços, bancos de dados e infraestrutura |
| **ApiIdentity** | Serviço de autenticação: ASP.NET Core Identity + emissão de JWT, refresh tokens, redefinição de senha, ativação por e-mail |
| **ApiCore** | API de negócio (WeatherForecast) — protegida por JWT |
| **Bff** | Gateway Backend-for-Frontend — proxy entre o app mobile e os backends (ApiIdentity/ApiCore), com Swagger, JWT, CORS e encaminhamento de `Accept-Language` |
| **Web** | Frontend Blazor Server — sessão por cookie, clientes HTTP tipados, seletor de idioma |
| **ServiceDefaults** | Extensões compartilhadas: OpenTelemetry, resiliência, descoberta de serviços, localização |
| **Shared** | DTOs e constantes compartilhados entre ApiIdentity, ApiCore, Web, Bff e Tests |
| **Tests** | Testes unitários e de integração (xunit.v3, bUnit, Moq, Bogus) |
| **Mobile** | App Flutter/Dart multiplataforma — clean architecture, Riverpod, Dio, GoRouter, i18n |

### Infraestrutura (desenvolvimento)

| Container | Finalidade |
|---|---|
| PostgreSQL | Dois bancos: `identitydb` (ASP.NET Identity) e `coredb` (dados de negócio) |
| Redis | Cache de saída para o projeto Web |
| Mailpit | Captura local de SMTP — intercepta todos os e-mails enviados em desenvolvimento |
| pgAdmin | Interface web para o PostgreSQL |

---

## Funcionalidades

### Autenticação e Autorização
- JWT access tokens (15 min) + refresh tokens (7 dias) com rotação e detecção de replay
- ASP.NET Core Identity com bloqueio de conta (5 tentativas, bloqueio de 5 minutos)
- Política de senha forte (mín. 8 caracteres, maiúscula, minúscula, dígito, não-alfanumérico)
- Perfis: `Admin`, `User` — criados automaticamente na inicialização
- Permissões baseadas em claims: `CanViewSettings`, `CanManageUsers`, `CanViewReports`, `CanManagePermissions`
- Blazor Server armazena o JWT na **Sessão ASP.NET** (não no localStorage)
- Mobile armazena tokens no `flutter_secure_storage`
- Cookie `SecurePolicy = Always`
- Ativação de conta por e-mail (link de confirmação obrigatório antes do login)

### Redefinição de Senha
- `POST /auth/forgot-password` — anti-enumeração: sempre retorna uma mensagem genérica de sucesso
- Token de identidade enviado por e-mail via MailKit (`SmtpEmailService`)
- Link de redefinição: `{App:BaseUrl}/reset-password?email=...&token=...`
- `POST /auth/reset-password` — valida o token e atualiza a senha

### Globalização / i18n
- Culturas suportadas: **pt-BR** (padrão), **en**, **es**
- Backend: cabeçalho `Accept-Language` → `IStringLocalizer<AuthMessages>` em `AuthController` e `SmtpEmailService`
- BFF: encaminha o cabeçalho `Accept-Language` do cliente para os backends
- Frontend Web: cultura armazenada em cookie de 1 ano (`.AspNetCore.Culture`)
- Endpoint `/set-culture?culture=en&redirectUri=/path` grava o cookie e redireciona
- Seletor de idioma no menu lateral (NavMenu) — força recarga completa via `data-enhance-nav="false"`
- Componentes `ValidationMessage` exibem erros na cultura ativa via `ErrorMessageResourceType = typeof(WebMessages)`
- `IStringLocalizer<WebMessages> L` injetado globalmente em `_Imports.razor`
- Mobile: arquivos ARB (`app_pt.arb`, `app_en.arb`, `app_es.arb`) com `LanguageInterceptor` no Dio para enviar o locale do dispositivo

### Observabilidade
- Traces e métricas via OpenTelemetry configurados no ServiceDefaults
- Aspire Dashboard (logs estruturados, traces, métricas) em `http://localhost:18888` no desenvolvimento

### Segurança
- Rate limiting (10 req/min) em `/auth/login` e `/auth/register`
- Expiração do JWT verificada no lado do cliente pelo `JwtAuthStateProvider`
- `UseSession` posicionado antes de `UseAuthentication` no pipeline de middleware
- Refresh tokens armazenados como hash SHA-256 (token bruto nunca persistido)
- Detecção de replay: reutilização de refresh token revogado invalida toda a família de tokens
- Alteração/redefinição de senha revoga todos os refresh tokens do usuário

---

## Como Executar

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para PostgreSQL, Redis, Mailpit)
- [Flutter SDK](https://flutter.dev/docs/get-started/install) (apenas para o app mobile)

### Executar

```bash
dotnet run --project InitialAspireProject.AppHost
```

O Aspire inicia todos os serviços e abre o Dashboard. O frontend Web fica disponível na URL exibida no Dashboard.

### Credenciais padrão

| Campo | Valor |
|---|---|
| E-mail | `admin@localhost` |
| Senha | `Admin123$` |
| Perfis | `Admin`, `User` |

As migrações do EF Core são aplicadas automaticamente na inicialização — nenhuma etapa manual é necessária.

### Build

```bash
dotnet build
```

### Executar testes

```bash
dotnet test

# Executar um teste específico
dotnet test --filter "FullyQualifiedName~NomeDoTeste"
```

### Mobile (Flutter)

```bash
cd InitialAspireProject.Mobile
flutter pub get
dart run build_runner build --delete-conflicting-outputs  # geração de código (freezed, json_serializable)
flutter analyze --no-fatal-infos
flutter test
```

> **Nota:** O app mobile aponta por padrão para `https://localhost:7040` (BFF). Em emuladores Android use `10.0.2.2` ou a IP da rede local. Sobrescreva com `--dart-define=BASE_URL=https://seu-ip:7040`.

---

## Fluxo de Autenticação

**Web (Blazor Server):**
1. O usuário envia as credenciais para `POST /auth/login` (ApiIdentity) → recebe JWT + refresh token
2. O Web armazena ambos os tokens na **Sessão ASP.NET** via `JwtAuthStateProvider`
3. O `JwtAuthStateProvider` interpreta os claims do JWT, verifica a expiração e fornece o `AuthenticationState` ao Blazor
4. Quando o JWT expira, `TokenRefreshService.TryRefreshAsync()` obtém novos tokens via `POST /auth/refresh`
5. Serviços autenticados estendem `AuthenticatedHttpService`, que anexa o Bearer token e tenta refresh no 401

**Mobile (Flutter):**
1. O app envia as credenciais para `POST /api/auth/login` (BFF) → BFF encaminha para ApiIdentity → retorna JWT + refresh token
2. Tokens armazenados no `flutter_secure_storage` via `TokenStorage`
3. `AuthInterceptor` no Dio anexa o Bearer token automaticamente em rotas não-públicas
4. No 401, o interceptor tenta refresh com `Completer` para deduplicar chamadas concorrentes
5. Se o refresh falha, `onForceLogout` limpa os tokens e redireciona para `/login`

---

## Endpoints da API

### ApiIdentity

| Método | Caminho | Auth | Descrição |
|---|---|---|---|
| POST | `/auth/register` | — | Registrar novo usuário |
| POST | `/auth/login` | — | Login, retorna JWT + refresh token |
| POST | `/auth/refresh` | — | Renovar JWT com refresh token |
| POST | `/auth/revoke` | JWT | Revogar refresh token (logout) |
| GET | `/auth/profile` | JWT | Perfil do usuário atual |
| PUT | `/auth/profile` | JWT | Atualizar nome do perfil |
| POST | `/auth/change-password` | JWT | Alterar senha (revoga todos os refresh tokens) |
| GET | `/auth/admin-only` | JWT + Admin | Endpoint exclusivo para Admin |
| POST | `/auth/forgot-password` | — | Enviar e-mail de redefinição de senha |
| POST | `/auth/reset-password` | — | Redefinir senha com token |
| POST | `/auth/confirm-email` | — | Confirmar e-mail com token de ativação |
| POST | `/auth/resend-activation` | — | Reenviar e-mail de ativação |
| GET | `/permissions/roles` | JWT + CanManagePermissions | Listar perfis e permissões |
| POST | `/permissions/roles/{role}` | JWT + CanManagePermissions | Atribuir permissão a um perfil |
| DELETE | `/permissions/roles/{role}/{perm}` | JWT + CanManagePermissions | Remover permissão de um perfil |

### ApiCore

| Método | Caminho | Auth | Descrição |
|---|---|---|---|
| GET | `/weatherforecast` | JWT | Dados de clima aleatórios |

### BFF (Gateway para Mobile)

O BFF espelha os endpoints sob o prefixo `/api/`:

| Método | Caminho | Auth | Descrição |
|---|---|---|---|
| POST | `/api/auth/login` | — | Proxy para ApiIdentity |
| POST | `/api/auth/register` | — | Proxy para ApiIdentity |
| POST | `/api/auth/refresh` | — | Proxy para ApiIdentity |
| POST | `/api/auth/revoke` | JWT | Proxy para ApiIdentity |
| POST | `/api/auth/forgot-password` | — | Proxy para ApiIdentity |
| POST | `/api/auth/reset-password` | — | Proxy para ApiIdentity |
| POST | `/api/auth/confirm-email` | — | Proxy para ApiIdentity |
| POST | `/api/auth/resend-activation` | — | Proxy para ApiIdentity |
| GET | `/api/profile` | JWT | Proxy para ApiIdentity |
| PUT | `/api/profile` | JWT | Proxy para ApiIdentity |
| POST | `/api/profile/change-password` | JWT | Proxy para ApiIdentity |
| GET | `/api/weather` | JWT | Proxy para ApiCore |

---

## Arquitetura do App Mobile

O projeto `InitialAspireProject.Mobile` segue **Clean Architecture** com 3 camadas por feature:

```
lib/
├── core/               # Infraestrutura compartilhada
│   ├── config/         # EnvConfig (BASE_URL via --dart-define)
│   ├── constants/      # ApiConstants, StorageKeys
│   ├── error/          # Failures, Result<T>, ErrorHandler
│   ├── network/        # DioProvider, AuthInterceptor, LanguageInterceptor, ErrorInterceptor
│   ├── router/         # GoRouter com auth guards
│   └── storage/        # TokenStorage (flutter_secure_storage)
├── features/
│   ├── auth/           # Login, registro, redefinição de senha, ativação por e-mail
│   ├── profile/        # Editar perfil, alterar senha
│   └── weather/        # Previsão do tempo
├── l10n/               # Arquivos ARB (pt, en, es)
└── shared/widgets/     # AppScaffold, LoadingOverlay, campos de formulário
```

**Gerenciamento de estado:** Riverpod — `authStateProvider`, `weatherStateProvider`, `profileStateProvider`

**Navegação:** GoRouter — rotas públicas (`/login`, `/register`, `/forgot-password`, `/reset-password`, `/confirm-email`) e rotas autenticadas sob `ShellRoute` (`/weather`, `/profile`)

**Geração de código:** `freezed` (modelos imutáveis) + `json_serializable` (JSON). Executar `dart run build_runner build --delete-conflicting-outputs` após alterar models.

---

## Variáveis de Ambiente (Produção)

Copie `.env.example` para `.env` e preencha os valores antes de publicar:

```env
POSTGRES_PASSWORD=senha-forte
REDIS_PASSWORD=senha-redis
JWT_KEY=chave-secreta-com-no-minimo-32-caracteres
JWT_ISSUER=https://seu-dominio.com
JWT_AUDIENCE=https://seu-dominio.com
WEB_PORT=8081
DASHBOARD_TOKEN=token-secreto-do-dashboard
SMTP_HOST=smtp.exemplo.com
SMTP_PORT=587
SMTP_USE_SSL=false
SMTP_USERNAME=usuario@exemplo.com
SMTP_PASSWORD=senha-smtp
APP_BASE_URL=https://seu-dominio.com
```

---

## Troubleshooting

**Erro de conexão com PostgreSQL** — verifique se o Docker está em execução e aguarde alguns segundos para o container ficar saudável.

**Erro de certificado SSL** — execute `dotnet dev-certs https --trust` para confiar no certificado de desenvolvimento.

**E-mails não chegando em desenvolvimento** — verifique o Mailpit (link exibido no Aspire Dashboard). Todo o tráfego SMTP é capturado lá em desenvolvimento.

**Idioma não muda** — o seletor usa um redirecionamento completo via `/set-culture`. Se a página continuar no idioma antigo, verifique se os cookies estão habilitados e se o cookie `.AspNetCore.Culture` está sendo gravado.

---

## Recursos

- [Documentação do .NET Aspire](https://learn.microsoft.com/pt-br/dotnet/aspire/)
- [ASP.NET Core Identity](https://learn.microsoft.com/pt-br/aspnet/core/security/authentication/identity)
- [Autenticação no Blazor Server](https://learn.microsoft.com/pt-br/aspnet/core/blazor/security/server/)
- [Localização no ASP.NET Core](https://learn.microsoft.com/pt-br/aspnet/core/fundamentals/localization)
- [Flutter](https://flutter.dev/docs)
- [Riverpod](https://riverpod.dev/)
- [GoRouter](https://pub.dev/packages/go_router)
