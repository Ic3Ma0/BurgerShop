# BS-SPEC-036 本地交付

实现：北侧独立加工区及自行车道、蓝色加工机、红色外卖包、黄色取货台、三人自行车队列、逐件装车、整单现金及零件掉落、玩家拾取、独立零件余额和 v11 存档。具体参数见 BS-SPEC-036；新线首版直接开放，由玩家搬运。零件没有消费或兑换入口。模型及图标均由项目代码绘制，无外部图片素材。

验证：Unity 6000.3.23f1 定向 EditMode 测试 64/64 通过（CourierLineTests、LocalSaveTests、GrowthEconomyTests、ColaProductTests）。覆盖物品守恒和满仓、错误物品不被消耗、缺货等待/逐件交付/仅完整订单结算、现金及零件拾取、暂停、零件余额溢出保护、旧档校验兼容及余额恢复。记录：Logs/courier/results.xml、Logs/courier/tests.log。

SampleScene 已通过 Unity GUI 实际 Play，隔离存档下确认北侧新区域、蓝色设备、黄色取货台、自行车及紧凑订单图标显示，未发现运行异常（Logs/courier/play.log）。完整交易链由定向逻辑测试验证；本次 GUI 未手动走完整交易链，不将其记作完整人工交互验收。Android 按用户要求留待统一验收。

本地体验：Unity 菜单 BurgerShop → Courier preview (isolated save)。玩家从新区域开始并携带汉堡；靠近蓝色机器正面绿圈投入，在右侧绿圈领取红包，再去黄色台前绿圈卸货。骑手收齐 4 包后留下 80 现金和 4 零件，靠近各自地面奖励拾取。退出 Play 恢复原存档路径，预览不写真实存档。普通 SampleScene Play 也包含新区域，从原大厅北侧中央缺口进入。

状态：实现完成、本地定向测试通过、Unity 基础可视运行通过；尚未合并或推送 GitHub，未构建/发布 APK。员工搬运、新设施升级、零件消费及兑换不属于本次实现。
