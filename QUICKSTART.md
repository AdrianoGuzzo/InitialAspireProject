# Início Rápido - IdentityServer com PostgreSQL

## 🚀 Como executar

1. **Executar o projeto:**
   ```bash
   dotnet run --project InitialAspireProject.AppHost
   ```

2. **Acessar os serviços:**
   - **Dashboard Aspire:** https://localhost:15888
   - **Frontend Web:** https://localhost:7002
   - **API:** https://localhost:7001

## 🔐 Teste de Autenticação

1. Acesse: https://localhost:7002
2. Clique em "Login" ou vá para: https://localhost:7002/login
3. Credenciais de teste:
   - **Email:** test@example.com
   - **Senha:** Test123!

## 🧪 Teste da API

```bash
# Obter token
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=api-client&client_secret=api-secret&scope=api"

# Usar token (substitua YOUR_TOKEN)
curl -X GET https://localhost:7001/api/weather \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## ✅ Funcionalidades

- ✅ PostgreSQL + pgAdmin
- ✅ ASP.NET Core Identity
- ✅ OpenIddict (OAuth 2.0/OIDC)
- ✅ API protegida com JWT
- ✅ Frontend com autenticação
- ✅ Migrações automáticas