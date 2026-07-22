# Feature 001 Implementation Review

## Scope

- Date: 2026-07-22
- Feature: `001-secure-access-shell`
- Orchestrator: OpenAI Codex
- External reviewer: Claude Code CLI, read-only
- Rounds: 2 of 2
- Result: clean after corrections, with one accepted hardening note

## Round 1

Claude reported five actionable findings:

1. Angular would echo the antiforgery cookie token instead of the paired
   request token. Accepted. The API now writes the request token to the
   JS-readable `XSRF-TOKEN` cookie while retaining a separate HttpOnly
   antiforgery cookie.
2. A `__Host-` cookie combined with request-dependent security could be
   rejected behind nginx. Accepted. Prefixes were removed and forwarded HTTPS
   is handled before cookie/antiforgery middleware.
3. The base configuration contained a demo SQL credential and trust-all TLS.
   Accepted. Production configuration now requires environment values; local
   trust is confined to development/test configuration.
4. API session, role matrix, and 403 Problem Details coverage was incomplete.
   Accepted. The real-SQL integration suite now covers those cases.
5. Angular guard, error-state, channel-neutral ID, login, and logout coverage
   was incomplete. Accepted. Vitest coverage was added.

The suggestion to replace `AddControllersWithViews` with `AddControllers` was
rejected: the narrower registration produced a verified runtime 500 because
the global antiforgery authorization filter was not registered.

## Round 2

Claude returned `CLEAN` and confirmed closure of all five findings. One low
hardening note was accepted: forwarded headers were restricted to the Docker
bridge network and nginx now supplies the HTTPS scheme rather than forwarding
a client-controlled value.

## Verification

- .NET build: passed with zero warnings and errors.
- Unit tests: 5 passed.
- SQL Server integration tests: 13 passed.
- Angular production build: passed.
- Vitest: 13 passed.
- Playwright Chromium happy path: 1 passed.
- Production npm dependency audit: 0 vulnerabilities.
- Compose configuration validation: passed.
- `git diff --check`: passed.
- Visual smoke: sign-in and authenticated customer workspace inspected at the
  available responsive viewport; no overlap or clipping was detected.

## Remaining Slice Work

Docker Desktop's Linux engine could not be started without elevated service
access, so the Compose runtime smoke remains open. The same Compose artifacts
must be built and exercised during deployment before T013/T014 and Stage 8 are
closed.
