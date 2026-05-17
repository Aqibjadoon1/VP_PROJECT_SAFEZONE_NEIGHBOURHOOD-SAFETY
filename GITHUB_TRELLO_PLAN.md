# SafeZone — 15-Day GitHub & Trello Plan

## CS-284L Visual Programming Lab | Air University Islamabad | Spring 2026

---

## 1. Git Architecture

### Branch Strategy

```
main ──────────────────────────────────────────────────────▶ (always working)
  │
  ├── aqib/ui ────────────────────────────────────────────▶ (Aqib's work)
  │     ├── day-01/landing-polish
  │     ├── day-02/mobile-sidebar
  │     └── ...
  │
  ├── talha/ops ───────────────────────────────────────────▶ (Talha's work)
  │     ├── day-01/groq-tests
  │     ├── day-02/slack-webhook
  │     └── ...
  │
  └── siddique/backend ────────────────────────────────────▶ (Siddique's work)
        ├── day-01/rate-limit
        ├── day-02/refresh-token
        └── ...
```

| Branch | Purpose | Who Pushes | Broken OK? |
|--------|---------|-----------|------------|
| `main` | Production-quality working code only | Nobody directly — only via PR merge | NEVER |
| `aqib/ui` | Aqib's frontend work | Aqib's agent | Yes (during day) |
| `talha/ops` | Talha's operations & voice work | Talha's agent | Yes (during day) |
| `siddique/backend` | Siddique's backend & DB work | Siddique's agent | Yes (during day) |

### Daily Git Workflow

```
08:00  Each agent pushes daily task to person's branch
       git checkout <person-branch>
       git add <files>
       git commit -m "<type>(<scope>): <description>"
       git push origin <person-branch>

12:00  Person verifies: dotnet build passes, feature works
       If broken → agent fixes → push again

18:00  PR from <person-branch> → main
       GitHub PR description template filled
       Another team member reviews (5 min)

18:30  PR approved → Squash merge to main
       git checkout main && git pull

19:00  All three rebase their branches on updated main
       git checkout <person-branch>
       git rebase main
       git push --force-with-lease  (only on personal branch!)
```

### Git Hygiene Rules

1. **Never commit to main directly.** Main is protected. All changes via PR.
2. **Squash merge only.** Each day's work = 1 clean commit on main.
3. **Rebase personal branches daily.** After main gets new PRs merged, rebase.
4. **No force push to main. Ever.**
5. **Commit format:** Conventional Commits
   ```
   feat(scope): what was added
   fix(scope): what was fixed
   style(scope): CSS/UI changes
   test(scope): test additions
   docs(scope): documentation
   chore(scope): tooling, config
   ```

### Branch Protection Rules (GitHub Settings)

```
main:
  ✅ Require pull request before merging
  ✅ Require approvals: 1
  ✅ Require status checks: dotnet build
  ✅ Require branches to be up to date
  ❌ Allow force pushes
  ❌ Allow deletions

aqib/ui, talha/ops, siddique/backend:
  ❌ No restrictions (personal work branches)
```

### PR Template (.github/PULL_REQUEST_TEMPLATE.md)

```markdown
## What
- [Brief description of today's changes]

## Files Changed
- `path/to/file1` — what changed
- `path/to/file2` — what changed

## Verification
- [ ] `dotnet build` passes
- [ ] Feature tested in browser
- [ ] No new warnings

## Screenshots (Aqib only)
| Before | After |
|--------|-------|
| | |
```

---

## 2. Trello Board Structure

### Board Name: `SafeZone — 15-Day Sprint`

### Lists (Columns)

| List | Meaning |
|------|---------|
| **📋 Backlog** | All planned tasks for the 15 days |
| **🏃 Week 1 (Days 1-5)** | Foundation & fixes |
| **🚀 Week 2 (Days 6-10)** | Features |
| **✅ Week 3 (Days 11-15)** | Polish & release |
| **🔧 In Progress** | Currently being worked (max 3 cards) |
| **👀 In Review** | PR submitted, waiting for review |
| **✔️ Done** | Merged to main, verified |

### Labels

| Label | Color | For |
|-------|-------|-----|
| 🎨 `ui-frontend` | Green | Aqib |
| ⚙️ `ops-voice-api` | Blue | Talha |
| 🗄️ `backend-db` | Red | Siddique |
| 🔴 `high-priority` | Red | Must finish today |
| 🟡 `medium-priority` | Yellow | Should finish today |
| 🟢 `low-priority` | Light green | Nice to have |
| 🐛 `bug` | Orange | Something broken |
| 📝 `docs` | Gray | Documentation |

### Card Template

```
Title: feat(scope): Short task description
Labels: [person-label] [priority-label]
Members: [assigned person]

Description:
**Goal:** What this task achieves
**Files:** Expected files to modify
**Verify:** How to confirm it works
**Depends on:** (any prerequisite task or "None")
**Agent prompt:** The exact instruction to give the coding agent

Checklist:
☐ Code written and pushed
☐ dotnet build passes
☐ Feature verified
☐ PR submitted
☐ PR merged
```

---

## 3. Day-by-Day Task Plan

### ── WEEK 1: FOUNDATION (Days 1-5) ──

```
GOAL: Set up infrastructure, fix bugs, stabilize codebase.
Each day: 3 parallel tasks → 3 PRs → squash merge to main.
```

---

#### DAY 1 — Git + CI + First Fixes

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Landing page hero animation smooth-out (remove jitter, add easing) | `index-effects.js` | `fix(ui): smooth Three.js hero rotation with damping` |
| **Talha** | Unit tests for GroqLlmService (mock HTTP, fallback path) | New: `Tests/GroqLlmServiceTests.cs` | `test(voice): add GroqLlmService unit tests` |
| **Siddique** | GitHub Actions CI pipeline + rate limiting middleware | New: `.github/workflows/dotnet.yml`, `Middleware/RateLimitingMiddleware.cs` | `feat(infra): CI pipeline and rate limiting middleware` |

**Siddique's CI file (`.github/workflows/dotnet.yml`):**
```yaml
name: .NET Build & Test
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
```

**Trello cards created:** 3 (one per person)

---

#### DAY 2 — Mobile + Voice Stub + Refresh Tokens

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Mobile sidebar close-on-tap-outside + slide animation | `DashboardLayout.razor`, `global.css` | `fix(layout): mobile sidebar closes on outside tap` |
| **Talha** | Voice Activity Detector interface implementation (energy threshold) | New: `Services/EnergyVadService.cs` | `feat(voice): add energy-threshold VAD implementation` |
| **Siddique** | Add RefreshToken field to User + persistence in AuthService | `Models/User.cs`, `Services/AuthService.cs` | `feat(auth): refresh token persistence in database` |

---

#### DAY 3 — Accessibility + Slack + Audit Log

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | `prefers-reduced-motion` disables all CSS animations | `global.css` | `feat(a11y): reduced-motion support for all animations` |
| **Talha** | Slack webhook service — posts to #authority-alerts on critical incidents | New: `Services/SlackNotificationService.cs` | `feat(integrations): slack webhook for authority alerts` |
| **Siddique** | Audit logging service — logs all create/update/delete actions | New: `Services/AuditLogService.cs`, `Middleware/AuditMiddleware.cs` | `feat(infra): audit logging for all data mutations` |

---

#### DAY 4 — CSS Optimize + Gmail Skeleton + Password Reset

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Remove unused CSS selectors, optimize paint-heavy rules (drop from 4100 → 3500 lines) | `global.css` | `perf(css): remove 600 lines of unused selectors` |
| **Talha** | Gmail API notification service skeleton (interface + empty methods + config) | New: `Services/GmailNotificationService.cs` | `feat(integrations): Gmail notification service skeleton` |
| **Siddique** | Password reset flow: generate token, send email, verify token, reset | `Services/AuthService.cs`, New: `Controllers/PasswordResetController.cs` | `feat(auth): password reset email flow` |

---

#### DAY 5 — PR Day: Bug Fixes + Merge

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Toast z-index overlap fix (toast above modal, both above sidebar) | `global.css`, `Toast.razor` | `fix(ui): toast always renders above modals and sidebar` |
| **Talha** | ElevenLabs webhook — handle missing dynamic variables gracefully | `Controllers/ElevenLabsWebhookController.cs` | `fix(webhook): graceful fallback for missing ElevenLabs fields` |
| **Siddique** | SeedData idempotent re-run (check exists before insert) | `Data/SeedData.cs` | `fix(db): make seed data idempotent on restart` |

**End of Day 5:**
- All 3 PRs reviewed and merged
- Rebase all personal branches on main
- Week 1 Trello cards → Done
- **Milestone:** `v0.1.0-foundation` tag on main

---

### ── WEEK 2: FEATURES (Days 6-10) ──

```
GOAL: Build new features. Each person's work is independent (different files).
```

---

#### DAY 6 — Kanban Animation + Twilio Live + PDF Generation

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Kanban: card slides to new column + brief scale-pulse on move | `KanbanBoard.razor`, `global.css` | `feat(kanban): animated card transition on status change` |
| **Talha** | Twilio outbound call — real HTTP call to Twilio API, fallback to mock | `Services/VoiceCallService.cs` | `feat(voice): live Twilio outbound calling with mock fallback` |
| **Siddique** | PDF generation for FIR reports using iTextSharp or QuestPDF | New: `Services/FirPdfService.cs` | `feat(fir): PDF report generation service` |

---

#### DAY 7 — Marker Clusters + Twilio Fallback + Analytics

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Leaflet marker clusters — group close markers into numbered circles | `map.js`, `DispatchMap.razor` | `feat(map): cluster incident markers on zoom out` |
| **Talha** | Twilio graceful degradation — auto-fallback to mock when credentials missing | `Services/VoiceCallService.cs`, `appsettings.json` | `fix(voice): Twilio auto-fallback to mock without key` |
| **Siddique** | Analytics controller — GET trends (incidents/day, severity dist, response times) | New: `Controllers/AnalyticsController.cs` | `feat(api): analytics endpoint for incident trends` |

---

#### DAY 8 — Contrast Fix + Gmail Live + Blob Storage

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Authority dashboard text contrast fix (WCAG AA, 4.5:1 minimum) | `global.css` | `fix(a11y): authority dashboard meets WCAG AA contrast` |
| **Talha** | Gmail sends email when FIR status changes (Accepted/Rejected) | `Services/GmailNotificationService.cs`, `Services/FirService.cs` | `feat(integrations): Gmail notification on FIR status change` |
| **Siddique** | Blob storage interface + local filesystem implementation (for evidence photos) | New: `Services/IBlobStorageService.cs`, `Services/LocalBlobStorageService.cs` | `feat(storage): blob storage interface with local implementation` |

---

#### DAY 9 — Skeleton Loaders + Retry Logic + File Upload

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Skeleton loaders on all authority pages (DispatchMap, FieldReports, FIRMgmt, SosLogs) | `LoadingSkeleton.razor`, 4 authority pages | `feat(ui): skeleton loaders on all authority pages` |
| **Talha** | API retry logic — 3 attempts with exponential backoff on Groq + Twilio calls | `Services/GroqLlmService.cs`, `Services/VoiceCallService.cs` | `feat(ops): retry with exponential backoff on external APIs` |
| **Siddique** | File upload endpoint — POST multipart form, save via blob storage | New: `Controllers/FileUploadController.cs` | `feat(api): file upload endpoint for evidence photos` |

---

#### DAY 10 — PR Day: Bug Fixes + Merge

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Weather map layer toggle — fix flicker on rapid switching | `WeatherMap.razor`, `map.js` | `fix(map): debounce layer toggle to prevent flicker` |
| **Talha** | CallHub — fix race condition on rapid disconnect/reconnect | `Hubs/CallHub.cs` | `fix(hubs): CallHub disconnect race condition handled` |
| **Siddique** | Incident number generation — use database lock to prevent collision | `Services/IncidentService.cs` | `fix(incident): atomic incident number generation` |

**End of Day 10:**
- All 3 PRs reviewed and merged
- Week 2 Trello cards → Done
- **Milestone:** `v0.2.0-features` tag on main

---

### ── WEEK 3: POLISH & RELEASE (Days 11-15) ──

```
GOAL: Testing, documentation, cleanup, final release.
```

---

#### DAY 11 — Test Suite

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Playwright test: login → dashboard → report incident → verify appears | New: `Tests/E2E/UserFlowTests.cs` | `test(e2e): Playwright critical user flow test` |
| **Talha** | Integration test: STT → LLM → TTS pipeline end-to-end | New: `Tests/Integration/VoicePipelineTests.cs` | `test(voice): integration test for voice pipeline` |
| **Siddique** | Unit tests for AuthService (register, login, JWT claims) + IncidentService (CRUD, stats) | New: `Tests/Unit/AuthServiceTests.cs`, `Tests/Unit/IncidentServiceTests.cs` | `test(services): unit tests for Auth and Incident services` |

---

#### DAY 12 — Documentation

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Shared component usage guide — screenshots and code examples for each component | `README2.md` (add section) | `docs(components): usage guide with screenshots` |
| **Talha** | Voice pipeline architecture document — data flow diagram, service descriptions | New: `docs/voice-pipeline.md` | `docs(voice): pipeline architecture documentation` |
| **Siddique** | Swagger XML comments — summary/remarks on all controller endpoints | All 9 controllers | `docs(api): Swagger descriptions for all 40+ endpoints` |

---

#### DAY 13 — Accessibility + Security

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | ARIA labels on all modals, dialogs, and interactive cards | 5+ razor files | `fix(a11y): ARIA labels on modals and interactive elements` |
| **Talha** | Environment variable guards — all mock services log warning if API key missing | 5 service files | `fix(ops): warning logs on missing API keys in production` |
| **Siddique** | JWT refresh token rotation — old token invalidated on use, single-use | `Services/AuthService.cs` | `fix(auth): single-use refresh token rotation` |

---

#### DAY 14 — Cleanup

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | CSS final purge — remove all unused selectors, consolidate duplicates, target 3100 lines | `global.css` | `chore(css): final purge, 1000+ lines removed` |
| **Talha** | Service cleanup — remove unused DI registrations, consolidate mock config | `Program.cs`, `appsettings.json` | `chore(services): consolidate mock mode configuration` |
| **Siddique** | EF migration review — remove unused indexes, verify FK relationships | `SafeZoneDbContext.cs` | `chore(db): migration cleanup and index optimization` |

---

#### DAY 15 — RELEASE DAY

| Who | Task | Files | Commit |
|-----|------|-------|--------|
| **Aqib** | Final UI QA — screenshot every page (24 pages), add to README | `README2.md` (add gallery) | `release: UI screenshot gallery added to README` |
| **Talha** | Smoke test — verify all 9 external APIs respond, log results | New: `docs/smoke-test-results.md` | `release: external API smoke test results` |
| **Siddique** | Production appsettings review, tag v1.0.0, generate release notes | `appsettings.json`, git tag | `release: v1.0.0 production config and release notes` |

**End of Day 15:**
- All 3 PRs reviewed and merged
- `git tag -a v1.0.0 -m "SafeZone v1.0.0 — 15-day sprint complete"`
- `git push origin v1.0.0`
- Week 3 Trello cards → Done
- **Board state:** All cards in ✔️ Done

---

## 4. Merge Conflict Prevention

Each person modifies **separate directories**. Conflicts are nearly impossible:

| Person | Directories Modified |
|--------|---------------------|
| **Aqib** | `Components/**`, `wwwroot/css/`, `wwwroot/js/`, `Pages/_Host.cshtml` (only CSS/JS links) |
| **Talha** | `Services/Voice*.cs`, `Services/GroqLlmService.cs`, `Services/SlackNotificationService.cs`, `Services/GmailNotificationService.cs`, `Hubs/CallHub.cs`, `Controllers/VoiceCallController.cs`, `Controllers/ElevenLabsWebhookController.cs`, `Controllers/SmsController.cs`, `Controllers/SosController.cs` |
| **Siddique** | `Models/`, `DTOs/`, `Data/`, `Migrations/`, `Services/AuthService.cs`, `Services/IncidentService.cs`, `Services/AlertService.cs`, `Services/FirService.cs`, `Services/AuditLogService.cs`, `Controllers/AuthController.cs`, `Controllers/IncidentController.cs`, `Controllers/AlertController.cs`, `Controllers/FirController.cs`, `Controllers/MapController.cs`, `Controllers/AnalyticsController.cs`, `Controllers/PasswordResetController.cs`, `Controllers/FileUploadController.cs`, `Middleware/`, `Helpers/`, `Program.cs`, `.github/` |

**Shared files (coordination needed):**
- `Program.cs` — Siddique owns it. Aqib and Talha tell Siddique what DI lines to add.
- `appsettings.json` — Siddique owns it. Talha tells Siddique what config keys to add.
- `SafeZone.Server.csproj` — Siddique owns it. Anyone adding NuGet packages tells Siddique.

---

## 5. Trello Board Initial Setup

### Step 1: Create Board
```
Board: "SafeZone — 15-Day Sprint"
Visibility: Team
```

### Step 2: Create Lists
```
📋 Backlog
🏃 Week 1 (Days 1-5)
🚀 Week 2 (Days 6-10)
✅ Week 3 (Days 11-15)
🔧 In Progress
👀 In Review
✔️ Done
```

### Step 3: Add All 45 Cards (15 days × 3 people)

For each task, create a card with:
- **Title:** `D{day} — {person}: {task}` (e.g., `D01 — Aqib: Smooth Three.js hero`)
- **Labels:** Person label + priority
- **Member:** Assigned person
- **Description:** Goal, files, verification steps, agent prompt
- **Checklist:** 5 items (code, build, verify, PR, merge)

### Step 4: Daily Routine

```
Morning (08:00):
  └─ Move today's 3 cards from Week list → In Progress
  └─ Agent pushes code to person's branch

Midday (12:00):
  └─ Each person verifies build: dotnet build
  └─ If broken → agent fixes → push

Evening (18:00):
  └─ Move 3 cards from In Progress → In Review
  └─ Submit PRs from person branches → main
  └─ Team reviews (5 min each)

Night (19:00):
  └─ Merge all 3 PRs (squash)
  └─ Move 3 cards from In Review → Done
  └─ Rebase all personal branches on main
  └─ Archive Done cards (clean board for next day)
```

---

## 6. Agent Prompt Template

Each day, each person gives their agent exactly:

```
───────────────────────────────────────────
TASK: {one-line description from plan}

BRANCH: {person-branch} (e.g., aqib/ui)
BASE: main (rebase first!)

FILES TO MODIFY:
  - path/to/file1 — {what to change}
  - path/to/file2 — {what to change}

COMMIT MESSAGE: {type}({scope}): {description}

VERIFICATION:
  1. dotnet build --no-restore → 0 errors
  2. {specific test to run}

PUSH: Yes, push to {person-branch} after commit

AFTER PUSH: Tell me the commit hash and I'll verify.
───────────────────────────────────────────
```

---

## 7. GitHub Settings Checklist

```
☐ Create repo: github.com/your-org/SafeZone
☐ Settings → Branches → Add rule for "main":
    ☐ Require pull request before merging
    ☐ Require approvals: 1
    ☐ Require status checks: "build (ubuntu-latest)"
    ☐ Do not allow bypassing
☐ Settings → Collaborators → Add Aqib, Talha, Siddique (Write access)
☐ Create .github/PULL_REQUEST_TEMPLATE.md
☐ Create .github/workflows/dotnet.yml
☐ Push initial code to main
☐ Create 3 branches: aqib/ui, talha/ops, siddique/backend
☐ Protect main branch
```

---

## 8. Summary of All 135 Commits

### Week 1 (15 commits to main via squash)

| Day | Aqib | Talha | Siddique |
|-----|------|-------|----------|
| 1 | `fix(ui): smooth Three.js hero rotation with damping` | `test(voice): add GroqLlmService unit tests` | `feat(infra): CI pipeline and rate limiting middleware` |
| 2 | `fix(layout): mobile sidebar closes on outside tap` | `feat(voice): add energy-threshold VAD implementation` | `feat(auth): refresh token persistence in database` |
| 3 | `feat(a11y): reduced-motion support for all animations` | `feat(integrations): slack webhook for authority alerts` | `feat(infra): audit logging for all data mutations` |
| 4 | `perf(css): remove 600 lines of unused selectors` | `feat(integrations): Gmail notification service skeleton` | `feat(auth): password reset email flow` |
| 5 | `fix(ui): toast always renders above modals and sidebar` | `fix(webhook): graceful fallback for missing ElevenLabs fields` | `fix(db): make seed data idempotent on restart` |

### Week 2 (15 commits to main via squash)

| Day | Aqib | Talha | Siddique |
|-----|------|-------|----------|
| 6 | `feat(kanban): animated card transition on status change` | `feat(voice): live Twilio outbound calling with mock fallback` | `feat(fir): PDF report generation service` |
| 7 | `feat(map): cluster incident markers on zoom out` | `fix(voice): Twilio auto-fallback to mock without key` | `feat(api): analytics endpoint for incident trends` |
| 8 | `fix(a11y): authority dashboard meets WCAG AA contrast` | `feat(integrations): Gmail notification on FIR status change` | `feat(storage): blob storage interface with local implementation` |
| 9 | `feat(ui): skeleton loaders on all authority pages` | `feat(ops): retry with exponential backoff on external APIs` | `feat(api): file upload endpoint for evidence photos` |
| 10 | `fix(map): debounce layer toggle to prevent flicker` | `fix(hubs): CallHub disconnect race condition handled` | `fix(incident): atomic incident number generation` |

### Week 3 (15 commits to main via squash)

| Day | Aqib | Talha | Siddique |
|-----|------|-------|----------|
| 11 | `test(e2e): Playwright critical user flow test` | `test(voice): integration test for voice pipeline` | `test(services): unit tests for Auth and Incident services` |
| 12 | `docs(components): usage guide with screenshots` | `docs(voice): pipeline architecture documentation` | `docs(api): Swagger descriptions for all 40+ endpoints` |
| 13 | `fix(a11y): ARIA labels on modals and interactive elements` | `fix(ops): warning logs on missing API keys in production` | `fix(auth): single-use refresh token rotation` |
| 14 | `chore(css): final purge, 1000+ lines removed` | `chore(services): consolidate mock mode configuration` | `chore(db): migration cleanup and index optimization` |
| 15 | `release: UI screenshot gallery added to README` | `release: external API smoke test results` | `release: v1.0.0 production config and release notes` |

---

## 9. Visual Timeline

```
Week 1                 Week 2                 Week 3
Foundation            Features               Polish & Release
━━━━━━━━━             ━━━━━━━━━              ━━━━━━━━━━━━━━━
D1  D2  D3  D4  D5    D6  D7  D8  D9  D10   D11 D12 D13 D14 D15
│   │   │   │   │     │   │   │   │   │      │   │   │   │   │
│   │   │   │   ├─PR  │   │   │   │   ├─PR   │   │   │   │   ├─RELEASE
│   │   │   │   │     │   │   │   │   │      │   │   │   │   │
▼   ▼   ▼   ▼   ▼     ▼   ▼   ▼   ▼   ▼      ▼   ▼   ▼   ▼   ▼
▌   ▌   ▌   ▌   ▌     ▌   ▌   ▌   ▌   ▌      ▌   ▌   ▌   ▌   ▌  (3 PRs/day → main)
▌   ▌   ▌   ▌   ▌     ▌   ▌   ▌   ▌   ▌      ▌   ▌   ▌   ▌   ▌
                   v0.1.0              v0.2.0              v1.0.0
```

---

*Every commit message is verifiable on GitHub. Every task is tracked on Trello. The branch history tells the complete story of 15 days of development.*
