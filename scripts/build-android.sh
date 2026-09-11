#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity}"
[[ -x "$unity_editor" ]] || { echo 'Set UNITY_EDITOR to Unity 6000.3.23f1.' >&2; exit 1; }
[[ ! -e "$project_dir/Temp/UnityLockfile" ]] || { echo 'Close this project in Unity before building.' >&2; exit 1; }
# Android tools reject non-ASCII paths. Stage source in a dedicated ASCII directory.
build_key="$(printf '%s' "$project_dir" | cksum | awk '{print $1}')"
build_dir="${ANDROID_BUILD_DIR:-/private/tmp/BurgerShop-Android-$build_key}"
if [[ -n "$(printf '%s' "$build_dir" | LC_ALL=C tr -d '\040-\176')" ]]; then
  echo 'ANDROID_BUILD_DIR must contain only ASCII characters.' >&2; exit 1
fi
if [[ -d "$build_dir" ]]; then
  [[ -f "$build_dir/.burgershop-source" && "$(cat "$build_dir/.burgershop-source")" == "$project_dir" ]] || {
    echo 'Build staging directory belongs to another source. Choose a new ANDROID_BUILD_DIR.' >&2; exit 1;
  }
fi
[[ ! -e "$build_dir/Temp/UnityLockfile" ]] || { echo 'Build staging project is already open in Unity.' >&2; exit 1; }
mkdir -p "$build_dir" "$project_dir/Logs" "$project_dir/Builds/Android"
printf '%s\n' "$project_dir" > "$build_dir/.burgershop-source"
for folder in Assets Packages ProjectSettings; do
  mkdir -p "$build_dir/$folder"
  rsync -a --delete --exclude '_Recovery*' --exclude 'LocalSceneBackup*' "$project_dir/$folder/" "$build_dir/$folder/"
done
# Keep the staged Library for subsequent builds; on macOS use an APFS clone for the first import.
if [[ ! -d "$build_dir/Library" && -d "$project_dir/Library" ]]; then
  if [[ "$(uname -s)" == Darwin ]]; then cp -cR "$project_dir/Library" "$build_dir/Library"
  else cp -R "$project_dir/Library" "$build_dir/Library"; fi
fi
set +e
"$unity_editor" -batchmode -quit -projectPath "$build_dir" -buildTarget Android \
  -executeMethod BurgerShop.Editor.AndroidBuild.BuildApk -logFile "$project_dir/Logs/goal09-android-build.log"
result=$?
set -e
if [[ -f "$build_dir/Logs/android-build-summary.json" ]]; then
  cp "$build_dir/Logs/android-build-summary.json" "$project_dir/Logs/android-build-summary.json"
fi
if [[ -f "$build_dir/Logs/android-runtime-components.txt" ]]; then
  cp "$build_dir/Logs/android-runtime-components.txt" "$project_dir/Logs/android-runtime-components.txt"
fi
[[ "$result" == 0 ]] || { echo "Build failed; staging retained at $build_dir" >&2; exit "$result"; }
cp "$build_dir/Builds/Android/BurgerShop-0.1.2-arm64.apk" "$project_dir/Builds/Android/"
(cd "$project_dir/Builds/Android" && shasum -a 256 BurgerShop-0.1.2-arm64.apk > BurgerShop-0.1.2-arm64.apk.sha256)
echo "$project_dir/Builds/Android/BurgerShop-0.1.2-arm64.apk"
