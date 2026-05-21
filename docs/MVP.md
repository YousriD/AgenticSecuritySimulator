# Agentic Security Simulator — MVP

## Stack

| Layer | Technology |
|-------|------------|
| UI | Blazor Server + MudBlazor |
| API | ASP.NET Core minimal APIs (`/api/v1/twins`) |
| Core | Graph twin, Lansweeper CSV ingest, resilience scoring |
| Simulation | Deterministic Monte Carlo engine (500–1000 runs) |
| Agents | Rule-based planners (POC $0); Microsoft Agent Framework package referenced for Phase 5 |
| Database | SQL Server / LocalDB via EF Core |

## Projects

- `AgenticSecuritySimulator.Web` — Blazor UI + API + EF Core
- `AgenticSecuritySimulator.Core` — Domain, ingest, scoring, scenarios
- `AgenticSecuritySimulator.Simulation` — Batch orchestrator + engine
- `AgenticSecuritySimulator.Agents` — Red/blue/narrative planner abstractions

## Screens

1. **Upload twin** — intelligent CSV analysis (Lansweeper, unified digital-twin, or generic). Supports `company_digital_twin_all_in_one.csv` format: devices, servers, network_devices, security_controls, network_links.
2. **Topology** — SVG graph + asset table
3. **Scenarios** — S1–S5 multi-select, EDR/RPO sliders, run count
4. **Results** — Mean R, P10/P90, weakest dimension, histogram
5. **Replay** — Timeline of median run events

## Attack catalogue (v1)

| ID | Scenario |
|----|----------|
| S1 | Zero-day exploitation |
| S2 | Ransomware |
| S3 | Credential theft / PtH |
| S4 | Supply chain compromise |
| S5 | Insider data exfiltration |

Definitions: `data/scenarios/*.json`

## Resilience factor

Per run: weighted composite of Availability, Detection, Containment, Recovery, and inverted Blast radius (see `ResilienceCalculator`).

Batch output: mean, P10, P90, weakest dimension frequency.

## Run locally

```bash
cd src/AgenticSecuritySimulator.Web
dotnet run
```

Open https://localhost:5xxx (see launchSettings). Use **Quick start** on Home or POST ` /api/v1/twins/import-sample`.

### Database

Default: **SQLite** (`agentic-security.db` in the Web project folder). Set `Database:Provider` to `SqlServer` and use the `SqlServer` connection string for LocalDB/Azure SQL. Full schema script: `database/scripts/001_CreateSchema.sql`.

## API

```http
POST /api/v1/twins
Content-Type: application/json

{
  "organizationName": "Contoso",
  "twinName": "API Twin",
  "assets": [ { "assetName": "SRV-01", "assetType": "Server", "criticalityTag": "High" } ],
  "dependencies": [ { "from": "SRV-01", "to": "DC01", "kind": "network" } ]
}
```

## POC vs production AI

| Mode | LLM | Cost |
|------|-----|------|
| POC (default) | None — rule-based agents | $0 |
| Phase 5 | Microsoft Agent Framework + Ollama or Azure OpenAI | Target ~$20/assessment |

## Next sprints

- S1: Lansweeper field mapping hardening
- S2: Hangfire/background batch for 1000-run UX
- S3: Agent Framework + Ollama batch planner
- S4: Entra ID auth, Azure SQL deploy
- S5: PDF executive summary
