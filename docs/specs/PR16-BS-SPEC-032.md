# BS-SPEC-032：玩家不可穿过实体家具

- 版本：v1.0
- 状态：可开发 / 开发中
- 对应基线：`feat/shop-rank` 已含 [030](BS-SPEC-030.md) 套装与 [031](BS-SPEC-031.md) 四人桌/方桌。
- 优先级与原因：玩家角色会穿过桌子、椅子、柜台、机器和墙；碰撞应贴合物体占位。
- 前置依赖：013 桌椅、019 加桌、031 新桌型、Goal01 运行时组装。不依赖 cola / PR #17。
- 用户已确认：角色不能穿实体店内物件。下列范围是 BurgerShop 规则，不是 Pizza Ready 测量。
- 产品建议或关键未定项：无。不新增文案、不改价格。

## 目标与范围

玩家目前：`CharacterController` 在玩家身上，但桌椅、柜台、烤台等 primitive 的碰撞体被关掉并销毁，所以能走进桌心和柜台内部。墙与地板仍有碰撞。

本次之后：实心家具/工位/墙按视觉占位挡住玩家。绿色购买圈、取餐/收银/升级圈仍可走进。椅朝向规则不变。

本次交付：

- 开局与加购的两人桌、031 四人桌、031 方桌：桌面、桌腿、椅座/靠背/椅腿保留启用、非 Trigger 碰撞体。
- 柜台（开局与加柜）、烤台机身、装箱台、打包柜、得来速窗口台、垃圾桶、HR 桌椅、强化房器械：同样挡住玩家。
- 大厅墙、房间墙、地板保持碰撞。门洞仍可走。
- 玩家仍用 `CharacterController`（半径 **0.4**、高度 **2**）。`link.xml` 继续保留 Box/Capsule/Sphere，并保留 `CharacterController`，避免 Android IL2CPP 剥掉。
- EditMode 断言：上述物件有阻挡碰撞；椅 `forward` 对桌心 Dot > 0.9。

本次不做：

- 顾客/员工刚体互撞、汽车挡路、新 Ragdoll。
- 改投入圈半径或 031 价格。
- 存档字段、离线收益、钻石/广告/可乐/商店等级。

## 玩家流程与规则

入口：`SampleScene` Play 后 `Goal01Bootstrap` 组装。玩家用摇杆/键盘移动。

| 项目 | 明确规则 |
| --- | --- |
| 谁被挡住 | 仅玩家控制角色。员工/顾客路径不在本刀改导航。 |
| 挡住 | 碰撞体启用、`isTrigger = false`，形状与该 primitive 视觉体积一致（桌面盒子、椅座盒子等），不是另做超大隐形墙。 |
| 不挡住 | 设施钱堆圈、紫/绿/白地面圈、圈上金币堆与设施图标、食物/纸箱/纸团/落地现金、屏幕 HUD。玩家必须仍能走进圈内投入或取餐。 |
| 门洞 | 东墙 HR 门、南墙 Boost 门保持开口；门框可挡。 |
| 朝向 | 换套或加碰撞不得改椅 `LookRotation`。靠背仍在外侧。 |
| 与 031 | 未买的四人/方桌格只有绿圈（可走）；买下后桌椅占位不可穿。 |

## 界面与资源

- 无新 HUD、无新英文句。圈颜色用途不变。
- 资源：现有 primitive + `RuntimeMaterials`。碰撞来自 `CreatePrimitive` 自带盒/胶囊/球，禁止只靠 `Shader.Find`。

## 边界、存档与兼容

不新增持久化状态。旧档位置若卡进家具内，以 Unity `CharacterController` 推出为准，不写传送。

允许开发自定：Skin Width、门框是否保留碰撞。不得关掉桌面/椅座/柜台/烤台机身/墙的阻挡。

## 验收标准

| 编号 | 初始状态 | 操作 | 必须出现的结果 |
| --- | --- | --- | --- |
| AC-01 | 开局三人桌已生成 | 从桌西侧沿水平方向对玩家胶囊做 CapsuleCast 指向桌心 | 命中桌面或椅的非 Trigger 碰撞；椅仍朝桌心 Dot > 0.9。 |
| AC-02 | 已买 031 四人桌与方桌 | 同上对两张新桌 | 4 椅与方桌高靠背均有阻挡碰撞；朝向断言仍成立。 |
| AC-03 | 开局烤台、柜台、墙 | 检查 Body / OrderCounter / Wall+Z | 碰撞启用且非 Trigger。 |
| AC-04 | 未买加桌 | 玩家胶囊穿过 `TABLE` 钱堆圈位置 | 不因绿圈/金币/图标被挡住。 |
| AC-05 | 玩家物体 | 检查组件 | 有 `CharacterController`，半径 0.4、高度 2；鼻部装饰无碰撞。 |

手机相关：云端成功不能代替 Editor。本云主机无 Unity `6000.3.23f1`，EditMode 须在本机跑过。未做 vivo。Play 时应无法走进桌面或柜台内部。

不应破坏的既有行为：031 购买与座位数；030 选套只换色；投入圈仍可站入；v1–v9 存档。

## 变更与交付记录

- v1.0：首次规格。

### 开发记录 · 2026-09-12

- 实现：`SolidOccupancy` 保留桌椅（含 031 四人/方桌）、开局与加柜 `OrderCounter`、烤台机身、装箱/打包台、得来速窗口、垃圾桶、HR 桌椅/门框/架、强化房器械、墙与地板的非 Trigger 碰撞。钱堆圈、紫/绿/白圈、圈上金币与设施图标、取餐圈仍拆除碰撞。`Goal01Bootstrap` 开局柜台走同一套占用。`link.xml` 增加 `CharacterController`。玩家仍用现有 `CharacterController`（0.4 / 2）。
- 测试：`Assets/Tests/EditMode/PlayerClippingTests.cs`（桌心 OverlapCapsule、西侧 CapsuleCast、椅朝向 Dot > 0.9、绿圈/金币不挡、HR/Boost 门洞 CheckBox 仍空）。
- **Unity EditMode 未跑**：本云主机无 Unity `6000.3.23f1`。云端成功 ≠ Editor / 真机。未做 Play Mode，未做 vivo。交付前须在本机跑完全部 EditMode，并在 SampleScene Play 确认走不进桌面与柜台内部、仍能走进投入圈与 HR/Boost 门洞。
