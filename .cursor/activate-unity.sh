#!/usr/bin/env bash
# Activate a Unity Editor license for headless (batchmode) use.
# Re-runs safely on every boot (idempotent): it simply re-applies whatever
# credentials are provided via Cloud Agent Secrets.
#
# Supported credential sets (never commit these; use the Secrets panel):
#   * Unity Pro/Plus serial (recommended for reproducible headless CI):
#       UNITY_EMAIL, UNITY_PASSWORD, UNITY_SERIAL
#   * Unity Personal license file (.ulf) contents:
#       UNITY_LICENSE   (the full XML text of a manually-activated .ulf)
#
# Exit codes: 0 = licensed, 1 = no credentials provided, 2 = activation failed.
set -uo pipefail

UNITY_VERSION="6000.3.23f1"
UNITY_BIN="/opt/unity/${UNITY_VERSION}/Editor/Unity"
LOG_DIR="${UNITY_LOG_DIR:-/tmp/unity-logs}"
mkdir -p "${LOG_DIR}"

log() { printf '[activate] %s\n' "$*"; }

if [ ! -x "${UNITY_BIN}" ]; then
  log "Unity Editor not found at ${UNITY_BIN}; run .cursor/install.sh first"
  exit 2
fi

run_unity() {  # $1 = logfile, rest = unity args
  xvfb-run -a "${UNITY_BIN}" -batchmode -nographics -logFile "$1" "${@:2}"
}

activation_ok() {  # $1 = logfile, $2 = unity exit code
  [ "$2" -eq 0 ] && ! grep -qiE 'No valid Unity Editor license|Failed to activate|Invalid|token is unavailable' "$1"
}

# --- Personal .ulf path ---
if [ -n "${UNITY_LICENSE:-}" ]; then
  log "Activating with provided .ulf (UNITY_LICENSE)"
  ulf="${LOG_DIR}/unity.ulf"
  printf '%s' "${UNITY_LICENSE}" > "${ulf}"
  act_log="${LOG_DIR}/activate-ulf.log"
  run_unity "${act_log}" -quit -manualLicenseFile "${ulf}"; rc=$?
  if activation_ok "${act_log}" "${rc}"; then
    log "Personal license activated"
    exit 0
  fi
  log "Personal (.ulf) activation failed (rc=${rc}); see ${act_log}"
  exit 2
fi

# --- Serial (Pro/Plus) path ---
if [ -n "${UNITY_SERIAL:-}" ] && [ -n "${UNITY_EMAIL:-}" ] && [ -n "${UNITY_PASSWORD:-}" ]; then
  log "Activating with serial (UNITY_SERIAL + UNITY_EMAIL/UNITY_PASSWORD)"
  act_log="${LOG_DIR}/activate-serial.log"
  run_unity "${act_log}" -quit \
    -serial "${UNITY_SERIAL}" -username "${UNITY_EMAIL}" -password "${UNITY_PASSWORD}"; rc=$?
  if activation_ok "${act_log}" "${rc}"; then
    log "Serial license activated"
    exit 0
  fi
  log "Serial activation failed (rc=${rc}); see ${act_log}"
  exit 2
fi

log "No Unity license credentials found."
log "Provide UNITY_LICENSE (.ulf contents) or UNITY_SERIAL + UNITY_EMAIL + UNITY_PASSWORD"
log "in the Cloud Agent Secrets panel, then re-run .cursor/start.sh."
exit 1
