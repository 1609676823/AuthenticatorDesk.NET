# 隐私说明

[English](PRIVACY.md)

本文描述本仓库公开的 AuthenticatorDesk 代码行为。非官方构建或经过修改的发行版本可能有
不同表现。

## 摘要

AuthenticatorDesk 是本地优先的 Windows 应用。当前应用：

- 不要求账户；
- 不包含广告或分析；
- 不发送遥测或自动崩溃报告；
- 不把数据同步到云服务；
- 不包含网络客户端或自动更新检查。

认证验证码在本机生成。AuthenticatorDesk 不运营接收用户数据的服务器。

## 本地保存的数据

根据用户启用的功能，AuthenticatorDesk 可能保存：

- 账户名称、发行方、备注、共享认证密钥、OTP 参数、服务商恢复字段、收藏、排序和全局
  快捷键选项；
- 主题、托盘行为、锁定行为、剪贴板延时、窗口布局、最近导入和导出目录等应用设置；
- 所选语言和用户安装的语言包；
- 包含时间、诊断代码、异常消息、堆栈跟踪以及可能的本地文件路径的诊断错误日志；
- 启用开机启动时，包含可执行文件路径的 Windows 启动注册表值。

当前保险库保存在所选数据目录的 `vault.json`。默认数据目录为可执行文件所在目录。如果该
目录不可用或用户主动更改，AuthenticatorDesk 会在应用旁或
`%LocalAppData%\AuthenticatorDesk\locations` 下记录路径定位信息。

语言偏好单独保存在 `%LocalAppData%\AuthenticatorDesk\ui-preferences.json`。用户语言包
可以安装到 `%LocalAppData%\AuthenticatorDesk\Languages`。

错误日志保存在数据目录的 `logs` 文件夹。应用的设计不会主动把共享密钥或生成的验证码写入
日志，但分享日志前仍应检查异常文本和路径。

## 保险库保护

所有保险库模式都使用 AES-256-GCM 封装，但访问保护能力不同：

- **便携模式**是新保险库的默认模式，会把 AES 密钥保存在 `vault.json` 自身。它便于复制，
  但任何取得该文件的人都可以打开它。
- **主密码模式**使用随机盐和 600,000 次 PBKDF2-HMAC-SHA256 迭代从密码派生 AES 密钥。
  项目不会保存密码，也无法帮助找回密码。
- **Windows 账户模式**使用当前用户的 Windows DPAPI 保护随机 AES 密钥。其他 Windows
  账户或电脑通常无法打开该保险库。

安全边界详见完整的[安全模型](docs/SECURITY-MODEL.zh-CN.md)，报告方式参见
[SECURITY.zh-CN.md](SECURITY.zh-CN.md)。

## 剪贴板、快捷键与通知

用户复制验证码或敏感值时，内容会进入 Windows 剪贴板，其他有剪贴板访问能力的软件可以
读取。从仪表盘或全局复制快捷键复制的验证码可以在设定延时后清理，但只有剪贴板仍包含
同一验证码时才会执行。通过各自对话框复制的共享密钥、恢复码和 OTP Auth URI 不使用该
定时清理流程。

配置后的全局快捷键可以复制验证码、在 Windows 通知中显示验证码，或通过自动输入发送到
当前焦点窗口。这些操作由用户触发，但会向对应的 Windows 子系统和目标应用暴露验证码。

## 导入、导出与备份

只有用户选择导入或导出操作后，应用才会读取或写入相应文件。AuthenticatorDesk 不会上传
这些文件。

- 受密码保护的 `.authdesk` 备份在加密封装中包含验证器条目。
- WinAuth XML 可以使用密码导出，也可以按明文导出。
- 导出的 OTP Auth URI 和二维码包含认证密钥。
- 更改数据目录会复制保险库，并有意保留旧副本以便恢复。
- 原子保存和旧数据迁移可能留下 `vault.json.bak`、`vault.json.migrated-*.bak` 或其他
  恢复产物。

用户负责保护导出文件、旧副本、截图、可移动介质、同步目录和系统备份。

## 数据保留与删除

由于 AuthenticatorDesk 不接收用户数据，项目没有需要保留或删除的服务器副本。删除本地
数据时：

1. 退出 AuthenticatorDesk；
2. 从当前和旧数据目录删除 `vault.json` 及其恢复产物；
3. 删除不再需要的 `.authdesk`、WinAuth、OTP URI 和二维码导出；
4. 不再需要诊断历史时删除 `logs` 文件夹；
5. 可选择删除 `%LocalAppData%\AuthenticatorDesk`，以移除语言偏好、用户语言包、旧数据
   和路径定位信息；
6. 移除应用前先在应用内关闭开机启动，或删除
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 下的 `AuthenticatorDesk` 值。

除非用户拥有有效备份或原始认证注册密钥，否则删除保险库或遗忘主密码不可恢复。

## 第三方与操作系统行为

AuthenticatorDesk 在本地使用第三方库提供界面和二维码处理能力，相关声明随项目分发。
Windows、杀毒软件、企业管理、备份工具、剪贴板管理器和修改后的发行版本不受本项目控制，
它们可能按自己的策略处理本地文件或应用活动。

## 变更与问题

涉及隐私的行为变化应同步更新本文档。一般问题可以创建仓库 Issue，但不要附带隐私数据。
疑似漏洞必须按 [SECURITY.zh-CN.md](SECURITY.zh-CN.md) 中的私有流程报告。
