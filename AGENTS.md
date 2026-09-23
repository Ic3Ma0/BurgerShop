# BurgerShop — Agent 协作规范

这是一个以 Android 为首要平台、使用 Unity 6.3 开发的汉堡餐厅游戏。已验收的 MVP 版本为 **0.1.2**。不要把占位几何体、当前数值或“将汉堡交给排队顾客”的实现，当作 Pizza Ready 的标准设计。

## 项目事实与权威入口

- 正式仓库仅限本目录。同级的 `BurgerShop-Goal04` … `Goal09` 是历史 Git 工作树，不要修改。
- GitHub：`https://github.com/Ic3Ma0/BurgerShop`。远端 `main` 已包含 Goal 00–09（通过 PR #14 合并）。
- Unity：`6000.3.23f1`（LTS）。URP 17.3.0，Input System 1.20.0。
- Play 入口：`Assets/Scenes/SampleScene.unity`。餐厅由 `Assets/_Project/Scripts/Core/Goal01Bootstrap.cs` 在运行时组装，并非由预先制作的预制体搭建。
- 保留 `_Recovery/` 和旧 stash。不要对历史备份执行 `stash apply`。

## 产品开发流程

用户方向 → 简明 spec（模板为 `docs/specs/SPEC_TEMPLATE.md`，编号为 `BS-SPEC-XXX`）→ 实现与验证。spec 定义玩家可见的规则；类、测试和 Git 操作由开发负责。用户需求已经明确行为时，写好 spec 后继续实现，不将 spec 当作第二道审批。只有尚未明确、且会实质改变范围或行为的产品决定才需要询问。

不要仅因“更像 Pizza Ready”就新增功能。当前长期产品方向是对标复现 Pizza Ready，但现有代码仍是原型；先通过 spec 明确本次行为。

新增玩法、扩建、成长或经济设计时，遵循 `docs/design/BURGER_RESTAURANT_DIRECTION.md` 的“新增玩法的分析要求”，并填写 `docs/specs/SPEC_TEMPLATE.md` 的相关部分。提出实现方案前，说明玩家当前需求、现实餐厅逻辑、独特决策、解锁顺序及经济影响。内容更多本身不构成设计理由；历史数值快照必须与当前代码核对。

基线快照：`docs/PRODUCT_AGENT_HANDOFF.md`。存档规则：`docs/goal-08-save.md`。`docs/goal-09-device-vivo.md` 仅为历史真机证据，不是当前验收要求。当前任务状态：`docs/CURRENT_TASK.md`。

## 接手与交接（跨工具）

- 当前任务状态唯一入口：`docs/CURRENT_TASK.md`。任何工具接手时先读它，再读相关 spec 与 `docs/architecture/CODE_BOUNDARIES.md`；旧聊天记录不是当前代码事实。
- 接手时先核对分支、HEAD、已暂存、未暂存和未跟踪文件；保护现有修改，不清理、重置或覆盖，也不为匹配某份文档而回退本地代码。
- 每个可验证检查点（功能可跑、修复确认、发现阻塞）更新 `CURRENT_TASK.md`；交付后把执行历史归档进对应 spec 的"技术执行与交接"区。交接不依赖上一个工具的最后一次对话。
- 修复或实现当前需求时，不顺手重构无关系统；发现别的问题记入 `CURRENT_TASK.md` 的待处理清单，本次范围照旧。

## 开发前明确代码边界

目标：改一个规则，不必理解半个项目。修改运行时代码前，检查受影响的入口，在当前 spec 中补充简明的 **技术边界与方案审查**（见 spec 模板）。说明复用的已有模块、每个变化状态的唯一所有者、依赖方向、失败与兼容行为，以及保护边界的自动检查。小修写清几行即可；这是工程审查，不新增用户审批步骤。

- 不要默认往大型 MonoBehaviour 里继续堆规则。本次修改确实跨越多种职责时，将规则与场景查找、显示、持久化分开。不要仅因文件大就拆分，也不要为假设中的复用引入框架。
- 涉及已列出的边界时，读取[代码边界说明](docs/architecture/CODE_BOUNDARIES.md)。只补充本次实际建立的边界；范围外的历史耦合不构成全项目重写的理由。
- 新的运行时代码改动需执行 `bash scripts/check-boundaries.sh --spec docs/specs/BS-SPEC-XXX-name.md`，并按影响运行行为测试。脚本检查已抽取规则的依赖及审查字段；Unity 程序集编译约束依赖隔离。两者都不能单独证明设计或玩法正确。
- 运行时 C# 有改动时，CI 要求本次变更包含带有技术审查的 spec。历史 spec 保留为历史证据；更新当前 spec 或新增范围明确的后续规格，不必重写所有旧规格。Android 构建脚本也会运行依赖检查，但 Android 构建执行仍延后。

## 工程约束

- 新工位、区域、员工、HUD 和存档字段沿用 `Assets/_Project/Scripts/` 的现有模式，测试放在 `Assets/Tests/EditMode/`。
- 除非 spec 另有规定，只持久化长期经营状态。已有字段以当前 `RestaurantSaveData` 和相关 spec 为准；离线收益与多槽存档已经存在。新增字段必须有明确的所有者和兼容规则，不要复制另一个模块已有的状态。
- 存档结构变化时，提升 `RestaurantSaveData` 版本；旧档进度必须保留。
- 玩家与员工共享烤台库存、收银冷却和钱包。不能让额外搬运者绕过收银频率限制。
- Android IL2CPP 会裁剪未引用的碰撞体和着色器。保留 `_Project/link.xml` 与 `RuntimeMaterials`（Resources 中的 Lit/Unlit），不能只依赖 `Shader.Find`。
- 项目路径包含中文。Android 构建使用 `scripts/build-android.sh`（在 ASCII 路径的临时目录中构建）；编辑器菜单构建仅适用于英文路径。
- APK 构建成功不等于真机验收通过。Android 构建和真机验证延后，直到用户明确启动后续统一验收阶段。

## 验证与当前开发优先级

用户最新要求：优先实现新功能并完成项目。遵循“需求 → 简明可执行 spec → 直接修改代码 → 与风险相称的本地 Unity 验证 → 及时交付”。

- 产品 spec 聚焦行为、范围、关键参数、兼容性及可复现的功能验收。不要把每个功能都变成复杂的验证项目。
- 逻辑优先使用定向自动测试。需要视觉、音频或交互证据时，通过 Computer Use 操作 Unity Play 中的 SampleScene。保护真实存档；隔离测试不能触碰真实 `persistentDataPath`。
- 小改动不例行要求长时间稳定性测试、重复录像或全量回归。只有相关风险、失败或新证据才扩大验证；没有理由时，不重复已经通过的检查。
- Android 打包、安装、手机操作及平台验收由用户安排在后续统一阶段，并非永久取消。不要在每个功能或批次之后自行启动，也不要将其视为当前交付的阻塞项。
- 当前功能及本地验收满足后及时交付，继续用户要求的功能。诚实记录实际结果和范围内问题；保留历史真机证据，不把后续平台验证工作当作当前缺陷。
- 开发与验证在本地进行。保留 Android 产品目标及平台兼容代码。

## 按任务读取的参考规范

- 当前开发与验证策略以上文为准。`.cursor/rules/burgershop-core.mdc` 和 `local-unity-verification.mdc` 指向该策略，不另设交付门槛。
- 空间、家具、HUD 或交互改动：参考 `.cursor/rules/burgershop-editmode-gate.mdc` 中相关回归示例。
- 经济、成长、解锁或商业化改动：遵循 `.cursor/rules/burgershop-pacing-invariants.mdc`；需要设计依据时读取 `docs/GAME_CORE.md`。历史数值快照必须与当前代码及已确认的 spec 核对。
- 仅修改文档时，检查差异、引用和格式即可，不要求启动 Unity 或运行玩法测试。

## 语言

用户沟通、spec 和游戏内玩家可见文案使用简体中文（BS-SPEC-086）。代码标识、存档键与内部规则字符串不翻译。
