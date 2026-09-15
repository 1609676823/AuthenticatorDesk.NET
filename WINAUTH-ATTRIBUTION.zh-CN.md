# WinAuth 归属与修改声明

[English](WINAUTH-ATTRIBUTION.md)

AuthenticatorDesk 为支持本地 OTP 行为以及 WinAuth 3.5 配置文件的迁移，包含了改编自
[WinAuth](https://github.com/winauth/winauth) 的代码。维护者已经确认：该实现由
OpenAI Codex 在参考 WinAuth 源代码的情况下生成。因此，本项目将其作为受 WinAuth
GNU GPL 第 3 版或任何更高版本约束的改编代码处理，而不再主张它是完全独立实现的
文件格式读取器。

## 上游作品

- 项目：WinAuth
- 作者：Colin Mackie 及其他 WinAuth 贡献者
- 项目版权声明：Copyright (C) 2010-2017 Colin Mackie
- 本次合规审查对应的上游版本：
  [`c57132f57b8a90e5219c628deb591f4603f27cb0`](https://github.com/winauth/winauth/tree/c57132f57b8a90e5219c628deb591f4603f27cb0)
- 上游许可证：
  [GNU General Public License 第 3 版或任何更高版本](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/LICENSE)

已审查的上游文件带有以下声明：

- `Authenticator/Authenticator.cs` — Copyright (C) 2011 Colin Mackie
- `Authenticator/SteamAuthenticator.cs` — Copyright (C) 2015 Colin Mackie
- `Authenticator/BattleNetAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `Authenticator/TrionAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/WinAuthConfig.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/WinAuthAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/HotKey.cs` — Copyright (C) 2013 Colin Mackie
- `Authenticator/HOTPAuthenticator.cs` — Copyright (C) 2015 Colin Mackie

## AuthenticatorDesk 的改编

以下本地文件包含或组织了相关改编实现：

- `AuthenticatorDesk.NET/Services/WinAuthCryptoService.cs`
- `AuthenticatorDesk.NET/Services/WinAuthConfigService.cs`
- `AuthenticatorDesk.NET/Services/OtpService.cs` 中面向特定服务商的部分

AuthenticatorDesk 贡献者于 2026-07-29 将这些材料改编并实质性修改到 .NET 10
Windows Forms 应用中。修改包括：围绕 AuthenticatorDesk 模型和服务重新组织实现；
支持部分 WinAuth 3.5 XML 格式的导入与导出；在可行处使用当前 .NET 加密 API；
增加校验和测试；不包含 WinAuth 的网络注册、网络校时、Steam 会话和交易确认功能。

兼容层使用的旧式 Blowfish engine 来自 Bouncy Castle C# API，并保留其独立版权和
宽松许可证；详见
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 许可证与项目关系

由于 WinAuth 改编代码被集成在同一程序中，AuthenticatorDesk 组合作品采用
[GNU GPL 第 3 版或任何更高版本](LICENSE.txt)发布。第三方组件仍保留各自适用的
兼容许可证和声明。

AuthenticatorDesk 是独立项目，不是 WinAuth 官方版本，也不隶属于 Colin Mackie
或 WinAuth 项目，未获得其背书。产品和服务商名称仅用于说明互操作性。

AuthenticatorDesk 贡献者感谢 Colin Mackie 和 WinAuth 贡献者公开源代码，以及这些
工作为用户迁移数据提供的基础。

分发二进制文件前必须阅读
[LICENSING-REVIEW.zh-CN.md](LICENSING-REVIEW.zh-CN.md)。切换到 GPL 解决了
WinAuth 代码的许可要求，但审查记录中的其他依赖和资源阻塞项仍可能妨碍合规发布。
