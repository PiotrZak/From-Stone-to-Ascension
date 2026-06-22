# TTS v2 — Design Exploration

Planning documents for the next evolution of *From Stone to Ascension*: procedural content, optional spatial gameplay, agent-forward pacing, and era remigration.

| Document | Topic |
|----------|-------|
| [agent-integration.md](agent-integration.md) | **Shipped** — how MAF agents plug into ticks, advisor API, tools, and dev testing |
| [tts4-start.md](tts4-start.md) | **Exploration** — remigrate default matches to start at TTS 4 (Information Age) |
| [procedural-generation.md](procedural-generation.md) | Seeded worlds, regions, tech fusion, events, LLM narrative |
| [hex-map.md](hex-map.md) | Hex grid layer, procedural terrain, territorial play — deferred until v2 |

**Status:** Mixed — [agent-integration.md](agent-integration.md) describes live code; other docs are planning. The current dashboard game ships at TTS 1 — see [current-state.md](../current-state.md).

**Suggested order:**

1. [agent-integration.md](agent-integration.md) — understand LLM ↔ simulation boundaries today  
2. [tts4-start.md](tts4-start.md) — why v2 may begin at Information Age (agents + crime + CSV data aligned)  
3. [procedural-generation.md](procedural-generation.md) — seeded `WorldBootstrap` to replace hardcoded factory  
4. [hex-map.md](hex-map.md) — spatial layer when territorial play is required  
