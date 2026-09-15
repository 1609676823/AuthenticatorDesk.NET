# Licensing review

> Review date: 2026-07-29
>
> Release status: **BLOCKED — GPL-3.0-or-later has been selected for the
> confirmed WinAuth adaptation, but do not publish the current repository or
> binaries until the AntdUI/SVG.NET license-compatibility issue and application
> icon rights described below are resolved.**

[简体中文](LICENSING-REVIEW.zh-CN.md)

This document is an engineering-oriented open-source compliance review, not
legal advice. It records the evidence found in this repository and in the exact
dependency versions restored at the review date.

## Executive summary

The project author confirmed on 2026-07-29 that the WinAuth-compatible
implementation was created with OpenAI Codex after reviewing WinAuth source
code in order to support file migration and interoperability. AuthenticatorDesk
therefore treats the identified implementation as adapted from WinAuth and has
selected **GNU GPL version 3 or, at the recipient's option, any later version
(GPL-3.0-or-later)** for the combined work. WinAuth attribution, copyright
notices, and prominent modification notices must be retained.

The declared runtime and test dependencies use MIT, Apache-2.0, the Microsoft
Public License, the Bouncy Castle permissive license, or the Unicode License;
their separate notices remain recorded in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md). GPL licensing of the
combined work does not erase or replace those licenses.

The repository is **still not cleared for public release**. AntdUI 2.4.3 is a
direct, same-process dependency and its compiled assembly contains an SVG.NET
implementation built from source governed by the Microsoft Public License
(Ms-PL). The Free Software Foundation classifies Ms-PL as GPL-incompatible.
AuthenticatorDesk cannot unilaterally add a GPL linking exception on behalf of
WinAuth copyright holders. The AntdUI/SVG.NET issue must therefore be resolved
before conveying the current source combination or binaries. Application-icon
rights are a second, independent release blocker.

| Area | Status | Required action |
| --- | --- | --- |
| AuthenticatorDesk combined work | License selected; distribution blocked | Apply GPL-3.0-or-later, retain upstream notices, and resolve the AntdUI/SVG.NET and icon blockers before release. |
| Runtime NuGet dependencies | Notice-ready | Retain the notices and license texts already added. Re-audit on every version change. |
| Bouncy Castle Blowfish source | Notice-ready | Keep its existing file header and permissive license notice. |
| Third-party portions inside AntdUI | **Release blocker** | Keep all notices and replace or rebuild AntdUI without Ms-PL material, obtain all necessary compatibility permissions, or establish a genuinely separate-process design with qualified legal review. |
| WinAuth-compatible implementation | GPL path selected | Treat the identified code as adapted, retain Colin Mackie's notices, identify the upstream files and review commit, and state the 2026-07-29 modification date. |
| Project icon and media | **Release blocker** | The documentation screenshots were regenerated from this app with synthetic data; confirm and record authorship or a redistribution license for the application icon. |
| Binary release packaging | GPL work remains | In addition to legal files, provide matching Complete Corresponding Source, release tag/commit, source archive, and required source access for bundled runtime and non-system components. |

## Scope and method

The review covered:

- all tracked source and asset paths visible on 2026-07-29;
- direct package references in
  `AuthenticatorDesk.NET/AuthenticatorDesk.NET.csproj` and
  `AuthenticatorDesk.SmokeTests/AuthenticatorDesk.SmokeTests.csproj`;
- the resolved runtime graph in
  `AuthenticatorDesk.NET/obj/project.assets.json`;
- license metadata and included license files in the exact restored NuGet
  packages;
- copyright/license headers in the source corresponding to AntdUI 2.4.3;
- focused comparison of the WinAuth-compatible local implementation with the
  GPL-licensed WinAuth upstream source at commit
  `c57132f57b8a90e5219c628deb591f4603f27cb0`;
- the project author's 2026-07-29 statement that Codex reviewed WinAuth source
  while implementing migration compatibility; and
- the compiled AntdUI 2.4.3 assembly and corresponding source paths that carry
  SVG.NET under Ms-PL.

This was a source and metadata review. It is not a legal opinion, a complete
software-composition-analysis scan, or a substitute for evidence from the
people who wrote the code and created the assets.

## Main license

AuthenticatorDesk has selected GPL-3.0-or-later for the combined work:

```text
Copyright (C) 2026 AuthenticatorDesk contributors

This program is free software: you can redistribute it and/or modify it under
the terms of the GNU General Public License as published by the Free Software
Foundation, either version 3 of the License, or (at your option) any later
version.
```

The complete, unmodified GNU GPL version 3 text belongs in
[`LICENSE.txt`](LICENSE.txt). The `or later` choice must also appear in
project-level and affected source-file notices. Directly adapted files must
retain the relevant WinAuth copyright lines and prominent notices that
AuthenticatorDesk modified them on 2026-07-29.

GPL-3.0-or-later applies to the covered combined work when it is conveyed; it
does not relicense third-party components that recipients may also use under
their original MIT, Apache-2.0, Ms-PL, Bouncy Castle, or Unicode terms. Those
notices and license texts must remain intact. Selecting GPL resolves the
license path for the confirmed WinAuth adaptation, but it does not by itself
resolve the separate Ms-PL compatibility problem described below.

## Dependency inventory

### Runtime graph

| Component | Version | License | Evidence |
| --- | ---: | --- | --- |
| AntdUI | 2.4.3 | Apache-2.0 package metadata; compiled SVG.NET portions under Ms-PL | `AntdUI/2.4.3/antdui.nuspec`; repository commit `a9de0f8d3b65e5cac10a5b334336fe27df2185f7`; binary/source inspection described below |
| QRCoder | 1.8.0 | MIT | `QRCoder/1.8.0/qrcoder.nuspec` and `QRCoder/1.8.0/LICENSE.txt` |
| ZXing.Net.Bindings.Windows.Compatibility | 0.16.14 | Apache-2.0 | `ZXing.Net.Bindings.Windows.Compatibility/0.16.14/*.nuspec` |
| ZXing.Net | 0.16.11 | Apache-2.0 | resolved transitively; `ZXing.Net/0.16.11/*.nuspec` |

The complete Apache-2.0 text is in
[`THIRD-PARTY-LICENSES/Apache-2.0.txt`](THIRD-PARTY-LICENSES/Apache-2.0.txt).
No separate upstream `NOTICE` file was found in the audited AntdUI 2.4.3 or
ZXing.Net package/source material. The absence of such a file does not remove
the obligation to retain copyright and license notices.

### Build and test graph

Direct test dependencies are:

| Component | Version | License |
| --- | ---: | --- |
| coverlet.collector | 6.0.4 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |

The audited restore graph also contained:

- MIT: Microsoft.CodeCoverage 17.14.1,
  Microsoft.TestPlatform.ObjectModel 17.14.1,
  Microsoft.TestPlatform.TestHost 17.14.1, and Newtonsoft.Json 13.0.3;
- Apache-2.0: xunit.abstractions 2.0.3, xunit.analyzers 1.18.0,
  xunit.assert 2.9.3, xunit.core 2.9.3, and the xunit extensibility packages
  2.9.3.

These packages are not part of an ordinary application runtime release.
Anyone redistributing test tooling must preserve the exact packages' notices
and license files.

## Embedded Bouncy Castle source

`AuthenticatorDesk.NET/Services/Legacy/BlowfishEngine.cs` declares that it is
derived from Bouncy Castle C# API v1.7.0 and retains:

```text
Copyright (c) 2000-2011 The Legion Of The Bouncy Castle
```

The source comparison is consistent with that declaration. The applicable
permissive Bouncy Castle terms are reproduced in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md). Keep the header in the
source file as well as the notice in every source and binary distribution.

`AuthenticatorDesk.NET/Services/Legacy/BouncyCastleCompat.cs` is supporting
compatibility code. This review does not label that separate file a Bouncy
Castle derivative.

## Third-party portions carried inside AntdUI

The AntdUI 2.4.3 NuGet package declares Apache-2.0 and identifies source commit
`a9de0f8d3b65e5cac10a5b334336fe27df2185f7`. The corresponding source compiles
several internally carried third-party portions. The following exact source
paths and upstream license evidence were used.

### SVG.NET

- AntdUI paths:
  `src/AntdUI/Lib/SVG/*.cs` (139 files in the audited commit).
- AntdUI header:
  `THE SVG PROJECT IS AN OPENSOURCE LIBRARY LICENSED UNDER THE MS-PL License`
  and `COPYRIGHT (C) svg-net`.
- Upstream project: <https://github.com/svg-net/SVG>
- Upstream verification commit:
  `e74a8b0c9a1c52e02a3fe1580bde52453fd96a94`.
- Upstream license source: `license.txt`.
- Upstream copyright metadata source: `Source/Svg.csproj`.
- Exact copyright metadata:

  ```text
  © Copyright 2009 Microsoft. All Rights Reserved.
  © Copyright 2011 vvvv Group. All Rights Reserved.
  © Copyright 2021 SVG.NET Contributors
  ```

- License: Microsoft Public License (Ms-PL). The complete text is in
  [`THIRD-PARTY-LICENSES/Microsoft-Public-License.txt`](THIRD-PARTY-LICENSES/Microsoft-Public-License.txt).

### Vanara

- AntdUI paths: `src/AntdUI/Lib/Vanara/*.cs` (15 files in the audited commit).
- AntdUI header:
  `THE Vanara PROJECT IS AN OPENSOURCE LIBRARY LICENSED UNDER THE MIT License`
  and `COPYRIGHT (C) dahall`.
- Upstream project: <https://github.com/dahall/Vanara>
- Upstream verification commit:
  `1eb490b7bd09ec8982a66c31a73321353ed7e9c7`.
- Upstream license source: `LICENSE`.
- Exact notice: `Copyright (c) 2017 David Hall`.
- License: MIT; the full notice and terms are reproduced in
  [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

### ChineseCalendar

- AntdUI path: `src/AntdUI/Lib/ChineseCalendar/ChineseDate.cs`.
- AntdUI header:
  `THE ChineseCalendar PROJECT IS AN OPENSOURCE LIBRARY LICENSED UNDER THE MIT License`,
  `COPYRIGHT (C) lpz`, and
  `https://gitee.com/lipz89/ChineseCalendar`.
- Upstream license source:
  <https://github.com/lipz89/ChineseCalendar/blob/master/LICENSE>.
- Exact notice: `Copyright (c) 2020 lipz89`.
- License: MIT; the full notice and terms are reproduced in
  [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

### Ant Design Icons

- AntdUI data path: `src/AntdUI/Properties/Resources.resx`.
- AntdUI lookup path: `src/AntdUI/Lib/SvgDb.cs`.
- Upstream project: <https://github.com/ant-design/ant-design-icons>.
- Upstream verification commit:
  `6c18c63fbcfcf71dae09cd6bd6d63a48f8b688f1`.
- Upstream license source: `LICENSE`.
- Exact notice:
  `Copyright (c) 2018-present Ant UED, https://xtech.antfin.com/`.
- License: MIT; the full notice and terms are reproduced in
  [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

### Unicode Character Database

- AntdUI path:
  `src/AntdUI/Lib/GraphemeSplitter/GraphemeSplitter.Data.cs`.
- The generated ranges and file annotations identify Unicode Character
  Database grapheme data, including Unicode 15, 16, and 17 updates.
- Reference data:
  <https://www.unicode.org/Public/17.0.0/ucd/auxiliary/GraphemeBreakProperty.txt>.
- License source: <https://www.unicode.org/license.txt>.
- License: Unicode License V3. Its complete copyright and permission notice is
  reproduced in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

These additional notices are conservative compliance hardening based on the
source corresponding to the exact AntdUI package. Re-check them whenever
AntdUI is upgraded.

### GPL compatibility blocker: SVG.NET under Ms-PL

AntdUI's package metadata declares Apache-2.0, but the source corresponding to
AntdUI 2.4.3 compiles SVG.NET files governed by Ms-PL into `AntdUI.dll`. A
binary inspection of the restored package confirms that the assembly contains
the compiled SVG.NET implementation. AuthenticatorDesk references AntdUI
directly, calls its types throughout the UI, and loads it into the same process.
AntdUI is therefore not reasonably treated as an operating-system System
Library or as a merely adjacent, independent work in an aggregate.

The Free Software Foundation lists
[Ms-PL as GPL-incompatible](https://www.gnu.org/licenses/license-list.html#ms-pl)
and
[explains that linking a GPL work with a GPL-incompatible library requires permission](https://www.gnu.org/licenses/gpl-faq.html#GPLIncompatibleLibs)
from the relevant copyright holders. AuthenticatorDesk contributors may license
their own work, but they cannot unilaterally grant a GPL version 3 section 7
linking exception for WinAuth-derived material owned by upstream copyright
holders. Keeping the Ms-PL notice is necessary, but attribution alone does not
resolve the incompatibility.

The current source combination and binaries therefore remain blocked from
public distribution. Resolve this item through one of these documented paths:

1. replace AntdUI with a GPL-3.0-or-later-compatible UI dependency, or build a
   verified AntdUI variant from which the SVG.NET Ms-PL material has been
   removed and replaced with compatible code;
2. obtain explicit written compatibility permission from every rights holder
   whose permission is necessary for the combined distribution, and retain
   that evidence; or
3. redesign the GPL-covered compatibility functionality as a genuinely
   independent program communicating at arm's length through an ordinary file
   format or command-line interface, then obtain qualified legal review of the
   resulting boundary before distribution.

Merely putting the assemblies in separate files, loading AntdUI dynamically,
adding another notice, or declaring an exception only from AuthenticatorDesk
contributors does not close this blocker.

## WinAuth GPL provenance — confirmed adaptation and GPL path

### Upstream license

The WinAuth source at commit
[`c57132f57b8a90e5219c628deb591f4603f27cb0`](https://github.com/winauth/winauth/tree/c57132f57b8a90e5219c628deb591f4603f27cb0)
contains GPL headers granting use under GNU GPL version 3 or, at the user's
option, any later version:

- `Authenticator/Authenticator.cs` — Copyright (C) 2011 Colin Mackie;
- `Authenticator/SteamAuthenticator.cs` — Copyright (C) 2015 Colin Mackie;
- `Authenticator/BattleNetAuthenticator.cs` — Copyright (C) 2013 Colin Mackie;
- `Authenticator/TrionAuthenticator.cs` — Copyright (C) 2013 Colin Mackie.

Other upstream contributors retain copyright in any contributions they made.
The repository and commit links below identify the version used for this
compliance review; they do not claim that Codex necessarily reviewed only that
exact revision.

### Identified adapted scope

Following the project author's confirmation that Codex reviewed WinAuth source
while implementing compatibility, the following comparisons identify the
adapted scope relevant to attribution and licensing:

- Local `AuthenticatorDesk.NET/Services/WinAuthCryptoService.cs:40` uses the
  same set of legacy format parameters as upstream
  [`Authenticator.cs:60-75`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/Authenticator.cs#L60-L75):
  8-byte salt, 2,000 PBKDF2 iterations, a 256-byte derived key, and the
  `WINAUTH3` header.
- Local `WinAuthCryptoService.cs:45` through approximately line 315 implements
  protection flags, layered encryption/decryption, header/hash processing,
  PBKDF2-SHA1, and Blowfish/ISO10126 behavior corresponding to upstream
  [`Authenticator.cs:442-500`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/Authenticator.cs#L442-L500)
  and
  [`Authenticator.cs:948-1308`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/Authenticator.cs#L948-L1308).
- Local `AuthenticatorDesk.NET/Services/OtpService.cs:12` and lines 158-172
  contain the same Steam alphabet and five-character reduction behavior as
  upstream
  [`SteamAuthenticator.cs:713-761`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/SteamAuthenticator.cs#L713-L761).
- Local `OtpService.cs:175-184` implements the provider-specific Trion
  six-digit behavior corresponding to upstream
  [`TrionAuthenticator.cs:318-374`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/TrionAuthenticator.cs#L318-L374).
- Local `OtpService.cs:186-209` and lines 260-289 implement Battle.net restore
  code processing corresponding to upstream
  [`BattleNetAuthenticator.cs:776-812`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/BattleNetAuthenticator.cs#L776-L812)
  and
  [`BattleNetAuthenticator.cs:879-905`](https://github.com/winauth/winauth/blob/c57132f57b8a90e5219c628deb591f4603f27cb0/Authenticator/BattleNetAuthenticator.cs#L879-L905).

Algorithms, constants, file formats, and functional compatibility are not by
themselves proof that every local expression was copied. The focused comparison
did not find the broad verbatim correspondence present in the separately
acknowledged Bouncy Castle file. Nevertheless, the author's description of the
development process and the identified structural correspondence support the
conservative decision to treat these portions as adapted from WinAuth.

Both local files first appear in initial project commit
`7c06570b11a2251f90c43027b75a2905a5ff8eed`, authored on 2026-07-29. That date
is the relevant modification date for the initial AuthenticatorDesk adaptation.

### License treatment and attribution

The selected compliance path is:

- license AuthenticatorDesk as a combined work under GPL-3.0-or-later;
- preserve the relevant `Copyright (C) 2011, 2013, 2015 Colin Mackie` notices
  and any other upstream contributor notices;
- add prominent notices to directly adapted files identifying the applicable
  WinAuth source files, review commit, and the fact that AuthenticatorDesk
  adapted and substantially modified the code on 2026-07-29;
- reproduce the complete GNU GPL version 3 text and state the `or later`
  option in project and source-file notices; and
- describe OpenAI Codex as a development tool, not as an upstream author,
  copyright holder, or licensor.

AuthenticatorDesk is separately maintained and is not affiliated with,
endorsed by, or an official successor to WinAuth or its authors. The GPL and
attribution path resolves the former WinAuth provenance uncertainty; it does
not clear the separate AntdUI/SVG.NET compatibility blocker.

## Asset provenance

The following application-icon assets have no embedded or adjacent
authorship/license record:

- `AuthenticatorDesk.NET/Assets/AuthenticatorDesk.Icon.ico`;
- `AuthenticatorDesk.NET/Assets/AuthenticatorDesk.Icon.png`.

Both files first appear in initial project commit
`7c06570b11a2251f90c43027b75a2905a5ff8eed` and are unchanged from that
commit. Their SHA-256 hashes are
`13B2F04ED39BBEB1A7200BFFE20C1B4D591AC1AF993C1D34BDC6CA5A517ABD91`
for the ICO and
`8F3B8C79471F1326F2373E770B6A8F31437CA3BA8B8B7AECF1DF2490034BA97F`
for the PNG. The repository contains no source design, generation record,
source URL, authorship declaration, or written permission for either file.
Adding an asset in an initial commit does not establish who created it or what
redistribution rights apply.

Before release, record who created the application icon and confirm that the
project has the right to redistribute and modify it. Do not assume an image
carries the repository's license merely because it is stored in the repository.
Until that record exists, the icon remains an independent release blocker.

The images under `docs/images/` were regenerated during this review from the
current AuthenticatorDesk build. They use English UI text, synthetic
`example.test` accounts, public test secrets, and no personal paths or real
credentials. They are first-party application screenshots; provider names are
shown only to describe local compatibility.

## Release packaging obligations

The current application project file is configured to copy the repository's
legal files into `dotnet publish` output. This was verified with both
framework-dependent and self-contained `win-x64` publishes during this review.
The reviewed outputs contained the complete GPL version 3 text and the files
listed below. Every final binary release archive or installer must be checked
again to ensure that it retains them in a readily accessible location:

- `LICENSE.txt`;
- `THIRD-PARTY-NOTICES.md`;
- `THIRD-PARTY-LICENSES/Apache-2.0.txt`; and
- `THIRD-PARTY-LICENSES/Microsoft-Public-License.txt`.

Copying those files is necessary but not sufficient for a GPL binary release.
For each binary, provide equivalent access at no further charge to the
machine-readable Complete Corresponding Source for that exact build. The
preferred GitHub/Gitee release arrangement is:

- create an immutable source tag for the exact commit used to build the
  binary;
- state that tag and full commit ID next to the binary;
- attach a source archive generated from that commit rather than pointing only
  to a moving default branch; and
- keep the source available for as long as the binary remains available.

The source archive must include the AuthenticatorDesk source, solution and
project files, build and packaging scripts, interface definitions, language and
resource inputs, and other material needed to generate, install, run, and
modify the covered work. For bundled non-system libraries, provide the
applicable source or durable, explicit, version-matched access to it. A list of
package names, license copies, or a `dotnet restore` command alone does not
substitute for Complete Corresponding Source.

For a self-contained .NET publish, the `CopySelfContainedRuntimeLegalFiles`
target copies licenses and notices from the exact restored runtime-pack
versions into
`THIRD-PARTY-LICENSES/dotnet-runtime/<package-id>-<version>/`. Do not remove
that directory from the release archive. Also provide version-matched source
access for the runtime components actually redistributed. Re-verify the output
and source mapping after SDK, runtime, target-architecture, or project-file
changes.

For a framework-dependent publish, the runtime is not bundled, but the notices
and corresponding-source obligations for non-system DLLs distributed with the
application still apply. A framework-dependent release is generally simpler
to document than a self-contained release, but it does not resolve the current
AntdUI/SVG.NET incompatibility.

Before distribution, add a convenient **About**, **Credits**, or equivalent
legal entry in the interactive application. It should display the relevant
copyright notices, identify GPL-3.0-or-later, state the absence of warranty,
provide a way to view the complete license, credit the identified WinAuth
upstream files and Colin Mackie, and state that AuthenticatorDesk is not
affiliated with or endorsed by WinAuth. Matching Corresponding Source access
must be provided with the binary release as described above. This gives users
an accessible legal-notice path and preserves upstream attribution.

If object code is conveyed in or with a GPLv3 “User Product,” also assess and
provide any Installation Information required by GPL version 3 section 6.

## Pre-release checklist

- [x] Replace the current root license text and project metadata with
      GPL-3.0-or-later.
- [x] Add WinAuth copyright, upstream-file, GPL, and 2026-07-29 modification
      notices to every directly adapted source file.
- [x] Update `THIRD-PARTY-NOTICES.md` so it candidly describes the confirmed
      WinAuth adaptation and Codex's role as a development tool.
- [ ] Resolve the AntdUI/SVG.NET Ms-PL compatibility blocker through a
      documented, legally sufficient path.
- [ ] Confirm and record application-icon provenance.
- [x] Add and verify an accessible in-application About/Credits legal entry.
- [x] Re-run dependency inventory after the final restore used for this review.
- [ ] Re-audit notices after any package version change.
- [ ] Package all repository legal files with every binary release.
- [ ] Publish an immutable matching source tag, full commit ID, and source
      archive next to every binary.
- [ ] Provide durable, exact-version source access for redistributed non-system
      libraries and self-contained .NET runtime components.
- [x] Verify the automatically copied .NET runtime notices in the reviewed
      self-contained `win-x64` publish; repeat this check for every release.
- [ ] Inspect the final ZIP/installer contents, not only the build directory.
- [ ] Obtain qualified legal review before relying on a process-separation or
      special-permission solution to the GPL/Ms-PL incompatibility.
