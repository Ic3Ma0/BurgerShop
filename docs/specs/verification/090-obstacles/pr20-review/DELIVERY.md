# PR #20 审查修订：P1 / P2

2026-09-23；Unity 6000.3.23f1；隔离项目 `/tmp/bs090-obstacles`，SampleScene 使用隔离存档。未触碰真实存档、091 并行修改或既有定价。

## P1：可达性与实际终点一致

- CanReach / Route 均使用同一个严格 Nearest 连接检查，起终点至网格的最后一段必须可走。
- 不再允许障碍中的目标借附近网格通过校验；Route 失败返回 null，成功路径最后一点必须是请求的目标。
- 垃圾桶的工作目标从桶中心改为明确的外侧点，避免把“可停在附近”隐藏在通用导航里。
- `NearbyReachableGridDoesNotMakeBlockedFinalLegReachable` 明确断言：附近网格可达，阻塞交互点不可达且无路径，清晰目标的路径准确结束在目标。原先“实体内目标停在外面”的 090 断言根据本次审查要求改为返回 null。

## P2：连续入座、离座

- 新增 Seating / LeavingSeat 阶段；使用有限移动预算走完椅侧到座位、座位到椅侧的动作。吃饭只累计入座后剩余时间；离座结束才释放座位。
- 椅子使用动作只允许进入自己被分配的椅子（使用较窄的就座姿态碰撞），继续检查桌面和其他实体。四人桌从桌外缘接近，避免切过桌角或邻椅。
- 删除拐点 0.04 米不扣时间的直接吸附；平滑路线生成后检查所有段，失败保留原始安全路线。
- `ActualChairMealHasContinuousMountAndDismount` 覆盖三种桌型全部八个椅位和 90 度旋转：逐帧位移不超过 1.92/60 米，必须经历入座/吃饭/离座，最终离店、留下两团垃圾、释放本人的座位。
- `SampleSceneShowsContinuousChairUse` 在真实 SampleScene 中完成同样过程，并渲染 640×640 画面。轨迹见 trajectory.csv；入座 35 帧、离座 35 帧，全流程最大步长 0.032 米；Eating 包含到座最后一小步，随后保持坐标。
- 已查看 Seating.png / Eating.png / LeavingSeat.png。程序化 Play 模式验证与渲染已完成，不声称已经完成用户的人工操作手感验收。

## 回归

`results.xml`：68 项，67 通过，1 既有失败：
`RestaurantWorkerTests.TwoHiredWorkersStayActiveAfterWorkingAndRestore`（期望恢复 2 员工，实际 0）。此失败在 089 中已有撤回修复后的基线证据，不属于此次 P1/P2；没有将其标为通过。

本次全部新增 P1/P2 测试、090 避障、085 实际场景移动柜台、043 几何摆放、员工搬运/清桌/丢垃圾、餐桌垃圾回归通过。

`DiningTrashTests.NextCustomerWaitsBesideADirtyTableFacingItUntilTrashIsCleared` 保留位置/朝向/座位/后续就餐断言，将固定推进 4 秒改为最多推进 15 秒并到达即止：取消免费吸附后，应按真实步行预算等待，不能用旧吸附带来的到达时间当期望。

边界脚本及 6 项脚本自测试、git diff --check 通过。未运行全量 EditMode、091 验证或 Android 验收；未合并 PR。
