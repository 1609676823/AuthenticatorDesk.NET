# Git 管理脚本与 GitHub Actions 发布

本项目沿用 RemoteHubStudio 的“共用构建 + 两个发布渠道 + 定时/手动入口”结构，
适配 AuthenticatorDesk.NET 的 .NET 10 Windows Forms 项目和 xUnit 烟雾测试。

## 根目录 BAT 脚本

| 文件 | 用途 |
| --- | --- |
| `setup-git-remotes.bat` | 幂等配置 `origin`：从 Gitee 拉取，同时向 Gitee、GitHub 推送。 |
| `force-push-git-remotes.bat` | 将本地 `master` 的已提交内容强制推送到两个仓库，分别报告结果。 |
| `reset-git-repository.bat` | 删除当前项目的本地 `.git`，重新初始化 `master`、配置远端、添加未忽略文件并创建 `Initial commit`。 |

远端地址：

- Gitee：<https://gitee.com/lnsyzjw/authenticator-desk.-net>
- GitHub：<https://github.com/1609676823/AuthenticatorDesk.NET.git>

双击脚本后窗口默认暂停，也可在命令行传入 `--no-pause`。
脚本按自身所在目录定位仓库，不依赖启动时的工作目录。
强制推送会覆盖远端 `master` 历史；重建脚本会删除本地历史、分支、标签、stash 和仓库配置。
这两个脚本沿用参考项目的直接执行方式，没有交互确认；`--no-pause` 仅控制结束时暂停。
两个仓库的推送不是原子操作，一端失败时需分别检查结果。

## 工作流入口

| 文件 / Actions 名称 | 触发方式 | 行为 |
| --- | --- | --- |
| `daily-release.yml` / Daily Releases (Scheduled) | 每日 UTC 16:00，即北京时间次日 00:00 | 并行更新 nightly、按需发布 stable；唯一的定时入口。 |
| `all-releases.yml` / All Releases (Manual) | 手动 | 与定时入口共用相同的 nightly/stable 发布流程。 |
| `nightly-release.yml` / Nightly Release | 手动或被调用 | 重新构建，更新当前版本的 nightly 标签和预发布附件。 |
| `release.yml` / Stable Release | 手动、被调用或推送 `vX.Y.Z` 标签 | 正常运行保留已经公开发布的 stable 附件；没有发布时构建并发布。 |
| `force-build.yml` / Force Build and Release (Manual) | 仅手动 | 可选 nightly、stable 或 both；重新构建并覆盖所选渠道的附件，正式版标签移动到本次构建提交。 |
| `build-release.yml` / 共用构建 | 手动或被调用 | 还原、Release 构建、xUnit 测试、打包和上传 Artifact；单独运行只生成 Artifact。 |

发布入口只允许主仓库 `1609676823/AuthenticatorDesk.NET` 的默认分支；正式版同时支持
与源码版本一致的版本标签。强制发布选错分支时明确失败。
总任务共用并发锁，nightly/stable 分别使用渠道锁，避免同时更新同一渠道。
GitHub 定时任务可能延迟；定时配置需要推送到默认分支才生效。
工作流默认只读，发布任务单独申请 `contents: write`，使用内置 `GITHUB_TOKEN`，无需额外令牌。
仓库或组织的 Actions 策略需要允许这些任务运行。

## 版本与默认包

只需修改 `AuthenticatorDesk.NET/Program.cs` 中的 `AppVersion`，格式为 `X.Y.Z`。
现有程序集版本和应用显示版本继续共用这个常量。

- 正式版标签：`vX.Y.Z`，例如 `v1.0.3`。
- 预览版标签：`vX.Y.Z-nightly`，每次运行更新。
- nightly 构建标识：`X.Y.Z-nightly.RUN.ATTEMPT`；应用内显示的版本仍是 `X.Y.Z`。
- 正常正式版任务发现已有公开 Release 会跳过构建和附件更新；已有未发布标签时使用标签
  指向的源码。只有专用的手动强制入口可以替换已发布正式版并移动标签。

修改 [release-settings.psd1](release-settings.psd1) 可统一调整定时/标签任务和手动
`repository-default` 选项的默认行为：

- `DeploymentMode`：`self-contained`（默认，附带运行时）、`framework-dependent`
  （需要 .NET 10 Windows Desktop Runtime）或 `both`。
- `RuntimeIdentifiers`：`win-x86`、`win-x64`、`win-arm64`。
- `IncludePortable`：默认 `true`，按 VS 可移植发布模式生成不绑定 CPU RID 的 AnyCPU DLL，
  并保留 `AuthenticatorDesk.exe`。EXE 对应构建 SDK 的平台和架构（当前 CI 为 Windows x64），
  本身不跨 CPU 通用；portable 始终需要 Windows 和对应进程架构的 .NET 10 Windows Desktop Runtime。

默认共四个 ZIP，选择 `both` 则共七个。文件名例如
`AuthenticatorDesk-v1.0.3-win-x64-self-contained.zip`，文件名规则保持不变。
所有 ZIP 内部唯一的顶层目录固定为 `AuthenticatorDesk`，不随版本、架构或部署模式变化，
便于解压后替换已有目录。
所有包完整解压后均可双击 `AuthenticatorDesk.exe`；portable 另保留
`Start-AuthenticatorDesk.cmd` 或 `dotnet AuthenticatorDesk.dll` 作为备用启动方式，
使用后者时进程架构由所调用的 `dotnet` 决定。portable 包名称不变。

包内包含外置语言包、GPL、WinAuth 归属声明及依赖许可证；self-contained 还包含两个
.NET 运行时包的许可证及其提供的第三方声明。打包时检查必要文件，生成 `SHA256SUMS.txt`
和双语 `release-notes.md`，标明运行时要求、构建来源、准确源码链接和未签名状态。
发布脚本验证校验清单后才上传对应附件。构建 Artifact 保留 7 天。

此流程没有代码签名、安装器或自动更新器。仓库既有的
[发布前审查说明](../LICENSING-REVIEW.zh-CN.md)仍适用，添加流水线并不表示相关事项已经解决。

## 本地验证与打包

使用 Windows、.NET 10 SDK 和 PowerShell 7，在仓库根目录运行：

```powershell
dotnet restore .\AuthenticatorDesk.NET.slnx
dotnet build .\AuthenticatorDesk.NET.slnx -c Release --no-restore
dotnet test .\AuthenticatorDesk.SmokeTests\AuthenticatorDesk.SmokeTests.csproj -c Release --no-build --no-restore
$version = & .\.github\scripts\get-release-version.ps1
pwsh -NoProfile -File .\.github\scripts\package-release.ps1 `
  -Channel stable -ReleaseTag "v$version" -Version $version
```

产物位于 `artifacts/packages`（已由 `.gitignore` 排除），本地打包不会发布到 GitHub。
发布逻辑的离线回归测试见 [tests/README.md](tests/README.md)。

## English quick reference

The three BAT scripts configure dual Gitee/GitHub remotes, force-push local `master`,
or recreate local Git history, respectively. The latter two execute without a
confirmation prompt; `--no-pause` only disables the final pause.

Use **All Releases (Manual)** for normal publication, **Force Build and Release (Manual)**
to replace existing releases, or the reusable build workflow for artifacts only.
The scheduled entry runs daily at 16:00 UTC from the default branch. Stable tags
match `Program.AppVersion`; existing public stable assets are preserved unless the
dedicated manual force entry is used. Default output is three self-contained Windows
architecture packages plus one framework-dependent Windows portable package.
ZIP filenames retain their version, architecture, and deployment mode. Every ZIP
contains a single top-level `AuthenticatorDesk` directory so extracted releases
can replace the existing directory without renaming it.
The portable package includes an AnyCPU DLL and an EXE matching the build SDK
platform and architecture (currently Windows x64 in CI). Extract the full package
and run `AuthenticatorDesk.exe`; `Start-AuthenticatorDesk.cmd` or
`dotnet AuthenticatorDesk.dll` remains available as a fallback. Each launch method
requires the .NET 10 Windows Desktop Runtime matching its process architecture.
All packages are unsigned and include SHA256 checksums, attribution and license files.
See the existing [release review](../LICENSING-REVIEW.md) before public distribution.
