# TTS Web Client

React governor dashboard for async TTS matches.

## Run (development)

Three terminals:

```bash
# 1. Orleans silo
dotnet run --project src/TTS.Server

# 2. REST API (Orleans client)
dotnet run --project src/TTS.Api

# 3. React dev server
cd src/TTS.Web && npm install && npm run dev
```

Open http://localhost:5173

The Vite dev server proxies `/api` to http://localhost:5000.

## Screens

- **Home** — list matches, create match, join by code
- **Match dashboard** — status, players, decision gates, away summary

Player session is stored in browser `localStorage` per match.
