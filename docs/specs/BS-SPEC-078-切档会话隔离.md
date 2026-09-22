# BS-SPEC-078：切档会话隔离（审查 PR2 / F01）

- 版本：v1.0
- 状态：**已完成并纳入本地提交**（未推 GitHub）
- 对应基线：077 存档契约 + BurgerShop_Review F01
- 优先级：切档不得把店 A 的设施/金币写进店 B
- 前置：BS-SPEC-068 多槽、BS-SPEC-077 索引与 256KiB 读写契约
- 用户已确认：只做 PR2 会话隔离；不改价格、等级、星星门；Android 延后
- 产品建议或关键未定项：无。F07 现金对象、双时钟、写出退避、热路径、架构拆分不在本刀

## 目标与范围

玩家目前可以在设置面板切槽或开新店。切档若先把 B 的存档 `ApplySave` 到仍活着的店 A，再下一帧重建 Goal01，则：

- `ShopExpansion.Restore` 只加不减（A 的第二烤台不会因为 B 没买而消失）
- `FacilityLayout.Restore` 合并、不删多余设施；`Restore(null)` 直接返回
- 半恢复时 `goals.Restore` → `ApplyUnlocks` / `EvaluateStarGateTasks` 可能 `Flush`，把混合世界写入 B
- `SettleOffline` 若在脏世界上 Capture，也会写入 B

本次之后：切档只把 A 写回 A 的文件，激活 B 的索引，再在**新的**空店上读 B。Flush 失败则仍留在 A，不拆店。

本次交付：

- 会话阶段 `Running | Quiescing | Hydrating | LoadFailed`
- `SwitchToSlot` / `StartNewGame`：停模拟写出 → Flush A（仅 A 的文件）→ 改活动槽 → **不**对旧世界 ApplySave / SettleOffline → 请求重建
- Play：销毁旧 Goal01 用 `Destroy` 并等到对象消失再 `InstallShop`；EditMode 测试仍可用 `DestroyImmediate`
- 新店 `Configure` 后再 ApplySave / SettleOffline；此后才允许心跳 Flush
- 读档不得走购买音效；Hydrating 期间 `EvaluateStarGateTasks` 即使发里程碑也不得 Flush

本次不做：

- F07+（现金对象、双时钟、写出退避、热路径）
- 架构拆分（PR4）、Android 发布（PR5）
- 改 `FacilityLayout.Restore` 为替换语义（全量重建后不再往残留厨房上合并）
- 重调价格 / 等级 / 星星门

## 玩家流程与规则

| 项目 | 明确规则 |
| --- | --- |
| 触发条件 | 设置面板 Load 其它槽，或 New game（077：先 Flush 成功才改活动槽） |
| 费用 / 奖励 | 不涉及 |
| 状态变化 | 见阶段机。切档成功后经营世界换成目标槽；失败则仍是当前店 |
| 与已有功能的关系 | 067 心跳 / 消费 / 晋级 Flush 仅在 `Running`。068 槽文件与清单不变。064 房间仍由 rank + 已购地块在新店上重建 |

### 阶段机

| 阶段 | 含义 | 允许 Flush | 允许 SettleOffline |
| --- | --- | --- | --- |
| `Running` | 当前店与活动槽一致，可经营 | 是（心跳、购买、里程碑、暂停） | 是 |
| `Quiescing` | 正在停笔、把当前店写入**当前槽文件** | 仅这次切档/新游戏的 Flush | 否 |
| `Hydrating` | 已指向目标槽，旧店待拆或新店正在 Configure | 否 | 仅新店 Configure 结束时一次 |
| `LoadFailed` | 目标槽不可读 / 更高版本 / 不可用 | 否 | 否 |

切档顺序：

1. 仅当 `Running` 才接受切档或新游戏。
2. 进入 `Quiescing`。停 LateUpdate / 里程碑 / 购买 Flush。
3. 若 `store.CanWrite`：Flush **A → A 的文件**。失败则回到 `Running`，活动槽仍是 A，返回 false，**不销毁 A**。
4. 清单激活 B（或创建新槽）。失败则回到 `Running`、仍绑定 A，返回 false。
5. 改绑定到 B 的文件。进入 `Hydrating`。不对旧对象 ApplySave，不 SettleOffline，不把 A 的运行时摘要记到 B。
6. 请求重建店铺。Play 等待旧根节点 `Destroy` 完成后再 `InstallShop`。
7. 新对象 `Configure`：阶段为 `Hydrating`，把 B Load 到空店，然后 SettleOffline；成功则 `Running`，读档失败则 `LoadFailed` 并给出明确 `Status`（沿用 067/077 文案，不得显示成正常新游戏自动保存）。

新游戏同样：Flush A → 创建空槽 → 重建为 Rank 1 / 0 金币的新店，不在旧店上 `ApplyNewGame`。

### EditMode

Persistence 的 `SwitchToSlot` / `StartNewGame` **不再原地 ApplySave**。没有 `PlayerMotor` 的 EditMode 夹具不会排队 Goal01 重建；测试应销毁厨房并重新 `BuildKitchen` + `Configure`（或同一套 reload 助手），使金币、等级、已购设施与目标文件一致。

## 边界、存档与兼容

| 情况 | 预期行为 |
| --- | --- |
| Flush A 失败 | 不切槽、不拆店、返回 false，阶段回到 Running |
| 激活/创建槽失败 | 同上，仍绑定 A |
| 切档后、重建前心跳/暂停/退出 | 不得把 A 的世界写入已激活的 B 文件 |
| 目标槽无文件 | 新店按新游戏恢复（Rank 1 / 0 金币），不改写其它槽 |
| 目标槽 NewerVersion / Unreadable | `LoadFailed` + 067 状态文案；该槽不可写 |
| 旧存档 | 不升 `RestaurantSaveData` 版本 |

不新增持久化字段。`FacilityLayout.Restore(null)` 仍不清理；本刀依赖全量重建，不在残留对象上切档。

允许开发自行决定：阶段枚举所在文件；重建协程挂在 `Goal01Bootstrap` 或 `ShopSlotReload`。

## 验收标准

| 编号 | 初始状态 | 操作 | 必须出现的结果 |
| --- | --- | --- | --- |
| AC-01 | 槽 A 有金币/等级，槽 B 不同 | `SwitchToSlot(B)`（不重建） | 不把 B 合并进 A；活动槽已是 B；阶段 `Hydrating`；`Flush` 为 false；B 文件内容不变 |
| AC-02 | 同上，随后销毁厨房并 `Configure` | 读 B | 金币与等级等于 B 文件 |
| AC-03 | A 已买第二烤台，B 未买 | 切到 B 并重建后 Flush B | `HasExtraGrill` 为 false；B 文件 `boughtExtraGrill` 仍为 false |
| AC-04 | `Running` 且已弄脏 A，写出被挡住 | `SwitchToSlot(B)` | 返回 false；仍在 A；阶段 `Running`；A 的对象还在 |
| AC-05 | 槽 A 经营中 | New game 成功并重建 | 当前 Rank 1 / 金币 0；A 文件仍是旧进度 |
| AC-06 | `Hydrating` | 里程碑/晋级路径调用 Flush | 不写出 |

不应破坏：077 契约与失败不切槽；068 齿轮与删除规则；隔离测试不得写 `persistentDataPath`。

## 变更与交付记录

- v1.0：F01 会话隔离。`RestaurantPersistence` 阶段机；切档/新游戏只 Flush 当前槽并改索引；ApplySave / SettleOffline 移到重建后；Play 销毁等待；`FacilityLayout.Restore` 合并语义未改。测试 `Spec078SlotSwitchIsolationTests` **3/3**、`Spec068SaveSlotTests` **6/6**、`Spec077SaveContractTests` **9/9**（合计 **18/18**）。Unity 6000.3.23f1，隔离工程 `/tmp/bs059-editmode`。日志 `Logs/spec078/`。未推 GitHub，未做 Android。

## 技术边界与方案审查

- 归属与复用：切档顺序归 RestaurantPersistence，复用 Goal01Bootstrap 重建入口，不新建会话框架。
- 状态归属：Running/Quiescing/Hydrating/LoadFailed 由存档协调器唯一管理；钱包仍归 RestaurantWallet。
- 依赖方向：UI 请求切档，协调器完成写入与激活后请求 Bootstrap 重建；新场景 Configure 负责恢复。
- 兼容与失败：沿用 077 操作结果；保存或激活失败保留当前店；旧世界不可向新槽写入；不改存档版本。
- 自动防线：078 跨槽金币/设施隔离、写失败及 Hydrating 禁止写出断言，068 多槽回归。

## 2026-09-22 顺序交付

在 077 之后单独提交阶段机、重建流程和对应测试；离线计算仍沿用拆分前实现，留给 079。原 078/068/077 共 18/18、后续整合 60/60 的通过记录保留。当前源码与已测隔离工程一致；本次 Unity 许可证连接超时，未获得新测试结果。未做 Android 或真实存档操作。
