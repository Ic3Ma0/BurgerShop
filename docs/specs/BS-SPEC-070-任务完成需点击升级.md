# BS-SPEC-070：任务完成需点击升级

- 版本：v1.0
- 状态：已完成，本地定向 EditMode 通过；已纳入本地提交 `0e93618`
- 开发时基线（历史）：本地 `main` + 未提交 057–069；取代 066 的自动晋级
- 优先级：试玩 Rank 2 被自动送到 3；Rank 3 员工出餐后没有 Upgrade，胶囊停死
- 前置：042 双门槛（星条 / RankAction / 升级面板点 `TryUpgradeRank`）；062 Rank 1 点击开堂食；065 清桌补星；066 自动升（本 spec 作废其晋级）；064 HR=Lv.3
- 用户已确认：与 Rank 1 相同，完成当前级里程碑后出现 Upgrade，必须再点一次才晋级；不要自动升；不要改桌数、房间等级、价格、存档 schema；不要实现 069 的 1–15 内容重排
- 产品建议或关键未定项：无

## 目标与范围

Rank 1：升烤台 → 左上 Upgrade → 点击 → Rank 2。这是唯一允许的晋级手感。

Rank 2：清桌后应同样出现 Upgrade。066 在 `NotifyCleanTable` / `Advance` 里直接 `TryUpgradeRank`，玩家没有点击就被送到 Rank 3。

Rank 3：员工完成一单只 `RecordMilestone` +2 星，`StarCap(3)=8`，`CanUpgrade` 为假，胶囊停在已完成标题上，没有下一步。

本次之后：完成**当前级列出的**里程碑就把星星补到 `StarCap`（065 从清桌推广到所有内容级），`CanUpgrade` 为真，胶囊 / 左上走与 Rank 1 相同的 Ready / Upgrade。只有现有 Upgrade 点击调用 `TryUpgradeRank`。

本次交付：

- 当前级里程碑完成 → `Stars = StarCap`（若仍不足）
- 不自动 `TryUpgradeRank`（清桌、`Advance` 补星、任何 tick）
- Rank 3 `WorkerOrder`（及其它当前级内容里程碑）同样补星 + 等待点击
- 点击后胶囊显示下一级已有 `ShopRanks` 标题，不得空白
- 改写 066 及其它期望自动升的测试

本次不做：改起步桌数量（仍为当前四/三桌实现）；改 HR=3 或其它 064 房间数字；改 `ShopRanks.Goals` / `NextUnlock` / 循环级；改价；改存档 schema；为 4–15 新建设施。

## 玩家价值与成长衔接

- 玩家已学会 Rank 1 的确认点击。066 拿走点击，070 还回去，并把 Rank 3 的死路补成同一环。
- 不新增动词。065 补星从清桌推广到「本级列出的那一件事」。
- 对经济：不改 `StarCap` 与价格。补星只解除内容级「差星无出口」。

## 经济与节奏依据

- `StarCap(2)=6`、`StarCap(3)=8`。清桌/员工出餐本身不花钱，+2 星不够。补满本级 `StarCap` 后点击扣星，余量规则与 042 相同。
- 不改 T(n)。循环级仍只看金币。

## 玩家流程与规则

| 项目 | 明确规则 |
| --- | --- |
| 触发 | 当前级 `ShopRanks.Goals(rank)[0]` 对应的里程碑首次完成（清桌、员工出餐、第二烤台产出等） |
| 补星 | 若仍 `Stars < StarCap`，设为 `StarCap`。只作用于**当前级**。过去级补记里程碑只 +2，不把当前级星星拉满 |
| 可升级 | `CanUpgrade`：内容级 = 里程碑已完成且星星够；循环级仍为金币 |
| 晋级 | **仅** `TryUpgradeRank(expectedRank)`，入口与 Rank 1 相同：星条打开面板再点 Upgrade / 现有 RankAction 路径。禁止从 `NotifyCleanTable`、`RecordMilestone`、`Advance`、tick 调用 |
| 旧档补星 | 当前级里程碑已记但星星不够：下一次同事件或 `Advance` 只补星并显示 Upgrade，**不升等级** |
| 其它 | Rank 1 仍要点击。064 HR 在 Rank 变为 3 之后才允许。桌数不改 |

## 界面与资源

- 可升级时：胶囊 `ShopRanks.RankUpCapsule`（`Tap Lv.{n} Ready`）；左上星条 `Lv.{n} Ready`；Rank 2+ 的 `GuideCopy` 为 `Upgrade`（Rank 1 仍为 `Unlock dining`）
- 点击晋级后：`OpeningGuide` 仅 Rank 1 有效；Rank≥2 用 `TaskCapsuleHud` 显示新级 `Title`（Rank 3→4 为当前 `Produce on the second grill`），不得空标题
- 界面语言：英文
- 反馈：点击成功后才走现有 Rank Up Success，清桌完成不播晋级

## 边界、存档与兼容

| 情况 | 预期行为 |
| --- | --- |
| 只清部分垃圾 | 不补星、不 Ready |
| 重复清桌 / 重复出餐 | 里程碑不重复发星 |
| 员工代清 / 员工出餐 | 与玩家同一记账 |
| 星星已够、里程碑未做 | 不可升级（042 双门槛仍在） |
| 旧会话已可自动升但未点 | 进入后补星并停在 Ready，等待点击 |
| 存档 | 不新增字段；晋级成功仍立刻 Flush（067），清桌本身不够成晋级 |

## 验收标准

| 编号 | 初始状态 | 操作 | 必须出现的结果 |
| --- | --- | --- | --- |
| AC-01 | Rank 2，目标 CleanTable，桌有垃圾 | 拾取至桌面干净 | Rank 仍为 2；`CanUpgrade`；胶囊 Ready；左上 Upgrade；`Allows(3)=false` |
| AC-02 | AC-01 之后 | `TryUpgradeRank(2)` | Rank=3；HR 允许；胶囊为 Rank 3 标题 |
| AC-03 | Rank 2，清桌位已记，星星不足 | `Advance` 或再清一桌 | 星星到 `StarCap`；`CanUpgrade`；Rank 仍 2 |
| AC-04 | Rank 1，里程碑与星星已齐 | `Advance` | Rank 仍 1；点击后 Rank 2 |
| AC-05 | Rank 3，目标 WorkerOrder | 记员工完成一单（或同等 `RecordMilestone`） | Rank 仍 3；`Stars==StarCap`；`CanUpgrade` |
| AC-06 | AC-05 之后 | `TryUpgradeRank(3)` | Rank=4；`Title` 为当前 Rank 4 目标，非空 |
| AC-07 | 起步桌布局 | 本轮任意操作 | `ShopLayout.Tables` 长度与开局桌数不变 |

不应破坏：062 开局胶囊、064 房间数字、042 无里程碑不可升、065 墙体/寻路、069 计划不落地、经营数值。

## 变更与交付记录

- v1.0：首次规格。065 补星推广到当前级里程碑；066 自动晋级作废。
- 2026-09-16：按本 spec 落地。`RecordMilestone` 在当前级完成时把星星补到 `StarCap`；`NotifyCleanTable` / `Advance` 只补星，不再 `TryUpgradeRank`。未改 `ShopRanks.Goals`、桌数、064 房间、存档 schema、价格。
- 2026-09-16 **BS-SPEC-075**：完成任务补满 `StarCap` 的规则已被 075 取代；晋级仍须点击 `TryUpgradeRank`，但门槛只看星星（Lv.1–15）或金币（Lv.16+）。
- Unity 6000.3.23f1，隔离工程 `/tmp/bs059-editmode`。定向 EditMode **25/25**：Spec070RankUpClickTests 5/5、Spec066Tests 4/4、Spec042Tests 9/9、Spec062OpeningTests 7/7。日志 `Logs/spec070/`。未推 GitHub，未做 Android，未改真实存档。
