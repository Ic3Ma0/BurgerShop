# Contributing

Thanks for helping with Burger Shop. Reviewers work in English. Product specs are written in Chinese. In-game copy stays English.

## What this repo is

An Android-first Unity 6.3 hamburger-shop prototype (`0.3.0`). Play entry: `Assets/Scenes/SampleScene.unity`. The shop is assembled at runtime by `Assets/_Project/Scripts/Core/Goal01Bootstrap.cs`, not by a large set of authored prefabs.

## Setup

1. Install [Unity Hub](https://unity.com/download) and Unity **6000.3.23f1** with **Android Build Support**.
2. Clone this repository and open the repo root in Unity Hub.
3. Open `Assets/Scenes/SampleScene.unity` and enter Play Mode.
4. Move with **WASD** or the on-screen stick.

Do not commit `Library/`, `Temp/`, `Logs/`, `Builds/`, signing keys, keystores, `key.properties`, or other secrets.

## How we change the game

1. Branch from `main`.
2. For player-visible behavior, add or update a spec under `docs/specs/` (Chinese; start from [`docs/specs/SPEC_TEMPLATE.md`](docs/specs/SPEC_TEMPLATE.md)).
3. Follow existing patterns under `Assets/_Project/Scripts/` and tests under `Assets/Tests/EditMode/`.
4. Prefer targeted EditMode tests for logic. Isolated tests must not touch real `persistentDataPath`.
5. Open a pull request against `main`.

Do not ship another game’s names, art, or audio.

## Android builds

```bash
bash scripts/build-android.sh
```

The default editor path in that script is macOS. On other machines, set `UNITY_EDITOR` to a Unity **6000.3.23f1** binary. A successful APK is not device acceptance.

## License

Contributions are accepted under the [MIT License](LICENSE).
