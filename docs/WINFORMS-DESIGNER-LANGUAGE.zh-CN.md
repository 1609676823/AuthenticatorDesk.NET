# WinForms 设计器宿主语言说明

本文说明 Visual Studio 中 .NET WinForms 设计器如何确定语言、AuthenticatorDesk 在设计时如何选择语言包，以及如何在不修改项目代码的前提下切换设计器语言。

## 结论

当前环境中的识别链路是：

```text
Visual Studio UI 语言：LCID 2052（zh-CN）
    ↓ Visual Studio 启动进程外设计器
DesignToolsServer.exe ... -l zh-CN
    ↓ 设计时首次调用 L.Get(...)
CultureInfo.CurrentUICulture：zh-CN
    ↓ AuthenticatorDesk 的中文脚本回退
zh-CN → zh-Hans → zh → en
    ↓ 过滤到项目中实际存在的语言包
zh-Hans → en
```

因此：

- 当前设计器宿主收到的 locale 是 `zh-CN`；
- 项目中没有名为 `zh-CN.json` 的语言包，但有 `zh-Hans.json`；
- `zh-CN` 被项目正常映射到 `zh-Hans`，所以设计器最终显示简体中文；
- 编译后程序保存的语言偏好不参与这条设计时链路。

> `DesignToolsServer.exe` 的 `-l` 参数是本机进程命令行中观察到的 Visual Studio 内部实现细节。Microsoft 没有把它作为面向用户的稳定切换接口公开。请通过 Visual Studio 设置或 `devenv /LCID` 切换，不要手工启动或修改 `DesignToolsServer.exe`。

## 什么是设计器宿主

现代 .NET WinForms 设计器采用进程外架构：

- `devenv.exe` 是 Visual Studio IDE 进程；
- `DesignToolsServer.exe` 是实际创建设计时窗体和控件的宿主进程；
- 设计器宿主按项目目标框架和平台运行，本项目当前实例的参数包含 `.NETCoreApp,Version=v10.0`。

这意味着设计器中的窗体不是由编译后程序的 `Program.Main` 启动，而是在 `DesignToolsServer.exe` 内创建。Microsoft 对这一架构的说明见 [The designer changes since .NET Framework](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls-design/designer-differences-framework)。

## AuthenticatorDesk 在设计时如何选语言

运行时和设计时使用同一个本地化类，但初始化入口不同。

| 场景 | 初始化入口 | 首选语言来源 |
| --- | --- | --- |
| 编译后程序 | `Program.Main` 读取 `LanguagePreferenceStore.Load()`，再调用 `L.Initialize(storedLanguage)` | 应用保存的语言偏好；选择 `system` 时才跟随系统 UI culture |
| Visual Studio 设计器 | 设计时窗体调用 `L.Get(...)`，随后由 `EnsureInitialized()` 执行 `Initialize(null)` | 设计器宿主进程的 `CultureInfo.CurrentUICulture` |

相关实现：

- [`Program.cs`](../AuthenticatorDesk.NET/Program.cs#L18) 第 18–19 行读取并应用运行时语言偏好；
- [`L.Get`](../AuthenticatorDesk.NET/Localization/L.cs#L125) 首先调用 `EnsureInitialized()`；
- [`EnsureInitialized`](../AuthenticatorDesk.NET/Localization/L.cs#L252) 在尚未初始化时调用 `Initialize(preference: null)`；
- [`DetectSystemLanguage`](../AuthenticatorDesk.NET/Localization/L.cs#L917) 读取 `CultureInfo.CurrentUICulture.Name`；
- [`GetChineseScriptFallback`](../AuthenticatorDesk.NET/Localization/L.cs#L837) 将中国大陆、新加坡和马来西亚的中文 locale 映射到 `zh-Hans`；
- [`zh-Hans.json`](../AuthenticatorDesk.NET/Languages/zh-Hans.json) 是项目的简体中文语言包。

`CurrentUICulture` 是 .NET 用于查找区域性 UI 资源的当前 culture。Microsoft API 说明见 [`CultureInfo.CurrentUICulture`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentuiculture?view=net-10.0)。

### 为什么 `zh-CN` 和 `zh-Hans` 同时出现

两者表示的层次不同：

- `zh-CN` 是区域型 locale，表示“中国大陆使用的中文”；
- `zh-Hans` 是文字脚本型 locale，表示“简体中文”；
- Visual Studio 把 `zh-CN` 交给设计器宿主；
- AuthenticatorDesk 将它映射到项目已有的 `zh-Hans` 语言包。

因此，在进程命令行中看到 `-l zh-CN`，而项目当前语言显示为 `zh-Hans`，属于预期行为。

## 本次环境的只读检查快照

检查日期：2026-07-29。

| 语言层 | 本次观察值 | 主要影响 |
| --- | --- | --- |
| Visual Studio UI 语言 | LCID `2052`，即 `zh-CN` | IDE 菜单、Visual Studio 启动的设计器宿主 |
| WinForms 设计器宿主 | `DesignToolsServer.exe ... -l zh-CN` | 设计时窗体的初始 UI culture |
| PowerShell 格式 culture | `zh-CN` | 日期、数字和货币格式 |
| PowerShell UI culture | `en-US` | 当前 PowerShell 会话自身的资源语言 |
| AuthenticatorDesk 运行时偏好 | `en` | 编译后应用的界面语言 |

这些值可以同时不同。尤其需要注意：

- PowerShell 的 `Get-UICulture` 不能代替设计器宿主的实际进程参数；
- AuthenticatorDesk 的运行时偏好不会改变 Visual Studio 的设计器语言；
- Visual Studio 的设计器语言也不会覆盖编译后程序保存的语言偏好。

## 查看当前设计器宿主语言

先在 Visual Studio 中打开任意 WinForms 窗体设计器，再在 PowerShell 中执行以下只读命令：

```powershell
$servers = Get-CimInstance Win32_Process -Filter "Name='DesignToolsServer.exe'"

$servers | ForEach-Object {
    $commandLine = [string]$_.CommandLine
    $localeMatch = [regex]::Match(
        $commandLine,
        '(?:^|\s)-l\s+(?<locale>\S+)')

    [pscustomobject]@{
        ProcessId = $_.ProcessId
        ParentProcessId = $_.ParentProcessId
        Locale = if ($localeMatch.Success) {
            $localeMatch.Groups['locale'].Value
        } else {
            '(未找到 -l 参数)'
        }
        CommandLine = $commandLine
    }
} | Format-List
```

重点查看 `Locale`：

```text
Locale : zh-CN
```

或：

```text
Locale : en-US
```

如果没有结果，通常只是尚未打开窗体设计器，或者设计器宿主已经退出。多个 Visual Studio 实例、目标框架或设计器会话也可能产生多个 `DesignToolsServer.exe`，此时应逐条检查。

以下命令只用于比较 Windows/PowerShell culture，不应据此推断设计器语言：

```powershell
Get-Culture
Get-UICulture
"PSCulture=$PSCulture"
"PSUICulture=$PSUICulture"
```

## 通过 Visual Studio 界面切换

这是推荐方式。

### 1. 确认目标语言包已安装

1. 打开 Visual Studio Installer。
2. 找到当前使用的 Visual Studio 实例，选择“修改（Modify）”。
3. 打开“语言包（Language packs）”页签。
4. 勾选“中文（简体）”或“English”等目标语言。
5. 选择“修改”，等待安装完成。

也可以从 Visual Studio 的“工具（Tools）→ 获取工具和功能（Get Tools and Features）”打开 Installer。官方步骤见 [Modify Visual Studio workloads, components, and language packs](https://learn.microsoft.com/en-us/visualstudio/install/modify-visual-studio?view=visualstudio)。

### 2. 切换 IDE 语言

1. 打开 Visual Studio。
2. 进入“工具（Tools）→ 选项（Options）”。
3. 进入“环境（Environment）→ 国际设置（International Settings）”。
4. 在“语言（Language）”中明确选择“中文（简体）”或“English”。
5. 保存设置。
6. 完全退出所有 Visual Studio 实例。
7. 重新启动 Visual Studio，打开解决方案和窗体设计器。
8. 用上一节的 PowerShell 命令确认新的 `-l` 值。

新版设置界面可能把入口显示为“All Settings → Environment → International Settings”。如果希望结果固定，建议选择明确语言，而不是“Same as Microsoft Windows”。

完整重启很重要：设计器宿主按需启动，并且 AuthenticatorDesk 的 `L` 在该宿主进程中首次初始化后会缓存语言状态。只关闭设计器标签页不一定会创建新的宿主进程。

## 通过命令行切换

Visual Studio 官方支持以下语法：

```text
devenv {/LCID|/L} LocaleID
```

常用值：

| 目标语言 | Culture | LCID |
| --- | --- | ---: |
| 中文（简体） | `zh-CN` | `2052` |
| English（美国） | `en-US` | `1033` |

先关闭所有 Visual Studio 实例，再从目标 Visual Studio 版本的 Developer PowerShell 或 Developer Command Prompt 执行：

```powershell
# 切换为简体中文
devenv /LCID 2052

# 切换为英文
devenv /LCID 1033
```

注意：

- `/LCID` 修改的是整个 Visual Studio IDE 的默认语言，不是单个项目或单个窗体；
- 该设置会跨 Visual Studio 会话持久保存，并非一次性参数；
- 如果目标语言包没有安装，Visual Studio 会忽略该设置；
- 安装了多个 Visual Studio 版本时，应使用目标版本的开发者命令行，或明确调用该实例的 `Common7\IDE\devenv.exe`。

官方说明见 [`/LCID (devenv.exe)`](https://learn.microsoft.com/en-us/visualstudio/ide/reference/lcid-devenv-exe?view=visualstudio) 和 [Devenv command-line switches](https://learn.microsoft.com/en-us/visualstudio/ide/reference/devenv-command-line-switches?view=visualstudio)。

## 哪些操作不会直接切换设计器

| 操作 | 设计器宿主 | 编译后程序 |
| --- | --- | --- |
| 修改 Visual Studio 的 IDE 语言 | 完整重启后会改变 | 不会改变应用已保存的语言偏好 |
| 在 AuthenticatorDesk 设置页切换语言 | 不会改变 | 会改变 |
| 编辑 `%LOCALAPPDATA%\AuthenticatorDesk\ui-preferences.json` | 不会改变 | 会改变，但不建议手工编辑 |
| 修改 Windows 显示语言 | 仅当 Visual Studio 选择“Same as Microsoft Windows”时会间接影响 | 仅当应用选择 `system` 时会影响 |
| 修改 Windows 区域格式或 `Set-Culture` | 通常不会改变 UI 翻译语言 | 主要影响日期和数字等格式 |
| 修改 PowerShell 的显示语言 | 不会改变 | 不会改变 |

## 常见现象与排查

| 现象 | 原因或处理方式 |
| --- | --- |
| 设计器是中文，编译后程序是英文 | 两者使用独立的初始化入口，这是正常现象 |
| 宿主是 `zh-CN`，项目选择 `zh-Hans` | 项目把区域型中文 locale 映射到简体中文脚本语言包，这是正常现象 |
| 在应用内切换语言后设计器不变 | 应用偏好不参与设计器宿主初始化 |
| 切换 Visual Studio 语言后设计器仍是旧语言 | 目标语言包可能未安装，或旧的 Visual Studio/设计器宿主仍在运行；完整退出所有实例后重试 |
| 找不到 `DesignToolsServer.exe` | 先打开一个 WinForms 窗体设计器 |
| 同时存在多个设计器宿主 | 分别检查每个进程的父进程、目标框架和 `-l` 参数 |
| `-l` 已正确，但仍有少量中英文混合 | 再检查语言包是否缺键、是否存在硬编码文字、第三方控件是否使用独立本地化，以及产品名、协议名或算法名是否有意保留英文 |

推荐排查顺序：

1. 确认目标 Visual Studio 语言包已经安装。
2. 完全退出所有 Visual Studio 实例。
3. 通过 GUI 或 `/LCID` 设置目标 IDE 语言。
4. 重新启动 Visual Studio 并打开一个窗体设计器。
5. 查看 `DesignToolsServer.exe` 的 `-l` 参数。
6. 只有在 `-l` 已正确后，再排查项目语言键、硬编码文字或第三方控件。

切换设计器语言不需要修改 `.csproj`、`.resx`、`Program.cs` 或语言包 JSON，也不需要先删除 `bin`、`obj` 或 Visual Studio 缓存。

## 相关项目文档

- [语言包说明](../AuthenticatorDesk.NET/Languages/README.zh-CN.md)
- [本地化实现](../AuthenticatorDesk.NET/Localization/L.cs)
- [运行时语言偏好存储](../AuthenticatorDesk.NET/Localization/LanguagePreferenceStore.cs)
- [程序启动入口](../AuthenticatorDesk.NET/Program.cs)
