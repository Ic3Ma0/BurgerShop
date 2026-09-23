# Spec 083 经济一致性修复交付记录

日期：2026-09-22。Unity：6000.3.23f1。入口：SampleScene。测试项目与存档均位于临时目录；未读取、重置或覆盖玩家真实存档。本轮没有推送 GitHub。

**A 的改动已实现并完成定向自动验证。B 仍是部分采样，不能称 0–15 已平衡。** 价格与收益表保持原值；汽车窗口能力按本 spec 生效，另修复采样实际发现的垃圾桶投放可达性。

## 行为及 diff 说明

| 改动 | 代码归属 | 结果与理由 |
| --- | --- | --- |
| 汽车窗口升级 | `Restaurant/DriveThruLane.cs`、`Building/FacilityInstance.cs`、`FacilityUpgradeBenefit.cs`、`UI/FacilityDetailsHud.cs` | 额外窗口 L1/L2/L3 开始交付间隔为 0.75/0.65/0.55 秒，共享冷却，与 0.42 秒动画重叠。仍收 100/200，每次 +2 星。已有等级重载立即生效。 |
| 同一购买报价 | `Building/FacilityPurchaseQuote.cs`、`FacilityLayout.cs`、`FacilityShopHud.cs` | 使用购买前持有数量定价；区分基础、当前完整、已付、应付。确认时价格变化先刷新，第二次确认才付新报价。取消和移动不付款。 |
| 抵扣与完整营业投入 | `Economy/BusinessOpeningQuote.cs`、`Restaurant/ShopExpansion.cs`、`UI/SessionGoalTracker.cs` | 可乐在自有空间缺全套时 450，选择侧翼为 750；纸袋缺全套 750。扣除已有设施和投资；首个免费汽车窗口不重复计费。兼容侧翼尚未创建购买地垫的旧投资，成交后清除已消费的抵扣。 |
| 投资推荐 | `Economy/Rules/InvestmentPriority.cs`、`InvestmentObservation.cs`、`UI/InvestmentGuide.cs`、`TaskCapsuleHud.cs`、`OpeningHudCopy.cs` | 前置优先；生产、运输、服务、座位用持续 10 秒观察和恢复滞后来排序，保留手动查看其他投资。携带中的库存也算上游供货；交付及顾客向前补位不被当成需求消失。脏桌优先清理，不拿它推荐买更多座位。 |
| 员工属性资格与事务 | `Restaurant/StaffUpgradeBoard.cs`、`WorkerHiringZone.cs`、`GrowthUpgrades.cs`、`UI/StaffUpgradeHud.cs` | 无员工提示先雇人，实际请求不扣款、不发星。旧属性保留，首雇后生效。付费升级在钱包事件发布前提交能力/星星，并禁止回调重入；已有自定义设施等级快照对齐时不重买、不补星、不降级。 |
| 离线报价 | `Economy/OfflineUpgradeCostSource.cs` | 未完成餐桌按合法候选完整价估值：已付 49 的最低参考仍为 50，实际尾款仍为 1。隐藏购买区、全付但未选样式也覆盖；已选样式正常退出。无人不纳入员工属性。051/079 的公式、8 小时上限、收据和保存顺序不改。 |
| 垃圾桶可达性 | `Restaurant/TrashBin.cs` | 模型放大后，碰撞会把人物中心挡在约 1.04 米外；原 1 米投放半径不可达。默认改为集中常量 1.25 米，保留真实碰撞、投放动画及频率。 |

上述路径相对 `Assets/_Project/Scripts/`。没有新货币、新玩法、工资、租金、动态涨价或存档字段。

## 验证

结果按测试完整名称去重，并以最后一次结果为准：**103 通过，4 项既有失败**。未把多次重跑相加，也未把单纯编译称为玩法验收。[逐项结果](latest-results.json)。

| 范围 | 证据 |
| --- | --- |
| 三档实际交付、不同操作者共享冷却、旧等级应用、尾款报价、持续观察及实际队列、脏桌排除 | `Spec083EconomyTests`，[k.xml](k.xml)；额外旧等级/事务测试 [l.xml](l.xml) |
| 实际商城抵扣、可乐不强制买侧翼、首窗免费、纸袋缺件、汽车升级付费/发星/重载、报价变动与取消/移动 | `Spec083EconomySceneTests`，[k.xml](k.xml) |
| 无员工禁买、旧员工属性、1 人/3 人全部应用新能力、重复回调 | `StaffUpgradeTests`，[k.xml](k.xml) |
| 离线保存失败重试、一次结算、报价边界与旧档 | `Spec051OfflineTests`、`Spec079OfflineBoundaryTests`，[k.xml](k.xml) |
| 初始晋级、设施直接购买、汽车服务与摆放边界 | `Spec080OpeningRevisionTests`、`FacilityDirectPurchaseTests`、`DriveThruTests`，[k.xml](k.xml)；`Spec043PlacementTests`，[l.xml](l.xml) |
| 垃圾收集/投放、桶外 1.1 米可投放和 2 米不可投放、真实行走采样 | `DiningTrashTests`、`Spec083EconomyPacingTests`，[j.xml](j.xml) |
| 手机 1080×1920、横屏 1600×1000：报价换行、文字高度、与价格按钮不重叠 | 最后一次 [n.xml](n.xml)；[竖屏图](quotes-phone.png)、[横屏图](quotes-landscape.png)。截图采用测试资金 5000，仅作 UI 验证，不参加收入采样。 |
| 规则边界与差异 | `check-boundaries.sh --spec docs/specs/BS-SPEC-083-经济一致性修复与节奏标定.md` 通过，6 项脚本自测试通过；`git diff --check` 通过。 |

Computer Use 首次连接超时，随后 Unity 截屏报 `-10005: The screen capture failed`。以上图片来自隔离 Play 的实际 UI 离屏渲染并经图片检查，**不是 Computer Use 人工点击验收，也不是手机真机验收**。

### 四项旧成长失败

使用开发前保留的 C# 快照，并恢复最早已修改的 5 个文件到此前 Git 基线后，在同一个隔离项目复跑 `GrowthEconomyTests`：[baseline.xml](baseline.xml) 为 2 通过、4 失败。当前 [k.xml](k.xml) 的这 4 项失败名称、位置及错误与基线一致；未修改它们的断言：

- `TablePurchaseAwardsOnceAndRankRequiresConfirmation`：第 56 行旧晋级预期失败。
- `UpgradePathEarnsEnoughStarsWithoutAutomaticRanks`：第 66 行旧晋级预期失败。
- `DiningTipIsLockedOnSeatingAndBagConstructionRestoresPartially`：第 150 行就座模拟未进入 Eating。
- `OneWorkerCanCompleteAllThreeLines`：第 142 行 800 秒手动模拟仍停在 `ToPackage/Pack`，三条线均未完成订单。

前两项涉及现有首单晋级语义；后两项仍需单独定位模拟夹具或实际行为。不能据此宣布员工跨三业务长期经营已通过。自然采样只证明本次单个员工参与基础经营的路径。

另有两处新场景夹具在 EnterPlayMode 脚本域重载后丢失捕获变量，已把捕获状态创建移到重载后；它们不是游戏内交易空引用。旧员工存档夹具也已补齐有效版本及真实保存步骤，避免把空目录当作含旧属性的存档。

## B：当前实际采样

[完整事件 TSV](natural-first-staff.tsv)。固定随机种子 830922；初始化完成后开始计时；游戏速度 1；经营采样菜单暂停 0。自动脚本只移动角色及执行合法购买，不传送、不关碰撞、不注资、不直接调用售卖结算或加速生产。用钱包正常入账事件与支出分别统计；没有拿余额差替代收入。

自动行走包含人为选定的绕桌、绕柜台路线，因此这些是**该路线的游戏内实际时间，不是人类实机耗时或最优玩法时间**。到 3 级后固定等级观察首雇经营，没有继续晋级至 15。

| 事件 | 从采样起点计时 | 钱包 / 地面现金 | 累计实收 / 支出 |
| --- | ---: | ---: | ---: |
| 首单完成并升到 2 级 | 28.31 秒 | 0 / 10 | 0 / 0 |
| 买第一次机器升级 | 42.62 秒 | 0 / 0 | 30 / 30 |
| 清桌后升到 3 级 | 43.55 秒 | 0 / 0 | 30 / 30 |
| 主厅支付 150 | 169.52 秒 | 0 / 90 | 180 / 180 |
| 已付主厅且有首雇所需 50，开始走向 HR | 169.87 秒 | 50 / 40 | 230 / 180 |
| 首位员工实际雇用 | 206.93 秒 | 40 / 10 | 270 / 230 |
| 首雇后观察窗口开始（地面现金归零） | 238.37 秒 | 70 / 0 | 300 / 230 |
| 180 秒窗口结束 | 418.37 秒 | 450 / 0 | 680 / 230 |

- 从升到 2 级到第一次 30 金币机器升级：**14.31 秒**。首单之前含户外顾客走入店内的过程。
- 3 级钱包 0 时，从该状态赚够主厅 150 + 首雇 50 的全部投入：**126.32 秒**；之后按该脚本路线前往 HR 并完成雇用，又用 **37.06 秒**。不能把走路全部算成价格等待。
- 首雇后窗口从地面现金 0 开始（`GroundValue` 包含收取途中的现金），避免把窗口前现金计为新产出；窗口末也为 0。正常入账 `680−300=380`，有效经营 `180` 秒，**R=126.67 金币/分钟**。
- 全段核对：正常实收 680 − 支出 230 = 钱包 450。支出仅机器升级 30 + 主厅 150 + 首雇 50。
- 未进行相同配置、相同长度的无员工/有员工配对窗口，顾客类型及订单会变动，**不计算员工 ROI、不宣称 126.67 是员工单独带来的收入**。

### 价格决策与剩余范围

本轮**没有改购买价格、升级价格、售卖单价、星阈值或收入加成**。汽车窗口服务参数只按 A1 修正；垃圾桶仅扩大可达投放距离。

主厅+首雇这一完整投入的采样超过 90 秒，仍需验证该阶段玩家属性、家具等可选投资是否能提供有价值的替代目标，不能仅凭“可以部分存钱”认定节奏合格。开局第一单 28.31 秒也需结合真实操作确认动线和引导，而不是直接涨收入。

尚未覆盖：

1. 一条连续自然新档到 Lv.15 的记录。
2. Lv.4–6 第二机器、可选柜台及蓝盒汽车业务的收入和操作对照。
3. Lv.7 自有空间 450 与侧翼 750 两条开业路线的 R/T。
4. Lv.8→9 自动化前后、骑手贡献与后续投资消化速度。
5. Lv.10 纸袋、Lv.12/Lv.15 的完整投入、下个有效目标与实际收入。
6. 生产/雇员路线与玩家/家具路线；同阶段 1 人/3 人的实际收入比较。目前 1 人/3 人仅完成属性作用测试。
7. 以上四项旧成长失败的后续处理，以及人工 Unity 操作检查。

本轮据此交付经济一致性修复和局部实测，B-01/B-02/B-03 不标为全部通过。离线完整公式和 Lv.16+ 长期增长关系仍按原 spec 列为独立后续项。

## 复现入口

- 行为回归：`Spec083EconomyTests`、`Spec083EconomySceneTests`，配合本记录列出的相关 fixture。
- 长采样已标 `Explicit`，避免普通回归每次运行约七分钟。需重新标定时单独选择 `Spec083EconomyPacingTests.NewGameToFirstEmployeeWithCollectedIncomeTrace`；输出 `/tmp/bs083-economy-natural.tsv`。它继承隔离存档夹具，不能改为真实存档目录。
- j/k/l/m/n 是不同定向轮次的原始 XML，并非可相加的测试数量；`latest-results.json` 保存去重后的最后结果。baseline 是修改前对照，不计入 103 项通过。
