# Security Policy

[简体中文](SECURITY.zh-CN.md)

AuthenticatorDesk stores credentials that can grant access to other services.
Please treat security reports and test data accordingly.

## Supported versions

Until a longer-term release policy is published, security fixes are made for
the latest published release and the current default branch. Older binaries and
unofficial builds may not receive fixes. Reproduce a report against the latest
available code when practical.

## Reporting a vulnerability

Do not disclose a suspected vulnerability in a public issue, discussion, pull
request, screenshot, or Gitee feedback thread.

Use the private vulnerability-reporting feature of the primary repository when
it is available. If no private channel is shown, open a public issue containing
only the title **Security contact request** and no technical details. A
maintainer can then arrange a private channel.

Include, where relevant:

- the affected version, commit, and Windows version;
- the protection mode involved: portable, master password, or Windows account;
- a concise description of the impact and affected component;
- reproducible steps or a minimal proof of concept using synthetic data;
- whether user interaction or an already-unlocked vault is required;
- suggested mitigations or fixes;
- whether you intend to request public credit.

Never send a real shared secret, current verification code, recovery code,
personal QR code, OTP Auth URI, `vault.json`, `.authdesk` backup, WinAuth
configuration, or unreviewed diagnostic log. Create a synthetic fixture that
demonstrates the issue. If a file is essential, remove unrelated records and
share it only through the agreed private channel.

The project aims to acknowledge a complete report within seven days and provide
an initial assessment within fourteen days. Volunteer availability and issue
complexity can affect these targets.

## Security model

AuthenticatorDesk is local-first. The current application has no account
service, cloud synchronization, telemetry, automatic crash reporting, or
network client. Verification codes are generated on the local computer.

The vault supports three protection modes:

| Mode | Key protection | Portability and limitation |
| --- | --- | --- |
| Portable | The AES key is stored in the same `vault.json` envelope | Easy to copy, but it does **not** provide account-level access protection. Anyone who obtains the file can open it. This is the default for a new vault. |
| Master password | PBKDF2-HMAC-SHA256 with a random salt and 600,000 iterations derives an AES key | The same file can be opened on another computer with the password. There is no password recovery. |
| Windows account | A random AES key is protected with Windows DPAPI for the current user | Convenient local protection, but the vault cannot normally be opened by another Windows account or computer. Disable this mode before migration. |

Vault and encrypted-backup payloads use AES-256-GCM for authenticated
encryption. A password-protected `.authdesk` backup uses its own password and
key derivation. The implementation has not received a formal independent
security audit; cryptographic terminology is not a guarantee that every host
environment or workflow is secure.

The full implementation boundaries and threat model are documented in
[docs/SECURITY-MODEL.md](docs/SECURITY-MODEL.md).

## Important limitations

- Portable mode is an interoperability feature, not access control.
- An attacker who controls the Windows account, administrator context, or
  running process may be able to access an unlocked vault.
- The application cannot protect codes from screen capture, malicious input
  software, clipboard monitors, or a compromised operating system.
- Timed clipboard clearing applies to verification codes copied from the
  dashboard or a global copy hotkey. It is best effort and occurs only while the
  clipboard still contains that same code; secret-bearing dialogs are not
  covered.
- Global-hotkey automatic typing sends a code to the currently focused window.
  The user must verify the destination.
- QR codes, OTP Auth URIs, plain-text WinAuth exports, and OTP URI text exports
  contain authentication secrets. They are not safe to publish or retain in an
  untrusted location.
- Windows account protection depends on Windows DPAPI and the continued
  availability of the original user profile.
- Error logs can contain exception details and local paths. Review and redact
  them before sharing.
- Third-party libraries and the .NET/Windows runtime remain part of the trusted
  computing base.

## In-scope examples

- vault, backup, password, key-handling, or authentication bypasses;
- disclosure or unintended persistence of secrets or generated codes;
- unsafe import parsing, path handling, file replacement, or migration;
- QR, OTP URI, WinAuth, Base32, or language-pack parser vulnerabilities;
- clipboard, global hotkey, auto-type, tray, or lock-state boundary failures;
- dependency vulnerabilities that are reachable in AuthenticatorDesk.

Usability problems without a security impact, unsupported-environment failures,
and reports that require a previously compromised operating system may be
handled as regular issues.

## Coordinated disclosure

Please allow maintainers a reasonable opportunity to reproduce, fix, test, and
publish an update before public disclosure. A release or advisory may credit
the reporter if requested. This project currently offers no paid bug-bounty
program.
