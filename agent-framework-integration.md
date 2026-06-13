# Microsoft Agent Framework Integration

**Project:** TTS — Technology Tier Simulation  
**Framework:** [Microsoft Agent Framework (MAF)](https://github.com/microsoft/agent-framework)  
**Status:** Phase 2 started — .NET simulation scaffold with agent tool surface (MAF not wired yet)

**Related:** [orleans-integration.md](orleans-integration.md) (distributed simulation server) · [async-multiplayer-gameplay.md](async-multiplayer-gameplay.md) (slow-evolving MP design) · [implementation-plan.md](implementation-plan.md) (master roadmap)

---

## 1. Purpose

This document describes how [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) can be used in *From Stone to Ascension* (TTS). It maps MAF capabilities to existing game systems defined in `README.md` and `tech-tree.md`, and proposes a practical architecture for when implementation begins.

MAF is **not** a game engine or traditional game AI library. It is a production framework for building, orchestrating, and operating **LLM-based agents and multi-agent workflows** in Python and .NET.

---

## 2. Project Structure (.NET 8)

```
From-Stone-to-Ascension/
├── From-Stone-to-Ascension.sln
├── src/
│   ├── TTS.Core/                    # Simulation engine (authoritative state)
│   │   ├── Models/
│   │   │   ├── TechTier.cs
│   │   │   ├── Region.cs
│   │   │   ├── Faction.cs
│   │   │   ├── Technology.cs
│   │   │   ├── Civilization.cs
│   │   │   ├── KnowledgeNetwork.cs
│   │   │   └── WorldState.cs
│   │   ├── Systems/
│   │   │   ├── StabilitySystem.cs
│   │   │   ├── TechTreeSystem.cs
│   │   │   ├── FactionSystem.cs
│   │   │   ├── GlobalEventSystem.cs
│   │   │   ├── KnowledgeDiffusionSystem.cs
│   │   │   ├── ForbiddenTechSystem.cs
│   │   │   └── WinLossSystem.cs
│   │   ├── Agents/                  # MAF integration boundary (TTS 5+)
│   │   │   ├── IGameToolSurface.cs
│   │   │   ├── GameToolSurface.cs
│   │   │   └── AgentOrchestrator.cs
│   │   ├── GameLoop.cs
│   │   └── SampleWorldFactory.cs
│   ├── TTS.Game/                    # Console host / demo
│   │   └── Program.cs
│   ├── TTS.Server/                  # Future — Orleans silo (see orleans-integration.md)
│   └── TTS.Tests/
│       └── CoreSystemsTests.cs
├── README.md
├── tech-tree.md
├── implementation-plan.md
├── ollama-scenarios.md          # Ollama integration — how it works
├── async-multiplayer-gameplay.md
├── agent-framework-integration.md
└── orleans-integration.md
```

### Getting started

```bash
# Requires .NET 8 SDK
dotnet build
dotnet test
dotnet run --project src/TTS.Game
```

### What is implemented

| Component | Status |
|-----------|--------|
| Core models (`TechTier`, `Region`, `Faction`, `Technology`, `Civilization`, `KnowledgeNetwork`) | Done |
| Game systems (stability, tech tree, factions, events, diffusion, forbidden tech, win/loss) | Done |
| Turn-based `GameLoop` | Done |
| `IGameToolSurface` agent contract | Done |
| `AgentOrchestrator` (stub — classical fallback below TTS 5) | Done |
| Microsoft Agent Framework package (`Microsoft.Agents.AI`) | Not yet |
| MAF workflows (alignment crisis, tech generation) | Not yet |

### Next MAF wiring step

Add a `TTS.Agents` project and reference `Microsoft.Agents.AI` with your chosen **LLM provider** (OpenAI, Gemini, or Ollama — not Azure Foundry required). See [§3.1 LLM Provider Strategy](#31-llm-provider-strategy).

```csharp
// Future: TTS.Agents/Tools/CivilizationTools.cs
// MAF tool methods call GameToolSurface — simulation stays authoritative.
```

---

## 3. What MAF Is — and Is Not

### MAF provides

- Single agents and multi-agent **workflows** (sequential, concurrent, handoff, group collaboration)
- **Tool calling** so agents act on external systems via a defined API
- **Middleware** for safety, logging, and request/response processing
- **Declarative agents** (YAML) and **Agent Skills** (domain knowledge from files/code)
- **Observability** (OpenTelemetry), streaming, checkpointing, human-in-the-loop
- Hosting patterns for local dev and cloud deployment (Azure Functions, Foundry, etc.)

### MAF does not replace

- Deterministic simulation (economy ticks, combat resolution, stability math)
- Classical game AI (utility AI, behavior trees, GOAP, pathfinding)
- Client rendering, input, or UI frameworks

### Recommended split

| Layer | Responsibility |
|-------|----------------|
| **Game client** | Presentation, player input, evolved UI per TTS |
| **Simulation server** | Authoritative world state, rules, validation |
| **MAF agent service** | Reasoning, dialogue, strategy proposals, procedural content |

The simulation remains the **source of truth**. Agents read state and propose actions; the engine validates and applies them.

---

### 3.1 LLM Provider Strategy

**Project choice:** Use **OpenAI**, **Google Gemini**, or a **free/local model** at the start. **Azure Foundry is optional** — not required for TTS.

| When | API keys needed? |
|------|------------------|
| **Now** (Phases 0–2, TTS 5+ stub) | **No** — `ClassicalAiSystem` + local `AgentOrchestrator` |
| **Phase 7–8** (real MAF agents) | **Yes** — one provider below |
| **MAF disabled / key missing** | **No** — fallback to `ClassicalAiSystem` |

#### Recommended providers (in order for getting started)

| Provider | Cost to start | .NET package | Best for |
|----------|---------------|--------------|----------|
| **Ollama** (local) | **Free** — runs on your machine | `Microsoft.Agents.AI.OpenAI` (OpenAI-compatible endpoint) | Dev, offline, zero API spend |
| **Google Gemini** | **Free tier** — [Google AI Studio](https://aistudio.google.com/apikey) | `Microsoft.Agents.AI.OpenAI` (OpenAI-compatible) or Python `agent-framework-gemini` | Cheap flash models, good quality |
| **OpenAI** | Pay-as-you-go | `Microsoft.Agents.AI.OpenAI` | Simplest docs, reliable tool calling |
| Azure Foundry | Azure subscription | `Microsoft.Agents.AI.Foundry` | Enterprise / Azure-only shops |

#### Default recommendation for this project

Start with **Ollama** (free, local) — **implemented** in `TTS.Agents`. See [ollama-scenarios.md](ollama-scenarios.md) for architecture and usage.

#### Environment variables (never commit to git)

Create a local `.env` or use `dotnet user-secrets` in `TTS.Agents`:

**Option A — OpenAI**

```bash
OPENAI_API_KEY=sk-...
OPENAI_MODEL=gpt-4.1-mini    # or gpt-4o-mini — pick a small/cheap model
```

**Option B — Google Gemini** (OpenAI-compatible endpoint)

```bash
GEMINI_API_KEY=...           # from Google AI Studio
GEMINI_MODEL=gemini-2.0-flash
# MAF/OpenAI client base URL (set in code):
# https://generativelanguage.googleapis.com/v1beta/openai/
```

**Option C — Ollama** (free, local — install from https://ollama.com)

```bash
# No API key. Pull a model first:
ollama pull llama3.2

OLLAMA_BASE_URL=http://localhost:11434/v1
OLLAMA_MODEL=llama3.2
```

#### Provider selection in code (sketch)

```csharp
// TTS.Agents/AgentProviderFactory.cs — pick via TTS_LLM_PROVIDER env var
// Values: "ollama" | "openai" | "gemini" | "none"

public static IChatClient? CreateClient()
{
    return Environment.GetEnvironmentVariable("TTS_LLM_PROVIDER") switch
    {
        "openai" => CreateOpenAi(),
        "gemini" => CreateGeminiOpenAiCompatible(),
        "ollama" => CreateOllama(),
        "none" or null or "" => null,   // classical AI fallback
        _ => null
    };
}
```

#### Fallback rule (important)

If no provider is configured or the call fails:

1. Log the error
2. `AgentOrchestrator` delegates to `ClassicalAiSystem` (same as today)
3. Game never blocks on LLM availability

This keeps TTS playable without any API key.

#### Cost control for TTS 5+

| Rule | Why |
|------|-----|
| MAF only at **TTS 5+** | Most ticks use free classical AI |
| MAF only for **AI civ turns** and **decision gates** | Not every tick, not every player action |
| Use **small models** (`gpt-4.1-mini`, `gemini-2.0-flash`, `llama3.2`) | Enough for research/diplomacy choices |
| **Rate limit** agent calls per match | Prevents runaway cost in multiplayer |

#### What to add to `.gitignore`

```
.env
.env.*
secrets.json
```

---

## 4. Alignment with Core Design Pillars

| Design pillar | How MAF supports it |
|---------------|---------------------|
| **Era-driven gameplay** | Agent capabilities unlock per TTS — AI assistance at TTS 4, autonomous civ reasoning at TTS 5+ |
| **Emergent civilization behavior** | Multi-agent workflows model factions, diplomacy, and competing priorities inside a civ |
| **Progress vs stability conflict** | Dedicated agents (or workflow branches) weigh advancement against instability before recommending actions |
| **Rule evolution** | Introducing MAF-backed behavior at higher tiers is itself a new rule: “intelligence is now agentic” |

---

## 5. Use Cases by Game System

### 5.1 AI civilizations and factions (TTS 5+)

**Relevant design:** AI-controlled civilizations, dynamic factions, AI collectives, faction competition over tech direction (`README.md` §6.3).

**MAF pattern:** Orchestrator workflow with specialist agents.

```
World state → Civilization orchestrator
                ├── Diplomacy agent
                ├── Research agent
                ├── Military / security agent
                └── Stability / faction agent
                      └── Tools → simulation API
```

- **Handoff:** Orchestrator delegates sub-tasks by domain.
- **Group collaboration:** Internal factions debate research and policy (accelerate vs suppress TTS).
- **Tools:** Agents call game APIs (`get_stability`, `propose_trade`, `set_research_priority`) instead of inventing state.

**When to run:** Once per turn or every N turns — not per simulation tick.

---

### 5.2 Procedural tech tree expansion

**Relevant design:** Fusion rules, procedural node generation, 500+ node target, “AI-generated tech trees” (`tech-tree.md`).

**MAF pattern:** Sequential or concurrent validation workflow.

| Step | Agent role |
|------|------------|
| 1 | **Generator** — proposes nodes from `fusion_tags`, tier, prerequisites |
| 2 | **Balance** — checks `risk_level`, tier gates, prerequisite validity |
| 3 | **Lore** — names and describes tech for UI and events |
| 4 | **Human review** — designer approves via human-in-the-loop (DevUI) |

**Agent Skills:** Load `tech-tree.md`, node schema, and fusion tables as domain knowledge so generation stays on-design.

**Output:** Structured node definitions the engine or content pipeline ingests — not free-form text.

---

### 5.3 Global events and crises

**Relevant design:** Global Event System — booms, alignment crises, nanotech outbreaks, temporal fractures (`README.md` §6.4).

**MAF pattern:** Event workflow driven by world snapshot.

**Inputs:** Current TTS, stability indices, active forbidden tech, faction tensions, recent history.

**Outputs:** Structured event payload, e.g.:

```json
{
  "event_type": "ai_alignment_crisis",
  "severity": 3,
  "affected_regions": ["region_12", "region_15"],
  "modifiers": { "technological_stability": -0.15 },
  "player_choices": ["regulate", "accelerate", "isolate"]
}
```

The simulation applies modifiers and triggers; the LLM layer supplies context, flavor, and branching logic.

---

### 5.4 In-world advisor and Player-as-AI mode

**Relevant design:** Semi-sentient AI assistants (TTS 5), optional Player-as-AI mode (`README.md` §5, §11).

**MAF pattern:** Single agent (or small workflow) with tier-gated tools.

| TTS | Advisor capability (example) |
|-----|-------------------------------|
| TTS 4 | Data analysis, network and market summaries |
| TTS 5 | Automation and research recommendations |
| TTS 6 | Bio/nano policy tradeoff briefings |
| TTS 7 | Timeline risk warnings, paradox context |

Use **middleware** for content safety, spoiler control, and preventing advisors from bypassing game rules.

---

### 5.5 Knowledge diffusion and diplomacy

**Relevant design:** Tech spread via trade, espionage, open science, AI networks; corruption and loss (`README.md` §6.5).

**MAF role:**

- Reason over what a civilization **knows** vs **suspects**
- Generate negotiation dialogue and espionage narratives
- Propose diffusion outcomes (e.g. partial leak, corrupted blueprint)

Final outcomes still pass through deterministic sim rules.

---

### 5.6 Forbidden technology and stability

**Relevant design:** Early unlocks with instability cost (`README.md` §6.6).

**MAF role:** “Risk assessor” agent in a workflow before a civ (or player advisor) commits to forbidden research. Outputs warnings and predicted stability impact; engine enforces hard limits.

---

### 5.7 Development and design tooling

Usable **before** gameplay code exists:

- Batch-generate and validate tech nodes from fusion schema
- Simulate “what if” scenarios for Experiment Mode via natural language
- Trace agent cost and latency with OpenTelemetry during balancing

---

## 6. TTS Tier Guidance

| Tier | MAF in gameplay | Rationale |
|------|-----------------|-----------|
| **TTS 1–2** | None (dev tooling only) | Classical AI/scripts; LLM cost and latency unjustified |
| **TTS 3–4** | Events, advisor flavor, content gen | Communication and information-age themes |
| **TTS 5** | **Primary integration** — civ AI, factions, alignment | AGI, autonomous governance, automation are core mechanics |
| **TTS 6–7** | Multi-agent collectives, paradox narratives | Complex branching reasoning and crisis storytelling |
| **TTS 8+** | Meta-agents, reality-layer UX | Abstract systems need rich explanation and player guidance |

**Design intent:** Higher tiers change **how** AI behaves in the product, not only what the fiction says. Turning on MAF-backed civ logic at TTS 5 is a deliberate rules change.

---

## 7. Proposed Architecture

```
┌─────────────┐     ┌──────────────────────┐     ┌─────────────────────┐
│ Game Client │ ←→  │ Simulation Server    │ ←→  │ MAF Agent Service   │
│ UI, input   │     │ Authoritative state    │     │ LLM workflows       │
│             │     │ Rules, validation    │     │ Tools → server only │
└─────────────┘     └──────────────────────┘     └─────────────────────┘
```

### Principles

1. **Authoritative simulation** — agents never write directly to save state without validation.
2. **Async decisions** — civ agent runs on turn boundaries; results cached between runs.
3. **Tier gating** — MAF features and tool sets unlock with TTS progression.
4. **Provider flexibility** — MAF supports multiple LLM providers; choice can evolve without rewriting orchestration.

### Language choice

| Simulation stack | MAF package |
|------------------|-------------|
| C# / Unity / .NET server | `Microsoft.Agents.AI` |
| Python simulation or tooling | `agent-framework` (Python) |

Both can coexist: Python for content/design pipelines, .NET for runtime if the game server is .NET.

### Hosting with Orleans (multiplayer)

For online play, MAF workflows can run inside Orleans `CivilizationGrain` instances. Grains **activate on demand** (e.g. civ turn, crisis event) and **deactivate when idle** — so many agent-backed civs can exist without keeping every LLM context in memory. See [orleans-integration.md §4](orleans-integration.md#4-agentic-grain-lifecycle-activate-on-demand) for the agentic turn flow diagram.

---

## 8. Tool API

Implemented in `TTS.Core/Agents/IGameToolSurface.cs` and `GameToolSurface.cs`.

Agents interact with the game only through tools. Example surface:

```python
# Read-only
get_civilization_state(civ_id: str) -> dict
get_faction_tensions(civ_id: str) -> dict
get_tech_tree_layer(tts: int) -> dict
get_global_events(active_only: bool) -> list

# Proposals (return validated result or rejection reason)
set_research_priority(civ_id: str, branch: str, weight: float) -> dict
propose_trade(from_civ: str, to_civ: str, offer: dict) -> dict
propose_diplomatic_action(civ_id: str, action: str, target: str) -> dict

# Content / events (engine applies structured payload)
emit_global_event(event: dict) -> dict
register_tech_nodes(nodes: list) -> dict
```

**Rules:**

- Tools return JSON the engine defines — not model free text.
- Rejected proposals include `reason` for agent retry or fallback behavior.
- Read tools may be scoped per civ (fog of war) to prevent omniscient opponents unless design allows it.

---

## 9. Example Workflows

### 9.1 TTS 5 — AI alignment crisis

```
Trigger: forbidden AI research + low technological stability
    → Crisis narrator (player-facing summary)
    → Faction debate (group collaboration: government vs corporate vs resistance)
    → Outcome proposer (structured choices + predicted stability)
    → Simulation applies player or AI civ choice
```

### 9.2 Tech fusion node generation

```
Input: parent nodes A, B with fusion_tags
    → Generator (3–12 child node candidates)
    → Balance validator (tier, risk, prerequisites)
    → Lore writer (name, description, event hooks)
    → Human approval (optional)
    → register_tech_nodes()
```

### 9.3 AI civilization turn

```
Per turn (AI civ):
    → Orchestrator loads civ state
    → Parallel: diplomacy, research, stability assessments
    → Orchestrator merges into action bundle
    → Simulation validates and executes
```

---

## 10. What to Avoid

| Anti-pattern | Why |
|--------------|-----|
| MAF on the hot path (combat, per-tick economy) | Latency, cost, non-determinism |
| LLM directly mutating save files | No validation, exploits, inconsistent replays |
| MAF for all civs at TTS 1–4 | Breaks pacing and budget |
| Replacing stability/math with LLM guesses | Undermines sim integrity |
| Unbounded agent memory | Cost drift; scope memory per civ/session |

---

## 11. Observability and Operations

For production or heavy playtesting:

- **OpenTelemetry** — trace workflow steps, tool calls, token usage per civ/turn
- **Checkpointing** — replay or debug agent decisions after bugs
- **Rate limits** — cap agent calls per turn and per TTS
- **Fallback** — if agent service fails, civ uses classical AI or last cached plan

---

## 12. Implementation Roadmap

> **Master plan:** [implementation-plan.md](implementation-plan.md) — unified Phases 0–9 across Core, async MP, Orleans, and MAF.

| Phase | This document | Status |
|-------|---------------|--------|
| 0–1 | Design + core scaffold | Done |
| 2–4 | Auto policy, decision gates, scheduled ticks | See [async-multiplayer-gameplay.md](async-multiplayer-gameplay.md) |
| 5–6 | Orleans silo + MP API | See [orleans-integration.md](orleans-integration.md) |
| **7–8** | **MAF tooling + in-game agents** | **This doc** |
| 9 | Cloud scale | All docs |

### Phase 7 — MAF tooling (offline)

- [ ] Add `TTS.Agents` with `Microsoft.Agents.AI.OpenAI` (not Foundry)
- [ ] Provider: Ollama (free local) or Gemini / OpenAI API key — see [§3.1](agent-framework-integration.md#31-llm-provider-strategy)
- [ ] `TTS_LLM_PROVIDER=none` keeps classical AI fallback
- [ ] Tech fusion workflow: generate → validate → export nodes
- [ ] Agent Skills from `tech-tree.md` / `README.md`

### Phase 8 — MAF in-game (TTS 5+)

- [ ] Replace `AgentOrchestrator` stub with real MAF workflow (OpenAI / Gemini / Ollama)
- [ ] Fallback to `ClassicalAiSystem` when `TTS_LLM_PROVIDER=none` or API fails
- [ ] Alignment crisis → `DecisionGate` + narration
- [ ] In-world advisor (read-only tools)
- [ ] Auto policy at TTS 5+ with classical fallback

Details and exit criteria: [implementation-plan.md § Phase 7–8](implementation-plan.md#phase-7--maf-tooling-offline).

---

## 13. References

| Resource | URL |
|----------|-----|
| Microsoft Agent Framework | https://github.com/microsoft/agent-framework |
| Python package | `pip install agent-framework` |
| .NET package | `Microsoft.Agents.AI` |
| Project README | `README.md` |
| Tech tree design | `tech-tree.md` |
| TTS progression diagram | `Diagram.md` |

---

## 14. Summary

Microsoft Agent Framework fits *From Stone to Ascension* as the **intelligence and content orchestration layer** — not as the core simulator. Use it where the design already implies AI: emergent factions, procedural tech expansion, global crises, tier-shifted behavior, and player-facing advisors from TTS 4 upward, with **full agentic civilization logic from TTS 5 onward**.

Keep the simulation authoritative, gate features by tier, and treat MAF as a backend service that proposes and narrates while the engine decides.
