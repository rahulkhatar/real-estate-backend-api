# Repo context for automated PR review

This file is read by the automated reviewer in `.github/workflows/claude-review.yml` on every
PR, and by any interactive Claude Code session working in this repo.

**The automated reviewer is currently paused** (as of 2026-09-06) -- every run started failing
instantly on a billing/quota/access rejection, not a code issue. See the comment block at the
top of `claude-review.yml` for the diagnosis and how to resume it. While paused, `dev` branch
protection still requires 1 approving review (previously satisfied by this bot, no other
reviewer configured) -- every PR into `dev` needs the repo owner's admin-bypass merge until
it's back.

## Project shape

Single repo, single deployable process now -- there is no more `RealEstate.Worker` project:
- `RealEstate.Api` -- the HTTP API. Also hosts the AI-reindex queue consumer as a hosted
  service (`AddMessagingConsumer()` in `Program.cs`) -- this used to run in a separate
  `RealEstate.Worker` process/deployment, folded in because the target hosting (BigRock
  shared hosting) can only run one process per app, not a second standalone worker.
- `RealEstate.Application`, `RealEstate.Core`, `RealEstate.Infrastructure` -- shared layers
  (Infrastructure includes the in-process cache and RabbitMQ connection/consumer setup)
- `RealEstate.Tests`

Deploy path is mid-migration: currently still Docker Hub (`rahulk86/real-estate:latest`) ->
Azure Container Apps via `aca-deploy.yml`, but the goal is moving off Container Apps entirely
to BigRock shared hosting (the same host the frontend already FTP-deploys to) to stop paying
for Azure compute. A prior BigRock FTP deploy setup for this exact API already existed before
it moved to Azure (`azure-pipelines.yml` / `azure-pipelines.dev.yml` at the repo root -- dead
weight now since GitHub doesn't run Azure Pipelines YAML, but valuable reference: it confirms
.NET 10 + ANCM v2 already work on the BigRock server, and has working, already-debugged logic
for a framework-dependent publish + FTP deploy). Until a GitHub Actions equivalent replaces
`aca-deploy.yml`, Container Apps is still the live deploy target -- don't assume BigRock
deploy is active yet.

RabbitMQ is hosted externally on CloudAMQP (a third-party service, not an Azure resource) --
this repo's only involvement is the client code in `RealEstate.Infrastructure` that talks to
it, which is fully in scope for review like anything else. There is no Redis (or any
distributed cache) anymore: `ICacheService` is backed by an in-process `IMemoryCache`
(`InMemoryCacheService`) -- a self-hosted Redis Container App used to run this, but it
couldn't scale to zero (persistent connections aren't traffic-scalable the way HTTP is) so it
cost money continuously; the cache was already a pure, fail-open performance optimization over
MongoDB, never a system of record, making the swap safe. Same reasoning is why RabbitMQ moved
off a self-hosted Container App to a hosted provider instead of also being dropped -- unlike
the cache, the reindex queue still needs a real broker, just not a self-hosted one.

## CI/CD map (so a review doesn't misjudge risk)

- `dev-ci.yml` -- required `Build & test` status check, runs on push to `dev` and on every PR
  into `dev` or `main`.
- `aca-deploy.yml` -- production deploy, triggered by push to `main`. Contains a fully
  commented-out `deploy-aks` job. **Do not suggest uncommenting, modifying, or otherwise
  touching that job** -- it's a Kubernetes deploy path intentionally paused/parked, not dead
  code to clean up. This whole file is itself expected to be replaced by a BigRock FTP deploy
  workflow soon (see Project shape above) -- don't be surprised if it disappears.

## Known footgun to specifically check for

`__N`-indexed environment-variable config (e.g. `Cors__AllowedOrigins__0`) must use **separate**
keys per array element (`__0`, `__1`, ...). A real bug in this exact codebase: two origins were
once jammed into one key as a comma-joined string
(`Cors__AllowedOrigins__0=https://a.com,https://b.com`). .NET's configuration array binding does
not split on commas -- that becomes a single nonsense array entry that matches neither real
value, silently breaking CORS with no error or exception anywhere. Flag this pattern on sight
in any `__N`-indexed config, connection string, or settings section.

## MediatR / AutoMapper are deliberately pinned -- do not approve a major-version bump

Both moved to a paid commercial license starting at MediatR 13.x / AutoMapper 13.x. This repo
is pinned to the last free versions (MediatR ~12.5, AutoMapper 12.0.1) as a deliberate, explicit
decision -- not an oversight. AutoMapper 12.0.1 has a known, accepted high-severity advisory
(GHSA-rvv3-g6hj-g44x, unpatched on 12.x) that comes with this choice; it is not something to fix
by upgrading. If a PR attempts a major-version bump on either package anyway, **request
changes** -- crossing into paid-license territory is a decision only the human owner can make,
never something to approve automatically, even when the PR's stated motivation is a real CVE
fix. Minor/patch bumps within the 12.x line are fine and don't need special scrutiny beyond the
usual review.

## Conventions to prefer

- Reuse existing patterns over introducing new libraries or abstractions.
- Match existing logging, error-handling, and DI registration conventions already used in the
  touched layer rather than introducing a new style.

## Scope boundary

Review and comment only. Never push commits, open PRs, or merge anything -- merging is always a
human action.
