# AuthenticatorDesk language packs

[简体中文](README.zh-CN.md)

Language packs are editable, distributable UTF-8 JSON files; no DLL compilation
is required. At startup, AuthenticatorDesk loads packs from the `Languages`
directory beside the executable and then from
`%LocalAppData%\AuthenticatorDesk\Languages`. A per-user pack has priority over
a pack with the same locale beside the executable, so translations can be
installed or updated without program-directory write access.

Every valid locale is added automatically to **Settings → Language**. Changing
the selected language restarts the application.

## Built-in languages

The repository currently includes:

| Locale | Language |
| --- | --- |
| `en` | English |
| `zh-Hans` | Simplified Chinese |
| `zh-Hant` | Traditional Chinese |
| `ja` | Japanese |
| `ko` | Korean |
| `de` | German |
| `fr` | French |
| `es` | Spanish |
| `pt-BR` | Brazilian Portuguese |
| `ru` | Russian |

English is the source-language baseline. All built-in packs must contain the
same complete key set as `en.json`.

## Adding a language

1. Copy `en.json` and rename it to a standard BCP-47 locale such as `it.json`,
   `nl.json`, or `pt-PT.json`.
2. Update the metadata:
   - keep `schemaVersion` set to `1`;
   - make `locale` exactly match the filename without `.json`;
   - put the English language name in `name`;
   - put the language's own name in `nativeName`;
   - optionally add translator names in an `authors` array.
3. Translate only the values inside `strings`; do not change the keys.
4. Preserve complete format items such as `{0}` and `{1}`, including how many
   times each one occurs. Their order may be changed, but alignment widths and
   format specifiers must not be added or altered.
5. Preserve product, provider, protocol, and algorithm names where appropriate,
   including AuthenticatorDesk, WinAuth, TOTP, HOTP, Base32, AES-GCM, and
   SHA-256.
6. Start the app and select the new language in Settings.

A community pack may contain only some keys; missing or invalid values fall
back to embedded English. Packs submitted for inclusion as built-in languages
must contain the complete English key set and pass the test suite.

## Matching and fallback

The default setting follows the current Windows display language. Resolution
proceeds from the full locale to a script or parent language and finally to
English. For example:

- `zh-CN` and `zh-SG` → `zh-Hans`
- `zh-TW`, `zh-HK`, and `zh-MO` → `zh-Hant`
- `fr-CA` → `fr`
- an unavailable or damaged pack → `en`

The selected language is stored in
`%LocalAppData%\AuthenticatorDesk\ui-preferences.json`, separately from vault
and authenticator data. This lets startup and vault-unlock dialogs use the
selected language before the vault is opened.

## Format and safety limits

See [language-pack.schema.json](language-pack.schema.json) for the JSON Schema.
The loader reads JSON text only—never scripts or assemblies—and enforces these
limits:

- at most 512 KiB per file;
- at most 5,000 strings;
- at most 256 characters per key;
- at most 4,096 characters per translated value;
- filename and `locale` must match;
- complete format items (argument, alignment, and format specifier) must match
  the embedded English baseline.

A malformed, oversized, or placeholder-incompatible pack cannot prevent the
app from starting; affected content safely falls back to English.

## Testing a contribution

From the repository root, run the complete test suite on Windows with the
.NET 10 SDK:

```powershell
dotnet test .\AuthenticatorDesk.NET.slnx -c Release
```

Also select the language in the application and inspect the dashboard, entry
editor, settings, startup/unlock dialogs, import/export menus, and narrow-window
layouts. Check that accelerator keys, format placeholders, punctuation, and
line wrapping remain usable.

The current UI has been adapted and visually checked for left-to-right
languages. Arabic, Hebrew, and other right-to-left translations are welcome,
but complete built-in support will also require WinForms/AntdUI RTL layout work
and visual testing.

For general contribution requirements, see the repository's
[contribution guide](../../CONTRIBUTING.md).
