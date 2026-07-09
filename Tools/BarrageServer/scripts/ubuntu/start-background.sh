#!/usr/bin/env bash
set -euo pipefail

PORT="${PORT:-37621}"
PUBLIC_BASE_URL="${PUBLIC_BASE_URL:-}"
ADMIN_TOKEN="${ADMIN_TOKEN:-}"
MAX_MESSAGE_LENGTH="${MAX_MESSAGE_LENGTH:-80}"
SEND_INTERVAL_MS="${SEND_INTERVAL_MS:-1200}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG_DIR="${LOG_DIR:-${SERVER_DIR}/logs}"
PID_FILE="${PID_FILE:-${SERVER_DIR}/barrage-server.pid}"

mkdir -p "${LOG_DIR}"

if [[ -f "${PID_FILE}" ]]; then
  OLD_PID="$(cat "${PID_FILE}" || true)"
  if [[ -n "${OLD_PID}" ]] && kill -0 "${OLD_PID}" 2>/dev/null; then
    echo "[BarrageServer] already running. PID=${OLD_PID}"
    echo "[BarrageServer] page: http://<server-host>:${PORT}/s/default"
    exit 0
  fi
fi

if [[ ! -d "${SERVER_DIR}/node_modules" ]]; then
  echo "[BarrageServer] installing npm dependencies..."
  npm install --prefix "${SERVER_DIR}"
fi

echo "[BarrageServer] starting in background..."
cd "${SERVER_DIR}"

PORT="${PORT}" \
PUBLIC_BASE_URL="${PUBLIC_BASE_URL}" \
ADMIN_TOKEN="${ADMIN_TOKEN}" \
MAX_MESSAGE_LENGTH="${MAX_MESSAGE_LENGTH}" \
SEND_INTERVAL_MS="${SEND_INTERVAL_MS}" \
LOG_DIR="${LOG_DIR}" \
nohup node server.js >>"${LOG_DIR}/server.out.log" 2>>"${LOG_DIR}/server.err.log" &

PID="$!"
echo "${PID}" > "${PID_FILE}"

echo "[BarrageServer] started. PID=${PID}"
if [[ -n "${PUBLIC_BASE_URL}" ]]; then
  echo "[BarrageServer] page: ${PUBLIC_BASE_URL%/}/s/default"
  echo "[BarrageServer] health: ${PUBLIC_BASE_URL%/}/health"
else
  echo "[BarrageServer] page: http://<server-host>:${PORT}/s/default"
  echo "[BarrageServer] health: http://<server-host>:${PORT}/health"
fi
echo "[BarrageServer] logs: ${LOG_DIR}"
