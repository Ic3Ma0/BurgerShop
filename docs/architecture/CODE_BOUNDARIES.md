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
