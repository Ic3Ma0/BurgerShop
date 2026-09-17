# BS-SPEC-062：开局一分钟指引

- 版本：v1.2
- 状态：已实现，本地定向验证通过；未推 GitHub
- 对应基线：本地 main + 未提交 057–061；Rank 1 当前代码已是 `Upgrade the burger machine`（`ShopGoalKind.UpgradeGrill`）
- 用户确认：要指引升级/新功能，不要教赚钱循环；不要恢复 Waiting/Serve 条
- 前置：BS-SPEC-042 店铺等级；BS-SPEC-061 已去掉 ActionHint

## 目标与范围

玩家开局已有汉堡机和柜台，会补货出餐，但不知道**怎么升级**，第一分钟没有一次解锁体验。本次之后，开局只指向第一次付费升级（紫圈烤台）和随后的店铺晋级（堂食/清理解锁）。

本次交付：

- 顶部任务胶囊只显示升级/解锁文案，不轮换 Collect / Stock / Serve / Pick up cash
- 高亮现有紫色烤台升级圈；升完且可晋级后改指星条 Rank Up
- 钱不够 30 时仍指向升级目的地，不写成去卖汉堡

本次不做：金钱循环教程；全局改价或降收入；新经营存档字段；恢复 ActionHint；Android。

## 不是金钱教程

胶囊和开局指针**禁止**逐步教：取餐 → 上柜 → 出餐 → 捡钱。玩家已经知道机台和柜台。钱不够时文案仍是升级烤台（差 30 / 紫圈），不是 “go sell burgers”。

## 玩家流程与规则

| 项目 | 明确规则 |
| --- | --- |
| 触发 | 新档或仍为店铺 Lv.1，且主烤台仍为 1 级 |
| 第一步 | 胶囊：`Upgrade the grill — 30 coins`（当前代码首次升级价 30，`GrillUpgradeZone` `{30,60}`）。世界：现有紫圈脉冲。不要求先成交。 |
| 钱不够 | 仍显示上述升级句，进度可为 `0/30`。不改步骤去 Collect/Serve。 |
| 升完烤台 | 记 Lv.1 里程碑（+2 星）+ 设施升级星（+2），满 4 星则可晋级。胶囊改为 `Tap Lv.1 Ready — unlock dining`。紫圈引导关闭，星条强调可点。 |
| 结束 | 店铺 ≥Lv.2 后胶囊回到等级目标（清理桌面等）。旧档 Rank≥2，或烤台已 >1 级且还不能晋级：跳过开局指引。 |
| 费用 | 不改 30/60、堂食 10、属性起步 50。不新增存档字段。 |

## 界面

- 复用 `TaskCapsuleHud` 和现有紫色 `GrillUpgradeSpot`，不新建挡角色的提示条，不创建 Serve/Waiting `ActionHint`（`InteractionFocus.Build` 保持 null）。
- 胶囊在左上星条/等级芯片正下方：左对齐，锚点 `(0,1)`，位置 `(32, -128)`，尺寸 `320×52`（与星条同宽）。不是屏幕中央大卡，高度不超过星条。
- 字号缩小（标题 20、进度 18）；左侧约 22 圆标；行尾显示 `n/n`；底部一条约 3px 细进度线。不要横贯屏幕的粗绿条，不要厚阴影大卡片。
- 不挡左下摇杆、不挡右上金币。开局文案仍由 `OpeningGuide` 提供，本条只改呈现。
- 星条 `RankAction` 可同步短句（Upgrade grill / Unlock dining）。
- 游戏内英文。

## 验收

| 编号 | 初始状态 | 操作 | 必须出现的结果 |
| --- | --- | --- | --- |
| AC-01 | 新档，0 金币，烤台 Lv.1 | 看胶囊与紫圈 | 升级烤台文案（含 30）；不是 Collect/Stock/Serve/Cash；紫圈可见；无 ActionHint |
| AC-02 | 手上有汉堡或柜上有货或地上有钱 | 看胶囊 | 文案仍是升级/解锁，不因补货出餐改成赚钱教程 |
| AC-03 | 钱包 ≥30 | 完成主烤台第一次升级 | 里程碑完成；可晋级则胶囊为解锁堂食 |
| AC-04 | 读档 Rank≥2 | 看胶囊 | 无开局升级句，显示当前等级目标。Rank 1 烤台已>1 级时 070 会补星并指向 Rank Up，仍须点击 |
| AC-05 | 竖屏 HUD | 观察胶囊 | 在左上 `Lv.` 星条正下方，宽约 320、高约 52；店面中间不被大卡挡住；进度是行尾数字加细线，不是粗绿条 |

不应破坏：042 双门槛与 Lv.2–10 解锁；061 无 Waiting 条；售价与升级价；开局升级文案。

## 交付记录

- 开局不教赚钱循环。Rank 1 里程碑改为烤台首次升级（`ShopGoalKind.UpgradeGrill`）。胶囊在 0 金币时为 `Upgrade the grill — 30 coins`（进度 0/30），升完且可晋级为 `Tap Lv.1 Ready — unlock dining`，店铺 Lv.2 后回到 `Clear a used dining table`。紫圈脉冲；不恢复 ActionHint；无新存档字段。
- v1.2：胶囊从屏幕中央 760×160 改为左上星条下 320×52 细胶囊；进度改为行尾 `n/n` + 3px 下划线。开局文案不变。
- Unity 6000.3.23f1，隔离工程 `/tmp/bs059-editmode`，隔离存档。未改真实存档，未推送，未做 Android。
- EditMode 40/40：`HudLayoutTests` 8/8、`Spec062OpeningTests` 6/6、`FeedbackHudTests` 3/3、`SessionGoalTests` 5/5、`Spec026028Tests` 18/18（`Logs/spec062/editmode.xml`）。

## 2026-09-16 核对（接 058 DirectInteraction / 061 无 ActionHint / 064–066）

规则与 Play HUD 一致，未改 30/60、未恢复 Waiting 条、未回退 064 开局店面。066 清桌直升已被 070 否决：

- 新档 0 金币仍是 `Upgrade the grill — 30 coins`（0/30）+ 紫圈；补货出餐捡钱不改成赚钱教程。
- 首次烤台升级且满 4 星后为 `Tap Lv.1 Ready — unlock dining`；星条短句 `Upgrade grill` / `Unlock dining`。
- Rank≥2 跳过开局句；Rank 1 烤台已 >1 时 070 补满星星并指向 Rank Up，仍须点击。
- 058 详情卡会关掉地面升级圈，开局高亮仍强制显示紫圈（`SetOpeningHighlight` 在 DirectInteraction 之后执行）。
- 320×52 胶囊标题改为单行 BestFit（20→最低 16），避免长句在细胶囊里被折行截断。

隔离 EditMode 41/41：`HudLayoutTests` 8/8、`Spec062OpeningTests` 7/7、`FeedbackHudTests` 3/3、`SessionGoalTests` 5/5、`Spec026028Tests` 18/18。日志 `Logs/spec062/editmode.xml`。未做 Play 截图、未做 Android。

