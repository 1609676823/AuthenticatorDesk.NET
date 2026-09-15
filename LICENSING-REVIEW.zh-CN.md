# 许可证与来源审查

> 审查日期：2026-07-29
>
> 发布状态：**阻塞——项目已为确认改编自 WinAuth 的部分选择 GPL-3.0-or-later；
> 但在解决下述 AntdUI/SVG.NET 许可证兼容问题和应用图标权利前，不应公开发布当前
> 仓库或二进制文件。**

[English](LICENSING-REVIEW.md)

本文是面向开源发布的工程合规审查记录，不构成法律意见。审查依据为本仓库内容、审查日
实际还原的依赖版本，以及对应的上游许可证和源代码。

## 结论摘要

项目作者于 2026-07-29 确认：为了支持文件迁移和互操作，WinAuth 兼容实现是在
OpenAI Codex 查阅 WinAuth 源码后完成的。因此，AuthenticatorDesk 将已识别的实现按
改编自 WinAuth 的代码处理，并为组合作品选择 **GNU GPL 第 3 版或由接收者选择任何
更高版本（GPL-3.0-or-later）**。必须保留 WinAuth 署名、版权声明和显著的修改声明。

已声明的运行时和测试依赖采用 MIT、Apache-2.0、Microsoft Public License、
Bouncy Castle 宽松许可证或 Unicode License；其独立声明继续汇总在
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。组合作品采用 GPL 不会抹去或
替换这些第三方许可证。

但是，本仓库**仍未通过公开发布审查**。AntdUI 2.4.3 是同一进程内的直接依赖，
其已编译程序集包含由 Microsoft Public License（Ms-PL）约束源码构建的 SVG.NET
实现。Free Software Foundation 将 Ms-PL 归类为与 GPL 不兼容。AuthenticatorDesk
无权代表 WinAuth 权利人单方面增加 GPL 链接例外。因此，在分发当前源码组合或二进制
文件前必须解决 AntdUI/SVG.NET 问题。应用图标权利是第二个独立发布阻塞项。

| 范围 | 状态 | 发布前要求 |
| --- | --- | --- |
| AuthenticatorDesk 组合作品 | 已选择许可证；分发阻塞 | 应用 GPL-3.0-or-later、保留上游声明，并在发布前解决 AntdUI/SVG.NET 和图标阻塞项。 |
| 运行时 NuGet 依赖 | 声明已准备 | 保留已添加的声明与许可证全文；升级版本时重新审查。 |
| Bouncy Castle Blowfish 源码 | 声明已准备 | 保留源文件头和宽松许可证声明。 |
| AntdUI 内含的第三方部分 | **发布阻塞** | 保留全部声明，并替换 AntdUI 或重新构建不含 Ms-PL 材料的版本、取得全部必要兼容授权，或建立真正独立进程的设计并接受合格法律复核。 |
| WinAuth 兼容实现 | 已选择 GPL 路径 | 将已识别代码按改编代码处理，保留 Colin Mackie 的声明，标明上游文件、审查提交和 2026-07-29 修改日期。 |
| 项目图标与媒体 | **发布阻塞** | 文档截图已使用合成数据从本应用重新生成；仍需确认并记录应用图标作者或再分发许可。 |
| 二进制发布打包 | 仍有 GPL 义务 | 除法律文件外，还需提供匹配的完整对应源码、发布 tag/commit、源码归档，以及所携带运行时和非系统组件所需的源码访问。 |

## 审查范围与方法

本次审查包括：

- 2026-07-29 可见的全部受版本控制源代码和资源；
- `AuthenticatorDesk.NET/AuthenticatorDesk.NET.csproj` 与
  `AuthenticatorDesk.SmokeTests/AuthenticatorDesk.SmokeTests.csproj`
  中的直接包引用；
- `AuthenticatorDesk.NET/obj/project.assets.json` 中的实际运行时依赖图；
- 对应 NuGet 精确版本内的许可证元数据和许可证文件；
- AntdUI 2.4.3 对应源代码内的版权与许可证文件头；
- 本地 WinAuth 兼容实现与 WinAuth 上游提交
  `c57132f57b8a90e5219c628deb591f4603f27cb0` 的针对性比较；
- 项目作者于 2026-07-29 作出的说明：Codex 在实现迁移兼容功能时查阅了 WinAuth
  源码；以及
- AntdUI 2.4.3 已编译程序集及对应源码中受 Ms-PL 约束的 SVG.NET 路径。

本审查不是法律意见，也不是完整的软件成分分析；对于代码和资源的真实创作过程，仍需由
作者提供证据。

## 主许可证

AuthenticatorDesk 已为组合作品选择 GPL-3.0-or-later：

```text
Copyright (C) 2026 AuthenticatorDesk contributors

This program is free software: you can redistribute it and/or modify it under
the terms of the GNU General Public License as published by the Free Software
Foundation, either version 3 of the License, or (at your option) any later
version.
```

完整且未经修改的 GNU GPL 第 3 版文本应放入
[`LICENSE.txt`](LICENSE.txt)。“或任何更高版本”的选择还必须出现在项目级声明和
受影响源码文件的声明中。直接改编的文件必须保留有关 WinAuth 版权行，并显著标明
AuthenticatorDesk 于 2026-07-29 对其进行了修改。

分发时，GPL-3.0-or-later 适用于受其覆盖的组合作品；它不会把第三方组件重新许可，
接收者仍可分别按原有 MIT、Apache-2.0、Ms-PL、Bouncy Castle 或 Unicode 条款使用
相应组件。必须完整保留这些声明和许可证文本。选择 GPL 已解决确认改编自 WinAuth
代码的许可路径，但不能单独解决下文的 Ms-PL 兼容问题。

## 依赖清单

### 运行时依赖图

| 组件 | 版本 | 许可证 | 证据 |
| --- | ---: | --- | --- |
| AntdUI | 2.4.3 | 包元数据为 Apache-2.0；已编译的 SVG.NET 部分采用 Ms-PL | `AntdUI/2.4.3/antdui.nuspec`；源提交 `a9de0f8d3b65e5cac10a5b334336fe27df2185f7`；下文所述二进制/源码检查 |
| QRCoder | 1.8.0 | MIT | `QRCoder/1.8.0/qrcoder.nuspec` 与 `QRCoder/1.8.0/LICENSE.txt` |
| ZXing.Net.Bindings.Windows.Compatibility | 0.16.14 | Apache-2.0 | `ZXing.Net.Bindings.Windows.Compatibility/0.16.14/*.nuspec` |
| ZXing.Net | 0.16.11 | Apache-2.0 | 间接还原；`ZXing.Net/0.16.11/*.nuspec` |

Apache-2.0 全文位于
[`THIRD-PARTY-LICENSES/Apache-2.0.txt`](THIRD-PARTY-LICENSES/Apache-2.0.txt)。
在本次审查的 AntdUI 2.4.3 和 ZXing.Net 包/源码中未发现独立的上游 `NOTICE`
文件；这不影响保留版权和许可证声明的义务。

### 构建与测试依赖

| 组件 | 版本 | 许可证 |
| --- | ---: | --- |
| coverlet.collector | 6.0.4 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |

已审查的间接测试依赖还包括：

- MIT：Microsoft.CodeCoverage 17.14.1、
  Microsoft.TestPlatform.ObjectModel 17.14.1、
  Microsoft.TestPlatform.TestHost 17.14.1、Newtonsoft.Json 13.0.3；
- Apache-2.0：xunit.abstractions 2.0.3、xunit.analyzers 1.18.0、
  xunit.assert 2.9.3、xunit.core 2.9.3 和 xunit extensibility 2.9.3。

普通应用发行物不包含这些测试包。如需再分发测试工具，应同时保留所分发精确包版本中的
许可证与声明。

## 内嵌的 Bouncy Castle 源代码

`AuthenticatorDesk.NET/Services/Legacy/BlowfishEngine.cs` 明确声明源自
Bouncy Castle C# API v1.7.0，并保留：

```text
Copyright (c) 2000-2011 The Legion Of The Bouncy Castle
```

源代码比较与该声明一致。适用的 Bouncy Castle 宽松许可证全文已收录在
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。源码文件头和第三方声明均应
保留在源代码与二进制发行物中。

`AuthenticatorDesk.NET/Services/Legacy/BouncyCastleCompat.cs` 是配套兼容代码；
本审查没有将该独立文件认定为 Bouncy Castle 衍生代码。

## AntdUI 内含的第三方部分

AntdUI 2.4.3 NuGet 包声明为 Apache-2.0，并指向源提交
`a9de0f8d3b65e5cac10a5b334336fe27df2185f7`。对应源码还编译了以下内部携带的
第三方部分。本次审查使用的确切路径与许可证证据如下。

### SVG.NET

- AntdUI 路径：`src/AntdUI/Lib/SVG/*.cs`（审查提交中共 139 个文件）。
- AntdUI 文件头：
  `THE SVG PROJECT IS AN OPENSOURCE LIBRARY LICENSED UNDER THE MS-PL License`
  和 `COPYRIGHT (C) svg-net`。
- 上游项目：<https://github.com/svg-net/SVG>
- 上游核验提交：`e74a8b0c9a1c52e02a3fe1580bde52453fd96a94`。
- 上游许可证来源：`license.txt`。
- 上游版权元数据来源：`Source/Svg.csproj`。
- 确切版权元数据：

  ```text
  © Copyright 2009 Microsoft. All Rights Reserved.
  © Copyright 2011 vvvv Group. All Rights Reserved.
  © Copyright 2021 SVG.NET Contributors
  ```

- 许可证：Microsoft Public License（Ms-PL）；全文位于
  [`THIRD-PARTY-LICENSES/Microsoft-Public-License.txt`](THIRD-PARTY-LICENSES/Microsoft-Public-License.txt)。

### Vanara

- AntdUI 路径：`src/AntdUI/Lib/Vanara/*.cs`（审查提交中共 15 个文件）。
- AntdUI 文件头：
  `THE Vanara PROJECT IS AN OPENSOURCE LIBRARY LICENSED UNDER THE MIT License`
  和 `COPYRIGHT (C) dahall`。
- 上游项目：<https://github.com/dahall/Vanara>
- 上游核验提交：`1eb490b7bd09ec8982a66c31a73321353ed7e9c7`。
- 上游许可证来源：`LICENSE`。
- 确切声明：`Copyright (c) 2017 David Hall`。
- 许可证：MIT；完整声明与条款已收录在
  [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。

### ChineseCalendar

- AntdUI 路径：`src/AntdUI/Lib/ChineseCalendar/ChineseDate.cs`。
- AntdUI 文件头：
  `THE ChineseCalendar PROJECT IS AN OPENSOURCE LIBRARY LICENSED UNDER THE MIT License`、
  `COPYRIGHT (C) lpz` 和 `https://gitee.com/lipz89/ChineseCalendar`。
- 上游许可证来源：
  <https://github.com/lipz89/ChineseCalendar/blob/master/LICENSE>。
- 确切声明：`Copyright (c) 2020 lipz89`。
- 许可证：MIT；完整声明与条款已收录在
  [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。

### Ant Design Icons

- AntdUI 数据路径：`src/AntdUI/Properties/Resources.resx`。
- AntdUI 查找映射路径：`src/AntdUI/Lib/SvgDb.cs`。
- 上游项目：<https://github.com/ant-design/ant-design-icons>。
- 上游核验提交：`6c18c63fbcfcf71dae09cd6bd6d63a48f8b688f1`。
- 上游许可证来源：`LICENSE`。
- 确切声明：
  `Copyright (c) 2018-present Ant UED, https://xtech.antfin.com/`。
- 许可证：MIT；完整声明与条款已收录在
  [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。

### Unicode Character Database

- AntdUI 路径：
  `src/AntdUI/Lib/GraphemeSplitter/GraphemeSplitter.Data.cs`。
- 生成的范围和文件注释表明其包含 Unicode Character Database 字素属性数据，
  包括 Unicode 15、16 和 17 的更新。
- 参考数据：
  <https://www.unicode.org/Public/17.0.0/ucd/auxiliary/GraphemeBreakProperty.txt>。
- 许可证来源：<https://www.unicode.org/license.txt>。
- 许可证：Unicode License V3；完整版权与许可声明已收录在
  [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。

以上额外声明是根据精确 AntdUI 包对应源码作出的保守合规处理。升级 AntdUI 时必须重新
审查。

### GPL 兼容阻塞项：采用 Ms-PL 的 SVG.NET

AntdUI 包元数据声明为 Apache-2.0，但 AntdUI 2.4.3 对应源码把受 Ms-PL 约束的
SVG.NET 文件编译进 `AntdUI.dll`。对实际还原包的二进制检查确认，该程序集包含已编译
的 SVG.NET 实现。AuthenticatorDesk 直接引用 AntdUI，在整个界面中调用其类型，并将
其加载到同一进程。因此，不能合理地将 AntdUI 视为操作系统的系统库，也不能视为聚合包
中仅相邻放置且彼此独立的作品。

Free Software Foundation
[将 Ms-PL 列为与 GPL 不兼容](https://www.gnu.org/licenses/license-list.html#ms-pl)，
并
[说明 GPL 作品链接与 GPL 不兼容的库时需要有关权利人许可](https://www.gnu.org/licenses/gpl-faq.html#GPLIncompatibleLibs)。
AuthenticatorDesk 贡献者可以许可自己的作品，但不能代表拥有 WinAuth 改编材料权利
的上游权利人，单方面授予 GNU GPL 第 3 版第 7 节链接例外。保留 Ms-PL 声明是必要
条件，但仅署名不能解决许可证不兼容。

因此，当前源码组合和二进制文件仍被阻止公开分发。应通过以下一种有记录的方式解决：

1. 将 AntdUI 替换为与 GPL-3.0-or-later 兼容的界面依赖；或者构建经过核验、
   已移除 SVG.NET Ms-PL 材料并用兼容代码替代的 AntdUI 版本；
2. 从组合作品分发所需许可涉及的每一位权利人处取得明确的书面兼容授权，并长期保留
   证据；或者
3. 将 GPL 覆盖的兼容功能重新设计为真正独立的程序，仅通过普通文件格式或命令行接口
   保持适当距离地通信，并在分发前就该边界取得合格法律复核。

仅把程序集拆成不同文件、动态加载 AntdUI、增加另一份声明，或只由 AuthenticatorDesk
贡献者宣布例外，都不能关闭此阻塞项。

## WinAuth GPL 来源——确认改编并选择 GPL 路径

### 上游许可证

WinAuth 提交
[`c57132f57b8a90e5219c628deb591f4603f27cb0`](https://github.com/winauth/winauth/tree/c57132f57b8a90e5219c628deb591f4603f27cb0)
中的下列文件均带有 GPL 文件头，许可为 GNU GPL 第 3 版或任何更高版本：

- `Authenticator/Authenticator.cs`——Copyright (C) 2011 Colin Mackie；
- `Authenticator/SteamAuthenticator.cs`——Copyright (C) 2015 Colin Mackie；
- `Authenticator/BattleNetAuthenticator.cs`——Copyright (C) 2013 Colin Mackie；
- `Authenticator/TrionAuthenticator.cs`——Copyright (C) 2013 Colin Mackie。

其他上游贡献者仍对其各自贡献保留版权。以下仓库和提交链接标识本次合规审查采用的版本；
并不声称 Codex 当时必然只查阅了该精确版本。

### 已识别的改编范围

鉴于项目作者已确认 Codex 在实现兼容功能时查阅了 WinAuth 源码，以下比较用于标识与
署名和许可有关的改编范围：

- 本地 `AuthenticatorDesk.NET/Services/WinAuthCryptoService.cs:40` 起使用了与上游
  [`Authenticator.cs:60-75`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/Authenticator.cs#L60-L75)
  相同的一组旧格式参数：8 字节盐、2,000 次 PBKDF2、256 字节派生密钥和
  `WINAUTH3` 文件头。
- 本地 `WinAuthCryptoService.cs:45` 至约 315 行实现了保护标志、多层加解密、
  头部/哈希、PBKDF2-SHA1 和 Blowfish/ISO10126 处理，与上游
  [`Authenticator.cs:442-500`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/Authenticator.cs#L442-L500)
  及
  [`Authenticator.cs:948-1308`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/Authenticator.cs#L948-L1308)
  对应。
- 本地 `AuthenticatorDesk.NET/Services/OtpService.cs:12` 及 158-172 行使用同一
  Steam 字符表和五字符归约处理，与上游
  [`SteamAuthenticator.cs:713-761`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/SteamAuthenticator.cs#L713-L761)
  对应。
- 本地 `OtpService.cs:175-184` 的 Trion 六位码处理与上游
  [`TrionAuthenticator.cs:318-374`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/TrionAuthenticator.cs#L318-L374)
  对应。
- 本地 `OtpService.cs:186-209` 和 260-289 行的 Battle.net 恢复码处理与上游
  [`BattleNetAuthenticator.cs:776-812`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/BattleNetAuthenticator.cs#L776-L812)
  及
  [`BattleNetAuthenticator.cs:879-905`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/BattleNetAuthenticator.cs#L879-L905)
  对应。

算法、常量、文件格式和功能兼容本身不能证明每一处本地表达都来自复制。针对性比较也
没有发现类似另行声明的 Bouncy Castle 文件那样的大范围逐字对应。但是，作者对开发
过程的说明与已识别的结构对应关系，支持将这些部分按改编自 WinAuth 的代码作保守处理。

两个本地文件首次出现在项目初始提交
`7c06570b11a2251f90c43027b75a2905a5ff8eed` 中，该提交创作日期为
2026-07-29。这是 AuthenticatorDesk 初次改编所适用的修改日期。

### 许可处理与署名

项目选择的合规路径为：

- 将 AuthenticatorDesk 组合作品按 GPL-3.0-or-later 许可；
- 保留有关 `Copyright (C) 2011, 2013, 2015 Colin Mackie` 声明及其他上游贡献者
  声明；
- 在直接改编的文件中增加显著声明，标识适用的 WinAuth 源文件、审查提交，并说明
  AuthenticatorDesk 于 2026-07-29 对代码进行了改编和实质性修改；
- 提供完整 GNU GPL 第 3 版文本，并在项目和源码文件声明中写明“或任何更高版本”；
  以及
- 将 OpenAI Codex 描述为开发工具，而不是上游作者、版权持有人或许可人。

AuthenticatorDesk 由不同团队独立维护，与 WinAuth 或其作者不存在关联、背书关系，
也不是其官方后继项目。GPL 与署名路径已解决原 WinAuth 来源不确定问题，但不能清除
独立存在的 AntdUI/SVG.NET 兼容阻塞项。

## 资源来源

以下应用图标资源目前没有相邻或内嵌的作者/许可证记录：

- `AuthenticatorDesk.NET/Assets/AuthenticatorDesk.Icon.ico`；
- `AuthenticatorDesk.NET/Assets/AuthenticatorDesk.Icon.png`。

两个文件均首次出现在项目初始提交
`7c06570b11a2251f90c43027b75a2905a5ff8eed` 中，且当前内容与该提交一致。ICO 的
SHA-256 为
`13B2F04ED39BBEB1A7200BFFE20C1B4D591AC1AF993C1D34BDC6CA5A517ABD91`，PNG 的
SHA-256 为
`8F3B8C79471F1326F2373E770B6A8F31437CA3BA8B8B7AECF1DF2490034BA97F`。
仓库中没有原始设计文件、生成记录、来源 URL、作者声明或书面授权。素材由初始提交加入
这一事实，不能证明创作者身份或适用的再分发权。

发布前应记录应用图标的创作者，并确认项目拥有再分发和修改权。不能因为图片存放在本
仓库中，就默认图片采用仓库的许可证。在形成该记录前，图标仍是独立的发布阻塞项。

`docs/images/` 下的图片已在本次审查期间使用当前 AuthenticatorDesk 构建重新生成。
截图使用英文界面、合成的 `example.test` 账户和公开测试密钥，不包含个人路径、真实凭据
或鼠标指针。它们是第一方应用截图，其中服务商名称只用于说明本地兼容性。

## 发行包义务

当前应用项目文件已配置为在 `dotnet publish` 时复制仓库法律文件。本次审查已使用
framework-dependent 和 self-contained `win-x64` 发布实际验证；审查输出已包含完整
GPL 第 3 版文本和下列文件。每个最终二进制 ZIP、安装包或其他发行物仍必须再次检查，
确认其在易于访问的位置实际保留：

- `LICENSE.txt`；
- `THIRD-PARTY-NOTICES.md`；
- `THIRD-PARTY-LICENSES/Apache-2.0.txt`；
- `THIRD-PARTY-LICENSES/Microsoft-Public-License.txt`。

复制这些文件是 GPL 二进制发行的必要条件，但并不充分。对于每个二进制文件，必须以
不另收费的等价方式提供该精确构建的机器可读完整对应源码。GitHub/Gitee 发行建议：

- 为构建二进制所用的精确提交创建不可变源码 tag；
- 在二进制旁标明该 tag 和完整 commit ID；
- 附加从该提交生成的源码归档，而不是只指向持续变化的默认分支；以及
- 二进制仍可获取期间，持续保证源码可获取。

源码归档必须包括 AuthenticatorDesk 源码、解决方案与项目文件、构建和打包脚本、
接口定义、语言和资源输入，以及生成、安装、运行和修改受覆盖作品所需的其他材料。
对于随包携带的非系统库，应提供适用源码，或者持久、明确且版本完全匹配的源码访问方式。
仅提供包名清单、许可证副本或 `dotnet restore` 命令，不能代替完整对应源码。

如果使用 .NET 自包含发布，`CopySelfContainedRuntimeLegalFiles` 目标会从精确还原的
运行时包版本复制许可证和声明，放入
`THIRD-PARTY-LICENSES/dotnet-runtime/<package-id>-<version>/`。最终发行物不得删除该
目录；还必须为实际再分发的运行时组件提供版本匹配的源码访问。SDK、运行时、目标架构
或项目文件变化后，必须重新核验输出和源码映射。

如果使用依赖框架的发布方式，则不打包 .NET 运行时；但随应用分发的非系统 DLL 仍有
声明和对应源码义务。依赖框架的发行通常比自包含发行更容易说明，但不能解决当前
AntdUI/SVG.NET 不兼容问题。

分发前，应在交互式应用中增加便于访问的**关于**、**致谢**或等效法律入口。该入口应
显示有关版权声明，标明 GPL-3.0-or-later，说明不提供担保，提供查看完整许可证的
方式，向已识别的 WinAuth 上游文件和 Colin Mackie 致谢，并说明 AuthenticatorDesk
与 WinAuth 不存在关联或背书关系。匹配的对应源码访问必须按上文要求随二进制发行物
提供。这样可以为用户提供可访问的法律声明路径并保留上游署名。

如果目标代码随 GPLv3 所定义的“用户产品”一同分发，还应评估并提供 GNU GPL 第 3 版
第 6 节要求的任何安装信息。

## 发布前检查表

- [x] 将当前根许可证文本和项目元数据替换为 GPL-3.0-or-later。
- [x] 为每个直接改编的源码文件添加 WinAuth 版权、上游文件、GPL 和
      2026-07-29 修改声明。
- [x] 更新 `THIRD-PARTY-NOTICES.md`，如实说明确认的 WinAuth 改编，以及 Codex
      作为开发工具的作用。
- [ ] 通过有记录且法律上充分的路径解决 AntdUI/SVG.NET Ms-PL 兼容阻塞项。
- [ ] 确认并记录应用图标的来源。
- [x] 增加并核验应用内便于访问的关于/致谢法律入口。
- [x] 已针对本次审查使用的最终还原结果重新生成依赖清单。
- [ ] 任一包版本变更后重新审查声明。
- [ ] 每个二进制发行物都打包全部仓库法律文件。
- [ ] 在每个二进制旁发布不可变的匹配源码 tag、完整 commit ID 和源码归档。
- [ ] 为再分发的非系统库和自包含 .NET 运行时组件提供持久、精确版本的源码访问。
- [x] 已核验本次审查的 self-contained `win-x64` 发布中自动复制的精确运行时
      声明；每次正式发布仍须重复核验。
- [ ] 检查最终 ZIP/安装包内容，而不只检查构建目录。
- [ ] 如需依赖进程隔离或特殊授权解决 GPL/Ms-PL 不兼容，应在发布前取得合格法律意见。
