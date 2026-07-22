# Stage 6 Pass 009 Evidence Manifest

Immutable review revision: `5c7d3351ec8e62af2cbf5dd44b26b6bdccdd6cf7`.

| Evidence | Purpose | SHA-256 |
|---|---|---|
| `codex-consolidator.md` | Fresh Codex full-scope inventory and findings | generated in repository |
| `claude-round1.json` | Claude initial independent review (`clean`) | `ECE6C7EEBC34819F2A9BAD5D34004D76625A4252E7624FBB0C8B92088BD62A97` |
| `claude-round2-c1.json` | First transported challenge item | `EE19E9554C50150580DE2E79A7C54CDD42A84CA272608091E0BAE2AC15A3745D` |
| `claude-round2-c2-c8.json` | Remaining challenge items and final adjudication | `04C6C67C666D9214F6E19985C814B830D75BC3F42EBB60A2F33DBCDA64761219` |

Claude interaction 27 used one fresh session. Its initial review consumed 39
turns. Round 2 was transported in two CLI invocations because the first
Windows command-line argument was truncated after C1; the same session and
same discussion round received C2-C8 over standard input. No model context
overflow occurred. Claude's final result was `findings`.
