# LinkedIn post — From Stone to Ascension

Copy the block below into LinkedIn. Attach a screenshot of the governor dashboard or an exported architecture diagram for better reach.

---

## Version A — technical (recommended)

I've been building **From Stone to Ascension** — a grand-strategy civilization sim where each technology era doesn't just add units, it rewrites the rules of the game.

🪵 TTS 1: survival and agriculture
🌐 TTS 4: crime data, digital economy, cybersecurity
🤖 TTS 5: your rival civilization can be an LLM agent
🌌 TTS 8+: post-singularity governance

The pitch: *"Advancing technology doesn't just make you stronger — it changes what strength means."*

**What makes it different:**
→ Each Technology Tier System is its own gameplay mode — new mechanics per era, not just +10% stats
→ Async matches (8h–48h wall clock) — 2–5 min governor check-ins; world advances on a schedule
→ Decision gates, policy presets, auto-research — crisis choices matter; no per-tick micromanagement
→ One authoritative rules engine; clients and LLMs never mutate state directly

**Architecture:**
`TTS.Core` is a pure .NET simulation engine — `GameLoop`, stability/faction/crime/event systems, ~70 tech nodes in `catalog.json`, regional crime/income CSV at TTS 4+. No HTTP, Orleans, or LLM inside the core. Everything else is a thin layer around it.

→ **TTS.Web** — React + Vite governor dashboard (away summary, gates, policy, tech tree, advisor)
→ **TTS.Api** — ASP.NET Core REST · **TTS.Contracts** — grain DTOs · **TTS.Grains** — `WorldGrain`
→ **TTS.Server** — Orleans silo · **TTS.Llm** — Microsoft Agent Framework · **TTS.Agents** — Ollama scenarios · **TTS.Game** — CLI

**Orleans:** Each async match = one `WorldGrain` keyed by match ID. Virtual actors activate on demand, hold state while the match runs, unload when idle — a natural fit for slow-evolving multiplayer. On activate: load `MatchHost` from JSON → restore `WorldState`. `IGrainTimer` fires scheduled ticks → `TryRunDueTick` → full phase pipeline. API calls are location-transparent. `MatchTickBackgroundService` is a safety net when grains go inactive. Scale path: cluster silos, durable storage (SQL/Azure).

**MAF (TTS 5+):** Rival civs and the strategic advisor use Microsoft Agent Framework — agents sit **outside** the simulation. Flow: `AgentTurnRunner` → `AgentOrchestrator` → `AgentToolWorkflow` → `GameToolRegistry` → `GameToolSurface`. Enum-defined tools (`propose_research`, diplomacy); engine validates and applies — or rejects. Classical AI fallback if the LLM fails.

Two MAF paths: **rival auto-turn** each tick + **strategic advisor** on refresh (classical at TTS 4). Ollama locally; OpenAI/Gemini in prod. Session limits cap LLM calls per tick. **`TTS.Core` never calls an LLM.**

**Shipped:** full tick pipeline · TTS 4 default start · decision gates + away summary · Orleans-hosted matches · React dashboard · MAF rival turns + advisor · JSON match persistence

**Next:** seeded procedural worlds · per-tier tech sub-trees · era-evolving UI · hex map · cloud Orleans cluster + OpenTelemetry · procedural events + LLM narrative

Early but end-to-end: create a match, let ticks run, resolve a crisis gate, watch an LLM rival research, climb tiers — or collapse trying.

https://github.com/PiotrZak/From-Stone-to-Ascension

Docs: `architecture-overview.md` · `v2/agent-integration.md` · `./dev.sh` to run locally. Stack diagram: `assets/architecture-technical-overview.png`.

If you're into .NET, Orleans, game sims, or LLM agents that don't corrupt your state — I'd love your feedback. ⭐ and PRs welcome.

#GameDev #IndieDev #DotNet #Orleans #MicrosoftAgentFramework #LLM #GrandStrategy #OpenSource #CSharp

---

## Version A — full (over LinkedIn limit; use as first comment or article source)

I've been building **From Stone to Ascension** — a grand-strategy civilization sim where each technology era doesn't just add units, it rewrites the rules of the game.

🪵 TTS 1: survival and agriculture
🌐 TTS 4: crime data, digital economy, cybersecurity
🤖 TTS 5: your rival civilization can be an LLM agent
🌌 TTS 8+: post-singularity governance

The pitch: *"Advancing technology doesn't just make you stronger — it changes what strength means."*

**What makes it different from a typical Civ-style game:**
→ Each Technology Tier System (TTS) is its own gameplay mode — new mechanics per era, not just +10% stats
→ Async matches (8h–48h wall clock) — 2–5 min governor check-ins; world advances on a schedule
→ Decision gates, policy presets, auto-research — crisis choices matter; no per-tick micromanagement
→ One authoritative rules engine; clients and LLMs never mutate state directly

---

**Architecture (the important bit):**

`TTS.Core` is a pure .NET simulation engine — `GameLoop`, stability/faction/crime/event systems, ~70 tech nodes in `catalog.json`, regional crime/income CSV at TTS 4+. No HTTP, no Orleans, no LLM inside the core.

Everything else is a thin layer around it:

→ **TTS.Web** — React + Vite governor dashboard (away summary, gates, policy, tech tree, advisor)
→ **TTS.Api** — ASP.NET Core REST (create/join match, resolve gates, civ dashboard)
→ **TTS.Contracts** — grain interfaces + wire DTOs (API ↔ Orleans boundary)
→ **TTS.Grains** — `WorldGrain` — one virtual actor per match
→ **TTS.Server** — Microsoft Orleans silo host
→ **TTS.Llm** — Microsoft Agent Framework (`Microsoft.Agents.AI`) tool workflows
→ **TTS.Agents** — offline Ollama scenario runner for agent testing without a live match
→ **TTS.Game** — console CLI for local dev and Orleans client

---

**How Orleans fits:**

Each async match = one `WorldGrain` keyed by match ID. Orleans gives us virtual actors that activate on demand, hold state while the match runs, and unload when idle — a natural fit for slow-evolving multiplayer.

On activate: grain loads `MatchHost` from JSON persistence → restores full `WorldState`.
While running: `IGrainTimer` fires scheduled ticks → `TryRunDueTick` → full phase pipeline (regions, civ turns, crime, events, decision gates).
API calls are location-transparent: `InitializeMatchAsync`, `AdvanceTickIfDueAsync`, `ResolveDecisionAsync`, advisor endpoints — the runtime routes to the right silo.
`MatchTickBackgroundService` in the API is a safety net when grains go inactive.

Orleans is the **distributed host** — not the game logic. `TTS.Core` stays deterministic and testable (xUnit). The scale path: cluster silos, durable grain storage (SQL/Azure), Orleans streams for global events.

---

**Microsoft Agent Framework (MAF) layer:**

At TTS 5+, rival civs and the strategic advisor use MAF — but agents sit **outside** the simulation.

Flow: `AgentTurnRunner` → `AgentOrchestrator` → `LlmTurnAgent` → `AgentToolWorkflow` (`ChatClientAgent` + tool loop) → `AgentGameToolBridge` → `GameToolRegistry` → **`GameToolSurface`** in `TTS.Core`.

Agents read structured world state and propose actions via enum-defined tools (`propose_research`, diplomacy, etc.). The engine validates and applies — or rejects. Classical AI is the fallback if the LLM fails or times out.

Two live gameplay paths:
→ **Rival auto-turn** — non-player civs at TTS 5+ on each scheduled tick
→ **Strategic advisor** — player-triggered briefing at TTS 5+ (classical analysis at TTS 4)

Local dev: Ollama. Production path: OpenAI / Gemini via env config. Session limits cap calls per tick so agent cost stays bounded.

Rule we enforce: **`TTS.Core` never calls an LLM.** Tools are the only write path. No hallucinated game state.

---

**What's working today:**
Full tick pipeline · TTS 4 default start (Information Age spine) · decision gates + away summary · Orleans-hosted matches · React dashboard · MAF rival turns + advisor · JSON match persistence

**What's next (v2 roadmap):**
→ Seeded procedural worlds (`WorldBootstrap` replacing hardcoded factory)
→ Deeper per-tier tech sub-trees + forbidden-tech consequences
→ UI that evolves per era (earthy TTS 1 → surreal TTS 8+)
→ Hex map / territorial layer (deferred — abstract regions today)
→ Cloud cluster: durable Orleans persistence, observability (OpenTelemetry), internet-scale async MP
→ Procedural events, fusion tech, LLM narrative pipelines (Phase 7+)

It's early, but the core loop works end-to-end: create a match, let ticks run, resolve a crisis gate, watch an LLM rival research, climb tiers — or collapse trying.

Open source on GitHub:
https://github.com/PiotrZak/From-Stone-to-Ascension

Docs: `architecture-overview.md` · `v2/agent-integration.md` · `./dev.sh` to run locally.

If you're into .NET, Orleans, game sims, or LLM agents that don't corrupt your state — I'd love your feedback. ⭐ and PRs welcome.

#GameDev #IndieDev #DotNet #Orleans #MicrosoftAgentFramework #LLM #GrandStrategy #OpenSource #SoftwareArchitecture #CSharp

---

## Version B — shorter

New project: **From Stone to Ascension** 🌍

A civilization sim where technology tiers change the *rules*, not just the stats.

• Async 8h–48h matches — 2–5 min check-ins
• TTS 4 start: crime, digital economy, cybersecurity
• TTS 5+: LLM-powered rival civs (validated game tools, no state hallucination)
• .NET core + Orleans + React dashboard

*"Advancing technology doesn't just make you stronger — it changes what strength means."*

Code & docs:
https://github.com/PiotrZak/From-Stone-to-Ascension

Feedback and contributors welcome.

#GameDev #DotNet #AI #OpenSource

---

## Version D — AI narrative focus (recommended for current ship)

What if your civilization sim didn't just *simulate* the Information Age — it *narrated* it?

I've been shipping new AI layers into **From Stone to Ascension (TTS)** — an async grand-strategy game where each technology era rewrites the rules, not just the stats.

Here's what's new on the narrative side:

📜 **LLM-enriched decision gates**
When a crime spike hits a city or a faction turns on you, the crisis card isn't generic filler. Ollama (or cloud LLM) rewrites the briefing in-world — era-aware tone at TTS 4, near-future sci-fi at TTS 5+. Tier shifts, forbidden tech, AI alignment crises each get their own narrative voice.

🧭 **"While you were away" digest**
Matches run on real time (8h–48h). Log back in and get a structured headline + bullet digest: who researched what, tier changes, events you missed, gates that expired with defaults applied. Built for 2–5 minute governor check-ins.

🤖 **LLM rival civilizations (TTS 5+)**
Your opponent isn't a script picking the next tech — it's an agent proposing research and diplomacy through validated game tools. If the model fails, classical AI takes over. The sim never trusts free-form LLM output.

🧠 **Strategic advisor**
At Information Age you get classical analysis. At Early AI, ask the advisor for a live LLM briefing on policy, risk, and research — same tool boundary, player-triggered, rate-limited.

Under the hood the rule is simple: **the simulation owns truth; AI adds flavor and intent.** No hallucinated game state.

Also landed recently: procedural seeded worlds, a governor dashboard rework (decision-first layout, territory map), and the full async loop from lobby → ticks → gates → victory.

Open source — run locally with `./dev.sh` (Ollama optional for narratives and rivals):

https://github.com/PiotrZak/From-Stone-to-Ascension

If you're building games + LLMs and care about *where* the model sits in the stack — I'd love your feedback.

#GameDev #AI #LLM #IndieDev #GrandStrategy #DotNet #OpenSource #GameDesign

---

## Version C — story-led

What if Civilization, but every era is a different game?

That's the idea behind **From Stone to Ascension** — a project I've been shaping from design docs into working code.

At the Information Age (TTS 4), you're managing regional crime pressure and digital markets — not sword upgrades. At Early AI (TTS 5), your opponent might be an LLM making research and diplomacy calls through a strict tool API. Higher tiers introduce bio/nano policy, temporal paradox risk, and post-singularity governance.

The player loop is async by design: log in, read what happened while you were away, resolve a crisis gate, set policy, leave. Matches run over hours or days.

Under the hood: one authoritative rules engine (`TTS.Core`), Orleans for multiplayer, React for the governor dashboard, and agents at the edge — never inside the sim.

It's open source and very much in progress. If this intersection of game design + systems architecture + LLM agents interests you:

https://github.com/PiotrZak/From-Stone-to-Ascension

Would love to hear what you'd build first at TTS 5.

#GameDesign #SoftwareEngineering #LLM #IndieGames #CSharp

---

## Posting tips

| Tip | Detail |
|-----|--------|
| **Length** | Version A ≈ **3,472 chars** (1,682 fewer than the full draft). If LinkedIn still rejects at 3,000, drop the docs line + 2 hashtags, or paste **Orleans/MAF** sections as the first comment. |
| **Best time** | Tue–Thu morning in your timezone |
| **Image** | `assets/architecture-technical-overview.png` (ready to upload) or `MatchPage` screenshot |
| **Link** | Paste GitHub URL on its own line — LinkedIn previews it better |
| **Follow-up comment** | Paste `./dev.sh` quick-start, link to `architecture-overview.md` + `v2/agent-integration.md` |
| **Article cross-post** | After Medium publish, share Version C with article link in first comment |
| **Hashtags** | Version A includes #Orleans #MicrosoftAgentFramework — trim if it feels heavy |
