# Skill: Git Commit

Create a well-formed git commit for the current changes, optionally on a
new branch, and push to origin.

------------------------------------------------------------------------

## Steps

### 1. Collect arguments

The skill accepts optional arguments:

    /commit [branch] [message]

| Argument | Default | Example |
|---|---|---|
| `branch` | current branch (unless on `main`) | `feature/my-feature` |
| `message`| auto-generated | `"fix: correct login redirect"` |

**Examples:**
```
/commit                                        # commit on current branch, auto message
/commit feature/my-feature                     # create branch, auto message
/commit feature/my-feature "feat: add login"   # create branch, explicit message
```

### Branch behavior

-   If a **branch argument is provided**, use it.
-   If **no branch is provided**:
    -   If the current branch **is NOT `main`**, commit on the current
        branch.
    -   If the current branch **is `main`**, automatically create a new
        branch before committing.

### 2. Inspect current state

Run all three in parallel:

```bash
git status
git diff HEAD
git log --oneline -5
```

Use the output to:

-   Identify all modified, added, and deleted files
-   Understand the nature of the changes
-   Follow the existing commit message style from the log

------------------------------------------------------------------------

### 3. Create branch (if required)

If a branch name was provided and it doesn't exist yet:

``` bash
git checkout -b <branch>
```

If the branch already exists:

``` bash
git checkout <branch>
```

If **no branch was provided and the current branch is `main`**, generate
a branch name and create it:

``` bash
git checkout -b ai/<generated-name>
```

The generated name must be based on the detected changes.

------------------------------------------------------------------------

### 4. Stage files

Stage only files relevant to the changes --- never include `.env`,
secrets, or build artefacts:

``` bash
git add <file1> <file2> ...
```

Prefer explicit file paths over `git add -A` or `git add .`.

------------------------------------------------------------------------

### 5. Compose commit message

If no message was provided, generate one based on the diff:

-   Use **Conventional Commits** format: `type(scope): description`
-   Common types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`
-   Subject line: **max 72 characters**, imperative mood, no trailing
    period
-   Body: explain **why**, not **what** (the diff shows the what)
-   Always append the co-author trailer:

```{=html}
<!-- -->
```
    Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>

**Example:**

    feat(auth): add ReturnUrl support to login page

    After a successful login the user is redirected to the page they
    originally tried to access instead of always landing on /.

    Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>

------------------------------------------------------------------------

### 6. Commit

Always pass the message via **HEREDOC** to preserve formatting:

``` bash
git commit -m "$(cat <<'EOF'
<subject line>

<body>

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

------------------------------------------------------------------------

### 7. Push

If the branch has no upstream yet:

``` bash
git push -u origin <branch>
```

Otherwise:

``` bash
git push
```

------------------------------------------------------------------------

### 8. Generate PR description

After pushing, generate a PR description in markdown

Run `git diff main...HEAD` (or the base branch if different) to understand all changes in the branch, then produce:

```markdown
## Summary

- <bullet: what changed and why, one per logical group of changes>

## Test plan

- [ ] <specific manual check the reviewer should perform>
- [ ] <another actionable check>
- [ ] `dotnet test` — all tests pass
```

Guidelines:
- Summary bullets explain **why** the change was made, not just list files
- Test plan items are specific and actionable (e.g. "Unauthenticated → `/weather` redirects to `/login?ReturnUrl=%2Fweather`")
- Aim for 3–6 summary bullets

To submit your changes for review, you must create a Pull Request using the GitHub CLI.

After pushing your branch to the remote repository, run the following command:

gh pr create

This command will guide you through creating the Pull Request interactively, allowing you to select the base branch (main) and provide the title and description.

Make sure the Pull Request targets the main branch.

------------------------------------------------------------------------

### 9. Confirm

Show the user:

- The commit hash and subject line (`git log --oneline -1`)
- The push result (branch URL or PR creation link printed by GitHub)
- The PR description from step 8

------------------------------------------------------------------------

## Rules

-   **Never** use `--no-verify` or skip hooks\
- **Never** amend a commit that has already been pushed
- **Never** stage `.env`, `*.pfx`, `*.key`, `appsettings.Production.json`, or any secrets
- **Never** commit `bin/`, `obj/`, or other build artefacts
- Do not push to `main` directly — warn the user if they request it
