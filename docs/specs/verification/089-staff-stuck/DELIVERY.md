# 089 员工柜台卡住修复

分支：fix/staff-stuck。保留工作区既有未提交改动，本次只修改员工柜台到达判断与其测试。

根因：Begin 以 1 米判断到达柜台，TickServing 要求 0.7 米以内。在距离柜台 0.7–1 米时，员工反复 Serving → Begin → Serving，没有实际移动。

修复：柜台行走结束与工作距离共用 CounterArrivalRadius=0.7。其他工位半径、员工速度、库存、任务优先级和寻路策略保持不变。检查了布局刷新逻辑，本次没有证据证明需要修改它，因此未扩大到寻路重写。

## 验证

- 隔离 Unity 6000.3.23f1，0.71/0.85/0.99 米三种位置，修复前全部失败：状态停在 Serving。
- 修复后三个案例均进入 ToCounter 并实际走进 0.7 米工作范围。
- RestaurantWorkerTests 与 StaffUpgradeTests：34 项，33 通过、1 失败。
- TwoHiredWorkersStayActiveAfterWorkingAndRestore 期待恢复 2 人、实际为 0。在隔离副本恢复本次修改前的员工代码，仍同样失败，属于已有存档恢复测试问题；未把它算作通过，亦未改断言掩盖。
- 边界脚本与 git diff --check 通过。没有运行真实玩家存档、桌面手动试玩或 Android 验收。

此修复针对已稳定复现的距离死循环；没有声称已排除所有员工停顿原因。真正被设施封死的通道也不通过瞬移来绕过。
