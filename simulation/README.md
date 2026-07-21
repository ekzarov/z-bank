# Bank of Z Stage 3 Legacy Simulator

This is a traceable substitute walkthrough for the unavailable IBM runtime. It
exercises source- and contract-derived observable behavior for CICS, IMS, DB2
persistence transitions, REST mappings, the existing web UI, and the monthly
statement batch. It does not execute COBOL/PL/I, CICS, IMS, DB2, 3270, Liberty,
or z/OS Connect and therefore never counts as live legacy verification.

Every response includes `X-Simulation-Only: true`. Relevant routes also include
an `X-Legacy-Evidence` header. The governed evidence index, including workbook
row numbers, is in `fixtures/legacy-fixture.json` and available from
`GET /simulation/evidence`.

## Run the repeatable harness

```powershell
cd simulation
npm test
npm run walkthrough
```

## Run the original legacy web UI against the simulator

```powershell
docker compose -f simulation/docker-compose.yml up --build -d
```

Open `http://localhost:3001/`. The original files under `legacy/` are mounted
read-only. The simulator API is also available at `http://localhost:9180/`.

Useful identifiers:

- CICS customer: `C0000000001`
- CICS empty portfolio: `C0000000099`
- IMS customer: `I000000001`
- CICS accounts: `10000001`, `10000002`
- IMS accounts: `101`, `102`
- IMS password for the message harness: `password`

Stop the contour with:

```powershell
docker compose -f simulation/docker-compose.yml down
```

The real legacy deployment under `deploy/` is deliberately separate and
unchanged. It remains the correct contour when authorized IBM infrastructure
becomes available.
