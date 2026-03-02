# Skill: Publish via SSH (Docker Compose)

Deploy the project to a remote Linux server over SSH by transferring source files and building Docker images directly on the server.

---

## Steps

### 1. Collect server info

The skill accepts optional arguments when invoked:

```
/publish [user@host] [services]
```

| Argument | Default | Example |
|---|---|---|
| `user@host` | `adriano@192.168.15.106` | `deploy@myserver.com` |
| `services` | `all` | `apicore apiidentity` |

**Examples:**
```
/publish                                   # deploys all services to adriano@192.168.15.106
/publish adriano@192.168.15.106            # same, explicit
/publish adriano@192.168.15.106 apicore    # redeploy only apicore
```

If no arguments are provided, use the defaults:
- **user**: `adriano`
- **host**: `192.168.15.106`
- **remote path**: `~/apps/initial-aspire`
- **services**: `all`

Only ask via `AskUserQuestion` if the user provides an ambiguous or incomplete argument.

---

### 2. Verify SSH connectivity

```bash
ssh <user>@<host> "echo SSH_OK"
```

If the command fails, stop and tell the user to:
- Confirm the host/user are correct
- Run `ssh-copy-id <user>@<host>` to set up key-based auth

---

### 3. Transfer source files

Run from the **project root** (`c:/Users/adria/source/repos/InitialAspireProject`):

```bash
tar czf - \
  --exclude='*/bin' \
  --exclude='*/obj' \
  --exclude='*/.aspire' \
  --exclude='*/infra' \
  InitialAspireProject.ApiCore \
  InitialAspireProject.ApiIdentity \
  InitialAspireProject.Web \
  InitialAspireProject.ServiceDefaults \
  docker-compose.yml \
  | ssh <user>@<host> "tar xzf - -C <remote_path>"
```

---

### 4. Build Docker images on the server

If redeploying **all services**:
```bash
ssh <user>@<host> "cd <remote_path> && docker compose build --parallel"
```

If redeploying **specific services** only:
```bash
ssh <user>@<host> "cd <remote_path> && docker compose build <services>"
```

---

### 5. Restart services

If redeploying **all services**:
```bash
ssh <user>@<host> "cd <remote_path> && docker compose up -d"
```

If redeploying **specific services** only:
```bash
ssh <user>@<host> "cd <remote_path> && docker compose up -d <services>"
```

---

### 6. Verify deployment

```bash
ssh <user>@<host> "cd <remote_path> && docker compose ps"
```

Show the output to the user. All target services should show `running`.

Then print the access URLs:

| Service | URL |
|---|---|
| Web frontend | `http://<host>:8081` |
| Aspire Dashboard | `http://<host>:18888` |
| pgAdmin | `http://<host>:5050` |

---

## First-time setup (one-off)

If the user mentions it's the first deploy, run these extra steps **before step 3**:

```bash
# Create the app directory on the server
ssh <user>@<host> "mkdir -p <remote_path>"

# Copy environment file template
scp .env.example <user>@<host>:<remote_path>/.env
```

Then tell the user to edit `<remote_path>/.env` on the server and fill in:
- `POSTGRES_PASSWORD`
- `REDIS_PASSWORD`
- `JWT_KEY` (min 32 chars)
- `JWT_ISSUER` and `JWT_AUDIENCE` (e.g. `http://<host>`)
- `DASHBOARD_TOKEN`

---

## Quick redeploy (all steps in one shot)

When the user wants a full deploy without prompts, execute steps 3→5 as a single pipeline:

```bash
tar czf - \
  --exclude='*/bin' --exclude='*/obj' --exclude='*/.aspire' --exclude='*/infra' \
  InitialAspireProject.ApiCore \
  InitialAspireProject.ApiIdentity \
  InitialAspireProject.Web \
  InitialAspireProject.ServiceDefaults \
  docker-compose.yml \
  | ssh <user>@<host> "tar xzf - -C <remote_path> && cd <remote_path> && docker compose build --parallel && docker compose up -d"
```

---

## Notes

- Never transfer `bin/`, `obj/`, `.aspire/`, or `infra/` — they are server-built artefacts.
- `AppHost` is NOT transferred — Docker Compose replaces Aspire orchestration in production.
- The `.env` file lives only on the server and is never committed to git.
- If the build fails due to a missing `.env`, guide the user through the first-time setup section.
