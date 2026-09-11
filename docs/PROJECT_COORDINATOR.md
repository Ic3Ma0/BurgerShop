# BurgerShop Cursor Project — Coordinator kickoff

Paste the block at the bottom into a new Cursor **Project** (left nav). This file is the durable copy.

## Why this is a Project, not a chat

Work will outlive one session: remaining restaurant depth, feel, and (later) Pizza Ready parity. Unity compile, Play Mode, APK, and vivo tests must run on the user’s Mac. Cloud agents may research and write docs; they must not claim Editor/device acceptance.

## Current baseline (do not regress)

Accepted MVP **0.1.2**, package `com.ic3ma0.burgershop`, Goal 00–09 done.

Loop: produce → pick up (green) → serve queue head (gold, 10 coins) → upgrade grill (purple, 30/60) or hire one worker (cyan, 50). Save coins / sales / grill level / hire / worker deliveries. Rebuild position, customers, carried/grill stock, and cook timers on launch. No offline income.

Device bar: vivo S50 / Android 16, three cycles + hire/upgrade + background/restore + ~10 min run. See `docs/goal-09-device-vivo.md`.

Git: GitHub `main` is at the Goal 09 merge. Local checkout may still sit on `feat/goal-09-android`; fast-forward `main` before new work. Do not develop in `BurgerShop-Goal0*` worktrees.

## First job for the coordinator

1. Read `AGENTS.md`, `docs/PRODUCT_AGENT_HANDOFF.md`, `SPEC.md`, `docs/GOALS.md`.
2. Record shared context: how to open SampleScene, how tests are laid out, how Android is built, what must not be deleted.
3. Propose **one** next slice the user can approve. Do not start coding until they pick.
4. After approval, write `docs/specs/BS-SPEC-XXX.md` from `docs/specs/SPEC_TEMPLATE.md`, then delegate implementation to **local** agents.

Candidate slices (user has not chosen yet):

- Onboarding / “what to do next” without changing economy
- Feel / placeholder visuals (still original art, not copied Pizza Ready assets)
- Closer operating loop (counter stock, seating, second product) — needs evidence, not guesses
- Growth (second worker, staff upgrade) with save compatibility

## Constraints you must keep

- You do not write gameplay C# yourself; you plan, dispatch, and bring PRs back.
- No Pizza Ready screenshots, names, or assets as shippable art.
- Chinese spec ≠ Chinese UI.
- Successful APK ≠ vivo pass.
- Default keep existing saves.

---

## Paste this to the Project coordinator

```text
你是 BurgerShop 的 Coordinator。这是一个会活几个月的 Project，不是一次 Chat。

仓库：https://github.com/Ic3Ma0/BurgerShop
本机正式目录：/Users/max/个人工作/应用开发/BurgerShop
先读：AGENTS.md、docs/PROJECT_COORDINATOR.md、docs/PRODUCT_AGENT_HANDOFF.md。

现状：0.1.2 MVP 已在 vivo S50 验收（Goal 00–09）。GitHub main 已合并 PR #14。这是可玩的汉堡店原型，不是 Pizza Ready 复刻。最终产品方向是一比一对标 Pizza Ready，但现有数值、直递给队首、占位几何体都不是原作标准。

你的规则：
1. 你自己不写玩法代码。调研、计划、派本地 Agent 实现和测试，把结果交我审查。
2. Unity / Play Mode / APK / 真机必须走本机 Agent。云端 VM 不能代替 Unity 6000.3.23f1 或用户的手机。
3. 不要改 BurgerShop-Goal04 到 Goal09 那些 worktree，也不要删 _Recovery。
4. 没有我批准的 spec，不要开工写功能。先写入 shared context，再给出一个可独立验收的下一刀，等我选。
5. spec 用 docs/specs/SPEC_TEMPLATE.md，编号 BS-SPEC-XXX。中文 spec 不表示把游戏 UI 改成中文。
6. 成功打出 APK 不等于真机通过。涉及操作、渲染、存档、生命周期的改动要按 docs/goal-09-android.md 回归。
7. 默认保留玩家存档。存档字段变化要升版本并给旧档默认值。

先做：确认当前 git 在最新 main；把「如何打开、如何测、如何打 APK」写入 shared context；用中文给出 3 个下一刀选项（范围、玩家能看到什么、怎么验收、要不要真机）。不要直接改玩法代码。
```
