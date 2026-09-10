#!/usr/bin/env bash
# Headless project validation for BurgerShop.
# Imports the project in batchmode and asserts there are zero C# compile errors
# (SPEC.md acceptance: "Unity Editor 零编译错误"). Requires an active license.
#
# Usage: .cursor/unity-check.sh [projectPath]   (defaults to repo root)
set -uo pipefail

UNITY_VERSION="6000.3.23f1"
UNITY_BIN="/opt/unity/${UNITY_VERSION}/Editor/Unity"
PROJECT_PATH="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
LOG_DIR="${UNITY_LOG_DIR:-/tmp/unity-logs}"
mkdir -p "${LOG_DIR}"
LOG="${LOG_DIR}/import.log"

echo "[check] Importing and compiling project at ${PROJECT_PATH}"
xvfb-run -a "${UNITY_BIN}" -batchmode -nographics -quit \
  -projectPath "${PROJECT_PATH}" \
  -logFile "${LOG}"
UNITY_EXIT=$?

echo "[check] Unity exit code: ${UNITY_EXIT}"

if grep -qiE 'No valid Unity Editor license' "${LOG}"; then
  echo "[check] FAIL: no active Unity license. Run .cursor/activate-unity.sh."
  exit 3
fi

if grep -qE 'error CS[0-9]+' "${LOG}" || grep -qiE 'Compilation failed|Scripts have compiler errors' "${LOG}"; then
  echo "[check] FAIL: C# compile errors detected:"
  grep -E 'error CS[0-9]+' "${LOG}" | sort -u | head -50
  exit 1
fi

if [ "${UNITY_EXIT}" -ne 0 ]; then
  echo "[check] FAIL: Unity exited non-zero. Last log lines:"
  tail -30 "${LOG}"
  exit "${UNITY_EXIT}"
fi

echo "[check] PASS: project imported and compiled with zero errors."
