# Product Requirements Document (PRD) & Architecture Specification

## 1. Executive Summary & Core Pitch

The **Agentic Security Simulator** is a visual attack-defense simulator designed for Chief Information Security Officers (CISOs), Security Operations Center (SOC) leads, and security consultants. It translates complex "what-if" security threats into an interactive, visual narrative and a statistically rigorous, repeatable resilience score.

Unlike typical posture assessments or raw vulnerability scans, this platform targets the core questions that sell to enterprise buyers:

| Layer | Customer Value ("What they buy") | Our Differentiator |
| :--- | :--- | :--- |
| **Story** | *“We understand our blast radius”* | Animated agents interacting directly on their dynamic infrastructure topology (cloud, AD, network, applications). |
| **Proof** | *“We can compare security posture over time”* | A repeatable, seed-based Monte Carlo **Resilience Factor ($R$)** composed of core security dimensions, not a simple vanity metric. |
| **Action** | *“We know exactly what to fix first”* | Direct scenario replay that highlights the weakest posture dimension and prioritizes control remediation. |

*The animation sells the meeting; the scoring model and repeatability sell the contract.*

---

## 2. Technology Stack & Architectural Principles

To optimize for local development, ease of hosting, and rapid solo/small-team prototyping, the system utilizes a unified, enterprise-ready Microsoft-centric stack.

```
                  ┌──────────────────────────────────────────────┐
                  │                 Presentation                 │
                  │         Blazor Server + MudBlazor UI         │
                  └──────┬────────────────────────────────┬──────┘
                         │                                │
                         ▼ (SignalR)                      ▼ (HTTP)
                  ┌──────────────────────┐        ┌──────────────────────┐
                  │  Simulation Replay   │        │     Ingestion API    │
                  │   & Graph Viewer     │        │    (/api/v1/twins)   │
                  └──────────────────────┘        └──────────┬───────────┘
                                                             │
                                                             ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                   Core Application                                      │
├──────────────────────────────┬──────────────────────────────┬───────────────────────────┤
│      Digital Twin Engine     │      Attack Catalog (S1-S5)  │    Resilience Scoring     │
│   (Asset Nodes/Trust Edges)  │   (MITRE-Mapped Pipelines)   │      Calculations         │
└──────────────┬───────────────┴──────────────┬───────────────┴──────────────┬────────────┘
               │                              │                              │
               ▼                              ▼                              ▼
┌──────────────────────────────┬──────────────────────────────┬───────────────────────────┐
│       Simulation Farm        │          AI Agents           │       Data Storage        │
│   Deterministic Simulator    │  Microsoft Agent Framework   │  SQL Server / LocalDB     │
│     (500 - 1000 Runs/Batch)  │     (Ollama / Azure OpenAI)  │      (EF Core Access)     │
└──────────────────────────────┴──────────────────────────────┴───────────────────────────┘
```

### Core Stack Selections
*   **User Interface**: **Blazor Server** (via .NET 8). Fast to ship, real-time page updates using SignalR, and ideal for single-developer workflows.
*   **UI Components**: **MudBlazor**. Provides sleek, consistent, and enterprise-grade UI widgets (tables, sliders, wizard dialogs, grids) out of the box with a curated dark mode theme.
*   **Topology Visuals**: Blazor HTML/CSS/SVG templates or lightweight **Cytoscape.js** JavaScript interop. No complex game engines (Unity/Pixi.js) are needed; clean CSS border-color animations and glowing trust edges represent the simulated "red spread."
*   **Backend & Ingestion APIs**: **ASP.NET Core Minimal APIs**. Clean endpoint definition for JSON digital twin uploads.
*   **AI Agents & Planning**: **Microsoft Agent Framework** (the successor to Semantic Kernel). Leveraged for attack planning, defender playbook decisions, and executive report generation.
*   **Database Engine**: **SQL Server** (Express locally, Azure SQL in production). High-performance relational model for assets, runs, events, and metrics using Entity Framework Core.
*   **Simulation Execution**: Parallelized in-memory **Background Services** (`IHostedService` or integrated Hangfire job manager) supporting massive Monte Carlo runs (500–1000 runs per batch) without visual lag.

---

## 3. Data Model & Ingestion Specification

The simulator requires an asset and relationship topology to build its **Digital Twin**. This can be uploaded as a CSV dump or ingested via REST API.

### 3.1 Lansweeper-Style Ingest Schema (CSV)
For local proofs-of-concept, the platform imports a flat CSV schema.

```csv
AssetName,AssetType,IPAddress,FQDN,Domain,OU,OS,Manufacturer,Model,MACAddress,Department,Site,LastPatched,State,CriticalityTag
DC01,Server,10.10.0.10,dc01.corp.contoso.com,corp.contoso.com,"OU=Domain Controllers,DC=corp,DC=contoso,DC=com",Windows Server 2022,Microsoft,Virtual Machine,00-15-5D-01-01-01,IT,HQ-Unreachable,2026-04-01,Active,Crown-Jewel
SRV-PAYROLL,Server,10.10.20.15,payroll.corp.contoso.com,corp.contoso.com,"OU=Finance,DC=corp,DC=contoso,DC=com",Windows Server 2019,Microsoft,Virtual Machine,00-15-5D-02-02-02,Finance,HQ-Unreachable,2026-03-15,Active,Crown-Jewel
SRV-FILE01,Server,10.10.20.20,file01.corp.contoso.com,corp.contoso.com,"OU=File Services,DC=corp,DC=contoso,DC=com",Windows Server 2022,Microsoft,Virtual Machine,00-15-5D-02-02-03,Finance,HQ-Unreachable,2026-05-01,Active,High
WS-FIN-042,Workstation,10.10.30.142,ws-fin-042.corp.contoso.com,corp.contoso.com,"OU=Finance Users,DC=corp,DC=contoso,DC=com",Windows 11 23H2,Dell,OptiPlex,AA-BB-CC-DD-EE-01,Finance,HQ-Unreachable,2026-05-10,Active,Medium
WS-ENG-017,Workstation,10.10.40.17,ws-eng-017.corp.contoso.com,corp.contoso.com,"OU=Engineering,DC=corp,DC=contoso,DC=com",Windows 11 23H2,Lenovo,ThinkPad,AA-BB-CC-DD-EE-02,Engineering,HQ-Unreachable,2026-05-12,Active,Medium
SRV-DEVOPS,Server,10.10.40.50,devops.corp.contoso.com,corp.contoso.com,"OU=Engineering,DC=corp,DC=contoso,DC=com",Ubuntu 22.04,Microsoft,Virtual Machine,00-15-5D-03-03-03,Engineering,HQ-Unreachable,2026-04-20,Active,High
SRV-CICD,Server,10.10.40.55,cicd.corp.contoso.com,corp.contoso.com,"OU=Engineering,DC=corp,DC=contoso,DC=com",Linux,Microsoft,Virtual Machine,00-15-5D-03-03-04,Engineering,HQ-Unreachable,2026-04-25,Active,High
AZ-APP-PAYAPI,CloudApp,,pay-api.azurewebsites.net,corp.contoso.com,,Azure App Service,Microsoft,PaaS,,Engineering,Azure-East,2026-05-01,Active,Crown-Jewel
FW-PERIM,Network,10.10.0.1,gw.corp.contoso.com,corp.contoso.com,,FortiOS 7.4,Fortinet,FortiGate,,IT,HQ-Unreachable,,Active,High
ENTRA-ID,Identity,,login.microsoftonline.com,corp.contoso.com,,Entra ID,Microsoft,SaaS,,IT,Cloud,,Active,Crown-Jewel
```

#### Graph Construction Rules
1.  **Node Types**: Derived from `AssetType` (e.g., `Server`, `Workstation`, `CloudApp`, `Network`, `Identity`).
2.  **Blast Zones**: Mapped using `OU` and `Site` fields (e.g., Finance Subnet, Domain Controller Pool, Engineering Subnet).
3.  **Criticality Mapping**: Mapped via `CriticalityTag` to define weighted nodes inside the core score $R$:
    *   `Crown-Jewel` $\rightarrow$ Weight: $1.0$ (e.g., Domain Controllers, core Database servers)
    *   `High` $\rightarrow$ Weight: $0.7$ (e.g., CI/CD Servers, Developer hosts)
    *   `Medium` $\rightarrow$ Weight: $0.4$ (e.g., standard Workstations)
4.  **Implicit Relationship Synthesis**: To avoid isolated nodes when relational links aren't explicitly provided, the parser auto-generates edges based on common heuristics:
    *   Assets sharing the same IP subnet (`10.10.x.*`) establish `network` communication edges.
    *   Domain Controllers (`OU=Domain Controllers`) establish `identity_trust` edges to and from internal domain assets.
    *   Development servers (`SRV-DEVOPS`, `SRV-CICD`) connect via `deploy` trust to production Apps (`AZ-APP-PAYAPI`).

### 3.2 Canonical JSON API Input
Enterprise systems can ingest digital twins programmatically.
```http
POST /api/v1/twins
Content-Type: application/json

{
  "organizationName": "Contoso Corp",
  "twinName": "Production Active Directory & Cloud Overlay",
  "assets": [
    {
      "assetName": "DC01",
      "assetType": "Server",
      "ipAddress": "10.10.0.10",
      "criticalityTag": "Crown-Jewel"
    },
    {
      "assetName": "AZ-APP-PAYAPI",
      "assetType": "CloudApp",
      "criticalityTag": "Crown-Jewel"
    }
  ],
  "dependencies": [
    {
      "from": "DC01",
      "to": "AZ-APP-PAYAPI",
      "kind": "network"
    }
  ]
}
```

### 3.3 Core SQL Server Schema
To ensure auditability, Monte Carlo runs are persistent.
*   `Organizations`: Multi-tenant boundary.
*   `Twins`: Holds the versioned network blueprint (contains many Nodes & Edges).
*   `Nodes` & `Edges`: Represents the digital twin graph topology.
*   `AttackScenarios`: Pre-scripted catalogs (S1 to S5) mapping back to MITRE ATT&CK techniques.
*   `SimulationBatches`: The root entity for a set of 500-1000 parallel runs with specific seeds and control configuration.
*   `SimulationRuns`: Tracks single runs within a batch (storing final score metrics).
*   `SimulationEvents`: Granular event-log (who, what, target, timestamp, result) used to build replays.
*   `ResilienceScores`: Normalized metric scores calculated at the end of each simulation run.

---

## 4. Attack Scenario Catalog (v1)

The simulator ships with five distinct, pre-packaged scenarios mapping to common CISO nightmare stories:

| Scenario | Title | Narrative & Chain of Event Steps | Exposes / Measures |
| :--- | :--- | :--- | :--- |
| **S1** | **Zero-Day Exploitation** | Initial Access $\rightarrow$ Exploit Public-Facing App (T1190) on `FW-PERIM` $\rightarrow$ Privilege Escalation $\rightarrow$ Lateral Movement to `SRV-DEVOPS`. | Effectiveness of perimeter isolation, WAF rules, and zero-day threat detection times. |
| **S2** | **Ransomware Spread** | Spearfishing Attachment (T1566) on `WS-FIN-042` $\rightarrow$ Local Command Execution (T1059) $\rightarrow$ Lateral Movement via SMB (T1021) $\rightarrow$ Crown Jewel Data Encryption (T1486) on `SRV-PAYROLL`. | Network micro-segmentation, Endpoint Detection and Response (EDR) agents, and Backup RPO/RTO metrics. |
| **S3** | **Credential Theft & PtH** | OS Credential Dumping (T1003) on `WS-ENG-017` $\rightarrow$ Pass the Hash (T1550) $\rightarrow$ Lateral Movement $\rightarrow$ Active Directory Domain Dominance on `DC01` via `ENTRA-ID`. | Identity access management (IAM), AD tiering, and credential guard effectiveness. |
| **S4** | **Supply Chain Compromise** | Compromise Software Dependency (T1195) $\rightarrow$ Infiltrate `SRV-CICD` pipeline $\rightarrow$ Code injection during build $\rightarrow$ Deploy backdoor to production `AZ-APP-PAYAPI`. | Trust boundary segregation, application code signing, and server outbound connection controls. |
| **S5** | **Insider Data Exfiltration** | Insider Account Creation (T1078) $\rightarrow$ Data Collection (T1115) from `SRV-FILE01` $\rightarrow$ Data Exfiltration over Web protocol (T1048) to external site. | Data Loss Prevention (DLP), User and Entity Behavior Analytics (UEBA), and outbound egress filters. |

---

## 5. Monte Carlo Engine & AI Cost Model

To support realistic statistics without incurring massive API charges or non-deterministic lag, the simulator divides execution into two operational tiers.

### 5.1 Hybrid Execution Model

```
                       ┌─────────────────────────────────────┐
                       │           Simulation Start          │
                       └──────────────────┬──────────────────┘
                                          │
                                          ▼
                       ┌─────────────────────────────────────┐
                       │      AI Agent Call (Batch Plan)     │
                       │   Red/Blue Strategist plots path    │
                       └──────────────────┬──────────────────┘
                                          │
                                          ▼
                      ┌───────────────────────────────────────┐
                      │    Generate Attack Plan Template      │
                      │  (JSON layout mapping path variables) │
                      └──────────────────┬──────────────────┘
                                         │
                 ┌───────────────────────┴───────────────────────┐
                 │                                               │ (Batch Loop)
                 ▼                                               ▼
     ┌───────────────────────┐                       ┌───────────────────────┐
     │       Run #1          │                       │      Run #N (1000)    │
     ├───────────────────────┤                       ├───────────────────────┤
     │ Deterministic Graph   │                       │ Deterministic Graph   │
     │ Execution Engine      │                       │ Execution Engine      │
     │ + Parameter Jitter    │                       │ + Parameter Jitter    │
     │ (Control effectiveness│                       │ (Control effectiveness│
     │  sliders, backup fail,│                       │  sliders, backup fail,│
     │  MTTD/MTTC variance)  │                       │  MTTD/MTTC variance)  │
     └───────────┬───────────┘                       └───────────┬───────────┘
                 │                                               │
                 └───────────────────────┬───────────────────────┘
                                         │
                                         ▼
                       ┌─────────────────────────────────────┐
                       │     Batch Aggregation & Metrics     │
                       │     Distribution (Mean, P10, P90)   │
                       └──────────────────┬──────────────────┘
                                          │
                                          ▼
                       ┌─────────────────────────────────────┐
                       │       Narrative AI Agent Call       │
                       │     Executive Summary Generator     │
                       └─────────────────────────────────────┘
```

1.  **AI Planning Tier (Low Cost/Batch-Level)**:
    *   At the start of a simulation batch, a **Red Agent** (Microsoft Agent Framework) evaluates the twin topology once. It queries target assets and outputs a high-level attack graph layout (technique sequences, branch forks, priority routes).
    *   A **Blue Agent** generates defensive playbooks, specifying which telemetry and isolation controls are active.
    *   This generates an **Attack-Defense Plan Template** (low token cost).
2.  **Deterministic Engine Tier (No Cost/Run-Level)**:
    *   The **Simulation Farm** spawns $500$ to $1000$ independent threads.
    *   Each thread takes the Plan Template and executes it deterministically against the graph topology using standard path-finding and probability state transitions.
    *   **Stochastic/Monte Carlo Jitter** is injected on every tick:
        *   *EDR effectiveness*: Slider sets probability ($P$) of detecting lateral movement.
        *   *Patch Lag*: Dynamic modifiers to the probability of exploiting an unpatched target.
        *   *MTTD/MTTC variation*: Timestamps for alerts and containment fluctuate within defined standard deviations.
        *   *Backup Failure Probability*: $P(\text{Recovery Fail})$ based on configuration.
3.  **Auditability & Reproducibility**:
    *   Every batch stores a static `Integer Seed` and `Configuration Parameters JSON` in the SQL Database.
    *   Running the engine with the exact same seed reproduces the exact same Monte Carlo distributions, allowing consultants to mathematically prove posture improvement after changes are implemented.

### 5.2 AI API Cost Capping (Production)
*   **Local POC Mode ($0)**: Uses local LLMs (e.g., `llama3.2` or `phi3` via an Ollama endpoint) or falls back to rule-based pre-built attack planners.
*   **Enterprise Production Mode (< $20/assessment)**:
    *   Maximum 1 Batch Planner Call ($\approx$ 3k-5k prompt tokens).
    *   Spot-checking: Only 1 in $50$ runs execute with real-time agent forks using small cost models (`gpt-4o-mini`).
    *   A final summary call is made to a **Narrative Agent** to write a human-readable summary of the worst-case scenario.

---

## 6. Resilience Scoring Model ($R$)

The score $R$ represents the overall capability of the organization's system to survive a scenario. It is calculated per run and aggregated at the batch level.

$$R = \alpha \cdot \text{Availability} + \beta \cdot \text{Detection} + \gamma \cdot \text{Containment} + \delta \cdot \text{Recovery} - \epsilon \cdot \text{BlastRadius}$$

### 6.1 Dimension Definitions
1.  **Availability**: The ratio of critical nodes (Crown Jewels) that remained uncompromised and fully functional throughout the simulation.
2.  **Detection**: Measured against Mean Time to Detect (MTTD) compared to target Service Level Objectives (SLOs).
    $$\text{Detection Score} = 1.0 - \min\left(1.0, \frac{\text{MTTD}}{\text{SLO}_{\text{Detection}}}\right)$$
3.  **Containment**: Measures the speed of blocking lateral spread relative to Mean Time to Contain (MTTC).
    $$\text{Containment Score} = 1.0 - \min\left(1.0, \frac{\text{MTTC}}{\text{SLO}_{\text{Containment}}}\right)$$
4.  **Recovery**: Measures data restoration speed (compared to Backup RPO/RTO sliders) and data loss margins.
5.  **Blast Radius (Inverted)**: The percentage of the total topology compromised by the adversary, weighted by the criticality tag of each node.

*Note: All weights ($\alpha, \beta, \gamma, \delta, \epsilon$) and SLO thresholds are stored in the SQL configuration tables, allowing consultants to tune them for finance, healthcare, or military infrastructures without rewriting code.*

---

## 7. Blazor Screen Specifications

The UI utilizes a high-contrast, dark-mode design system courtesy of MudBlazor, ensuring immediate visual appeal for executive sales.

### 1. Ingest / Import Twin
*   **Functionality**: File drag-and-drop support for Lansweeper CSVs, direct JSON text editing, or sample quickstarts.
*   **Visual Elements**: Validation data grid displaying parsed assets, flags for unresolved network zones, and dynamic configuration feedback.

### 2. Topology View
*   **Functionality**: Interactive layout of nodes and connection edges.
*   **Visual Elements**:
    *   Nodes colored by subnet/zone and styled by type (Servers $\rightarrow$ Server icons, Cloud $\rightarrow$ Cloud icons).
    *   Crown Jewels marked with golden lock badges.
    *   Adversary foothold highlighted with a glowing red perimeter.

### 3. Scenario Configuration Wizard
*   **Functionality**: Choose which attack scenarios to run (S1 to S5 checkboxes) and specify Monte Carlo simulation constraints.
*   **Visual Elements**:
    *   *Parameters Sliders*: EDR Maturity (%), Backup RPO (Hours), Patching Lag (Days).
    *   *Simulation Count Selector*: $500$ or $1000$ runs.

### 4. Results & Statistical Distribution Dashboard
*   **Functionality**: Summary metrics from the completed simulation batch.
*   **Visual Elements**:
    *   A primary composite **Resilience Score** radial dial.
    *   A radar chart displaying the 5 core dimensions: Availability, Detection, Containment, Recovery, and Blast Radius (Inverted).
    *   Histogram detailing the distribution of $R$ (visualizing the $P(R < 40)$ risk profile).
    *   "Weakest Link" diagnostic alert (e.g., *"In 73% of simulations, Containment speed was the failure vector"*).

### 5. Playback / Replay Arena
*   **Functionality**: Step-by-step playback of a representative run (the worst-case or median scenario).
*   **Visual Elements**:
    *   Timeline scrub bar with play, pause, speed, and step-forward controls.
    *   Live topology node updates: edges turn red and pulse during lateral movement, shields appear when blue actions trigger, and target nodes lock down or show "compromised" states.

---

## 8. Implementation Plan & Phased Roadmap

A structured 6-phase sequence designed to produce a highly marketable demo within weeks while keeping future enterprise additions clean.

```
┌────────────────────────────────────────────────────────┐
│ Phase 1: Digital Twin & Ingest (Weeks 1-2)             │
│ - Implement CSV/JSON parsers + SQL Db storage          │
│ - Build Blazor Ingest interface & Topology preview     │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ Phase 2: Core Simulation Engine (Weeks 3-4)            │
│ - Design deterministic state engine + S2 Scenario      │
│ - Ingest parameter sliders and run Monte Carlo loops   │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ Phase 3: Analytics & Composite R (Weeks 5-6)           │
│ - Formulate dimensional sub-scores and weights         │
│ - Develop statistical metrics dashboards & histograms  │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ Phase 4: Dynamic Replay & UI Polish (Weeks 7-8)        │
│ - Create SignalR playback engine                       │
│ - Implement interactive timeline visualizer            │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ Phase 5: Microsoft Agent Integration (Weeks 9-10)      │
│ - Integrate Microsoft Agent Framework                  │
│ - Replace static planners with Ollama/Azure OpenAI     │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ Phase 6: Enterprise Hardening (Ongoing)                │
│ - Implement Entra ID authentication                   │
│ - Secure PDF report outputs & API hooks for GRC        │
└────────────────────────────────────────────────────────┘
```

*This roadmap guarantees a highly interactive sales tool at Phase 4, postponing heavy AI token costs and operational integration until after the value proposition is fully validated.*
