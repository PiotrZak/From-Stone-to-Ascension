# TTS — Technical Architecture (High Overview)

**Project:** [From-Stone-to-Ascension](https://github.com/PiotrZak/From-Stone-to-Ascension)  
**Use for:** LinkedIn / Medium / README embeds  
**Source:** [`architecture-technical-overview.mmd`](architecture-technical-overview.mmd)  
**Full doc:** [`architecture-overview.md`](../architecture-overview.md)

---

## Diagram

```mermaid
flowchart TB
    subgraph presentation ["Presentation layer"]
        direction LR
        WEB["TTS.Web<br/>React · Vite · governor dashboard"]
        CLI["TTS.Game<br/>Console · Orleans client"]
        OFFLINE["TTS.Agents<br/>Offline LLM scenarios"]
    end

    subgraph gateway ["Gateway layer"]
        direction LR
        API["TTS.Api<br/>ASP.NET Core REST"]
        REG["MatchRegistry<br/>Lobby · join codes"]
        BG["MatchTickBackgroundService<br/>Tick fallback"]
    end

    subgraph contracts ["Contracts"]
        IC["TTS.Contracts<br/>IWorldGrain · wire DTOs"]
    end

    subgraph orleans ["Microsoft Orleans — distributed runtime"]
        direction TB
        SILO["TTS.Server<br/>Orleans silo cluster"]
        WG["WorldGrain<br/>1 virtual actor per match"]
        TIMER["IGrainTimer<br/>Scheduled wall-clock ticks"]
        SILO --> WG
        TIMER --> WG
    end

    subgraph core ["TTS.Core — authoritative simulation"]
        direction TB
        MH["MatchHost<br/>Load · save · tick scheduler"]
        GL["GameLoop"]
        TP["TurnPhasePipeline"]
        SYS["Systems<br/>tech · crime · factions · gates · events · stability"]
        GTS["GameToolSurface<br/>Validated agent read/write API"]
        MH --> GL --> TP --> SYS
        GTS --> SYS
    end

    subgraph maf ["TTS.Llm — Microsoft Agent Framework"]
        direction TB
        LTA["LlmTurnAgent"]
        ATW["AgentToolWorkflow<br/>ChatClientAgent · tool loop"]
        BR["AgentGameToolBridge"]
        REGTOOLS["GameToolRegistry<br/>propose_research · diplomacy · …"]
        LTA --> ATW --> BR --> REGTOOLS --> GTS
    end

    subgraph data ["Data & persistence"]
        direction LR
        JSON["catalog.json<br/>~70 tech nodes"]
        CSV["Crime / income CSV<br/>TTS 4+ regions"]
        SAVE["Match JSON<br/>grain persistence"]
    end

    WEB --> API
    CLI --> SILO
    CLI -.->|local dev| MH
    OFFLINE --> ATW

    API --> IC
    BG --> IC
    IC --> WG

    WG --> MH
    TP --> LTA

    SYS --> JSON
    SYS --> CSV
    MH --> SAVE

    classDef authoritative fill:#1a3a2a,stroke:#3d9970,color:#e8f5ee
    classDef orleans fill:#1a2a3a,stroke:#4a90d9,color:#e8f0fa
    classDef maf fill:#2a1a3a,stroke:#9b59b6,color:#f3e8fa
    classDef client fill:#2a2a1a,stroke:#c9a227,color:#faf6e8

    class MH,GL,TP,SYS,GTS authoritative
    class SILO,WG,TIMER orleans
    class LTA,ATW,BR,REGTOOLS maf
    class WEB,CLI,OFFLINE client
```

---

## Layer summary

| Layer | Projects | Role |
|-------|----------|------|
| **Presentation** | `TTS.Web`, `TTS.Game`, `TTS.Agents` | Dashboard, CLI, offline agent scenarios |
| **Gateway** | `TTS.Api` | REST API, lobby, tick fallback service |
| **Contracts** | `TTS.Contracts` | `IWorldGrain` + DTOs — API ↔ Orleans boundary |
| **Orleans** | `TTS.Server`, `TTS.Grains` | One `WorldGrain` per match; timers; JSON persistence |
| **Core** | `TTS.Core` | **Authoritative** rules — `MatchHost` → `GameLoop` → systems |
| **MAF** | `TTS.Llm` | Microsoft Agent Framework tool workflows (TTS 5+) |
| **Data** | `src/data/`, saves | Tech catalog, crime CSV, match JSON |

**Rule:** clients and LLMs never write game state directly. Agents use `GameToolSurface` only; the engine validates every action.

---

## Export as PNG (LinkedIn / Medium)

```bash
npx @mermaid-js/mermaid-cli \
  -i assets/architecture-technical-overview.mmd \
  -o assets/architecture-technical-overview.png \
  -b transparent \
  -w 2400
```

Or open [mermaid.live](https://mermaid.live), paste contents of `architecture-technical-overview.mmd`, export PNG/SVG.

---

## Simplified one-glance flow

```
Player (browser)
    → TTS.Api (REST)
        → TTS.Contracts (IWorldGrain)
            → WorldGrain (Orleans, 1 per match)
                → MatchHost (TTS.Core)
                    → GameLoop → Systems
                    → GameToolSurface ← MAF agents (TTS 5+)
                → JSON persistence
```
