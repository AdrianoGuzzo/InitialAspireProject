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

---

## Publicação via SSH com Docker Compose

Este guia descreve o processo completo para publicar o projeto em um servidor Linux remoto via SSH usando Docker Compose, sem necessidade de um registro de imagens (as imagens são construídas direto no servidor).

### Pré-requisitos no servidor

- Linux (Ubuntu/Debian recomendado)
- Docker instalado
- Usuário com acesso ao Docker (`docker` group)
- Acesso SSH configurado (preferencialmente com chave, sem senha)

### 1. Configurar acesso SSH sem senha (uma única vez)

No seu computador local, gere um par de chaves SSH e copie a chave pública para o servidor:

```bash
ssh-keygen -t ed25519 -C "deploy"
ssh-copy-id user@192.168.1.100
```

Teste o acesso:

```bash
ssh user@192.168.1.100 "echo ok"
```

### 2. Adicionar o usuário ao grupo Docker (uma única vez)

Conecte-se ao servidor e adicione o usuário ao grupo `docker` para evitar `permission denied` ao executar comandos Docker sem `sudo`:

```bash
ssh user@192.168.1.100
sudo usermod -aG docker $USER
# Faça logout e login novamente para aplicar
exit
```

### 3. Criar o diretório da aplicação no servidor

```bash
ssh user@192.168.1.100 "mkdir -p ~/apps/initial-aspire"
```

### 4. Configurar as variáveis de ambiente no servidor

Copie o arquivo de exemplo `.env.example` para `.env` no servidor e preencha os valores:

```bash
scp .env.example user@192.168.1.100:~/apps/initial-aspire/.env
ssh user@192.168.1.100 "nano ~/apps/initial-aspire/.env"
```

Exemplo de `.env` preenchido:

```env
POSTGRES_PASSWORD=senha-forte-postgres
REDIS_PASSWORD=senha-redis
JWT_KEY=chave-secreta-com-no-minimo-32-caracteres
JWT_ISSUER=http://192.168.15.106
JWT_AUDIENCE=http://192.168.15.106
WEB_PORT=8081
DASHBOARD_TOKEN=token-secreto-do-dashboard
```

> O `JWT_ISSUER` e `JWT_AUDIENCE` devem ser o endereço IP ou domínio pelo qual o usuário acessa a aplicação.

### 5. Enviar os arquivos do projeto para o servidor

Como o `rsync` pode não estar disponível no ambiente local (ex: Git Bash no Windows), use `tar` via SSH:

```bash
cd /caminho/do/projeto

tar czf - \
  --exclude='*/bin' --exclude='*/obj' --exclude='*/.aspire' --exclude='*/infra' \
  InitialAspireProject.ApiCore \
  InitialAspireProject.ApiIdentity \
  InitialAspireProject.Web \
  InitialAspireProject.ServiceDefaults \
  docker-compose.yml \
  | ssh user@192.168.1.100 "tar xzf - -C ~/apps/initial-aspire"
```

> Execute esse comando sempre que quiser enviar atualizações de código para o servidor.

### 6. Construir as imagens Docker no servidor

```bash
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose build --parallel"
```

Para reconstruir apenas um serviço específico (ex: após alterar só o `apicore`):

```bash
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose build apicore"
```

### 7. Subir todos os serviços

```bash
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose up -d"
```

Para reiniciar apenas os serviços alterados após uma atualização:

```bash
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose up -d apicore apiidentity"
```

### 8. Verificar os serviços em execução

```bash
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose ps"
```

### 9. Acompanhar os logs

```bash
# Todos os serviços
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose logs -f"

# Serviço específico
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose logs --tail=50 apicore"
```

### 10. Acessar a aplicação

Após subir os serviços, acesse pelo navegador:

| Serviço | URL |
|---|---|
| Aplicação Web | `http://192.168.1.100:8081` (porta configurável via `WEB_PORT`) |
| Dashboard Aspire (observabilidade) | `http://192.168.1.100:18888` |

> O Dashboard exige o token configurado em `DASHBOARD_TOKEN` para fazer login.

### 11. Atualizar o projeto após alterações no código

Fluxo completo de atualização:

```bash
# 1. Enviar arquivos atualizados
tar czf - \
  --exclude='*/bin' --exclude='*/obj' \
  InitialAspireProject.ApiCore \
  InitialAspireProject.ApiIdentity \
  InitialAspireProject.Web \
  InitialAspireProject.ServiceDefaults \
  docker-compose.yml \
  | ssh user@192.168.1.100 "tar xzf - -C ~/apps/initial-aspire"

# 2. Reconstruir e reiniciar
ssh user@192.168.1.100 "cd ~/apps/initial-aspire && docker compose build --parallel && docker compose up -d"
```

### Estrutura dos serviços no Docker Compose

| Serviço | Descrição |
|---|---|
| `dashboard` | Aspire Dashboard para observabilidade (OpenTelemetry) |
| `postgres` | Banco de dados PostgreSQL 17 |
| `redis` | Cache Redis |
| `apiidentity` | API de autenticação (JWT, Identity) |
| `apicore` | API de negócio (WeatherForecast) |
| `web` | Frontend Blazor Server |

### Volumes persistentes

Os dados críticos são mantidos em volumes Docker nomeados para sobreviver a reinicializações de contêineres:

| Volume | Conteúdo |
|---|---|
| `postgres-data` | Dados do PostgreSQL |
| `web-keys` | Chaves de Data Protection do Web |
| `apiidentity-keys` | Chaves de Data Protection do ApiIdentity |
| `apicore-keys` | Chaves de Data Protection do ApiCore |