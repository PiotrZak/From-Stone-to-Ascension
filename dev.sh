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

if [[ -n "${OLLAMA_PID:-}" ]] || curl -sf http://localhost:11434/api/tags >/dev/null 2>&1; then
  export TTS_LLM_PROVIDER="${TTS_LLM_PROVIDER:-ollama}"
  export TTS_LLM_TURN_TIMEOUT_SEC="${TTS_LLM_TURN_TIMEOUT_SEC:-20}"
  export TTS_LLM_MAX_CALLS_PER_TICK="${TTS_LLM_MAX_CALLS_PER_TICK:-4}"
  export TTS_LLM_MAX_TURN_CALLS_PER_TICK="${TTS_LLM_MAX_TURN_CALLS_PER_TICK:-4}"
  export TTS_LLM_MAX_ADVISOR_CALLS_PER_TICK="${TTS_LLM_MAX_ADVISOR_CALLS_PER_TICK:-1}"
  export OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.2}"
  export OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://localhost:11434}"
  if command -v ollama >/dev/null 2>&1; then
    ollama pull "${OLLAMA_MODEL:-llama3.2}" >/dev/null 2>&1 || true
  fi
else
  export TTS_LLM_PROVIDER="${TTS_LLM_PROVIDER:-none}"
fi

LLM_ENV=(
  "TTS_LLM_PROVIDER=${TTS_LLM_PROVIDER:-none}"
  "TTS_LLM_TURN_TIMEOUT_SEC=${TTS_LLM_TURN_TIMEOUT_SEC:-20}"
  "TTS_LLM_MAX_CALLS_PER_TICK=${TTS_LLM_MAX_CALLS_PER_TICK:-4}"
  "TTS_LLM_MAX_TURN_CALLS_PER_TICK=${TTS_LLM_MAX_TURN_CALLS_PER_TICK:-4}"
  "TTS_LLM_MAX_ADVISOR_CALLS_PER_TICK=${TTS_LLM_MAX_ADVISOR_CALLS_PER_TICK:-1}"
  "OLLAMA_MODEL=${OLLAMA_MODEL:-llama3.2}"
  "OLLAMA_BASE_URL=${OLLAMA_BASE_URL:-http://localhost:11434}"
)

log "Starting Orleans silo (TTS.Server)"
env "${LLM_ENV[@]}" dotnet run --project "$ROOT/src/TTS.Server" >"$ROOT/.dev-server.log" 2>&1 &
PIDS+=($!)
sleep 2

log "Starting API (TTS.Api) on http://localhost:5000"
env "${LLM_ENV[@]}" dotnet run --project "$ROOT/src/TTS.Api" >"$ROOT/.dev-api.log" 2>&1 &
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
echo "  Agents:  TTS_LLM_PROVIDER=${TTS_LLM_PROVIDER:-ollama} (turn ${TTS_LLM_MAX_TURN_CALLS_PER_TICK:-4}/tick, advisor ${TTS_LLM_MAX_ADVISOR_CALLS_PER_TICK:-1}/tick)"
echo "  Logs:    .dev-server.log  .dev-api.log  .dev-web.log  .dev-ollama.log"
echo ""
echo "Press Ctrl+C to stop all services."

wait -n "${PIDS[@]}" 2>/dev/null || wait
