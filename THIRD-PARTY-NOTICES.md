# Third-party notices

AuthenticatorDesk as a combined program is offered under GNU GPL version 3 or
any later version because it includes code adapted from WinAuth. The complete
project license is in [`LICENSE.txt`](LICENSE.txt), and the WinAuth source and
modification record is in
[`WINAUTH-ATTRIBUTION.md`](WINAUTH-ATTRIBUTION.md).

That project-level license does not erase or replace third-party copyrights,
licenses, patent terms, or attribution requirements. The works identified below
remain subject to their own notices. The unresolved compatibility findings in
[`LICENSING-REVIEW.md`](LICENSING-REVIEW.md) must be addressed before public
distribution.

This file and the referenced files under
[`THIRD-PARTY-LICENSES`](THIRD-PARTY-LICENSES/) must be retained in source and
binary distributions of AuthenticatorDesk.

## Runtime dependencies

### AntdUI 2.4.3

Project: <https://gitee.com/AntdUI/AntdUI>

Copyright © Tom 2024-2030

Licensed under the Apache License, Version 2.0. A complete copy is provided in
[`THIRD-PARTY-LICENSES/Apache-2.0.txt`](THIRD-PARTY-LICENSES/Apache-2.0.txt).

AntdUI's corresponding source contains additional third-party portions. Their
notices appear under [Components carried inside AntdUI](#components-carried-inside-antdui).

### QRCoder 1.8.0

Project: <https://github.com/Shane32/QRCoder>

The MIT License (MIT)

Copyright (c) 2013-2025 Raffael Herrmann

Copyright (c) 2024-2025 Shane Krueger

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

### ZXing.Net.Bindings.Windows.Compatibility 0.16.14 and ZXing.Net 0.16.11

Project: <https://github.com/micjahn/ZXing.Net>

Copyright (C) ZXing authors and ZXing.Net authors

Licensed under the Apache License, Version 2.0. A complete copy is provided in
[`THIRD-PARTY-LICENSES/Apache-2.0.txt`](THIRD-PARTY-LICENSES/Apache-2.0.txt).

## Embedded source

### Bouncy Castle C# API — Blowfish engine

The WinAuth 3.5-compatible import/export layer includes a narrowly scoped
derivative of `BlowfishEngine.cs` from the Bouncy Castle C# API v1.7.0. It is
retained because the legacy format uses a 256-byte Blowfish key that current
Bouncy Castle releases correctly reject as non-standard. The derivative is
used only while importing or exporting password-protected WinAuth XML.

Copyright (c) 2000-2011 The Legion Of The Bouncy Castle
(<http://www.bouncycastle.org>)

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sub license, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Components carried inside AntdUI

The AntdUI 2.4.3 package declares Apache-2.0. Its corresponding source at
commit `a9de0f8d3b65e5cac10a5b334336fe27df2185f7` also contains or compiles the
following third-party portions. These notices are reproduced independently of
AntdUI's package-level license statement.

### SVG.NET

Project: <https://github.com/svg-net/SVG>

© Copyright 2009 Microsoft. All Rights Reserved.

© Copyright 2011 vvvv Group. All Rights Reserved.

© Copyright 2021 SVG.NET Contributors

Licensed under the Microsoft Public License (Ms-PL). A complete copy is
provided in
[`THIRD-PARTY-LICENSES/Microsoft-Public-License.txt`](THIRD-PARTY-LICENSES/Microsoft-Public-License.txt).

The [Free Software Foundation classifies
Ms-PL](https://www.gnu.org/licenses/license-list.html#ms-pl) as
GPL-incompatible. Because SVG.NET is compiled into the AntdUI assembly that
AuthenticatorDesk links in the same process, this is an unresolved
distribution blocker, not a notice-only item; see
[`LICENSING-REVIEW.md`](LICENSING-REVIEW.md).

### Vanara

Project: <https://github.com/dahall/Vanara>

MIT License

Copyright (c) 2017 David Hall

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

### ChineseCalendar

Project: <https://gitee.com/lipz89/ChineseCalendar>

MIT License

Copyright (c) 2020 lipz89

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

### Ant Design Icons

Project: <https://github.com/ant-design/ant-design-icons>

MIT LICENSE

Copyright (c) 2018-present Ant UED, <https://xtech.antfin.com/>

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

### Unicode Character Database data

AntdUI's grapheme-splitting source contains generated property data derived
from the Unicode Character Database. The following notice applies to that
data.

UNICODE LICENSE V3

COPYRIGHT AND PERMISSION NOTICE

Copyright © 1991-2026 Unicode, Inc.

NOTICE TO USER: Carefully read the following legal agreement. BY
DOWNLOADING, INSTALLING, COPYING OR OTHERWISE USING DATA FILES, AND/OR
SOFTWARE, YOU UNEQUIVOCALLY ACCEPT, AND AGREE TO BE BOUND BY, ALL OF THE
TERMS AND CONDITIONS OF THIS AGREEMENT. IF YOU DO NOT AGREE, DO NOT
DOWNLOAD, INSTALL, COPY, DISTRIBUTE OR USE THE DATA FILES OR SOFTWARE.
Permission is hereby granted, free of charge, to any person obtaining a
copy of data files and any associated documentation (the "Data Files") or
software and any associated documentation (the "Software") to deal in the
Data Files or Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, and/or sell
copies of the Data Files or Software, and to permit persons to whom the
Data Files or Software are furnished to do so, provided that either (a)
this copyright and permission notice appear with all copies of the Data
Files or Software, or (b) this copyright and permission notice appear in
associated Documentation.
THE DATA FILES AND SOFTWARE ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF
THIRD PARTY RIGHTS.
IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE
BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES,
OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THE DATA
FILES OR SOFTWARE.
Except as contained in this notice, the name of a copyright holder shall
not be used in advertising or otherwise to promote the sale, use or other
dealings in these Data Files or Software without prior written
authorization of the copyright holder.

License source: <https://www.unicode.org/license.txt>

## Development and test dependencies

The following packages are used to build or test the project and are not part
of an ordinary AuthenticatorDesk runtime distribution:

| Package | Version | License |
| --- | ---: | --- |
| coverlet.collector | 6.0.4 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |

Their normal transitive restore graph includes Microsoft.CodeCoverage,
Microsoft.TestPlatform.ObjectModel, Microsoft.TestPlatform.TestHost,
Newtonsoft.Json, xunit.abstractions, xunit.analyzers, xunit.assert,
xunit.core, and xunit.extensibility packages. Anyone redistributing build or
test tooling must also preserve the license and notice files from the exact
NuGet packages redistributed.

## WinAuth-derived compatibility code

Portions of AuthenticatorDesk's WinAuth compatibility implementation were
adapted with assistance from OpenAI Codex after reviewing source code from the
WinAuth project. The purpose is local OTP compatibility and migration to and
from selected WinAuth 3.5 configuration-file forms.

Upstream project: <https://github.com/winauth/winauth>

Reference revision used for this compliance review:
[`c57132f57b8a90e5219c628deb591f4603f27cb0`](https://github.com/winauth/winauth/tree/c57132f57b8a90e5219c628deb591f4603f27cb0)

Copyright (C) 2010-2017 Colin Mackie.

Additional WinAuth contributors retain copyright in their contributions. The
reviewed upstream file notices include:

- `Authenticator/Authenticator.cs` — Copyright (C) 2011 Colin Mackie
- `Authenticator/SteamAuthenticator.cs` — Copyright (C) 2015 Colin Mackie
- `Authenticator/BattleNetAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `Authenticator/TrionAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/WinAuthConfig.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/WinAuthAuthenticator.cs` — Copyright (C) 2013 Colin Mackie
- `WinAuth/HotKey.cs` — Copyright (C) 2013 Colin Mackie
- `Authenticator/HOTPAuthenticator.cs` — Copyright (C) 2015 Colin Mackie

The adapted implementation is principally in:

- `AuthenticatorDesk.NET/Services/WinAuthCryptoService.cs`;
- `AuthenticatorDesk.NET/Services/WinAuthConfigService.cs`; and
- provider-specific portions of
  `AuthenticatorDesk.NET/Services/OtpService.cs`.

These files were adapted and substantially modified for AuthenticatorDesk on
2026-07-29 by the AuthenticatorDesk contributors. The adapted portions and
AuthenticatorDesk as a combined work are licensed under GNU GPL version 3 or,
at the recipient's option, any later version. See [`LICENSE.txt`](LICENSE.txt)
for the complete terms and warranty disclaimer.

OpenAI Codex is identified as a development tool, not as an upstream author,
copyright holder, or licensor. AuthenticatorDesk is independently maintained;
it is not an official WinAuth release and is not affiliated with or endorsed by
WinAuth or Colin Mackie.

## .NET runtime distributions

Framework-dependent releases rely on the user's installed .NET runtime. For a
self-contained publish, the project target copies the license and third-party
notice files from the exact restored Microsoft.NETCore.App and
Microsoft.WindowsDesktop.App runtime packs into
`THIRD-PARTY-LICENSES/dotnet-runtime/<package-id>-<version>/`. Verify those
files are present in the final archive.
