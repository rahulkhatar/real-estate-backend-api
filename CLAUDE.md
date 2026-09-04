# Repo context for automated PR review

This file is read by the automated reviewer in `.github/workflows/claude-review.yml` on every
PR, and by any interactive Claude Code session working in this repo.

## Project shape

Single repo containing both the API and the background Worker:
- `RealEstate.Api` -- the HTTP API
- `RealEstate.Worker` -- background job processor (RabbitMQ consumer)
- `RealEstate.Application`, `RealEstate.Core`, `RealEstate.Infrastructure` -- shared layers
  (Infrastructure includes the Redis cache client and RabbitMQ connection/consumer setup)
- `RealEstate.Tests`

Deploy path: Docker Hub (`rahulk86/real-estate:latest` / `:worker-latest`) -> Azure Container
Apps. Redis and RabbitMQ run as their own Container Apps (official images, no custom code) --
this repo's only involvement with them is the client code in `RealEstate.Infrastructure` that
talks to them, which is fully in scope for review like anything else.

## CI/CD map (so a review doesn't misjudge risk)

- `dev-ci.yml` -- required `Build & test` status check, runs on push to `dev` and on every PR
  into `dev` or `main`.
- `aca-deploy.yml` / `aca-deploy-worker.yml` -- production deploy, triggered by push to `main`
  (API and Worker respectively). Both contain a fully commented-out `deploy-aks` job.
  **Do not suggest uncommenting, modifying, or otherwise touching that job** -- it's a
  Kubernetes deploy path intentionally paused/parked, not dead code to clean up.

## Known footgun to specifically check for

`__N`-indexed environment-variable config (e.g. `Cors__AllowedOrigins__0`) must use **separate**
keys per array element (`__0`, `__1`, ...). A real bug in this exact codebase: two origins were
once jammed into one key as a comma-joined string
(`Cors__AllowedOrigins__0=https://a.com,https://b.com`). .NET's configuration array binding does
not split on commas -- that becomes a single nonsense array entry that matches neither real
value, silently breaking CORS with no error or exception anywhere. Flag this pattern on sight
in any `__N`-indexed config, connection string, or settings section.

## Conventions to prefer

- Reuse existing patterns over introducing new libraries or abstractions.
- Match existing logging, error-handling, and DI registration conventions already used in the
  touched layer rather than introducing a new style.
- Prefer the pinned MediatR/AutoMapper versions already in use; don't suggest switching them.

## Scope boundary

Review and comment only. Never push commits, open PRs, or merge anything -- merging is always a
human action.
