# BurgerShop 协作说明

更新日期：2026-09-10。当前只搭建协作环境，玩法开发暂缓。

## 项目入口

| 平台 | 地址 | 职责 |
| --- | --- | --- |
| GitHub | [Ic3Ma0/BurgerShop](https://github.com/Ic3Ma0/BurgerShop) | 私有代码仓库、Issue、PR、版本 |
| Slack | [Coding / burgershop](https://app.slack.com/client/T0C027LE90E/C0C1NLB6YRE) | 私有项目频道、通知和讨论 |
| Notion | [BurgerShop](https://app.notion.com/p/3d7dae010b8e8147b0bed4556df7f413) | 项目主页、产品说明、验收 |
| Notion 看板 | [BurgerShop 任务看板](https://app.notion.com/p/50ab7a261b03486e800d722698b7f952) | 协作配置与验收，关联 Issue/PR |

## 当前接通状态

- [x] 私有仓库创建，初始 Unity 项目上传到 main。
- [x] 本地 origin 已设置，main 跟踪 origin/main。
- [x] Slack 私有频道建立。
- [x] Notion 私有项目主页、设计子页和任务看板建立。
- [x] GitHub、Slack、Notion 入口链接建立。
- [ ] GitHub 官方 Slack 应用安装、账号连接及频道订阅确认。
- [ ] Notion 自动同步方案：已确认当前为免费版或 Plus，不支持原生 GitHub 关联属性；需选择替代方案，尚未启用自动同步。
- [ ] 真实 PR 事件的通知与状态同步验证。

普通链接和通过助手发送的消息不代表原生自动同步已接通。

## 日常流程

1. Notion 记录产品需求与验收标准，GitHub Issue 拆解具体实施任务。
2. 从最新 main 建立功能分支，一项任务对应一个聚焦的 PR。
3. PR 关联 GitHub Issue 和 Notion 任务，说明改动与验证结果。
4. 完成检查、审查；涉及玩法时实际试玩，再合并。
5. Slack 接收相关事件通知，Notion 记录产品验收结果。

GitHub 是代码和具体实施任务状态的依据。Notion 的产品验收与 PR 合并状态分开记录。

## 接通 GitHub → Slack

在 [GitHub for Slack](https://slack.github.com/) 将官方应用加入 Coding 工作区。随后进入 burgershop 频道，逐条执行：

```text
/invite @github
/github signin
/github subscribe Ic3Ma0/BurgerShop issues pulls releases reviews
/github unsubscribe Ic3Ma0/BurgerShop commits deployments
/github subscribe list
```

账号连接中选择 Ic3Ma0，并按页面提示授权 BurgerShop 私有仓库。收到订阅成功回复后，通过文档 PR 验证；后续存在实际 CI 工作流时再按需开启 workflows 通知。

当前助手的 Slack 工具支持频道和消息管理，没有安装 GitHub 应用、完成 OAuth 或执行 Slack 斜杠命令的接口。将这些命令作为普通消息发出不会执行订阅，需在 Slack 界面中运行。

参考：[安装说明](https://docs.github.com/en/integrations/how-tos/slack/integrate-github-with-slack)、[通知配置](https://docs.github.com/en/integrations/how-tos/slack/customize-notifications)。

## 接通 GitHub → Notion

任务看板已包含状态、类别、BS 前缀唯一编号、Issue 链接与 PR 链接。两个链接字段只用于跳转。

**套餐前提：Notion 官方注明 GitHub 关联属性仅限 Business 和 Enterprise。** 用户已确认当前工作区为免费版或 Plus，因此跳过原生 PR 属性配置路径。此前说明遗漏了这一前提，现已更正。[官方套餐限制](https://www.notion.com/help/connected-properties)

如果是 Business 或 Enterprise，在任务看板切换到 `Default view` 表格视图，点击表格最右侧新增属性的 `+`，搜索 `GitHub` 并选择 `GitHub Pull Requests`，再按提示连接 GitHub。符合套餐条件却找不到时，按 [Notion 官方说明](https://www.notion.com/help/github)从设置中的 Connections 添加 GitHub Pull Request 连接。仓库属于个人账号，优先使用 PR 属性的账号连接流程；不要把组织级 GitHub Workspace 连接当作已配置完成。

如果是免费版或 Plus，现有项目页、任务看板及 Issue/PR 链接仍可使用。按需由 Codex 更新状态不等于事件触发的自动同步；若需要持续自动同步，需要另行配置其他集成方案，目前尚未启用。不必仅为完成当前项目基础协作而升级套餐。

将实际文档 PR 填入原生关联属性。如当前工作区支持 Auto-update，可将 PR 打开映射为“进行中”、合并映射为“完成”。涉及多个 PR 的产品需求需要单独验收。当前 Notion 工具可创建页面、数据库和普通属性，原生连接授权与 PR 专用属性需在 Notion 界面完成。

## 联动验收

使用协作说明的文档 PR，关联 [Notion 验证任务](https://app.notion.com/p/3d7dae010b8e814b8591ca45e21453b6)。只有观察到以下结果后，才更新为接通：

1. Slack 出现 GitHub 应用发出的真实 PR 事件。
2. Notion 原生 PR 属性显示正确的 PR 和状态。
3. PR 状态变化后，两端更新可核对。

## 本地与版本管理

- 仓库根目录是 `BurgerShop/`，历史 `work/` 目录位于仓库外。
- 提交 Assets 及其 `.meta`、Packages、ProjectSettings 与项目文档。
- Library、Temp、Logs、UserSettings 和 Builds 属于忽略内容。
- 同一工作目录一次只安排一个工具主动写入。需要并行实现时使用独立分支与 worktree。
