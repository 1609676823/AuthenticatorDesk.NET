# WinAuth attribution and modification notice

[简体中文](WINAUTH-ATTRIBUTION.zh-CN.md)

AuthenticatorDesk includes code adapted from
[WinAuth](https://github.com/winauth/winauth) to support local OTP behavior and
migration to and from WinAuth 3.5 configuration files. The maintainer confirmed
that this implementation was produced with OpenAI Codex while referring to the
WinAuth source. It is therefore treated as adapted code under WinAuth's
GNU GPL version 3 or any later version, not as an independently implemented
file-format reader.

## Upstream work

- Project: WinAuth
- Author: Colin Mackie, with other WinAuth contributors
- Project copyright notice: Copyright (C) 2010-2017 Colin Mackie
- Upstream revision used for the compliance review:
  [`c57132f57b8a90e5219c628deb591f4603f27cb0`](https://github.com/winauth/winauth/tree/c57132f57b8a90e5219c628deb591f4603f27cb0)
- Upstream license:
  [GNU General Public License, version 3 or any later version](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/LICENSE)

The reviewed upstream files carry these notices:

- `Authenticator/Authenticator.cs` — Copyright (C) 2011 Colin Mackie
- `Authenticator/SteamAuthenticator.cs` — Copyright (C) 2015 Colin Mackie
- `Authenticator/BattleNetAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `Authenticator/TrionAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/WinAuthConfig.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/WinAuthAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/HotKey.cs` — Copyright (C) 2013 Colin Mackie
- `Authenticator/HOTPAuthenticator.cs` — Copyright (C) 2015 Colin Mackie

## AuthenticatorDesk adaptations

The following local files contain or organize the adapted implementation:

- `AuthenticatorDesk.NET/Services/WinAuthCryptoService.cs`
- `AuthenticatorDesk.NET/Services/WinAuthConfigService.cs`
- provider-specific portions of
  `AuthenticatorDesk.NET/Services/OtpService.cs`

AuthenticatorDesk contributors adapted and substantially modified this material
on 2026-07-29 for a .NET 10 Windows Forms application. The changes include
reorganizing the implementation around AuthenticatorDesk models and services,
supporting import and export of selected WinAuth 3.5 XML forms, using current
.NET cryptographic APIs where possible, adding validation and tests, and
omitting WinAuth network enrollment, time synchronization, Steam session, and
trade-confirmation features.

The legacy Blowfish engine used by the compatibility layer comes from the
Bouncy Castle C# API and retains its separate copyright and permissive license;
see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## License and relationship

Because WinAuth-derived code is integrated into the same program,
AuthenticatorDesk as a combined work is offered under
[GNU GPL version 3 or any later version](LICENSE.txt). Third-party components
remain under their own compatible terms and notices.

AuthenticatorDesk is an independent project. It is not an official WinAuth
release, and it is not affiliated with or endorsed by Colin Mackie or the
WinAuth project. Product and provider names are used only to describe
interoperability.

The AuthenticatorDesk contributors are grateful to Colin Mackie and the WinAuth
contributors for making WinAuth source code available and for the migration
path that work makes possible.

Before distributing a binary, read
[LICENSING-REVIEW.md](LICENSING-REVIEW.md). Changing the project license to GPL
addresses the WinAuth license requirement, but other recorded dependency and
asset blockers may still prevent a compliant release.
