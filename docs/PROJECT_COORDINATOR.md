# BurgerShop Cursor Project — Coordinator kickoff

Paste the block at the bottom into a new Cursor **Project** (left nav). This file is the durable copy.

## 最新本地开发记录 · 2026-09-12

正式目录的本地开发版已集成 013–022，并完成本轮 023–025 实现；版本为 0.2.0 / Android code 4，最终 Unity 回归 234/234。逐项状态、真机证据及体验路径见 [023–025 集成交付记录](specs/BS-SPEC-023-025-DELIVERY.md)。下面的 0.1.2 / 013 coordinator kickoff 保留为历史，不能作为当前开发范围或当前代码状态。这批内容已于 2026-09-12 通过 [PR #15](https://github.com/Ic3Ma0/BurgerShop/pull/15) 合并到 main（`646eed0`）；正式目录已切换并同步 main。

## Why this is a Project, not a chat

Work will outlive one session: remaining restaurant depth, feel, and (later) Pizza Ready parity. Unity compile, Play Mode, APK, and vivo tests must run on the user’s Mac. Cloud agents may research and write docs; they must not claim Editor/device acceptance.

## Current baseline (do not regress)

Accepted MVP **0.1.2**, package `com.ic3ma0.burgershop`, Goal 00–09 done.

Loop: produce → pick up (green) → stock the counter → stand in the white cashier circle to serve from counter stock (10 coins) → customer sits and eats ~3s → leave. Upgrade grill (purple, 30/60) or hire one worker (cyan, 50). Save coins / sales / grill level / hire / worker deliveries. Rebuild position, customers, carried/grill/counter stock, and cook timers on launch. No offline income.

Device bar: vivo S50 / Android 16, three cycles + hire/upgrade + background/restore + ~10 min run. See `docs/goal-09-device-vivo.md`.

Git: GitHub `main` includes Goal 00–09 (PR #14) and agent onboarding (`d3a1b41`). Work on this checkout only. Do not develop in `BurgerShop-Goal0*` worktrees.

## First job for the coordinator

Shared context (SampleScene Play, EditMode tests, `scripts/build-android.sh`, keep `_Recovery/`) is already in `AGENTS.md` and `docs/PRODUCT_AGENT_HANDOFF.md`.

**Current slice is [BS-SPEC-013](specs/BS-SPEC-013.md)** (counter stock, dining table, top task HUD), approved by the user's reference shots. 010 / 011 / 012 remain optional later work in [`docs/specs/NEXT_SLICE_CANDIDATES.md`](specs/NEXT_SLICE_CANDIDATES.md); they are not a gate. Do not shrink the long-term restaurant-management goal; do not invent features outside an approved spec.

010 可执行草案仍在 [`docs/specs/BS-SPEC-010.md`](specs/BS-SPEC-010.md)，不要删。Cloud agents may only edit docs unless the user names a new spec.

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

现状：0.1.2 MVP 已在 vivo S50 验收（Goal 00–09）。GitHub main 含 PR #14 与 onboarding 文档 d3a1b41。当前实现刀是 docs/specs/BS-SPEC-013.md（柜台出餐、桌子用餐、顶部任务 HUD）。这是可玩的汉堡店原型，不是 Pizza Ready 复刻。最终产品方向是一比一对标 Pizza Ready，但现有数值和占位几何体都不是原作标准。

你的规则：
1. 你自己不写玩法代码。调研、计划、派本地 Agent 实现和测试，把结果交我审查。
2. Unity / Play Mode / APK / 真机必须走本机 Agent。云端 VM 不能代替 Unity 6000.3.23f1 或用户的手机。
3. 不要改 BurgerShop-Goal04 到 Goal09 那些 worktree，也不要删 _Recovery。
4. 没有批准的 spec，不要开工写功能。当前刀是 BS-SPEC-013，不要再阻塞在必须回复 010 / 011 / 012。候选见 docs/specs/NEXT_SLICE_CANDIDATES.md。
5. spec 用 docs/specs/SPEC_TEMPLATE.md，编号 BS-SPEC-XXX。中文 spec 不表示把游戏 UI 改成中文。
6. 成功打出 APK 不等于真机通过。涉及操作、渲染、存档、生命周期的改动要按 docs/goal-09-android.md 回归。
7. 默认保留玩家存档。存档字段变化要升版本并给旧档默认值。

先做：跟进 BS-SPEC-013 的本地实现与 Editor 验证；不要把 010/011/012 当成唯一下一刀。
```
