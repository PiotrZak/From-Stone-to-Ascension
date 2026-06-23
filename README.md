# From Stone to Ascension

**TTS — Technology Tier Simulation**

An async grand-strategy civilization sim where matches advance through **Technology Tier Systems (TTS 1–8+)**. Each era changes the rules — not just the stats. You play as a **governor**: check in between ticks, resolve crises, set policy, and steer your civ toward victory while rivals and the world evolve on a schedule.

> Advancing technology doesn’t just make you stronger — it changes what strength means.

---

## What’s in this repo

| Layer | Project | Role |
|-------|---------|------|
| Simulation | `TTS.Core` | Authoritative game loop, systems, save/load |
| Multiplayer | `TTS.Server` + `TTS.Grains` | Orleans silo — one grain per match |
| API | `TTS.Api` | REST gateway, lobby, join codes |
| UI | `TTS.Web` | React governor dashboard |
| LLM | `TTS.Llm` | Optional Ollama/cloud agents (rivals TTS 5+, gate fables) |
| CLI | `TTS.Game` | Local demo, status, Orleans client |
| Scenarios | `TTS.Agents` | Offline Ollama test scenarios |

**Default match start:** TTS 4 (Information Age) with a curated tech spine. Classic stone-age ascent is available as `classic-stone`.

---

## Quick start

**Requirements:** .NET 10, Node.js, npm. Ollama optional (local LLM narratives and rival agents).

```bash
./dev.sh
```

| Service | URL |
|---------|-----|
| Web UI | http://localhost:5173 |
| API | http://localhost:5000 |
| Ollama | http://localhost:11434 (if installed) |

1. Open the UI → **Create or join** a match.
2. Share the join code with other players.
3. **Ready up** → host starts the match.
4. Return between ticks → resolve **decision gates**, skim **while you were away**, adjust **policy**.

For manual stack startup or CLI usage, see [current-state.md](current-state.md).

---

## How a match works

**Async by design.** Ticks fire on a wall-clock interval (8h–48h depending on mode, or 3 min in dev). The simulation runs automatically while you’re away.

**Each tick (simplified):**

1. Expire overdue gates → apply default choice  
2. Civs research via **policy** (not manual tech picks)  
3. Diffusion, factions, crime (TTS 4+), events  
4. New **decision gates** may appear (tier shifts, crime spikes, forbidden tech, crises)  
5. Turn history saved for the away summary  

**Your job as governor:**

- Resolve gates before the timer runs out  
- Set research policy (balanced, tech rush, stability-first, …)  
- Watch rivals and stability — advancement increases power *and* fragility  

**Victory:** reach the mode’s target tier with enough average stability. Details per mode in [match-modes.md](match-modes.md).

---

## Technology tiers (TTS 1–8)

| TTS | Era | Gameplay shift |
|-----|-----|----------------|
| 1 | Pre-Industrial | Survival, agriculture, early expansion |
| 2 | Industrial | Factories, pollution, social unrest |
| 3 | Early Electronics | Grids, radio, early computing |
| 4 | **Information Age** | Internet, data, digital warfare, crime perspective |
| 5 | Early AI | Autonomous systems, LLM rival agents unlock |
| 6 | Bio / Nano | Genetic engineering, nanotech |
| 7 | Temporal | Time manipulation, paradox risk |
| 8+ | Post-Singularity | Superintelligence, reality-level systems |

Full sub-tree design: [tech-trees-by-tier.md](tech-trees-by-tier.md).

---

## Match modes

| Mode ID | Pace | Players | Start | Victory target |
|---------|------|---------|-------|----------------|
| `sprint-8h` | 8h | 2–4 | TTS 4 | TTS 6 |
| `blitz-24h` | 24h | 2–6 | TTS 4 | TTS 7 |
| `standard-36h` | 36h | 2–8 | TTS 4 | TTS 7 |
| `extended-48h` | 48h | 2–8 | TTS 4 | TTS 8 |
| `classic-stone` | 36h | 2–8 | TTS 1 | TTS 6 |
| `dev-blitz-3m` | 3 min | 2–4 | TTS 4 | TTS 6 |

---

## Architecture (short)

```
TTS.Web  →  TTS.Api  →  WorldGrain (Orleans)  →  MatchHost  →  GameLoop  →  systems
                                                              ↘  TTS.Llm (optional)
```

- **Single source of truth:** `TTS.Core` — clients and LLMs never write state directly.  
- **Agents** use `GameToolSurface` with validation; rivals at TTS 5+ can use `LlmTurnAgent`.  
- **Persistence:** match state on disk per grain; turn history powers away summaries.  

Diagrams and deeper breakdown: [architecture-overview.md](architecture-overview.md) · [assets/architecture-technical-overview.png](assets/architecture-technical-overview.png)

---

## Development

```bash
dotnet test src/TTS.Tests/TTS.Tests.csproj   # unit tests
dotnet run --project src/TTS.Game              # instant local demo
dotnet run --project src/TTS.Agents -- list    # Ollama scenarios
```

**Key data:**

- `src/data/tech/catalog.json` — 70 technologies across tiers  
- `state_crime_income_merged.csv` — TTS 4 socioeconomic profiles  

**LLM env vars** (set by `dev.sh` when Ollama is available): `TTS_LLM_PROVIDER`, `OLLAMA_MODEL`, `TTS_LLM_MAX_TURN_CALLS_PER_TICK`. See [llm-deployment.md](llm-deployment.md).

---

## Documentation map

| Doc | Contents |
|-----|----------|
| [current-state.md](current-state.md) | What’s implemented today, commands, API surface |
| [implementation-plan.md](implementation-plan.md) | Roadmap Phases 0–9 |
| [architecture-overview.md](architecture-overview.md) | Technical + gameplay architecture |
| [player-experience.md](player-experience.md) | Async governor UX, check-in flow |
| [match-modes.md](match-modes.md) | Tick intervals, victory rules |
| [ui-design.md](ui-design.md) | Web dashboard design |
| [llm-deployment.md](llm-deployment.md) | Ollama vs cloud, costs |
| [tech-trees-by-tier.md](tech-trees-by-tier.md) | Per-tier tech branches |
| [crime-data.md](crime-data.md) | TTS 4 crime/income data |
| [async-multiplayer-gameplay.md](async-multiplayer-gameplay.md) | Async MP concept |
| [v2/](v2/) | Agent integration notes, next iterations |

---

## Design pillars

- **Era-driven gameplay** — each TTS is a distinct mode, not a stat bump  
- **Progress vs stability** — power and fragility rise together  
- **Emergent civ behavior** — factions, diffusion, events, rivals  
- **Async governor loop** — short check-ins, long-running matches  

---

## License

See repository defaults. Design docs and game content are part of this project workspace.
