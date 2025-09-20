# Projeto ASP.NET Core Aspire com IdentityServer e PostgreSQL

Este projeto demonstra como configurar um sistema de autenticação completo usando ASP.NET Core Aspire, PostgreSQL, ASP.NET Core Identity.

## Arquitetura

O projeto consiste em:

1. **InitialAspireProject.AppHost** - Orquestrador do Aspire que gerencia todos os serviços
2. **InitialAspireProject.ApiIdentity** - API protegida que também atua como IdentityServer
3. **InitialAspireProject.Web** - Frontend web que consome a API protegida
4. **PostgreSQL** - Banco de dados para armazenar usuários e dados do OpenIddict
5. **Redis** - Cache distribuído
6. **pgAdmin** - Interface web para gerenciar o PostgreSQL

## Funcionalidades Implementadas

### IdentityServer (API Service)
- ✅ ASP.NET Core Identity para gerenciamento de usuários
- ✅ PostgreSQL como banco de dados
- ✅ API protegida com JWT Bearer tokens

### API Protegida
- ✅ Validação de tokens JWT

### Frontend Web
- ✅ Integração com o IdentityServer
- ✅ Chamadas autenticadas para a API
- ✅ Endpoints de login/logout

### Banco de Dados
- ✅ PostgreSQL configurado via Aspire
- ✅ Migrações do Entity Framework
- ✅ Tabelas do ASP.NET Core Identity

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

## Testando a Autenticação

### 1. Usuário de Teste
O sistema cria automaticamente um usuário de teste:
- **Email:** admin@localhost
- **Senha:** Admin123$

### 2. Fluxo de Autenticação

1. Acesse o frontend web
2. Clique em "Login" ou acesse
4. Use as credenciais de teste acima
5. Após o login, você será redirecionado de volta ao frontend
6. O frontend agora pode fazer chamadas autenticadas para a API

### Migrações
As migrações são aplicadas automaticamente na inicialização da aplicação.

## Troubleshooting

### Problemas Comuns

1. **Erro de conexão com PostgreSQL:**
   - Verifique se o Docker está rodando
   - Aguarde alguns segundos para o PostgreSQL inicializar

2. **Erro de certificado SSL:**
   - Para desenvolvimento, os certificados são gerados automaticamente
   - Use `dotnet dev-certs https --trust` se necessário

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
- [Documentação do ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)