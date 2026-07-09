#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PID_FILE="${PID_FILE:-${SERVER_DIR}/barrage-server.pid}"

if [[ ! -f "${PID_FILE}" ]]; then
  echo "[BarrageServer] stopped."
  exit 0
fi

PID="$(cat "${PID_FILE}" || true)"
if [[ -n "${PID}" ]] && kill -0 "${PID}" 2>/dev/null; then
  echo "[BarrageServer] running. PID=${PID}"
else
  echo "[BarrageServer] pid file exists, but process is not running. PID=${PID}"
fi
