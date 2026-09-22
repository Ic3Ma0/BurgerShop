# BS-SPEC-068：设置与多槽存档

- 版本：v1.0
- 状态：已实现，本地定向验证通过；未推 GitHub
- 对应基线：本地 `main` + 未提交 057–067；存档 schema `RestaurantSaveData.CurrentVersion` = **17**（本 spec 不升版本）
- 优先级：要能开新店，同时保住正在经营的店，并在本地槽位之间切换
- 前置：BS-SPEC-067（单槽 schema、写出时机、校验/备份）；060 商城 `timeScale=0` 停 BGM；062 开局指引由 Rank/烤台推导；064 房间由等级+地块重建
- 用户已确认：右侧齿轮、超市按钮旁边；新游戏 / 保存现有进度 / 切换已存进度；最多 8 槽；067 单文件迁到槽 1；无云、无账号
- 产品建议或关键未定项：无。不改价格/产能、不做导出导入、不在本面板加 Sound 开关、不改 Android 商店页

## 目标与范围

067 仍是**每个槽位**的经营 JSON（v17）和写出时机。本 spec 只加本地多槽表面：齿轮入口、清单、新游戏、切换、删除非当前槽。

本次交付：

- HUD 右侧、超市按钮旁的齿轮
- 设置/存档面板（暂停经营，关闭时恢复原暂停状态）
- 最多 8 个本地槽；清单 + 每槽独立文件
- 067 的 `restaurant-save.json` 在无清单时迁为槽 1，不丢正在经营的店
- 新游戏先 Flush 当前槽，再进入 Rank 1 / 0 金币的空档；旧槽仍可读
- 切换先 Flush 再按 067 的 Restore 读入目标槽（不重复扣费、不播购买音）

本次不做：云存档、账号、导出/导入、自定义店名（超出 `Shop N` / 用等级自动标注）、Sound 开关、全局经济、Android 商店列出、第三方图标包、新 PNG/音频资源

## 玩家流程与规则

| 项目 | 明确规则 |
| --- | --- |
| 触发条件 | 点右侧齿轮打开面板；点 Close / 再次点齿轮 / Esc 关闭 |
| 槽位上限 | 8。满员时不能再「New game」 |
| 状态变化 | 新游戏、切换、打开面板都会先 Flush 当前槽；删除只作用于非当前槽 |
| 与已有功能的关系 | 067 的心跳/消费 LateUpdate/晋级 Flush/暂停 Flush 不变。`SessionGoalTracker` 晋级仍立即 Flush。经营 JSON 不写槽号 |

### 文件

路径均在 `Application.persistentDataPath`（Editor 测试必须改用隔离目录，沿用 `RestaurantPersistence.EditorDirectoryKey` / 显式 `directory`，禁止写玩家正式存档）。

| 文件 | 用途 |
| --- | --- |
| `restaurant-slots.json` | 清单：slot id、displayName、lastPlayedUtcTicks、shopRank、coins（摘要）以及该槽文件名。不写完整经营字段 |
| 槽 1 | 沿用 067：`restaurant-save.json`（迁移时**不改名**，避免丢掉正在经营的店） |
| 槽 2–8 | `restaurant-save-{id}.json` |
| `.bak` | 各槽主文件由现有 `LocalSaveStore`（tmp+bak+checksum）维护 |

无清单且存在 `restaurant-save.json`：视为槽 1，写出清单，金币/等级保留。无清单且无存档：内存中槽 1，不预建幽灵经营文件（仍由 067 首次 Flush/自动保存创建）。

活跃槽 = Persistence 当前 Flush/Load 的那一个文件。槽号不写入 `RestaurantSaveData`。

### 新游戏

1. Flush 当前槽（打开面板时已 Flush 过则再写一次无妨）
2. 分配新空槽（或最小未用 id 1–8）
3. Persistence 改指该槽文件
4. 运行时回到 Rank 1、0 金币的新档（烤台 1、无雇佣）。旧槽文件保持可 Load
5. 不立即写新槽幽灵文件；之后按 067 自动保存。清单立即登记该槽（Rank 1 / 0 金币），以便面板显示当前档
6. 062 开局指引不写盘，由 Rank 与主烤台推导：新档会再出现；读入 Lv.2+ 不会

### 切换

Flush 当前 → Load 目标槽 → 按 067 同一条 Restore 恢复钱包/等级/员工/设施/地块。064 房间仍由 rank + 已购地块重建。读档不扣费、不播购买音。

### 保存现有

打开面板时 Flush。面板提供 `Save now`，立即 Flush 当前槽。切换/新游戏/暂停前的 Flush 067 已有，本面板仍要在打开时和任何切换/新游戏前 Flush。

### 删除

可删**非当前**槽，需确认文案。不能删当前槽；不能删仅剩的那一个槽（空白店请用 New game）。删除时去掉该槽 json/bak/tmp，并从清单移除。

## 界面与资源

- 齿轮：HUD **右侧集群**，紧挨现有超市/购物车按钮（同 Y、同尺寸 96、圆角奶油底）。超市已在右侧；齿轮在它旁边（购物车左侧，避免压到金币条）。无大段教程字。
- 图标：程序化齿轮（`HudChrome` 几何/sprite），不引进第三方图标，不新增 PNG/音频。
- 面板：对齐 `FacilityShopHud` / 暂停。超市已把 `timeScale` 置 0 时本面板同样置 0；060 BGM 在 `timeScale=0` 时已暂停。关闭恢复进入前的 `timeScale`。
- 语言：与超市一致，英文 HUD。
- 标题：`Saves`
- 行：`Shop N` + `Lv.n` + 金币 + 当前标记 `Now`
- 按钮：`New game` / `Load` / `Save now` / `Close`；删除为 `Delete`，确认文案英文短句
- 资源：沿用现有 uGUI / TextMesh / `HudChrome` 字体

## 边界、存档与兼容

| 情况 | 预期行为 |
| --- | --- |
| 067 旧单文件、无清单 | 迁为槽 1，进度保留 |
| 槽位已满 8 | New game 不可用；不覆盖任一现有槽 |
| 目标槽文件缺失 | 当作该槽新档（Rank 1 / 0 金币），不改写其它槽 |
| 清单损坏 | 按目录下 `restaurant-save.json` 与 `restaurant-save-{id}.json` 重建清单；经营文件不删 |
| 读档 NewerVersion / Unreadable | 067 规则：不覆盖；该槽保持不可写 |
| Sound / 开局指引步骤 | 不写入经营 JSON |

允许开发自行决定的可逆细节：清单是否带 `fileName` 字段；齿轮在购物车左或右的数像素间隙（必须同侧、同高、不相交金币条）。

## 验收标准

当前优先高效实现功能：按改动做必要的本地 Unity 验证。Android 打包与手机验收留待后期用户统一安排。

| 编号 | 初始状态 | 操作 | 必须出现的结果 |
| --- | --- | --- | --- |
| AC-01 | 隔离目录仅有 067 的 `restaurant-save.json`，无清单 | Persistence.Configure | 清单槽 1 保留原金币与 `shopRank`；原文件仍在；未写 `persistentDataPath` |
| AC-02 | 槽 1 已有金币与等级 | New game | 先 Flush 旧槽；当前为 Rank 1 / 金币 0；旧槽文件 Load 仍是旧金币 |
| AC-03 | 槽 A、B 各有不同金币/等级 | 切 A→B→A | 回到 A 的金币与等级 |
| AC-04 | SampleScene/HUD 已建超市按钮 | 查齿轮 | 齿轮与购物车同级或紧邻，同高、同尺寸，位于右侧集群 |
| AC-05 | EditMode | 任何写出 | 路径不包含 `Application.persistentDataPath` |
| AC-06 | 两个槽，当前为 B | 删 A；再删 B；只剩一槽时再删 | 非当前可删；当前不可删；最后一槽不可删 |

不应破坏：067 写出时机与 v17 schema；超市按钮布局与暂停；062/064；隔离测试不得写真实存档。

## 变更与交付记录

- v1.0：首次规格。067 仍为单槽经营 schema；本 spec 只加本地最多 8 槽与齿轮入口。
- 2026-09-16：按本 spec 落地。不升 `RestaurantSaveData`（仍 v17），不重写 `LocalSaveStore`。槽 1 沿用 `restaurant-save.json`；槽 2–8 为 `restaurant-save-{id}.json`；清单 `restaurant-slots.json`。齿轮在超市购物车左侧 8px，同 96 尺寸、同高、右侧集群。`Spec068SaveSlotTests` 覆盖 AC-01–06。Unity 6000.3.23f1，隔离工程 `/tmp/bs059-editmode`，隔离临时目录，未写真实经营存档。`Spec068SaveSlotTests` 6/6 通过。未推 GitHub，未做 Android。Play 下切槽/新游戏会重建 Goal01，使 064 房间与设施与读档一致；EditMode 走同一条 Persistence Restore。
- 2026-09-17：078 起 `SwitchToSlot` / `StartNewGame` 不再对旧世界 ApplySave。`Spec068SaveSlotTests` 切档与新游戏后销毁厨房再 `Configure`，与 Play 重建对齐。
