#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
adb_tool="${ADB:-/Applications/Unity/Hub/Editor/6000.3.23f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb}"
apk_path="${1:-$project_dir/Builds/Android/BurgerShop-0.2.1-arm64.apk}"
package_id='com.ic3ma0.burgershop'
[[ -x "$adb_tool" ]] || { echo 'Set ADB to the Android platform-tools adb executable.' >&2; exit 1; }
[[ -f "$apk_path" ]] || { echo "APK not found: $apk_path" >&2; exit 1; }
# Require exactly one authorized device. Do not choose between personal devices implicitly.
count="$("$adb_tool" devices | awk '$2 == "device" { n++ } END { print n+0 }')"
[[ "$count" == '1' ]] || { "$adb_tool" devices -l; echo 'Connect one authorized test device.' >&2; exit 1; }
serial="$("$adb_tool" devices | awk '$2 == "device" {print $1}')"
abi="$("$adb_tool" -s "$serial" shell getprop ro.product.cpu.abilist | tr -d '\r')"
[[ "$abi" == *arm64-v8a* ]] || { echo 'This APK requires an ARM64 device.' >&2; exit 1; }
"$adb_tool" -s "$serial" install -r "$apk_path"
activity="$("$adb_tool" -s "$serial" shell cmd package resolve-activity --brief "$package_id" | tr -d '\r' | tail -1)"
[[ "$activity" == "$package_id/"* ]] || { echo 'Could not resolve game launch activity.' >&2; exit 1; }
"$adb_tool" -s "$serial" shell am start -W -n "$activity"
echo 'Installed and launched. Complete the device checklist in docs/goal-09-android.md.'
