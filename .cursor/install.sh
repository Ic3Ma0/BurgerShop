#!/usr/bin/env bash
# Idempotent Cloud Agent install for the BurgerShop Unity project.
# Ensures the pinned Unity Editor and its Linux runtime libraries are present.
# License activation is intentionally NOT done here (it needs runtime secrets);
# see .cursor/start.sh / .cursor/activate-unity.sh for that.
set -euo pipefail

UNITY_VERSION="6000.3.23f1"
UNITY_CHANGESET="09d2ecc7fb28"
UNITY_ROOT="/opt/unity/${UNITY_VERSION}"
UNITY_BIN="${UNITY_ROOT}/Editor/Unity"
DL_URL="https://download.unity3d.com/download_unity/${UNITY_CHANGESET}/LinuxEditorInstaller/Unity.tar.xz"

log() { printf '\n[install] %s\n' "$*"; }

SUDO=""
if [ "$(id -u)" -ne 0 ]; then
  if command -v sudo >/dev/null 2>&1; then SUDO="sudo"; fi
fi

log "Ensuring Unity Linux runtime libraries are present"
REQUIRED_PKGS=(
  libgtk-3-0t64 libglu1-mesa libnss3 libasound2t64 libxtst6 libxss1
  libx11-6 libxcursor1 libxrandr2 libgbm1 libxi6 libxrender1 libxext6 libsm6
  libxcomposite1 libxdamage1 libxfixes3 libnspr4 xvfb libunwind8 libssl3t64
  zlib1g libstdc++6 xz-utils curl ca-certificates
)
MISSING=()
for pkg in "${REQUIRED_PKGS[@]}"; do
  if ! dpkg -s "$pkg" >/dev/null 2>&1; then MISSING+=("$pkg"); fi
done
if [ "${#MISSING[@]}" -gt 0 ]; then
  log "Installing missing packages: ${MISSING[*]}"
  $SUDO apt-get update -qq
  $SUDO DEBIAN_FRONTEND=noninteractive apt-get install -y -qq "${MISSING[@]}"
else
  log "All runtime libraries already installed"
fi

log "Ensuring Unity Editor ${UNITY_VERSION} is installed at ${UNITY_ROOT}"
if [ -x "${UNITY_BIN}" ]; then
  log "Editor already present: $(${UNITY_BIN} -version 2>/dev/null | head -n1 || echo unknown)"
else
  log "Editor not found; downloading (~4.5 GB) from Unity"
  $SUDO mkdir -p "${UNITY_ROOT}"
  $SUDO chown -R "$(id -u):$(id -g)" /opt/unity
  tmp_dl="$(mktemp -d)"
  curl -fL --retry 6 --retry-delay 4 -C - -o "${tmp_dl}/Unity.tar.xz" "${DL_URL}"
  log "Extracting editor (this takes a few minutes)"
  tar -xf "${tmp_dl}/Unity.tar.xz" -C "${UNITY_ROOT}"
  rm -rf "${tmp_dl}"
  log "Editor installed: $(${UNITY_BIN} -version 2>/dev/null | head -n1 || echo unknown)"
fi

log "Install complete"
