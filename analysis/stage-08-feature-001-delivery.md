# Stage 8 Delivery - Feature 001

## Scope

- Feature: `001-secure-access-shell`
- Date: 2026-07-22
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The modern SQL Server, API, and Angular UI run as the separate `bank-of-z`
Compose project. The UI is bound to server loopback port `8088`; public traffic
passes through the existing HTTPS nginx origin. Legacy containers were not
recreated or modified.

Database migration and demo identity provisioning were invoked explicitly:

```text
docker compose --profile tools run --rm setup migrate
docker compose --profile tools run --rm setup provision-demo
```

Normal API startup performs neither operation.

## Smoke Result

- SQL Server: healthy.
- API: healthy.
- UI: running on `127.0.0.1:8088`.
- Legacy `/z-bank/`: HTTP 200 after deployment.
- Modern `/z-bank-new/`: HTTP 200.
- Modern `/z-bank-new/api/session`: HTTP 200.
- Public HTTPS Playwright sign-in/navigation/sign-out: passed.
- Landing page contains both legacy and modern Bank of Z links.

The initial smoke exposed an incorrectly quoted SQL healthcheck password and an
unnecessary runtime dependency on an Ubuntu package mirror. Both were corrected
and the complete deployment was repeated successfully.
