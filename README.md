# Initial Aspire Project

Um starter de microsserviços .NET 9 Aspire pronto para produção, com autenticação, autorização, redefinição de senha, globalização e observabilidade — pronto para clonar e estender.

## Arquitetura

Seis projetos orquestrados pelo .NET Aspire 13.1.2:

| Projeto | Função |
|---|---|
| **AppHost** | Orquestrador do Aspire — define todos os serviços, bancos de dados e infraestrutura |
| **ApiIdentity** | Serviço de autenticação: ASP.NET Core Identity + emissão de JWT, redefinição de senha, e-mail |
| **ApiCore** | API de negócio (WeatherForecast) — protegida por JWT |
| **Web** | Frontend Blazor Server — sessão por cookie, clientes HTTP tipados, seletor de idioma |
| **ServiceDefaults** | Extensões compartilhadas: OpenTelemetry, resiliência, descoberta de serviços, localização |
| **Tests** | Testes unitários e de integração (xunit.v3, bUnit, Moq, Bogus) |

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
- JWT Bearer tokens (validade de 1 hora) emitidos pelo ApiIdentity
- ASP.NET Core Identity com bloqueio de conta (5 tentativas, bloqueio de 5 minutos)
- Política de senha forte (mín. 8 caracteres, maiúscula, minúscula, dígito, não-alfanumérico)
- Perfis: `Admin`, `User` — criados automaticamente na inicialização
- Blazor Server armazena o JWT na **Sessão ASP.NET** (não no localStorage)
- Cookie `SecurePolicy = Always`

### Redefinição de Senha
- `POST /auth/forgot-password` — anti-enumeração: sempre retorna uma mensagem genérica de sucesso
- Token de identidade enviado por e-mail via MailKit (`SmtpEmailService`)
- Link de redefinição: `{App:BaseUrl}/reset-password?email=...&token=...`
- `POST /auth/reset-password` — valida o token e atualiza a senha

### Globalização / i18n
- Culturas suportadas: **pt-BR** (padrão), **en**, **es**
- Backend: cabeçalho `Accept-Language` → `IStringLocalizer<AuthMessages>` em `AuthController` e `SmtpEmailService`
- Frontend: cultura armazenada em cookie de 1 ano (`.AspNetCore.Culture`)
- Endpoint `/set-culture?culture=en&redirectUri=/path` grava o cookie e redireciona
- Seletor de idioma no menu lateral (NavMenu) — força recarga completa via `data-enhance-nav="false"`
- Componentes `ValidationMessage` exibem erros na cultura ativa via `ErrorMessageResourceType = typeof(WebMessages)`
- `IStringLocalizer<WebMessages> L` injetado globalmente em `_Imports.razor`

### Observabilidade
- Traces e métricas via OpenTelemetry configurados no ServiceDefaults
- Aspire Dashboard (logs estruturados, traces, métricas) em `http://localhost:18888` no desenvolvimento

### Segurança
- Rate limiting (10 req/min) em `/auth/login` e `/auth/register`
- Expiração do JWT verificada no lado do cliente pelo `JwtAuthStateProvider`
- `UseSession` posicionado antes de `UseAuthentication` no pipeline de middleware

---

## Como Executar

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para PostgreSQL, Redis, Mailpit)

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

---

## Fluxo de Autenticação

1. O usuário envia as credenciais para `POST /auth/login` (ApiIdentity) → recebe um JWT
2. O Web armazena o JWT na **Sessão ASP.NET** via `JwtAuthStateProvider`
3. O `JwtAuthStateProvider` interpreta os claims do JWT, verifica a expiração e fornece o `AuthenticationState` ao Blazor
4. O `WeatherApiService` lê o token da sessão e o anexa como cabeçalho `Bearer` nas chamadas ao ApiCore

---

## Endpoints da API

### ApiIdentity

| Método | Caminho | Auth | Descrição |
|---|---|---|---|
| POST | `/auth/register` | — | Registrar novo usuário |
| POST | `/auth/login` | — | Login, retorna JWT |
| GET | `/auth/profile` | JWT | Perfil do usuário atual |
| GET | `/auth/admin-only` | JWT + perfil Admin | Endpoint exclusivo para Admin |
| POST | `/auth/forgot-password` | — | Enviar e-mail de redefinição de senha |
| POST | `/auth/reset-password` | — | Redefinir senha com token |

### ApiCore

| Método | Caminho | Auth | Descrição |
|---|---|---|---|
| GET | `/weatherforecast` | JWT | Dados de clima aleatórios |

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

## Publicação em Produção (SSH + Docker Compose)

### Configuração inicial no servidor (única vez)

```bash
# 1. Configurar SSH sem senha
ssh-keygen -t ed25519 -C "deploy"
ssh-copy-id user@192.168.1.100

# 2. Adicionar usuário ao grupo docker no servidor
ssh user@192.168.1.100 "sudo usermod -aG docker \$USER"
# Faça logout e login novamente para aplicar

# 3. Criar o diretório da aplicação
ssh user@192.168.1.100 "mkdir -p ~/apps/initial-aspire"

# 4. Copiar e configurar o arquivo .env
scp .env.example user@192.168.1.100:~/apps/initial-aspire/.env
ssh user@192.168.1.100 "nano ~/apps/initial-aspire/.env"
```

### Publicar

```bash
# Enviar arquivos-fonte para o servidor (sem artefatos de build)
tar czf - \
  --exclude='*/bin' --exclude='*/obj' --exclude='*/.aspire' \
  InitialAspireProject.ApiCore \
  InitialAspireProject.ApiIdentity \
  InitialAspireProject.Web \
  InitialAspireProject.ServiceDefaults \
  docker-compose.yml \
  | ssh user@192.168.1.100 "tar xzf - -C ~/apps/initial-aspire"

# Build e iniciar
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose build --parallel && docker compose up -d"
```

### Atualizar após alterações no código

```bash
# Reenviar arquivos, reconstruir e reiniciar
tar czf - \
  --exclude='*/bin' --exclude='*/obj' \
  InitialAspireProject.ApiCore \
  InitialAspireProject.ApiIdentity \
  InitialAspireProject.Web \
  InitialAspireProject.ServiceDefaults \
  docker-compose.yml \
  | ssh user@192.168.1.100 "tar xzf - -C ~/apps/initial-aspire"

ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose build --parallel && docker compose up -d"
```

### URLs dos serviços

| Serviço | URL |
|---|---|
| Frontend Web | `http://ip-do-servidor:8081` (configurável via `WEB_PORT`) |
| Aspire Dashboard | `http://ip-do-servidor:18888` (requer `DASHBOARD_TOKEN`) |

### Serviços do Docker Compose

| Serviço | Descrição |
|---|---|
| `postgres` | PostgreSQL 17 |
| `redis` | Cache Redis |
| `apiidentity` | API de autenticação |
| `apicore` | API de negócio |
| `web` | Frontend Blazor Server |
| `dashboard` | Aspire Dashboard (OpenTelemetry) |

### Volumes persistentes

| Volume | Conteúdo |
|---|---|
| `postgres-data` | Dados do PostgreSQL |
| `web-keys` | Chaves de Data Protection do Web |
| `apiidentity-keys` | Chaves de Data Protection do ApiIdentity |
| `apicore-keys` | Chaves de Data Protection do ApiCore |

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
