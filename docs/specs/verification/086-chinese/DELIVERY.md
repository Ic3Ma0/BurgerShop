# 086 中文与文字适配交付（2026-09-22）

## 修改

- 所有生产 UI Text 创建入口接入 `LocalizedText`，统一显示简体中文。HUD、商城三页、摆放提示、设施/人物升级、餐桌换款、存档与删除确认、离线收益、互动提示均通过同一显示文案表。
- 保留内部对象名、业务字符串、按钮回调和存档字段；没有修改费用或升级规则。原开局文案复用同一文案表。
- 沿用已有 Noto Sans CJK 字体，取消全局最小 28 号字和溢出显示；尊重原组件字号，按容器自动换行与适度缩小。奖励星符号替换为字体支持的 ★。
- 仍直接渲染的道路/扩建/店铺招牌使用中文字体与宽度限制；隐藏的内部 TextMesh 标签不影响玩法判断。
- 存档面板原为固定 920×1100，横屏被裁边；现在根据 Canvas 可用尺寸缩放，保留至少左右 20、上下 30 的本地坐标留白。

## 证据与验收边界

| 验收 | 证据 |
|---|---|
| 中文覆盖及数字保留 | `focused.xml`：15 项 086 测试通过。包含动态金额/等级、离线叙事、摆放、删除、招牌与字形；真实 SampleScene 覆盖等级 1–15 × 2 分辨率 × 11 界面状态，共 330 次界面检查，扫描活动 Text 是否含英文字母。 |
| 字号与边界 | 横屏 1280×720、竖屏 720×1280；断言字体、换行、按实际拟合字号计算的完整文字高度；存档面板四角转到父 Canvas 本地坐标后断言留白。已检查附带的实际 Unity 渲染截图。 |
| 长提示与重要信息 | 商城费用/差额/星奖励、扩建用途、设施产能、离线 8 小时与 123,456 金币、删除后果均保留。确认界面截图为隔离测试夹具，不会删除真实存档。 |
| 交互保持 | `focused.xml` 合计 16/16：额外包含原有 `ContinueReceivesPointerClickAndDismissesWithoutPayingAgain`；删除取消按钮也由测试点击并断言关闭。原有设施/人物升级定向测试通过，见下方回归结果。 |

截图由隔离 Unity 6000.3.23f1 的 SampleScene 实际渲染，不是设计稿。CUA 读取桌面时返回电脑锁屏，因此未进行桌面手动点击验收。没有访问真实玩家存档；未执行 Android 打包或真机验收。

## 回归结果

`regression.xml`：80 项，73 通过、7 失败。下列 7 项在隔离副本恢复原 HudChrome/SalesHud/WorldLabelHud、禁用本次中文入口后仍以相同原因失败（`baseline-without-localization.xml`）。本轮未修改这些旧玩法断言来制造通过结果，也未扩展到修复其经营/迁移夹具。

- `EmptyHandsStillAskToPickUpABurgerWhenBothMachinesHaveStock`：Expected string length 26 but was 22. Strings differ at index 0.   Expected: "Upgrade the burger machine"   But was:  "Sell your first burger"   -----------^
- `PersistenceWritesVersion10AndOldSavesKeepGrillProgressAtColaLv1`：Expected: 2   But was:  1
- `SampleSceneSpawnsOrdersAndRefillsQueueThroughUpdate`：Expected: 3   But was:  2
- `EarnSpendUpgradeAndKeepServingInRealScene`：Keyboard route blocked at (-0.60, 1.08, -0.68), target (2.50, 0.00, 12.00)   Expected: less than 0.219999999f   But was:  13.0541945f
- `EarnHireAndShareWorkWithAnAutonomousEmployeeInRealScene`：Expected: not null   But was:  null
- `SaveRestoresChosenSetAndPendingChoiceWithoutCharging`：Expected: True   But was:  False
- `SaveRestoresFourSeatPatioWithoutChargingAgain`：Expected: True   But was:  False

## 手工复验

解锁电脑，Unity 停止并重新进入 Play。查看商城购买/扩建/我的设施、点击机器与人物升级、打开存档与删除取消、查看离线结算；旋转或调整 Game 窗口比例，检查文字与按钮边界。当前自动覆盖范围之外的极端分辨率及 Android 真机仍待后续复验。
