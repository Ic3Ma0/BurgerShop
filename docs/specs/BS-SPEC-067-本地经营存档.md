# BS-SPEC-067：本地经营存档

- 版本：v1.0
- 状态：已实现，本地定向验证通过；未推 GitHub
- 对应基线：本地 `main` + 未提交 057–066；存档 schema `RestaurantSaveData.CurrentVersion` = **17**
- 优先级：重开后必须保住已付钱的店，不能靠云存档或多槽位
- 前置：Goal 08 文件/校验/备份；042 星星与里程碑；043 设施摆放；058 地块；059 餐桌档位；064 房间由等级重建
- 用户已确认：单店本地存档；持久化经营进度；会话物与开局指引步骤不写盘；Sound 走 PlayerPrefs
- 产品建议或关键未定项：无。不新增云同步、离线公式、全局经济。多槽见 BS-SPEC-068。

## 目标与范围

玩家关掉应用或杀进程后再进，店还是他买过、升过、雇过的那一家。飞行中的汉堡/垃圾/现金、顾客、桌面垃圾和地上钱堆重新开始。

本次交付：把现行本地存档写成可验收规则，并补上相对这版规则的缺口（消费/晋级后写入、暂停写出、059/058/042 字段往返）。不重写已能工作的 `LocalSaveStore`。

本次不做：云存档、改价格/产能、回退 061–066 / 058 / 059 / 062 / 064、把 Sound 或开局指引步骤写入经营 JSON、Android。多槽入口见 BS-SPEC-068（067 仍是每个槽位的 schema 与写出时机）。

## 玩家流程与规则

单文件、单店。路径：`Application.persistentDataPath/restaurant-save.json`，备份同目录 `restaurant-save.json.bak`。Editor 测试必须改用隔离目录（`RestaurantPersistence.EditorDirectoryKey` / 显式 `directory`），禁止写玩家正式存档。

### 写入

| 时机 | 规则 |
| --- | --- |
| 花钱 / 升级 / 购买 / 雇人 | 本事务结束后的 LateUpdate 写出（同一帧多次扣款只写一次） |
| 店铺晋级（点击 `TryUpgradeRank` 成功） | 晋级成功当次立即 `Flush`，不等 2 秒心跳 |
| 暂停、失焦、退出 | 立即 `Flush` |
| 售卖入账 | 不每帧、不每单必写；沿用 ≤2 秒心跳。暂停/退出仍会带走未写出的入账 |
| 摆放提交中 | `FacilityLayout.Committing` 时不写，避免半成品布局 |

先写同目录 `.tmp` 并刷盘，再替换主文件；替换时留下上一份有效主文件为 `.bak`。未完成的 `.tmp` 不当成已提交进度。

### 持久化（长期）

| 内容 | 字段（现行 v17） |
| --- | --- |
| 金币、成交笔数 | `coins`, `completedSales` |
| 店铺等级、星星、里程碑、循环余数 | `shopRank`, `upgradeStars`, `milestoneMask`, `legacyAccess`, `incomeRemainder`（`goalIndex` / `goalProgress` 由里程碑派生） |
| 设施实例（种类、等级、位姿、餐桌套装与已付） | `layout[]`：`id,kind,purchased,x,z,yaw,level,tableSet,investment` |
| 开局三桌及扩建桌的套装与已付 | `table0..2Set` / `Investment`，`extraTable*`，`fourSeat*`，`squareTable*` |
| 雇佣与强化 | `hiredWorkerCount`（旧档 `workerHired`→1 人）、`workerDeliveries` / `workerClears`；`playerSpeedTier` / `playerCarryTier`；`staffSpeedTier` / `staffCarryTier` |
| 已购地块与附属产线 | 饮品侧翼 `boughtSideWing`；卫生间 `restroomBuilt` / `restroomInvestment`；西侧 `westExpanded`；以及既有烤台/柜台/打包/车道/加桌等购买与投入 |
| 读档后必须能摆回来的布局 | 上表 `layout` + 地块购买结果。**不另存** `hrOpen` / `annexOpen` / 开局指引步骤 |

房间与门洞按 **064**：用当前 `shopRank` + 已购地块重建（HR≥3，汽车厢≥5 或已买打包/车道，侧翼/卫生间看购买，西侧≥10）。禁止第二套“房间已开”标记与等级打架。

餐桌档位按 **059**：Bistro 50 / Diner 80 / Patio 120；旧档已选套且投入 80 视为该款已付清，不补收。缺字段当未买/开局套，默认 0。

星星与里程碑按 **042**：读档不发星、不重复扣晋级费；缺字段默认 0；v13 及更早按已有 `ResolvedMilestones` / `legacyAccess` 迁移一次。

### 不持久化（会话）

飞行中的汉堡/垃圾/现金、顾客身体与订单、桌面垃圾、地上现金堆、开局指引步骤（由 Rank 与主烤台等级推导）、`Sound On/Off`（`PlayerPrefs` `BurgerShop.SoundEnabled`）。

既有零件、离线收据、卫生间脏位等其它已入 schema 的长期字段保持，本次不删也不改公式。

### 可靠性与版本

- schema 变更时增加 `RestaurantSaveData` 版本；`CurrentVersion` 现为 17，本次不升版本（无新字段）。
- 旧档可读：缺字段默认；已买权益与金币保留；永不因读档再收费或回收设施。
- 主文件不可读但 `.bak` 有效 → 用备份恢复并在下次写出时修复主文件，保留有效备份。
- 两份都不可读，或版本高于 `CurrentVersion`：保留原文件，界面说明不可用，**不用新档覆盖**。
- SHA-256 校验发现截断/误改；不是防作弊。v1–7 校验字节序保持。
- 读档恢复钱包/等级/员工不触发售卖或消费事件，不播购买音。

## 边界、存档与兼容

| 情况 | 预期行为 |
| --- | --- |
| 新游戏 | 不创建幽灵文件；状态为新档自动保存 |
| 旧 v7 / v13 / v14 | 金币与已买设施保留；餐桌默认开局套；星星/里程碑按当时版本解析 |
| 清桌后未点 Upgrade 即杀进程 | 盘上仍是 Rank 2，星星已补满；点击晋级后才写新 `shopRank` |
| 暂停时地上有钱、桌上有垃圾 | JSON 不含这些会话物；重进后房间/等级/金币仍在，垃圾与钱堆重新开始 |
| Sound 开关 | 只在 PlayerPrefs，经营 JSON 无此字段 |

## 验收标准

| 编号 | 初始状态 | 操作 | 必须出现的结果 |
| --- | --- | --- | --- |
| AC-01 | 隔离目录新档 | 写入金币、等级、星星、里程碑、设施 layout、059 三档桌投入、058 侧翼/卫生间/西侧、雇佣与强化 | 新 `LocalSaveStore` 读回一致；版本为当前 17 |
| AC-02 | v14 旧档（有金币与星星） | 加载 | 金币与星星保留；不重新扣费 |
| AC-03 | 主文件损坏、备份有效 | 加载 | `RecoveredBackup`；进度为备份快照 |
| AC-04 | 桌上有垃圾、地上有钱堆 | `Flush` 后读 JSON | 无会话垃圾/钱堆字段；重开新对象后桌面干净、地上无堆 |
| AC-05 | Rank 2 清桌后点击 Upgrade | 清完桌再 `TryUpgradeRank(2)` | **不等 2 秒心跳**，盘上 `shopRank==3`。仅清桌不得把盘上等级写成 3 |
| AC-06 | 有未写出的消费或入账 | `OnApplicationPause(true)` | 立即写出 |

不应破坏：061–066、058、059、062、064；Sound PlayerPrefs；隔离测试不得写真实 `persistentDataPath`。

## 变更与交付记录

- v1.0：首次规格。冻结单店本地存档范围、写出时机与 042/058/059/064 兼容。
- 2026-09-16：070 之后清桌不再自动升；晋级 Flush 仍只在 `TryUpgradeRank` 成功时。AC-05 改为点击后才写 `shopRank==3`。
