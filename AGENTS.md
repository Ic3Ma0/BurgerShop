# BurgerShop — Agent instructions

This is an Android-first Unity 6.3 hamburger-shop game. The accepted MVP is version **0.1.2**. Do not treat placeholder geometry, current numbers, or “hand burger to queued customer” as Pizza Ready canon.

## Source of truth

- Official repo: this directory only. Sibling folders `BurgerShop-Goal04` … `Goal09` are historical git worktrees. Do not edit them.
- GitHub: `https://github.com/Ic3Ma0/BurgerShop`. Remote `main` already contains Goal 00–09 (merge of PR #14).
- Unity: `6000.3.23f1` (LTS). URP 17.3.0. Input System 1.20.0.
- Play entry: `Assets/Scenes/SampleScene.unity`. The shop is assembled at runtime by `Assets/_Project/Scripts/Core/Goal01Bootstrap.cs`, not by authored prefabs.
- Keep `_Recovery/` and old stashes. Do not `stash apply` historical backups.

## Product process

User direction → a concise spec (`docs/specs/SPEC_TEMPLATE.md`, id `BS-SPEC-XXX`) → implement and verify. Specs own player-visible rules; you own classes, tests, and Git. When the user's request establishes the behavior, write the spec and continue implementation; a spec is not a second approval gate. Ask only about unresolved product decisions that would materially change scope or behavior.

Do not add features because they would be “more like Pizza Ready.” Current long-term product intent is a one-to-one of Pizza Ready, but existing code is a prototype. Freeze behavior in a spec first.

For new gameplay, expansion, progression or economic design, use `docs/design/BURGER_RESTAURANT_DIRECTION.md` → “新增玩法的分析要求” and the relevant sections of `docs/specs/SPEC_TEMPLATE.md`. Explain the player's current need, restaurant logic, distinct decision, unlock sequence and economy impact before proposing implementation. More content alone is not a design justification; historical numeric snapshots must be checked against current code.

Baseline snapshot: `docs/PRODUCT_AGENT_HANDOFF.md`. Save rules: `docs/goal-08-save.md`. Historical device evidence only: `docs/goal-09-device-vivo.md` (not a current acceptance requirement).

## Engineering constraints

- New stations, zones, workers, HUD, and save fields follow existing patterns under `Assets/_Project/Scripts/` and tests under `Assets/Tests/EditMode/`.
- Persist only long-lived restaurant state unless the spec says otherwise. Today that is coins, sales, grill level, hired worker, worker delivery count. Do not add offline earnings by default.
- Bump `RestaurantSaveData` version when the schema changes; old saves must keep progress.
- Player and staff share grill stock, cashier cooldown, and wallet. Do not let extra carriers skip the cashier throttle.
- Android IL2CPP strips unreferenced colliders and shaders. Keep `_Project/link.xml` and `RuntimeMaterials` (Resources Lit/Unlit). Never rely on `Shader.Find` alone.
- Project path contains Chinese. Android builds go through `scripts/build-android.sh` (ASCII staging dir). Editor menu build is English-path only.
- APK success is not device acceptance. Android builds and device verification are deferred until the user explicitly starts a later consolidated acceptance phase.

## Verification and current development priority

Latest user clarification: prioritize new feature implementation and completing the project. Follow requirements → concise actionable spec → direct code changes → proportionate local Unity verification → prompt delivery.

- Product specs focus on behavior, scope, essential parameters, compatibility and reproducible functional acceptance. Do not turn each feature into an elaborate validation project.
- Prefer targeted automated tests for logic. Use Computer Use on SampleScene in Unity Play when visual, audio or interaction evidence is needed. Protect real saves; isolated tests must not touch real `persistentDataPath`.
- Do not routinely require long endurance runs, repeated recordings or full regression for small changes. Expand verification only for relevant risk, failures or new evidence. Stop repeating passed checks without a reason.
- Android packaging, installation, phone operation and platform acceptance are deferred to a later consolidated phase arranged by the user, not permanently cancelled. Do not initiate them after every feature/batch or treat them as current delivery blockers.
- Deliver once the current functional/local acceptance is satisfied and proceed with requested features. Record actual results and in-scope issues honestly; retain historical device evidence without making future platform work a current defect.
- Development and verification run locally. Preserve the Android product target and platform compatibility code.

## Task-specific references

- Current development and verification policy is defined above. `.cursor/rules/burgershop-core.mdc` and `local-unity-verification.mdc` route to it rather than adding separate delivery gates.
- Spatial, furniture, HUD or interaction changes: use `.cursor/rules/burgershop-editmode-gate.mdc` for relevant regression examples.
- Economy, progression, unlock or monetization changes: use `.cursor/rules/burgershop-pacing-invariants.mdc`; read `docs/GAME_CORE.md` when design rationale is needed. Historical numeric snapshots must be checked against current code and approved specs.
- Documentation-only work needs diff, reference and format checks; it does not require launching Unity or running gameplay tests.

## Language

User and specs are Chinese. In-game copy is English unless a spec changes that.
