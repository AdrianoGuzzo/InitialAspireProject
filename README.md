# Projeto ASP.NET Core Aspire com IdentityServer e PostgreSQL

Este projeto demonstra como configurar um sistema de autenticação completo usando ASP.NET Core Aspire, PostgreSQL, ASP.NET Core Identity e OpenIddict.

## Arquitetura

O projeto consiste em:

1. **InitialAspireProject.AppHost** - Orquestrador do Aspire que gerencia todos os serviços
2. **InitialAspireProject.ApiService** - API protegida que também atua como IdentityServer usando OpenIddict
3. **InitialAspireProject.Web** - Frontend web que consome a API protegida
4. **PostgreSQL** - Banco de dados para armazenar usuários e dados do OpenIddict
5. **Redis** - Cache distribuído
6. **pgAdmin** - Interface web para gerenciar o PostgreSQL

## Funcionalidades Implementadas

### IdentityServer (API Service)
- ✅ ASP.NET Core Identity para gerenciamento de usuários
- ✅ OpenIddict como servidor de autorização OAuth 2.0/OpenID Connect
- ✅ PostgreSQL como banco de dados
- ✅ Endpoints de autenticação (`/connect/authorize`, `/connect/token`)
- ✅ API protegida com JWT Bearer tokens
- ✅ Seed automático de cliente OAuth para desenvolvimento

### API Protegida
- ✅ Controller `WeatherController` protegido com `[Authorize]`
- ✅ Endpoint `/api/weather` que requer autenticação
- ✅ Validação de tokens JWT

### Frontend Web
- ✅ Autenticação OpenID Connect
- ✅ Integração com o IdentityServer
- ✅ Chamadas autenticadas para a API
- ✅ Endpoints de login/logout

### Banco de Dados
- ✅ PostgreSQL configurado via Aspire
- ✅ Migrações do Entity Framework
- ✅ Tabelas do ASP.NET Core Identity
- ✅ Tabelas do OpenIddict

## Como Executar

### Pré-requisitos
- .NET 8.0 SDK
- Docker (para PostgreSQL e Redis)

### Passos

1. **Restaurar dependências:**
   ```bash
   dotnet restore
   ```

2. **Executar o projeto:**
   ```bash
   dotnet run --project InitialAspireProject.AppHost
   ```

3. **Acessar os serviços:**
   - **Dashboard do Aspire:** https://localhost:15888
   - **API:** https://localhost:7001
   - **Web Frontend:** https://localhost:7002
   - **pgAdmin:** https://localhost:8080

## Testando a Autenticação

### 1. Usuário de Teste
O sistema cria automaticamente um usuário de teste:
- **Email:** test@example.com
- **Senha:** Test123!

### 2. Fluxo de Autenticação

1. Acesse o frontend web: https://localhost:7002
2. Clique em "Login" ou acesse: https://localhost:7002/login
3. Você será redirecionado para o IdentityServer
4. Use as credenciais de teste acima
5. Após o login, você será redirecionado de volta ao frontend
6. O frontend agora pode fazer chamadas autenticadas para a API

### 3. Testando a API Diretamente

Para testar a API diretamente, você precisa obter um token:

```bash
# 1. Obter token de acesso
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code&client_id=api-client&client_secret=api-secret&code=YOUR_AUTH_CODE"

# 2. Usar o token para acessar a API protegida
curl -X GET https://localhost:7001/api/weather \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## Configuração do Banco de Dados

### Conexão
O PostgreSQL é configurado automaticamente pelo Aspire com:
- **Host:** localhost
- **Porta:** 5432 (mapeada automaticamente)
- **Database:** identitydb
- **Username:** postgres
- **Password:** postgres

### Migrações
As migrações são aplicadas automaticamente na inicialização da aplicação.

## Estrutura do Projeto

```
InitialAspireProject/
├── InitialAspireProject.AppHost/          # Orquestrador Aspire
│   ├── AppHost.cs                         # Configuração dos serviços
│   └── InitialAspireProject.AppHost.csproj
├── InitialAspireProject.ApiService/       # IdentityServer + API
│   ├── Controllers/
│   │   ├── AuthorizationController.cs     # Endpoints OAuth/OIDC
│   │   └── WeatherController.cs           # API protegida
│   ├── Data/
│   │   └── ApplicationDbContext.cs        # Context do EF
│   ├── Models/
│   │   └── ApplicationUser.cs             # Modelo do usuário
│   ├── Services/
│   │   └── OpenIddictSeeder.cs           # Seed de clientes OAuth
│   └── Program.cs                         # Configuração da aplicação
├── InitialAspireProject.Web/              # Frontend web
│   ├── Components/                        # Componentes Blazor
│   ├── WeatherApiClient.cs               # Cliente HTTP autenticado
│   └── Program.cs                         # Configuração OIDC
└── InitialAspireProject.ServiceDefaults/  # Configurações compartilhadas
```

## Configurações Importantes

### OpenIddict Client
- **Client ID:** api-client
- **Client Secret:** api-secret
- **Scopes:** api, profile, email
- **Grant Types:** authorization_code, refresh_token

### URLs de Redirecionamento
- **Redirect URIs:** 
  - https://localhost:7001/signin-oidc
  - https://localhost:7002/signin-oidc
- **Post Logout Redirect URIs:**
  - https://localhost:7001/signout-callback-oidc
  - https://localhost:7002/signout-callback-oidc

## Troubleshooting

### Problemas Comuns

1. **Erro de conexão com PostgreSQL:**
   - Verifique se o Docker está rodando
   - Aguarde alguns segundos para o PostgreSQL inicializar

2. **Erro de certificado SSL:**
   - Para desenvolvimento, os certificados são gerados automaticamente
   - Use `dotnet dev-certs https --trust` se necessário

3. **Erro de autenticação:**
   - Verifique se as URLs de redirecionamento estão corretas
   - Confirme que o cliente OAuth foi criado corretamente

### Logs
Os logs detalhados estão disponíveis no Dashboard do Aspire e no console da aplicação.

## Próximos Passos

Para produção, considere:

1. **Certificados:** Use certificados SSL válidos em vez dos certificados de desenvolvimento
2. **Secrets:** Mova secrets para Azure Key Vault ou similar
3. **Banco de Dados:** Configure backup e alta disponibilidade para PostgreSQL
4. **Monitoramento:** Adicione Application Insights ou similar
5. **Segurança:** Implemente rate limiting, CORS adequado, etc.

## Recursos Adicionais

- [Documentação do ASP.NET Core Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Documentação do OpenIddict](https://documentation.openiddict.com/)
- [Documentação do ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)