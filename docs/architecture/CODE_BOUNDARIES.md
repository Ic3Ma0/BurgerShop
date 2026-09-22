# 当前已建立的代码边界

宗旨：改一个规则，不必理解半个项目。本文件只记录已实现的边界；不是全项目重新分层计划。

## 离线收益（BS-SPEC-079）

| 责任 | 位置 | 可以做 | 不可以做 |
| --- | --- | --- | --- |
| 收益、封顶、收据结果 | `Assets/_Project/Scripts/Economy/Rules/` | 用显式数值输入返回不可变结算方案 | 查场景、用 UI/存档 DTO、写钱包/磁盘、读系统时钟 |
| 可用升级报价 | `Economy/OfflineUpgradeCostSource.cs` | 读取现有升级系统的报价与解锁条件 | 重写设施定价/门槛，发钱或写档 |
| 结算提交与会话 | `Persistence/RestaurantPersistence.cs` | 提供时间/经营快照；保存方案；成功后发布内存状态 | 再定义离线公式或另开一套收据状态 |
| 结算显示 | `UI/OfflineSettleHud.cs` | 读已到账收据，关闭收据 | 再次领取/计算收益 |

数据流：现有升级系统 → 报价适配器 → 纯规则输入；纯规则返回方案 → 存档一次保存金币/时间/收据 → 内存钱包和 UI 更新。

钱包余额的唯一运行时所有者仍是 `RestaurantWallet`。已消费时间戳及收据由 `RestaurantPersistence` 管理，`RestaurantSaveData` 是持久化快照。`OfflineSettlementPlan` 是暂存结果，不能作为新的长期状态或在保存失败后先显示到账。

### 后续修改放哪里

- 改离线时间上限/公式/收据规则：先写产品 spec，改 Rules 与对应纯规则测试。
- 新设施需要参与离线参考价：复用该设施的报价，在适配器纳入；不让纯规则认识新 MonoBehaviour。
- 改写盘、失败重试、切档：改 Persistence；规则不关心文件或槽位。
- 改结算面板：改 HUD；不触碰公式或再次给钱。

### 可执行检查

`BurgerShop.Economy.Rules.asmdef` 无项目/插件引用且 `noEngineReferences=true`，Unity 编译阻止规则引用场景层。`scripts/check-boundaries.sh` 补充检查 IO、环境时钟与程序集约束；自测试会主动注入违规依赖验证检查失败。新边界需要明确契约后才加入脚本，不能把当前原型所有历史依赖一次判死。

- 本地：`bash scripts/check-boundaries.sh --spec docs/specs/BS-SPEC-079-离线结算职责边界.md`
- CI：push/PR 跑边界、自测试及当前变更的 spec 技术审查字段检查。字段检查不能证明内容正确或真的发生在编码前。
- 构建：现有 Android 脚本先跑依赖检查；并不要求本轮构建 APK。
- 行为：079 纯规则/场景报价测试，051 离线到账与失败重试，077/078 存档及切档隔离回归。

CI 文件提交并推送后才会在 GitHub 执行；阻止合并还需要仓库 required checks 配置。本次不改远端保护规则或安装个人 Git hook。

## 经济报价与投资建议（BS-SPEC-083-经济一致性修复与节奏标定）

- `FacilityPurchaseQuote` 只记录目录基础价、购买前持有数对应的完整价、既有投入和剩余应付；`FacilityLayout` 提供同一报价并在确认时校验变化。各建设地垫仍拥有既有投入，成交后消费对应抵扣。`BusinessOpeningQuote` 只汇总缺失设备与可选土地，不创造第二套购买状态。
- `InvestmentObservation` 读取实际生产、携带、柜台队列及桌位；`Economy/Rules/InvestmentPriority` 接收显式状态与经营时间，保存本会话的持续观察结果，不读取 Unity、钱包、存档或系统时钟。`InvestmentGuide` 用真实报价排序，HUD 负责展示及定位。
- `DriveThruLane` 拥有窗口共享冷却；`FacilityInstance` 和已有 `GrowthUpgrades` 记录/恢复其等级。详情参数来自实际服务间隔函数，不另算宣传数值。读档对齐等级不扣费、不补星。
- `Spec083EconomyTests` 验证窗口实际交付、观察适配与尾款报价；场景测试验证缺件、抵扣、交易及重载。经营采样单独记录收入、支出和地面现金，不把测试加钱或理论产能当作自然收入。

## 人物外观（BS-SPEC-082）

- `Core/CharacterAppearance` 管理玩家/员工/顾客使用的外观色板及顾客搭配目录。顾客造型用票号确定，不消耗经营随机流。
- `Core/CharacterVisualFactory` 生成外观；`CharacterVisualResources` 管理本角色材质与合并网格的生命周期；`HumanoidVisual` 只驱动视觉四肢。
- 玩家、员工、顾客创建入口调用工厂，位置、库存、服务、排队及持久化仍由各自原模块拥有。不得从外观层修改钱包、存档、碰撞尺寸或寻路状态。
- 改发型、配色、服饰在外观模块完成；角色行为变化仍需修改原业务模块及行为测试。082 测试保护随机流、无额外碰撞、资源释放、特殊顾客及实际创建入口，既有顾客/员工测试保护经营行为。

## 小店轮廓、顾客行走与中文提示（BS-SPEC-083）

- `MainHallExpansion` 拥有当前地块、付款及布局版本；`ShopLayout` 提供该版本的默认点位。存档只记录布局标识，HUD 和外观不能决定已购土地。关着的附属房间不得重新显示未购主厅边缘的墙体。
- `CustomerQueue` 拥有排队顺序与服务槽位；`CustomerWalkPath` 生成稳定的个体路线，在物理可通行范围内平滑转弯，不修改订单、付款、排队容量或经营随机流。`CustomerAgent` 用实际位移驱动朝向。
- `OpeningHudCopy` 只把已有任务、解锁与报价显示为简短中文，不创建任务或重新计算价格。`HudChrome.ChineseFont()` 加载项目随附字体，原英文 UI 仍使用原字体。
- `Spec083PlaytestFixTests` 保护几何、路线/朝向/队列、字体及显示契约；`Spec083SceneTests` 在隔离存档的 Play 模式验证实际墙体、首单到扩建、v18/v19 切档与门口进出。

## 场景点击升级（BS-SPEC-084）

- `WorldDetailsInput` 是设施与人物唯一的场景点击入口：共用短按/拖动/UI/多指过滤，按场景遮挡选择目标；只在按下/抬起读取人物渲染边界，不给员工增加碰撞。短按捕获人物身份，避免员工走动导致抬手时选不中。
- `FacilityDetailsHud` 管设施卡，`PlayerUpgradeHud` / `StaffUpgradeHud` 管各自属性展示；`StatUpgradePopup` 拥有属性卡的暂停、遮罩和关闭恢复，不参与定价。`BoostUpgradeZone` / `StaffUpgradeBoard` 仍唯一拥有等级及购买校验，员工属性仍全体共享。
- 投资提示只引用动态人物目标并请求打开同一卡片；不复制等级或现金状态。`Spec084CharacterUpgradeTests` 检查场景鼠标/触屏、互斥、暂停恢复、共享属性、存档及画面适配。

## 当前柜台进店路线（BS-SPEC-085）

- CustomerQueue 从固定门口通道读取当前 QueuePositions 队尾，复用 FacilityLayout.Route 的真实障碍导航；固定 queueEntry 仅保留为 Configure 兼容参数，不再作为必须到访的目标。
- 入店导航只在物理可通行、捷径仍全部位于开放地面时删去网格多余拐点；不使用基于旧柜台位置的直角转换。FIFO、槽位、布局修订与持久化边界不变。Spec085ArrivalTests/Spec085ArrivalSceneTests 分别保护回头复现、实际避障与移动旋转后的逐帧入店。
