# Start — TTS local dev

Quick guide to run the full stack: **Orleans silo → API → Ollama → React UI**.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|--------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.x | `dotnet --version` |
| [Node.js](https://nodejs.org/) | 18+ | for `TTS.Web` |
| [Ollama](https://ollama.com/) | optional | tier fables; install + `ollama pull llama3.2` |

---

## One command (recommended)

From the repo root:

```bash
./dev.sh
```

Starts everything and prints URLs. **Ctrl+C** stops all services.

| Service | URL |
|---------|-----|
| **UI** | http://localhost:5173 |
| **API** | http://localhost:5000 |
| **Ollama** | http://localhost:11434 |

Logs (repo root):

- `.dev-server.log` — Orleans silo
- `.dev-api.log` — REST API
- `.dev-web.log` — Vite frontend
- `.dev-ollama.log` — Ollama (if started by script)

---

## Manual start (4 terminals)

If you prefer separate terminals:

```bash
# 1 — Orleans silo (must run first)
dotnet run --project src/TTS.Server

# 2 — REST API
dotnet run --project src/TTS.Api

# 3 — Ollama (skip if already running)
ollama serve

# 4 — React UI
cd src/TTS.Web && npm install && npm run dev
```

---

## Play a match

1. Open http://localhost:5173
2. Enter your name, pick **Dev Blitz (3 min)** (or another mode), click **Create**
3. Share the **join code** with a second player (or open in another browser)
4. Resolve **decision gates** when they appear — no manual tick needed
5. At the final tick the match **ends** and shows **Final results** + **Match log**

Ticks advance automatically (background service + UI polling).

---

## Where data is saved

| Data | Path |
|------|------|
| Match simulation + tick history | `src/TTS.Server/bin/Debug/net10.0/matches/{matchId}.json` |
| Join codes & players | `src/TTS.Api/bin/Debug/net10.0/Data/match-registry.json` |
| Local CLI demo (no web) | `./match-state.json` in project root |

Results and logs are **computed from** the match JSON (`TurnHistory` + civ state), not stored as separate files.

---

## Tests & console demo

```bash
dotnet test                                    # unit tests
dotnet run --project src/TTS.Game              # instant 8-tick console demo
dotnet run --project src/TTS.Agents -- list    # Ollama scenario list
```

---

## Troubleshooting

**`Unknown match mode 'dev-blitz-3m'`** — restart the Orleans silo after pulling new code (old silo = old binaries).

**API errors / empty matches** — ensure `TTS.Server` is running before `TTS.Api`.

**Port in use** — stop other `./dev.sh` or manual `dotnet run` instances.

**No tier fables** — run `ollama serve` and `ollama pull llama3.2`; gates still work with default text.

---

## Related docs

- [current-state.md](current-state.md) — what's built
- [ui-design.md](ui-design.md) — dashboard UX
- [match-modes.md](match-modes.md) — Sprint / Blitz / Standard presets
