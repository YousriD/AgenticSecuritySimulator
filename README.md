# 🛡️ Agentic Security Simulator

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?style=for-the-badge&logo=blazor&logoColor=white)
![MudBlazor](https://img.shields.io/badge/MudBlazor-9-594AE2?style=for-the-badge&logo=mui&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-ORM-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-Default_DB-003B57?style=for-the-badge&logo=sqlite&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-blue?style=for-the-badge)

**Monte Carlo infrastructure resilience simulator for CISO teams and executive leadership.**  
Upload your digital twin, watch attacks unfold live, and get a plain-English resilience verdict with prioritised actions.

[Quick Start](#-quick-start) · [Screens](#-screens) · [Architecture](#-architecture) · [Attack Scenarios](#-attack-scenarios) · [Resilience Score](#-resilience-score-r) · [API](#-api) · [Roadmap](#-roadmap)

</div>

---

## ✨ What It Does

The **Agentic Security Simulator** translates complex "what-if" security threats into an interactive visual narrative and a repeatable resilience score — the three things that matter to enterprise buyers:

| Layer | Customer Value | Our Differentiator |
|:---|:---|:---|
| **Story** | *"We understand our blast radius"* | Live topology map animates as each attack run executes — nodes turn red, blue defences fire, zones flash |
| **Proof** | *"We can compare posture over time"* | Seed-based Monte Carlo **Resilience Factor R** — statistically rigorous, not a vanity metric |
| **Action** | *"We know exactly what to fix first"* | Executive report with plain-English verdict, business-impact cards, and top 3 priority actions |

> The animation sells the meeting. The scoring model sells the contract.

---

## 🚀 Quick Start

```bash
dotnet restore
cd src/AgenticSecuritySimulator.Web
dotnet run
```

1. Open the app in your browser (see console for the URL).
2. Click **Quick start: load Contoso sample twin** — or upload `data/samples/lansweeper-dummy-export.csv`.
3. Select scenarios (S1–S5), tune EDR/RPO sliders, set run count (100–1000).
4. Watch the **live simulation feed** as the batch runs — topology map updates in real time.
5. Review the **executive resilience report** and step through the **attack replay**.

---

## 🖥️ Screens

### Live Simulation Feed
While the Monte Carlo batch runs, the Scenarios page shows a live panel — no waiting, no blank screen:
- Progress bar with completed runs, live mean R, min and max scores
- Event timeline of the latest run updating every few runs
- SVG topology map animating in real time: nodes turn red when compromised, green when blocked by EDR, grey when isolated

### Executive Resilience Report
Designed for non-technical leadership, not just the security team:
- **Verdict banner** — a plain-English rating (Strong / Resilient / Moderate / At Risk / Critical) with a one-sentence summary and a visual score gauge
- **Six business-impact cards** — Critical Asset Protection, Threat Detection Speed, Stopping the Spread, Recovery Capability, Defence Consistency, Overall Risk — each written in business language with a clear status indicator
- **Top 3 priority actions** — ranked by urgency, with concrete steps the security team should take
- **Technical detail** — collapsed by default, expandable for the security team: dimension scores, P10/P90, histogram

### Attack Replay Arena
Step through the median simulation run event-by-event:
- Play / Pause / Step forward & back / Jump to end
- Scrub bar for direct timeline navigation
- Bookmarks — mark key moments and jump between them
- Actor filter — show only red (attacker) or blue (defender) events
- Run selector — switch between median, worst-case, and best-case runs
- Animated SVG map with glow effects, zone highlighting, and a callout for the current event
- Resilience dimension bars at the bottom of the map

---

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────────┐
│                      Blazor Server UI                    │
│                  MudBlazor 9 · Dark Mode                 │
└──────────┬───────────────────────────────────┬───────────┘
           │ SignalR (live feed + replay)       │ HTTP
           ▼                                   ▼
  ┌─────────────────┐                ┌──────────────────────┐
  │  ReplayHub      │                │  Ingestion API       │
  │  Live feed hub  │                │  /api/v1/twins       │
  └─────────────────┘                └──────────┬───────────┘
                                                │
┌───────────────────────────────────────────────▼──────────┐
│                      Core Application                    │
│   Digital Twin Engine · Attack Catalog · Resilience R    │
└──────────┬──────────────────┬──────────────────┬─────────┘
           ▼                  ▼                  ▼
  ┌──────────────┐  ┌──────────────────┐  ┌────────────────┐
  │  Simulation  │  │    AI Agents     │  │  SQL Server /  │
  │  Farm        │  │  (Agent Fwk /    │  │  SQLite        │
  │  100–1000    │  │   Rule-based)    │  │  EF Core       │
  │  runs/batch  │  └──────────────────┘  └────────────────┘
  └──────────────┘
```

### Stack

| Layer | Technology |
|---|---|
| UI | Blazor Server + MudBlazor 9 (dark mode) |
| Real-time | SignalR — live batch feed + replay streaming |
| API | ASP.NET Core Minimal APIs |
| Core | Graph twin, CSV ingest, resilience scoring |
| Simulation | Deterministic Monte Carlo (100–1000 runs), per-run progress callbacks |
| Agents | Rule-based planners (POC, $0); Microsoft Agent Framework for Phase 5 |
| Database | SQLite (default) or SQL Server via EF Core |

---

## 📁 Solution Layout

```
src/
  AgenticSecuritySimulator.Web/
    Components/Pages/
      Scenarios.razor       # Run config + live simulation feed
      Results.razor         # Executive resilience report
      Replay.razor          # Interactive attack replay arena
      Topology.razor        # Infrastructure graph viewer
      UploadTwin.razor      # CSV / JSON twin import
    Hubs/
      ReplayHub.cs          # SignalR hub for replay streaming
    Services/
      BatchRunService.cs    # Batch orchestration + persistence
      ReplayStateService.cs # Replay session state (bookmarks, speed)
      TopologyLayoutService.cs
  AgenticSecuritySimulator.Core/    # Twin, scoring, ingest
  AgenticSecuritySimulator.Simulation/  # Monte Carlo engine
  AgenticSecuritySimulator.Agents/      # Red/blue/narrative planners
data/
  samples/                  # Lansweeper-style dummy CSV
  scenarios/                # S1–S5 attack definitions (JSON)
database/
  scripts/                  # SQL Server schema
docs/
  MVP.md                    # Architecture, API, sprint plan
  requirements.md           # Full PRD & spec
```

---

## ⚔️ Attack Scenarios

Five pre-packaged MITRE ATT&CK-mapped scenarios:

| ID | Scenario | Attack Chain | Measures |
|:---|:---|:---|:---|
| **S1** | Zero-Day Exploitation | Exploit perimeter (T1190) → Privilege Escalation → Lateral Movement | Perimeter isolation, WAF, zero-day detection |
| **S2** | Ransomware Spread | Spearphishing (T1566) → Execution (T1059) → SMB lateral (T1021) → Encryption (T1486) | Micro-segmentation, EDR, Backup RPO/RTO |
| **S3** | Credential Theft / PtH | Credential dump (T1003) → Pass-the-Hash (T1550) → AD Domain Dominance | IAM, AD tiering, Credential Guard |
| **S4** | Supply Chain Compromise | Dependency compromise (T1195) → CI/CD infiltration → Backdoor deploy | Trust boundaries, code signing, egress controls |
| **S5** | Insider Data Exfiltration | Rogue account (T1078) → Data collection (T1115) → Exfil over web (T1048) | DLP, UEBA, outbound egress filters |

Scenario definitions live in `data/scenarios/*.json`.

---

## 📊 Resilience Score R

Each simulation run produces a composite score:

$$R = \alpha \cdot \text{Availability} + \beta \cdot \text{Detection} + \gamma \cdot \text{Containment} + \delta \cdot \text{Recovery} - \epsilon \cdot \text{BlastRadius}$$

| Dimension | What It Measures |
|---|---|
| Availability | Crown Jewel nodes that stayed uncompromised |
| Detection | Mean Time to Detect (MTTD) vs. SLO target |
| Containment | Mean Time to Contain (MTTC) vs. SLO target |
| Recovery | Backup RPO/RTO adherence |
| Blast Radius (inverted) | % of topology compromised, weighted by criticality |

Batch output: **mean R**, **P10**, **P90**, weakest dimension frequency.

### Executive Rating Scale

| Score | Rating | Meaning |
|---|---|---|
| 80–100 | **Strong** | Controls working well, maintain posture |
| 65–79 | **Resilient** | Good overall, targeted improvements needed |
| 45–64 | **Moderate** | Clear gaps, action needed within 90 days |
| 25–44 | **At Risk** | Significant exposure, urgent attention required |
| 0–24 | **Critical** | Immediate escalation required |

All weights and SLO thresholds are stored in the database — tunable per industry (finance, healthcare, defence) without code changes.

---

## 🔌 API

### Import a digital twin via JSON

```http
POST /api/v1/twins
Content-Type: application/json

{
  "organizationName": "Contoso Corp",
  "twinName": "Production AD & Cloud Overlay",
  "assets": [
    { "assetName": "DC01",          "assetType": "Server",   "criticalityTag": "Crown-Jewel" },
    { "assetName": "AZ-APP-PAYAPI", "assetType": "CloudApp", "criticalityTag": "Crown-Jewel" }
  ],
  "dependencies": [
    { "from": "DC01", "to": "AZ-APP-PAYAPI", "kind": "network" }
  ]
}
```

### Load the built-in Contoso sample

```http
POST /api/v1/twins/import-sample
```

### CSV ingest (Lansweeper-style)

Upload `data/samples/lansweeper-dummy-export.csv` via the UI drag-and-drop, or the unified format `data/samples/company_digital_twin_all_in_one.csv`.

---

## 🗄️ Database

Default: **SQLite** (`agentic-security.db` in the Web project folder) — zero config, runs anywhere.

To switch to SQL Server / LocalDB:

```json
// appsettings.json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionStrings": {
      "SqlServer": "Server=(localdb)\\mssqllocaldb;Database=AgenticSecurity;Trusted_Connection=True"
    }
  }
}
```

Full schema: `database/scripts/001_CreateSchema.sql`.

---

## 🤖 AI Agents & Cost Model

| Mode | Engine | Cost |
|---|---|---|
| POC (default) | Rule-based planners — no LLM calls | **$0** |
| Phase 5 | Microsoft Agent Framework + Ollama (`llama3.2` / `phi3`) | ~$0 local |
| Enterprise | Agent Framework + Azure OpenAI (`gpt-4o-mini`) | **< $20 / assessment** |

The Monte Carlo engine is fully deterministic — AI is only called once per batch (planning) and once at the end (narrative summary), keeping costs minimal.

---

## 🗺️ Roadmap

| Phase | Scope | Status |
|---|---|---|
| 1 | Digital Twin & CSV/JSON Ingest | ✅ Done |
| 2 | Core Monte Carlo Simulation Engine | ✅ Done |
| 3 | Analytics & Composite R Dashboard | ✅ Done |
| 4 | Dynamic Replay, Live Feed & Executive Report | ✅ Done |
| 5 | Microsoft Agent Framework + Ollama/Azure OpenAI | 🔜 Planned |
| 6 | Entra ID auth, Azure SQL, PDF executive reports | 🔜 Planned |

### Phase 4 — What was delivered

- **Live simulation feed** — topology map and event timeline animate in real time as the Monte Carlo batch runs; no waiting for completion
- **SignalR ReplayHub** — streams replay steps to all connected clients; session state (bookmarks, speed, run mode) persists across re-renders
- **Attack Replay Arena** — full playback controls (play/pause/step/scrub/bookmarks), actor filter, run selector (median/worst/best), animated SVG map with glow effects
- **Executive Resilience Report** — plain-English verdict with score gauge, six business-impact cards, top 3 priority actions ranked by urgency; technical detail collapsed for the security team

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 👤 Developer

**Yousri Dardouri**  
📧 [yousri.dardouri@gmail.com](mailto:yousri.dardouri@gmail.com)

---

<div align="center">
  <sub>Built with ❤️ using .NET 10, Blazor Server, and MudBlazor</sub>
</div>
