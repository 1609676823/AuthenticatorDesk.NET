# Contributing to AuthenticatorDesk

[简体中文](CONTRIBUTING.zh-CN.md)

Thank you for helping improve AuthenticatorDesk. Contributions to code, tests,
documentation, translations, accessibility, and user experience are welcome.

## Before you begin

- Search existing issues and pull requests before opening a new one.
- Use the latest revision of the default branch when reproducing a problem.
- Do not publish authentication secrets, current verification codes, QR codes,
  OTP Auth URIs, `vault.json`, `.authdesk` backups, WinAuth exports, or private
  log content.
- Report suspected vulnerabilities privately as described in
  [SECURITY.md](SECURITY.md).
- Keep changes focused. Discuss large UI redesigns, storage-format changes,
  cryptographic changes, or new dependencies before investing substantial work.

## Development environment

AuthenticatorDesk is a Windows Forms application targeting .NET 10. Building
and running the complete application and test suite requires Windows and the
.NET 10 SDK.

```powershell
dotnet restore .\AuthenticatorDesk.NET.slnx
dotnet build .\AuthenticatorDesk.NET.slnx -c Release --no-restore
dotnet test .\AuthenticatorDesk.NET.slnx -c Release --no-build
dotnet run --project .\AuthenticatorDesk.NET\AuthenticatorDesk.NET.csproj -c Release
```

The application normally stores `vault.json` beside the executable. A
development run therefore creates local data under the build output directory
unless another data directory is selected. Never commit that data.

## Making a change

1. Fork the repository and create a short, descriptive branch.
2. Make the smallest coherent change that solves the problem.
3. Add or update tests when behavior changes.
4. Run the Release build and complete test suite.
5. Update user-facing documentation when behavior, compatibility, storage, or
   security expectations change.
6. Open a pull request describing the problem, the solution, verification, and
   any remaining limitations.

## Code guidelines

- Follow the existing C# style and keep nullable reference types enabled.
- Keep user secrets local. Do not add telemetry, network access, crash
  reporting, or update checks without prior design and privacy review.
- Treat authentication secrets, recovery data, QR payloads, and OTP Auth URIs
  as sensitive throughout their lifetime.
- Preserve atomic-write, recovery-file, and key-zeroing behavior when changing
  vault or backup code.
- Do not weaken validation limits for imported files or language packs without
  a documented reason and tests.
- Put user-visible text in the language-pack resources instead of hard-coding
  it, except for product, provider, protocol, and algorithm names that are
  intentionally invariant.
- Keep Windows Forms layouts usable at different DPI values, window widths, and
  text lengths.
- Avoid unrelated formatting or mechanical changes in the same pull request.

## Tests

The xUnit smoke-test project covers OTP standards, QR and migration formats,
vault and backup transitions, storage recovery, localization, and responsive
UI behavior. Tests use Windows Forms and single-threaded apartment state, so
they must run on Windows.

Every bug fix should include a regression test when practical. Security,
cryptography, import, storage, and migration changes require both success and
failure-path coverage. Do not use real authentication secrets in tests; use
published RFC vectors or clearly marked synthetic fixtures.

## Translations

English is the source-language baseline. See
[the language-pack guide](AuthenticatorDesk.NET/Languages/README.md) and its
[Simplified Chinese version](AuthenticatorDesk.NET/Languages/README.zh-CN.md).

Built-in language packs must contain the complete English key set and preserve
all composite-format placeholders. Community packs may be partial because
missing values fall back to embedded English.

When adding or changing a user-visible English string:

1. update `Languages/en.json`;
2. update every built-in language pack;
3. verify placeholders and metadata;
4. run the complete test suite;
5. check the affected layout with long translated text.

Machine translation can be a starting point, but a contributor should review
the result for meaning, terminology, formatting, and UI fit.

## Documentation and screenshots

- Repository-level documentation is English by default. Keep the corresponding
  `*.zh-CN.md` document synchronized when one exists.
- Use English UI text for default README screenshots.
- Use only synthetic accounts and public test secrets in screenshots.
- Remove cursors, personal paths, usernames, notification content, and other
  identifying data before committing an image.
- Use relative links so documentation works on both GitHub and Gitee.

## Dependencies and third-party material

New dependencies increase security, maintenance, and license obligations.
Explain why a dependency is needed and use only packages that can be
distributed in a GPL-3.0-or-later combined work. A package being "open source"
is not enough: check license compatibility as well as attribution and source
obligations. Update the third-party notices and license files when required.

Do not contribute code, icons, translations, screenshots, or other material
unless you have the right to license it to this project.
For adapted code, identify the exact upstream revision, preserve copyright and
license notices, and state what was modified and when.

## Pull request checklist

- [ ] The change is focused and its user impact is explained.
- [ ] Release build and tests pass on Windows.
- [ ] New behavior has tests, including relevant failure cases.
- [ ] No real secret, vault, backup, QR code, OTP URI, or private log is included.
- [ ] User-visible strings are localized.
- [ ] English and Chinese documentation remain synchronized where applicable.
- [ ] Security, privacy, compatibility, and migration implications are documented.
- [ ] Required third-party notices and license files are updated.

## Licensing contributions

By submitting a contribution, you confirm that you have the right to submit it
and agree that it may be distributed under the project's
[GNU GPL version 3 or any later version](LICENSE.txt). Third-party material
remains subject to its own compatible license and notice requirements.
