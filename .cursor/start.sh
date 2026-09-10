#!/usr/bin/env bash
# Per-boot startup for the BurgerShop Unity environment.
# Activates the Unity license when credentials are available. This is a no-op
# when no license secrets are set, so the agent still boots for read-only work.
set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -x "${HERE}/activate-unity.sh" ]; then
  if "${HERE}/activate-unity.sh"; then
    echo "[start] Unity license is active."
  else
    echo "[start] Unity license NOT active (missing credentials or activation failed)."
    echo "[start] Compilation/build steps will fail until a license is provided."
  fi
fi

echo "[start] Ready."
