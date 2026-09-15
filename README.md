# AuthenticatorDesk

**A local-first open-source OTP authenticator for Windows.**

[English](README.md) | [简体中文](README.zh-CN.md)

[Security](SECURITY.md) · [Privacy](PRIVACY.md) ·
[Contributing](CONTRIBUTING.md) · [License](LICENSE.txt) ·
[Licensing review](LICENSING-REVIEW.md) ·
[WinAuth attribution](WINAUTH-ATTRIBUTION.md)

AuthenticatorDesk manages and generates one-time passwords on your own Windows
device. It supports standards-based TOTP and HOTP, several provider-oriented
profiles, QR and `otpauth` migration, encrypted backups, and WinAuth
compatibility. The application source does not implement a cloud account,
telemetry client, synchronization service, or network time synchronization.

> **Important security notice**
>
> - A new vault starts in **Portable mode**. Its payload is encrypted, but the
>   encryption key is stored in the same `vault.json` file. Portable mode is
>   convenient for copying the data directory; it **does not protect the vault
>   from someone who obtains the file**. Set a master password or enable Windows
>   account protection if file-level access control is required.
> - AuthenticatorDesk has **not received an independent security audit**. Review
>   the [security model](#security-model) and keep a tested backup before relying
>   on it for important accounts.
> - Google, Microsoft, Okta, Guild Wars, Steam, Battle.net, and Trion names refer
>   to local OTP profiles and compatibility behavior. They are **not official
>   integrations, push-approval clients, or endorsements** by those providers.

> **License and release status**
>
> AuthenticatorDesk includes code adapted from WinAuth and is therefore
> licensed as a combined work under
> [GNU GPL version 3 or any later version](LICENSE.txt). The upstream author and
> modifications are documented in
> [WINAUTH-ATTRIBUTION.md](WINAUTH-ATTRIBUTION.md). Public distribution remains
> **blocked** until the AntdUI/Ms-PL compatibility issue and application-icon
> rights recorded in [LICENSING-REVIEW.md](LICENSING-REVIEW.md) are resolved.

![AuthenticatorDesk dashboard in English dark mode](docs/images/dashboard-dark.png)

## Highlights

- Standards-based TOTP and HOTP generation with Base32 secrets.
- SHA-1, SHA-256, and SHA-512; 4–10 digits; configurable TOTP periods and HOTP
  counters.
- Steam-style five-character codes, Battle.net profile and restore-code display,
  and a Trion/Glyph-compatible profile.
- Search, favorites, time/counter filters, notes, accent colors, and manual
  ordering.
- QR image, clipboard, `otpauth://`, and Google Authenticator migration imports.
- Password-encrypted native backups and WinAuth 3.5 XML import/export.
- Three vault protection modes: Portable, Windows account, and master password.
- Auto-lock, lock-on-minimize, code masking, and timed clearing for copied
  verification codes.
- Global hotkeys that copy, type, or show a code.
- System, light, dark, and Windows high-contrast-aware appearance.
- Tray operation, start-minimized behavior, always-on-top, remembered window
  layout, and optional Windows startup registration.
- Ten built-in languages plus validated external UTF-8 JSON language packs.

## Authenticator types

| Type | Local behavior | Important boundary |
| --- | --- | --- |
| Generic TOTP | RFC 6238-style time-based OTP with configurable algorithm, digits, period, and imported time offset | No network clock correction is performed |
| HOTP | RFC 4226-style counter-based OTP with an explicit **Generate next** action | Advancing the counter is saved immediately |
| Google | TOTP profile with Google-oriented labeling and migration detection | No Google account or push integration |
| Microsoft | TOTP profile with Microsoft-oriented labeling | No Microsoft account or push integration |
| Okta Verify | TOTP profile with Okta-oriented labeling | No Okta API or push integration |
| Guild Wars 2 | TOTP profile with Guild Wars-oriented labeling | Not affiliated with ArenaNet |
| Steam Guard | Five-character Steam-style code generation | Imported Steam metadata is preserved for compatibility; no Steam session management |
| Battle.net | Eight-digit profile with optional local restore-code display when the required serial and secret are available | Provider behavior may change; keep the provider's own recovery method |
| Trion / Glyph | Six-digit Trion-style code generation | Compatibility profile only |

Provider presets apply known local parameters and preserve selected migration
metadata. They do not enroll an account with a provider, approve sign-in
requests, synchronize tokens, or replace provider recovery options.

## Dashboard and entry editor

The dashboard refreshes visible time-based codes and countdowns locally. Entries
can be searched by account name, issuer, or notes, filtered by favorites or OTP
type, reordered, edited, exported, and deleted. HOTP cards expose their current
counter and a deliberate next-code action.

The entry editor supports:

- account name, issuer, shared secret, and authenticator profile;
- SHA algorithm, digit count, refresh period, and HOTP counter;
- provider fields such as serial, device ID, and preserved Steam data;
- notes, favorite state, accent color, code masking preferences, and hotkeys;
- loading a Base32 secret, an `otpauth` URI, or a QR image from the clipboard or
  disk;
- a local verification preview so the generated code can be compared manually
  with the service before saving.

![AuthenticatorDesk entry editor in English dark mode](docs/images/entry-editor-dark.png)

## Import and export compatibility

| Format or source | Import | Export | Notes |
| --- | :---: | :---: | --- |
| AuthenticatorDesk `.authdesk` backup | Yes | Yes | Password-encrypted entry backup; application settings are not included |
| WinAuth 3.5 XML, unprotected | Yes | Yes | Plaintext files contain reusable secrets |
| WinAuth 3.5 XML, password-protected | Yes | Yes | Legacy compatibility encryption; prefer `.authdesk` for new secure backups |
| WinAuth Windows-user or machine protection | Context-dependent | No | Import works only when Windows can decrypt the original DPAPI layer |
| WinAuth per-authenticator passwords | Yes | No | The importer can prompt separately for protected entries |
| WinAuth YubiKey slot protection | No | No | Explicitly rejected because the required YubiKey flow is not implemented |
| `otpauth://totp` and `otpauth://hotp` | Yes | Yes | Supports common fields plus AuthenticatorDesk provider extensions |
| Google Authenticator `otpauth-migration://` | Yes | No | Imports the accounts contained in one payload; no automatic multi-QR batch assembly |
| QR image | Yes | Yes | Reads image files or a clipboard image; there is no camera capture |
| Base32 secret | Yes | N/A | Can be pasted or loaded from the clipboard in the editor |
| Plaintext list of OTP URIs | Yes | Yes | Every exported URI contains the corresponding secret |

Supported QR image inputs include PNG, JPEG, BMP, and GIF. A text import may
contain multiple `otpauth` lines. When importing into the main vault, duplicates
are detected by authenticator type plus normalized secret; two accounts with
the same type and secret are treated as duplicates even if their names differ.

WinAuth compatibility intentionally preserves selected legacy fields, including
skin names, Steam data, and advanced hotkey script text. AuthenticatorDesk does
not execute WinAuth advanced scripts. Imported WinAuth application settings are
limited: tray and Windows-startup preferences are considered, while local
always-on-top and window-layout choices remain under AuthenticatorDesk control.

> **Secret-bearing exports**
>
> A QR code, copied `otpauth` URI, URI text export, or unprotected WinAuth XML
> can recreate the authenticator. Store and transmit these outputs as carefully
> as the original enrollment secret.

## Security model

All vault modes encrypt the serialized vault payload with AES-256-GCM using a
random 96-bit nonce, a 128-bit authentication tag, and versioned associated
data. The practical protection depends on how the 256-bit vault key is
protected:

| Mode | Key protection | Portability | What it protects against |
| --- | --- | --- | --- |
| **Portable** (default) | The random vault key is stored in the same `vault.json` envelope as the ciphertext | The data directory can be copied and opened directly | Accidental plaintext disclosure and undetected modification, **not theft of the vault file** |
| **Windows account** | The random vault key is protected with Windows DPAPI for the current user | Normally tied to the Windows user context that protected it | Offline access to the copied vault without that DPAPI context |
| **Master password** | PBKDF2-HMAC-SHA-256, 600,000 iterations, 128-bit random salt, producing the AES key | Can be opened on another compatible Windows system with the password | Offline access subject to the strength and secrecy of the password |

There is no password-reset or recovery-key mechanism. If a master password is
forgotten, the encrypted vault cannot be recovered by the application.

### Native encrypted backups

The `.authdesk` format uses PBKDF2-HMAC-SHA-256 with 600,000 iterations and
AES-256-GCM. It contains cloned authenticator entries and provider metadata, but
not application settings. Use a strong, unique backup password and test an
import before treating a backup as reliable.

Password-protected WinAuth export uses WinAuth's legacy Blowfish-based format
for interoperability. It should be treated as a compatibility path, not as a
modern replacement for the native encrypted backup.

### Locking and clipboard behavior

- Manual lock saves the vault first, unregisters global hotkeys, attempts to
  clear an unchanged copied verification code, drops in-session key material,
  and replaces the active entry collection.
- Auto-lock is based on keyboard and mouse messages received by this
  application. It is not the same as Windows-wide idle detection.
- Only verification codes copied through the dashboard or a copy hotkey use the
  configurable clear timer. Shared secrets, restore codes, and copied OTP URIs
  use their dialogs' normal clipboard operation and are **not** automatically
  cleared by that timer.
- Managed .NET strings cannot be guaranteed to be erased immediately from
  process memory. The implementation zeroes sensitive byte arrays where
  practical, but it does not claim secure-memory guarantees.
- The current per-entry option labeled **Confirm before each reveal** masks the
  card until Reveal is selected; it does not request the master password or
  Windows re-authentication. Copying a code also does not perform per-entry
  re-authentication.

![AuthenticatorDesk security settings in English dark mode](docs/images/security-settings-dark.png)

### Files and recovery artifacts

Normal vault saves use a temporary file and keep the previous encrypted
`vault.json.bak`. Recovery artifacts are not restored automatically. Changing
the data directory copies and switches to a new vault; it does not delete the
old vault. Review both locations and any `.bak` files when decommissioning a
device or directory.

For security-sensitive deployments, review the source and threat model, use a
master password or Windows account protection, restrict filesystem access, keep
encrypted backups, and validate the behavior in the intended Windows
environment. See the detailed
[security model](docs/SECURITY-MODEL.md) and
[Security limitations](#security-limitations) before use.

## Data locations and portability

AuthenticatorDesk is portable-first rather than installer-first:

| Data | Default or fallback location |
| --- | --- |
| Main vault | `vault.json` beside the executable |
| Automatic vault backup | `vault.json.bak` beside the active vault |
| Error logs | `logs` inside the active data directory |
| Program-side location file | `AuthenticatorDesk.paths.json` beside the executable |
| Per-user location fallback | `%LocalAppData%\AuthenticatorDesk\locations\<program-id>.json` |
| UI language preference | `%LocalAppData%\AuthenticatorDesk\ui-preferences.json` |
| Per-user language packs | `%LocalAppData%\AuthenticatorDesk\Languages` |
| Optional startup registration | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |

On a new installation, the application attempts to use the executable
directory. If that directory is not writable, it asks the user to choose a data
directory. A configured location must already contain a valid `vault.json` when
selected as an existing vault location.

Older data found at `%LocalAppData%\AuthenticatorDesk\vault.json` may be migrated
to the program directory when no higher-priority vault or location file exists.
The matching legacy source is archived with a
`vault.json.migrated-<timestamp>-<id>.bak` name rather than silently deleted.

The UI language preference remains per-user even when the vault is portable.
Enabling Windows startup also writes a per-user registry value, so Portable mode
does not mean that the application leaves no state outside its directory.

The current vault envelope and encrypted backup formats are version 1. The
payload contains a schema version, entries, settings, and timestamps, but the
project does not currently promise forward compatibility with future,
unreleased schema versions.

## Localization

The application follows the current Windows display language by default and
falls back to English. The built-in language packs are:

- English
- Simplified Chinese
- Traditional Chinese
- Japanese
- Korean
- German
- French
- Spanish
- Brazilian Portuguese
- Russian

Language changes are applied after an automatic application restart. External
UTF-8 JSON packs can be placed in `Languages` beside the executable or in the
per-user language-pack directory. The loader validates file size, locale,
string limits, and .NET format placeholders; invalid or missing strings fall
back to the embedded English baseline.

See the [English language-pack contribution guide](AuthenticatorDesk.NET/Languages/README.md),
its [Simplified Chinese version](AuthenticatorDesk.NET/Languages/README.zh-CN.md),
and the [JSON Schema](AuthenticatorDesk.NET/Languages/language-pack.schema.json).
The current visual layout is designed and tested for left-to-right languages;
complete right-to-left layout support is not yet claimed.

## Installation

### Published binary

GitHub Actions builds ZIP packages for Windows x86, x64 and ARM64 (self-contained
by default), plus a portable package requiring the Desktop Runtime. See the
[release workflow guide](.github/WORKFLOWS.md). No installer, automatic updater or
code-signing configuration is provided. When a maintainer publishes a binary archive:

1. Verify the archive and any checksum or signature described by that release.
2. Extract the complete archive to a trusted directory.
3. Keep the accompanying `Languages` directory and license files with the
   executable.
4. Start `AuthenticatorDesk.exe`. Administrator privileges are not requested.
5. On first start, choose a writable data directory if the executable directory
   cannot be used.
6. Open **Settings → Security** and choose a protection mode appropriate for
   your threat model before adding important accounts.

Runtime requirements depend on whether that specific release is
framework-dependent or self-contained. Check its release notes rather than
assuming that the .NET runtime is bundled.

The portable package also includes `AuthenticatorDesk.exe` for double-click
launch after full extraction. Its main DLL remains AnyCPU, while the EXE matches
the build SDK platform and architecture (currently Windows x64 in GitHub Actions);
the EXE is not CPU-independent. `Start-AuthenticatorDesk.cmd` or
`dotnet AuthenticatorDesk.dll` is available as a fallback. All portable launch
methods require the .NET 10 Windows Desktop Runtime matching the process architecture.

### CET startup compatibility

For machines reporting `Your Windows doesn't fully support CET`, the project
keeps .NET 10 and sets `CetCompat=false` for the generated EXE in normal builds
and framework-dependent, self-contained, and single-file publishes. This follows
[Microsoft's CET opt-out guidance](https://learn.microsoft.com/en-us/dotnet/core/compatibility/interop/9.0/cet-support):
the application gives up CET hardware-enforced stack protection for compatibility;
it does not change Windows security settings or add support for Windows 7/8.

Use a newly built package and start `AuthenticatorDesk.exe` directly. A
self-contained package matching the machine's architecture is recommended for
end users. The CMD fallback and `dotnet AuthenticatorDesk.dll` use the system
`dotnet` host, whose CET behavior is not changed by this project setting.

This setting does not repair Visual Studio/Roslyn `ServiceHub` crashes, including
exit code `0x80131506`. If the build tools fail, update Windows and Visual Studio,
or publish the application on a working build machine.

### Build from source

Prerequisites:

- Windows with desktop APIs available;
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0);
- Git, if cloning rather than downloading a source archive.

From a PowerShell prompt in the repository root:

```powershell
dotnet restore .\AuthenticatorDesk.NET.slnx
dotnet build .\AuthenticatorDesk.NET.slnx -c Release --no-restore
dotnet run --project .\AuthenticatorDesk.NET\AuthenticatorDesk.NET.csproj -c Release
```

The project targets `net10.0-windows` and Windows Forms. It is not a
cross-platform desktop application.

## Tests

The smoke-test project covers, among other areas:

- Base32 and RFC OTP vectors;
- `otpauth` and Google migration parsing;
- QR encode/decode;
- native backup and vault protection transitions;
- WinAuth fixtures, encryption layers, and per-entry passwords;
- storage-location selection and legacy migration;
- built-in language-pack completeness and fallbacks;
- responsive WinForms layout, themes, high-contrast behavior, and editor
  validation.

Run it on Windows:

```powershell
dotnet test .\AuthenticatorDesk.SmokeTests\AuthenticatorDesk.SmokeTests.csproj `
  -c Release --no-build
```

Several tests construct Windows Forms controls and run in STA threads. Use a
Windows runner with the required desktop APIs; the suite is not intended to run
as a platform-neutral test project.

## Publishing

A framework-dependent publish with an EXE, matching VS portable mode, can be created with:

```powershell
dotnet publish .\AuthenticatorDesk.NET\AuthenticatorDesk.NET.csproj `
  -c Release --self-contained false -p:UseAppHost=true -o .\artifacts\publish
```

An example self-contained x64 publish is:

```powershell
dotnet publish .\AuthenticatorDesk.NET\AuthenticatorDesk.NET.csproj `
  -c Release -r win-x64 --self-contained true `
  -o .\artifacts\publish-win-x64
```

These commands create individual publish directories. The complete test,
packaging and GitHub Release process is described in the
[release workflow and BAT guide](.github/WORKFLOWS.md). Deployment defaults and
architectures are configured in [release-settings.psd1](.github/release-settings.psd1);
the release version comes from `AppVersion` in `AuthenticatorDesk.NET/Program.cs`.
Before publishing an open-source release:

1. resolve every blocker in
   [LICENSING-REVIEW.md](LICENSING-REVIEW.md), including the AntdUI/Ms-PL
   compatibility issue and application-icon rights;
2. build and run the smoke tests on the supported Windows environments;
3. manually exercise vault creation, password and DPAPI modes, import/export,
   tray behavior, and an upgrade from the previous release;
4. include the external `Languages` files, [GNU GPL](LICENSE.txt),
   [WinAuth attribution](WINAUTH-ATTRIBUTION.md), and
   [third-party notices](THIRD-PARTY-NOTICES.md);
5. give binary recipients equivalent no-charge access to the complete
   Corresponding Source for the exact release, including project and build
   files, and place clear source directions next to the binary download;
6. document whether the release is framework-dependent or self-contained and
   which architecture it targets;
7. publish checksums and describe the actual signing status without implying a
   signature that is not present.

## Dependencies

Runtime NuGet dependencies are deliberately small:

| Package | Version in the project | Purpose | License |
| --- | ---: | --- | --- |
| AntdUI | 2.4.3 | Windows Forms UI controls and theming | Apache-2.0 package metadata; embedded SVG.NET portions use Ms-PL |
| QRCoder | 1.8.0 | QR generation | MIT |
| ZXing.Net.Bindings.Windows.Compatibility | 0.16.14 | QR image decoding | Apache-2.0 |
| ZXing.Net | 0.16.11, transitive | Barcode decoding core | Apache-2.0 |

AntdUI 2.4.3 also carries SVG.NET source under the Microsoft Public License
(Ms-PL). The [Free Software Foundation classifies
Ms-PL](https://www.gnu.org/licenses/license-list.html#ms-pl) as
GPL-incompatible.
Because AuthenticatorDesk links AntdUI in the same process, this is a release
blocker, not merely an attribution item; see
[LICENSING-REVIEW.md](LICENSING-REVIEW.md).

The WinAuth compatibility layer also contains a narrowly scoped derivative of
the Bouncy Castle C# API 1.7 Blowfish engine. See
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for attribution and applicable
terms.

## Repository layout

```text
AuthenticatorDesk.NET.slnx
├─ AuthenticatorDesk.NET/              Windows Forms application
│  ├─ Languages/                       Built-in and distributable JSON packs
│  ├─ Localization/                    Pack loading, validation, and fallback
│  ├─ Models/                          Vault, settings, and entry models
│  ├─ Services/                        OTP, crypto, storage, QR, hotkeys, WinAuth
│  └─ UI/                              Main window, cards, controls, and dialogs
├─ AuthenticatorDesk.SmokeTests/       xUnit smoke and compatibility tests
├─ docs/images/                        English UI screenshots
├─ THIRD-PARTY-LICENSES/               Redistributed license texts
├─ LICENSING-REVIEW.md                 Pre-release compliance review
├─ WINAUTH-ATTRIBUTION.md               Upstream credit and modification notice
├─ LICENSE.txt                         Project license
└─ THIRD-PARTY-NOTICES.md              Third-party attribution
```

## Security limitations

The following are known boundaries, not hidden guarantees:

- Portable mode stores its key with its ciphertext and is not protection
  against vault-file theft.
- The project has not received an independent security audit or formal
  certification.
- Provider profiles are best-effort local compatibility and may need updates if
  a provider changes its format.
- There is no cloud sync, push MFA, browser extension, mobile client, camera QR
  capture, network time sync, automatic update, or password recovery.
- Windows-account vaults and DPAPI-protected WinAuth data depend on the Windows
  account or machine context that can decrypt them.
- YubiKey-protected WinAuth configurations are unsupported.
- Plaintext and QR exports expose reusable secrets.
- Timed clipboard clearing applies to copied verification codes, not every
  secret-bearing dialog.
- Global **Type code** hotkeys use `SendKeys` and therefore type into whichever
  window has focus. Confirm the target before using this action.
- With **Auto-refresh** disabled, the current implementation does not regenerate
  the card's displayed code when Reveal is clicked; copying the code does
  calculate a current value.
- **Confirm before each reveal** currently masks the code but does not perform
  master-password or Windows re-authentication.
- Auto-lock observes activity in AuthenticatorDesk rather than system-wide idle
  time.
- Changing the data directory and normal backup behavior can leave additional
  encrypted copies that must be managed separately.
- No installer, official OS support matrix, release signing pipeline, or
  reproducible-build claim is defined in the repository today.

Please follow [SECURITY.md](SECURITY.md) when reporting a suspected
vulnerability. Do not attach real OTP secrets, vaults, recovery codes, or
unredacted exports to a public issue.

## Contributing

Issues and pull requests are welcome. Read the full
[contribution guide](CONTRIBUTING.md) before starting a change. In summary:

1. keep the application Windows-local and avoid introducing network or telemetry
   behavior without an explicit design discussion;
2. add or update smoke tests for security, storage, migration, or format changes;
3. run the Release build and smoke-test commands above;
4. keep user-facing strings in the language-pack system rather than hard-coding
   new UI text;
5. document compatibility and security tradeoffs precisely.

Translation contributors should start with the
[language-pack contribution guide](AuthenticatorDesk.NET/Languages/README.md).
Never use real authenticator secrets in tests, screenshots, issues, or pull
requests.

Unless stated otherwise, contributions submitted to this repository are
offered under [GNU GPL version 3 or any later version](LICENSE.txt). Submit only
material that you have the right to provide under terms compatible with that
license.

## License

AuthenticatorDesk contains code adapted from WinAuth, so the complete combined
program is licensed under
[GNU GPL version 3 or any later version](LICENSE.txt). The project preserves
Colin Mackie's copyright notices, identifies the modified files and date, and
thanks the WinAuth contributors in
[WINAUTH-ATTRIBUTION.md](WINAUTH-ATTRIBUTION.md).

This GPL change addresses the WinAuth license requirement but does not by itself
clear the current release: the AntdUI/Ms-PL compatibility issue and application
icon rights remain open in [LICENSING-REVIEW.md](LICENSING-REVIEW.md).
Third-party components remain subject to the terms collected in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Product and provider names are used only to describe interoperability.
AuthenticatorDesk is not affiliated with or endorsed by Google, Microsoft,
Okta, ArenaNet, Valve, Blizzard Entertainment, or Trion Worlds.
