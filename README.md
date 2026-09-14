<p align="center">
  <img src="docs/images/banner.png" alt="Burger Shop" width="100%">
</p>

<h1 align="center">Burger Shop</h1>

<p align="center">
  <strong>面向 Android 的单人 3D 汉堡店经营原型</strong><br>
  斜俯视 · 单指摇杆 · 制作、送餐、扩店
</p>

<p align="center">
  <img alt="Unity" src="https://img.shields.io/badge/Unity-6.3%20LTS-222C37?style=for-the-badge&logo=unity&logoColor=white">
  <img alt="URP" src="https://img.shields.io/badge/URP-17.3-46A0F0?style=for-the-badge">
  <img alt="Android" src="https://img.shields.io/badge/Android-25+-3DDC84?style=for-the-badge&logo=android&logoColor=white">
  <img alt="Version" src="https://img.shields.io/badge/Version-0.3.0-E27D60?style=for-the-badge">
  <img alt="Status" src="https://img.shields.io/badge/Status-Prototype-F5A623?style=for-the-badge">
</p>

<p align="center">
  <a href="https://github.com/Ic3Ma0/BurgerShop">GitHub</a> ·
  <a href="#核心循环">核心循环</a> ·
  <a href="#现在能玩什么">现在能玩什么</a> ·
  <a href="#本地打开">本地打开</a> ·
  <a href="#文档">文档</a>
</p>

<p align="center">
  <img src="docs/images/readme/restaurant.png" alt="当前店铺与街区" width="88%">
</p>
<p align="center">
  <img src="docs/images/readme/lounge.png" alt="饮品休闲区" width="52%">
  <img src="docs/images/readme/supermarket.png" alt="当前商城与顶栏 HUD" width="28%">
</p>

---

## 关于游戏

玩家经营一间汉堡店：烤制、取餐、送到柜台或餐桌、收金币，再把收入投入设备、员工和扩建。自动化只在手动循环跑通之后出现，节奏随升级加快。

| | |
| --- | --- |
| 类型 | 3D Idle Arcade / Tycoon |
| 平台 | Android 优先，竖屏 |
| 视角 | 斜俯视第三人称 |
| 操作 | 单指虚拟摇杆（Editor 也可用 WASD） |
| 版本 | `0.3.0` 原型，尚未上架商店 |

首版用原创占位几何体验证手感与节奏，不依赖付费素材。

## 核心循环

```mermaid
flowchart LR
  A[接单] --> B[制作汉堡]
  B --> C[送餐]
  C --> D[收金币]
  D --> E[升级设备 / 员工]
  E --> F[提高效率]
  F --> G[解锁新区域]
  G --> A
```

- 玩家始终清楚下一步去哪里
- 每次交付都有金币、音效和视觉反馈
- 先手动跑通循环，再引入自动化
- 金额、库存和队列状态必须守恒

## 现在能玩什么

MVP **0.1.2**（Goal 00–09）已完成：移动、烤台、取餐搬运、顾客排队、收银、升级、雇人、本地存档，以及 Android 真机可玩。

当前 **0.3.0** 在此之上继续扩店，包括柜台库存与堂食、打包与得来速、员工与人事办公室、店铺等级与侧翼设施、本地进度保存。

完整勾选列表见 [`docs/GOALS.md`](docs/GOALS.md)。规格与交付记录在 [`docs/specs/`](docs/specs/)。

## 技术基线

| 项目 | 版本 |
| --- | --- |
| Unity | 6.3 LTS `6000.3.23f1` |
| 渲染 | Universal Render Pipeline `17.3.0` |
| 输入 | Input System `1.20.0` |
| UI | uGUI `2.0.0` |
| 最低 SDK | Android 25 |
| 包名 | `com.ic3ma0.burgershop` |

场景在运行时由 `Assets/_Project/Scripts/Core/Goal01Bootstrap.cs` 组装，而不是靠预先摆好的大量 prefab。

## 本地打开

```bash
git clone https://github.com/Ic3Ma0/BurgerShop.git
```

1. 安装 [Unity Hub](https://unity.com/download) 与 **Unity `6000.3.23f1`**，并勾选 **Android Build Support**。
2. 用 Hub 打开仓库根目录，打开 `Assets/Scenes/SampleScene.unity`。
3. 进入 Play Mode。用 **WASD** 或左下角摇杆在店内走动。
4. 烤台出餐后走到取餐点携带汉堡，再送到柜台或餐桌完成交付、收金币。
5. 赚到金币后可升级烤台、雇佣员工、解锁新设施。进度会写入本地存档。

Android 构建与安装：

```bash
bash scripts/build-android.sh
bash scripts/install-android.sh
```

环境说明见 [Goal 09 Android](docs/goal-09-android.md)。`Library/`、`Temp/`、`Logs/`、`Builds/` 已从版本控制中排除。

## 文档

| 文档 | 内容 |
| --- | --- |
| [`SPEC.md`](SPEC.md) | MVP 范围与首版验收 |
| [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md) | 品类、第一间店与体验原则 |
| [`docs/GAME_CORE.md`](docs/GAME_CORE.md) | 经营内核与节奏 |
| [`docs/GOALS.md`](docs/GOALS.md) | 分阶段目标 |
| [`docs/specs/`](docs/specs/) | 功能规格与交付记录 |

仓库：https://github.com/Ic3Ma0/BurgerShop
