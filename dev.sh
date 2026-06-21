#!/usr/bin/env bash
# Start TTS full stack: Orleans silo, API, Ollama, React UI
# Usage: ./dev.sh

set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

log() { printf '\n[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }

PIDS=()

cleanup() {
  log "Stopping services..."
  for pid in "${PIDS[@]}"; do
    kill "$pid" 2>/dev/null || true
  done
  wait 2>/dev/null || true
}
trap cleanup EXIT INT TERM

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Missing required command: $1" >&2
    exit 1
  }
}

require_cmd dotnet
require_cmd npm
require_cmd curl

if ! command -v ollama >/dev/null 2>&1; then
  echo "Warning: ollama not found — tier fables will use fallback text only." >&2
  OLLAMA_PID=""
else
  if curl -sf http://localhost:11434/api/tags >/dev/null 2>&1; then
    log "Ollama already running on :11434"
    OLLAMA_PID=""
  else
    log "Starting Ollama on :11434"
    ollama serve >"$ROOT/.dev-ollama.log" 2>&1 &
    OLLAMA_PID=$!
    PIDS+=("$OLLAMA_PID")
    for _ in {1..20}; do
      curl -sf http://localhost:11434/api/tags >/dev/null 2>&1 && break
      sleep 0.5
    done
  fi
fi

log "Starting Orleans silo (TTS.Server)"
dotnet run --project "$ROOT/src/TTS.Server" >"$ROOT/.dev-server.log" 2>&1 &
PIDS+=($!)
sleep 2

log "Starting API (TTS.Api) on http://localhost:5000"
dotnet run --project "$ROOT/src/TTS.Api" >"$ROOT/.dev-api.log" 2>&1 &
PIDS+=($!)
sleep 2

if [[ ! -d "$ROOT/src/TTS.Web/node_modules" ]]; then
  log "Installing frontend dependencies..."
  npm install --prefix "$ROOT/src/TTS.Web"
fi

log "Starting frontend on http://localhost:5173"
npm run dev --prefix "$ROOT/src/TTS.Web" >"$ROOT/.dev-web.log" 2>&1 &
PIDS+=($!)

log "Stack running:"
echo "  UI:      http://localhost:5173"
echo "  API:     http://localhost:5000"
echo "  Ollama:  http://localhost:11434"
echo "  Logs:    .dev-server.log  .dev-api.log  .dev-web.log  .dev-ollama.log"
echo ""
echo "Press Ctrl+C to stop all services."

wait -n "${PIDS[@]}" 2>/dev/null || wait
