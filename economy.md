# World Economy — Regions, Cities & Socioeconomic Simulation

**Status:** Partial — region growth + TTS 4 crime pressure implemented; city economy layer in progress  
**Related:** [crime-data.md](crime-data.md) · [README.md §5](README.md#5-tts-progression-system) · [tech-trees-by-tier.md](tech-trees-by-tier.md)

---

## 1. Purpose

The TTS world is one planet (or compact theatre) divided into **territories** that behave like **cities and their hinterlands**. Economy is not a separate mini-game — it feeds the three stability pillars (political, economic, technological) and unlocks new rule layers as tiers advance.

Design goal: a governor opens the dashboard and sees **where** their civ lives, **how rich or strained** each city is, and **why** crime or unrest matters — not a hidden CSV mapping to “California” behind a fantasy name.

---

## 2. World structure

```
WorldState
├── Civilizations (1–8 players / AI)
│   ├── Stability: political · economic · technological
│   ├── Policy → auto-research each tick
│   └── ControlledRegionIds[]
├── Regions (cities / territories)
│   ├── Population, resources, infrastructure
│   └── SocioeconomicProfile (TTS 4+ data-backed)
├── Technologies (shared catalog)
└── Match (tick schedule, victory tier)
```

| Layer | Model | Grain (future) |
|-------|--------|----------------|
| World | `WorldState` | `WorldGrain` |
| Civ | `Civilization` | `CivilizationGrain` |
| City / territory | `Region` | embedded in civ or `RegionGrain` |

**Demo match (today):** two civs, two cities each.

| City | Civ | Real-world data anchor | Role |
|------|-----|------------------------|------|
| **Meridian Bay** | Aurora Collective | California 2015 | High GDP, moderate inequality, tech-services hub |
| **Redstone Harbor** | Iron Dominion | Louisiana 2015 | Lower GDP, higher poverty, industrial / port |

Data anchor is **explicit in UI** (`source: California 2015`) so players understand stats are grounded, not random.

---

## 3. Economy by technology tier

Each TTS tier adds economic rules (from [README.md](README.md)):

| TTS | Era | Economic focus | Simulation hook (target) |
|-----|-----|----------------|---------------------------|
| 1 | Pre-Industrial | Subsistence, agriculture | Region `Resources`, food tech |
| 2 | Industrial | Factories, logistics | `Infrastructure` growth, pollution → stability |
| 3 | Early Electronics | Grids, transport | Infrastructure bonus to research diffusion |
| 4 | Information Age | Global markets, digital divide | **Socioeconomic CSV** — GDP, unemployment, crime |
| 5 | Early AI | Automation vs labor | Policy + agent layer (Phase 8) |
| 6+ | Bio-Nano / beyond | Post-scarcity tension | Forbidden tech, stability caps |

**Today:** TTS 1–3 use generic region growth; TTS 4+ uses CSV-backed **crime pressure** and **city economy** modifiers.

---

## 4. Region (city) attributes

```csharp
Region
├── Id, Name              // e.g. "Meridian Bay"
├── Population            // from CSV when profile attached
├── Resources (0–100)     // natural / agricultural base
├── Infrastructure (0–100) // industrial / digital capacity
├── ControllingCivilizationId
└── CrimeProfile          // RegionalCrimeProfile — socioeconomic record
```

### Socioeconomic profile (`RegionalCrimeProfile`)

Loaded from [state_crime_income_merged.csv](src/data/state_crime_income_merged.csv). Despite the name, the record includes **economy and governance**, not only crime:

| Field | Use in sim |
|-------|------------|
| `GdpPerCapita` | Economic output → `EconomicStability` bonus |
| `UnemploymentRate` | Economic drag |
| `PovertyRate` | Political + economic pressure |
| `GiniCoefficient` | Inequality → political pressure |
| `ViolentCrimeRate` | Crime pressure index |
| `CorruptionConvictionsPerMillion` | Governance drag |

**Composite indices:**

- **Crime pressure** (0–100) — gates, political decay (existing)
- **Economic health** (0–100) — GDP, employment, poverty (new)

---

## 5. Systems (code)

### 5.1 Implemented

| System | Phase | Effect |
|--------|-------|--------|
| `RegionGrowthPhase` | Every tick | `Resources` +0.5, `Infrastructure` +0.2 per region |
| `StabilityDecayPhase` | Every tick | Baseline stability erosion |
| `CrimeSystem` | TTS 4+ | Crime pressure → political / economic penalty |
| `CrimeSystem.GetPerspective` | TTS 4+ | Dashboard + Ollama context |

### 5.2 Added (city economy pass)

| System | Effect |
|--------|--------|
| `EconomySystem` | Region stats + CSV → `EconomicStability` each tick |
| `EconomySystem.GetCityPerspective` | Per-city GDP, unemployment, health for UI |
| City bootstrap | Population / infra / resources seeded from CSV |

### 5.3 Planned

| Feature | Tier | Notes |
|---------|------|-------|
| Inter-city trade routes | 3+ | Knowledge network analogue for goods |
| Production chains | 2+ | Industrial branch weights affect regions |
| Tax / welfare policy knobs | 4+ | Extends `CivilizationPolicy` |
| Dynamic CSV year | 4+ | Advance data year with match ticks |
| `RegionGrain` | Scale | Only when 20+ territories |

---

## 6. Turn flow (economy-relevant)

```
RegionGrowth          → resources / infrastructure tick
StabilityDecay        → baseline erosion
CivilizationTurn      → research (blocked by gates)
KnowledgeDiffusion    → tech spread
FactionInfluence      → internal politics
EconomySystem         → city GDP / unemployment → economic stability   [NEW]
CrimeSystem           → crime pressure → political / economic stability  [TTS 4+]
GlobalEventGeneration → shocks
```

Economy runs **before** crime so high-GDP cities partially offset crime drag.

---

## 7. Decision gates & economy

| Gate | Economic trigger |
|------|------------------|
| Crime pressure | Avg crime index ≥ 65 at TTS 4+ |
| Faction crisis | Civ stability low (economic collapse contributes) |
| Tier advancement | Often follows infrastructure / GDP milestone |

Future: **economic crisis gate** when `EconomicStability` &lt; 30 or unemployment spike.

---

## 8. UI & API

### Dashboard (target layout)

```
Meridian Bay · pop 39.1M · GDP/cap $55k · unemployment 6.2%
Economic health ████████░░ 72 · Crime pressure ████░░░░░░ 41
Infrastructure 68 · Resources 54
```

- **Always visible** — cities panel on match page (not only TTS 4 crime block)
- **TTS 4+** — full socioeconomic breakdown + crime panel
- **Source label** — `Based on California 2015` for transparency

### API

| Endpoint | Field |
|----------|-------|
| `GET /api/matches/{id}` | `regions[]` — city list with economy + crime stats |

---

## 9. Data file

**Path:** `src/data/state_crime_income_merged.csv`  
**Copied to:** `TTS.Core/Data/` and output `Data/` at runtime  

See [crime-data.md](crime-data.md) for column reference. Economy doc treats this file as the **socioeconomic atlas** for TTS 4 cities until procedural generation exists.

---

## 10. Victory & scoring

Match presets define `VictoryTier` + `VictoryStabilityMin`. Economy feeds **average stability** via the economic pillar:

- Strong cities → slower decay, faster recovery after gates
- Weak cities → faction crisis, crime gates, defeat on collapse

End-of-match ranking: tier → stability → tech count (unchanged).

---

## 11. Implementation checklist

- [x] Document world economy model (this file)
- [x] Name cities + seed population / infra from CSV
- [x] `EconomySystem` — GDP / unemployment → stability
- [x] Expose `regions` on match summary API
- [x] Cities panel in web UI
- [ ] Economic crisis gate
- [ ] Policy knobs (tax / welfare)
- [ ] Procedural city generation (Phase 7+)

---

## 12. Related files

| File | Role |
|------|------|
| `TTS.Core/Models/Region.cs` | City / territory |
| `TTS.Core/Models/RegionalCrimeProfile.cs` | Socioeconomic record |
| `TTS.Core/Systems/EconomySystem.cs` | City economy tick |
| `TTS.Core/Systems/CrimeSystem.cs` | TTS 4+ crime pressure |
| `TTS.Core/Systems/CrimeDataRepository.cs` | CSV loader |
| `TTS.Core/SampleWorldFactory.cs` | Demo city setup |
| `TTS.Core/Simulation/TurnPhases.cs` | Turn pipeline |
