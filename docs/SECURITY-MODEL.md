# AuthenticatorDesk security model

[English](SECURITY-MODEL.md) | [简体中文](SECURITY-MODEL.zh-CN.md)

This document describes the security boundaries implemented by the current
source tree. It is not a security certification. AuthenticatorDesk has not
undergone an independent security audit.

## Assets and scope

The local vault may contain shared secrets, account and issuer labels, notes,
provider metadata, counters, and application settings. Anyone who obtains a
shared secret can normally generate valid one-time passwords for that account.
Treat vaults, backups, OTP Auth URIs, QR images, WinAuth exports, and screenshots
of secrets as credentials.

The application is designed to:

- generate OTP codes locally without a server or cloud account;
- detect modification or corruption of encrypted vault contents;
- offer password-based or Windows-account-based access protection;
- remove decrypted entries and session keys from the active application state
  when the vault is locked; and
- reduce, but not eliminate, exposure through the clipboard and desktop UI.

It is not designed to protect secrets from malware, debuggers, keyloggers,
screen capture, an administrator, or another process that can inspect the
application while the vault is unlocked.

## Vault protection modes

Every current vault mode encrypts its JSON payload with AES-256-GCM, a fresh
12-byte nonce, a 16-byte authentication tag, and version-specific associated
data. The important difference is how the AES key is protected.

| Mode | Key handling | Portability | Security boundary |
| --- | --- | --- | --- |
| Portable (default) | A random 256-bit key is stored in the same vault envelope as the ciphertext | Can be copied to another Windows computer | Detects corruption and avoids plain JSON, but **does not provide access protection if the vault file is copied or stolen** |
| Master password | PBKDF2-HMAC-SHA256 derives the key from the password using a 16-byte random salt and 600,000 iterations | Portable when the password is known | Security depends on password strength; there is no password recovery |
| Windows account | A random 256-bit key is protected with Windows DPAPI for the current user | Bound to the same Windows user context | Helps protect a copied vault from a different account or computer; it does not defend against code already running as that Windows user |

New users should choose a strong master password or Windows account protection
unless unprotected portability is an explicit requirement.

## Locking and memory

When AuthenticatorDesk locks successfully, it first saves pending changes,
unregisters global hotkeys, clears an unchanged OTP code placed on the clipboard
through the protected copy path, zeroes its session-key byte arrays, removes
entry secrets and provider data from the active model, and replaces the
dashboard with the lock view.

This is defense in depth, not a secure-memory guarantee. The application uses
.NET managed objects and strings, so copies may remain until reclaimed by the
runtime. A process with memory-inspection capability while the vault is unlocked
is outside the protection boundary.

## Clipboard and automatic typing

OTP codes copied from the main UI or a global copy hotkey can be cleared after
the configured delay. The application clears the clipboard only if it still
contains the exact value that AuthenticatorDesk placed there, so it does not
erase newer clipboard content.

This timer does not cover every sensitive dialog. Shared secrets, restore codes,
and complete OTP Auth URIs shown or copied through export dialogs must be
handled as plaintext credentials. Other desktop applications may read clipboard
content before it is cleared. Automatic typing and notification actions also
expose a code to the selected desktop target or notification surface.

## Backups, imports, and exports

- Native `.authdesk` backups contain authenticator entries, not all application
  settings. They use PBKDF2-HMAC-SHA256 with 600,000 iterations and AES-256-GCM.
- Plain OTP Auth URI exports, QR images, and unencrypted WinAuth XML contain
  recoverable shared secrets.
- Password-protected WinAuth XML uses WinAuth's legacy compatibility scheme
  (PBKDF2-HMAC-SHA1 with 2,000 iterations and Blowfish). Use the native
  `.authdesk` format for new encrypted backups.
- Windows-protected WinAuth data can be opened only in a compatible Windows
  protection context. YubiKey-protected WinAuth data is not supported.
- Imported data is trusted only as input data, but a successful import still
  brings its secrets into the local vault. Verify the source and delete
  plaintext migration files securely when they are no longer needed.

The vault save path uses a temporary file and keeps a `.bak` copy. A backup copy
has the same protection properties as the primary vault and must be protected
accordingly.

## Local data and diagnostics

The current application source does not implement a network client, cloud sync,
telemetry, an account service, update checks, or network time synchronization.
The Windows system clock is used for TOTP generation.

Fatal-error logs are written under the selected data directory. They can contain
exception details, file paths, and diagnostic context. They are not uploaded
automatically, but should be reviewed before being shared publicly.

See [PRIVACY.md](../PRIVACY.md) for the data-flow summary.

## Threats not covered

AuthenticatorDesk cannot by itself protect against:

- malware or a hostile administrator on the same Windows installation;
- a weak or reused master password and offline password guessing;
- disclosure through screenshots, clipboard managers, shell history, exports,
  backups, or cloud-synced folders selected by the user;
- compromise of the service that issued the OTP secret;
- loss of the only vault copy or forgotten master passwords;
- an incorrect or maliciously modified system clock; or
- vulnerabilities in Windows, .NET, or bundled dependencies.

Keep recovery codes separately, maintain tested backups, lock the workstation,
install operating-system updates, and review dependency updates before release.

## Review status

The cryptographic design and implementation have automated compatibility and
known-vector tests, but no independent audit. Please report suspected
vulnerabilities according to [SECURITY.md](../SECURITY.md), without posting
real credentials or secrets in a public issue.
