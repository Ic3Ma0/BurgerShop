# BurgerShop — Agent instructions

This is an Android-first Unity 6.3 hamburger-shop game. The accepted MVP is version **0.1.2**. Do not treat placeholder geometry, current numbers, or “hand burger to queued customer” as Pizza Ready canon.

## Source of truth

- Official repo: this directory only. Sibling folders `BurgerShop-Goal04` … `Goal09` are historical git worktrees. Do not edit them.
- GitHub: `https://github.com/Ic3Ma0/BurgerShop` (private). Remote `main` already contains Goal 00–09 (merge of PR #14).
- Unity: `6000.3.23f1` (LTS). URP 17.3.0. Input System 1.20.0.
- Play entry: `Assets/Scenes/SampleScene.unity`. The shop is assembled at runtime by `Assets/_Project/Scripts/Core/Goal01Bootstrap.cs`, not by authored prefabs.
- Keep `_Recovery/` and old stashes. Do not `stash apply` historical backups.

## Product process

User direction → a spec (`docs/specs/SPEC_TEMPLATE.md`, id `BS-SPEC-XXX`) → implement and verify. Specs own player-visible rules; you own classes, tests, and Git.

Do not add features because they would be “more like Pizza Ready.” Current long-term product intent is a one-to-one of Pizza Ready, but existing code is a prototype. Freeze behavior in a spec first.

Baseline snapshot: `docs/PRODUCT_AGENT_HANDOFF.md`. Save rules: `docs/goal-08-save.md`. Device bar: `docs/goal-09-device-vivo.md`.

## Engineering constraints

- New stations, zones, workers, HUD, and save fields follow existing patterns under `Assets/_Project/Scripts/` and tests under `Assets/Tests/EditMode/`.
- Persist only long-lived restaurant state unless the spec says otherwise. Today that is coins, sales, grill level, hired worker, worker delivery count. Do not add offline earnings by default.
- Bump `RestaurantSaveData` version when the schema changes; old saves must keep progress.
- Player and staff share grill stock, cashier cooldown, and wallet. Do not let extra carriers skip the cashier throttle.
- Android IL2CPP strips unreferenced colliders and shaders. Keep `_Project/link.xml` and `RuntimeMaterials` (Resources Lit/Unlit). Never rely on `Shader.Find` alone.
- Project path contains Chinese. Android builds go through `scripts/build-android.sh` (ASCII staging dir). Editor menu build is English-path only.
- APK success is not device acceptance. Phone changes require install → real touch → logcat → save regression on the user’s vivo S50 when the spec says so.

## Verification

- Run Unity EditMode tests after gameplay or save changes. Isolated save tests must not touch the real `persistentDataPath`.
- Editor Play on SampleScene for the player-visible loop.
- Cloud VMs cannot replace Unity Editor, APK signing, or the physical phone. Implementation and test agents for this repo should run **locally**.

## Language

User and specs are Chinese. In-game copy is English unless a spec changes that.
