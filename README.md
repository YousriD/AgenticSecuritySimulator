# Agentic Security Simulator

Monte Carlo infrastructure resilience simulator for CISO teams — digital twin, red/blue agent attack paths, and composite resilience score **R**.

## Quick start

```bash
dotnet restore
cd src/AgenticSecuritySimulator.Web
dotnet run
```

1. Open the app in your browser.
2. Click **Quick start: load Contoso sample twin** or upload `data/samples/lansweeper-dummy-export.csv`.
3. Configure scenarios (S1–S5) and run count (500–1000).
4. Review results and replay.

## Documentation

See [docs/MVP.md](docs/MVP.md) for architecture, API, and sprint plan.

## Solution layout

```
src/
  AgenticSecuritySimulator.Web/       # Blazor Server + API
  AgenticSecuritySimulator.Core/      # Twin, scoring, ingest
  AgenticSecuritySimulator.Simulation/
  AgenticSecuritySimulator.Agents/
data/samples/                         # Lansweeper-style dummy CSV
data/scenarios/                       # S1–S5 attack definitions
database/scripts/                     # SQL Server schema
```
