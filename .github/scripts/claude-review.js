// @ts-check
// Claude PR Code Review — no npm dependencies required (uses Node 18+ native fetch)
import { readFileSync } from 'fs';
import { existsSync } from 'fs';

const ANTHROPIC_API_KEY = process.env.ANTHROPIC_API_KEY;
const GITHUB_TOKEN = process.env.GITHUB_TOKEN;
const PR_NUMBER = process.env.PR_NUMBER;
const REPO = process.env.REPO;

if (!ANTHROPIC_API_KEY) throw new Error('ANTHROPIC_API_KEY is not set');
if (!GITHUB_TOKEN) throw new Error('GITHUB_TOKEN is not set');
if (!PR_NUMBER) throw new Error('PR_NUMBER is not set');
if (!REPO) throw new Error('REPO is not set');

// Read diff file produced by the workflow step
const DIFF_FILE = 'pr.diff';
if (!existsSync(DIFF_FILE)) {
  console.log('No diff file found — skipping review.');
  process.exit(0);
}

const rawDiff = readFileSync(DIFF_FILE, 'utf8').trim();
if (!rawDiff) {
  console.log('Diff is empty (no C# or project file changes) — skipping review.');
  process.exit(0);
}

// Truncate very large diffs to stay within Claude's token limit (~80k chars ≈ ~20k tokens)
const MAX_DIFF_CHARS = 80_000;
const diff =
  rawDiff.length > MAX_DIFF_CHARS
    ? rawDiff.slice(0, MAX_DIFF_CHARS) + '\n\n[... diff truncated — only the first 80 000 chars are shown ...]'
    : rawDiff;

console.log(`Sending diff (${diff.length} chars) to Claude for review...`);

// --- Call Claude API ---
const claudeResponse = await fetch('https://api.anthropic.com/v1/messages', {
  method: 'POST',
  headers: {
    'x-api-key': ANTHROPIC_API_KEY,
    'anthropic-version': '2023-06-01',
    'content-type': 'application/json',
  },
  body: JSON.stringify({
    model: 'claude-opus-4-6',
    max_tokens: 2048,
    messages: [
      {
        role: 'user',
        content: `You are a senior .NET engineer reviewing a pull request for a **.NET 9 / Aspire microservices** project (ASP.NET Core Web APIs + Blazor Server + PostgreSQL + Redis + Entity Framework Core).

Analyse the git diff below and provide **concise, actionable** feedback. Focus only on real issues — do not invent problems.

Review for:
1. **Clean Code** — unclear naming, magic strings/numbers, dead code, oversized methods, missing null checks
2. **SOLID** — Single Responsibility violations, Open/Closed violations, Dependency Inversion (new-ing concrete types instead of using DI), Interface Segregation bloat
3. **Performance** — unnecessary allocations, LINQ ToList() inside loops, synchronous I/O, missing \`ConfigureAwait(false)\`
4. **Security** — missing input validation, SQL injection risk, secrets in code, insecure defaults, missing \`[Authorize]\` on endpoints
5. **Async/await** — \`.Result\` or \`.Wait()\` blocking calls, missing \`CancellationToken\` parameter propagation, fire-and-forget without error handling
6. **N+1 queries** — EF Core queries inside loops, missing \`Include()\` / \`ThenInclude()\`, Select without projection

Format the response in **Markdown**:
- Start with a **one-line summary** (e.g. "✅ Clean PR — no issues found." or "⚠️ 3 issues found across 2 files.")
- Use a heading per category **only if you found issues in it** — skip clean categories entirely
- Under each heading list specific file + line references where possible
- End with a brief overall recommendation

If the diff is clean, say so in 2-3 sentences maximum.

\`\`\`diff
${diff}
\`\`\``,
      },
    ],
  }),
});

if (!claudeResponse.ok) {
  const errorBody = await claudeResponse.text();
  throw new Error(`Claude API error ${claudeResponse.status}: ${errorBody}`);
}

const claudeData = await claudeResponse.json();
const review = claudeData.content[0].text;

console.log('Claude review received. Posting to GitHub...');

// --- Post comment to GitHub PR ---
const comment = `## 🤖 Claude Code Review

${review}

---
*Powered by [Claude](https://claude.ai) · Model: \`claude-opus-4-6\` · Reviews \`.cs\` and \`.csproj\` changes only*`;

const githubResponse = await fetch(
  `https://api.github.com/repos/${REPO}/issues/${PR_NUMBER}/comments`,
  {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${GITHUB_TOKEN}`,
      'Content-Type': 'application/json',
      Accept: 'application/vnd.github+json',
      'X-GitHub-Api-Version': '2022-11-28',
    },
    body: JSON.stringify({ body: comment }),
  }
);

if (!githubResponse.ok) {
  const errorBody = await githubResponse.text();
  throw new Error(`GitHub API error ${githubResponse.status}: ${errorBody}`);
}

console.log(`Review posted successfully to PR #${PR_NUMBER}.`);
