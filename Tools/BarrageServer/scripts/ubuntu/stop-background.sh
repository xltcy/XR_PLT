#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PID_FILE="${PID_FILE:-${SERVER_DIR}/barrage-server.pid}"

if [[ ! -f "${PID_FILE}" ]]; then
  echo "[BarrageServer] pid file not found. Server may not be running."
  exit 0
fi

PID="$(cat "${PID_FILE}" || true)"
if [[ -z "${PID}" ]]; then
  rm -f "${PID_FILE}"
  echo "[BarrageServer] empty pid file removed."
  exit 0
fi

if kill -0 "${PID}" 2>/dev/null; then
  kill "${PID}"
  echo "[BarrageServer] stopped. PID=${PID}"
else
  echo "[BarrageServer] process not found. PID=${PID}"
fi

rm -f "${PID_FILE}"
