# 090 人物实体避障交付

2026-09-23，Unity 6000.3.23f1；隔离项目 `/tmp/bs090-obstacles`，自动测试的存档由 SaveIsolatedGameplayTest 隔离，未修改真实存档。

## 实现

- 统一地面高度下的人体碰撞探测，识别 Box/Capsule 等实体，排除地板、触发器和角色自己；员工与顾客每段移动前检查，不因长帧穿过障碍。
- 复用现有布局导航。起点连接不能穿墙，实体内交互目标结束在外侧；无路不再退回穿透直线。
- 新购、移动、旋转设施以及机器升级变大后更新导航。保留队列中顾客世界位置，重新走向当前柜台，不随柜台瞬移。
- 玩家继续由 CharacterController 与真实机器、桌椅碰撞；不增加自动操控摇杆。
- 清桌使用桌旁交互点；顾客从椅侧入座/离座。用餐中的餐桌暂不可移动，避免把正在就餐的顾客带走。

## 证据

| 验收 | 证据 | 结果 |
| --- | --- | --- |
| 玩家被真实汉堡机、餐桌挡住 | Spec090ObstacleTests.PlayerCannotWalkThroughActualMachineOrTable ×2；PlayerControllerCannotCrossNewSolid | 通过 |
| 矩形/旋转/圆形实体绕行 | RoutesAroundSolidIncludingRotatedAndRoundBodies ×3；OccupiedInteractionTargetEndsOutsideBody | 通过 |
| 无路等待、长帧不穿透 | FullyBlockedRouteNeverFallsBackThroughWall；CustomerLongFrameDetoursInsteadOfCrossingFurniture | 通过 |
| 动态新增/移动、升级变大刷新 | CommittedObstacleInvalidatesCachedRoute；EnlargedMachineRefreshesNavigation | 通过 |
| 员工真实搬运不只是停住 | RestaurantWorkerTests.WorkerRoutesAroundNewObstacleAndStillDelivers：60 秒模拟，中途移动障碍，逐步检查并断言交付发生 | 通过 |
| 真实场景柜台移动、旋转及顾客到达 | Spec085ArrivalSceneTests：先生成顾客，再移动柜台，检查世界坐标保持、FIFO、朝向、全程不重叠实体、三个槽位到达 | 通过 |
| 布局变更不瞬移 | RelayoutDoesNotTeleportCustomer | 通过 |
| 就餐、垃圾、清桌回归 | DiningTrashTests 11 项 | 通过 |

以上 25 项最终均通过。`core-and-service.xml` 为 24 通过、1 个坐标逐位相等断言失败：世界坐标经过父变换逆变换后存在浮点误差。改为 0.0001 米容限（没有取消检查），只复验受影响的实际场景，`moved-guests.xml` 1/1 通过。

扩展回归：
- `worker-regression.xml`：37/38 通过。唯一失败 `TwoHiredWorkersStayActiveAfterWorkingAndRestore` 在 089 修复前也失败，见 `../089-staff-stuck/restore-baseline.xml`。仍是待处理项。
- `placement-regression.xml`：40/43 通过。三个商城失败为 DesktopMatchesApprovedPreviewHoverClickAndUiFreeze / DoneCommitsFrozenPreviewAndUiGesturesNeverMoveIt / ShopBuysMovesRotatesCancelsAndSavesRepeatedTables。
- `placement-baseline.xml`：独立 `/tmp/bs090-baseline` 把 090 所改运行时文件恢复到 76ee841 内容，保留本轮前已存在的 085 队列修复；商城仍为 6/9，同三项同原因失败。不是本轮引入；没有调整原断言或宣称全量回归通过。
- 边界脚本、自测试 6 项、git diff --check 通过。

人工试玩未替代为自动验收：在 SampleScene 中，用摇杆持续朝机器/桌子推动；通过商城移动、旋转设施到原通路，确认顾客和员工绕行；升级机器后重复。已执行隔离 Play 模式程序化场景验证，未进行本轮人工手感验收或 Android 验收。
