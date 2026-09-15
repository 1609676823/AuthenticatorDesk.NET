# Privacy

[简体中文](PRIVACY.zh-CN.md)

This document describes the behavior of the AuthenticatorDesk code published in
this repository. Unofficial builds or modified distributions may behave
differently.

## Summary

AuthenticatorDesk is a local-first Windows application. The current
application:

- does not require an account;
- does not include advertising or analytics;
- does not send telemetry or automatic crash reports;
- does not synchronize data to a cloud service;
- does not include a network client or automatic update checker.

Authentication codes are generated locally. AuthenticatorDesk does not operate
a server that receives user data.

## Data stored locally

Depending on the features used, AuthenticatorDesk can store:

- account names, issuers, notes, shared authentication secrets, OTP parameters,
  provider recovery fields, favorites, ordering, and global-hotkey choices;
- application settings such as theme, tray behavior, lock behavior, clipboard
  delay, window layout, and recent import/export directories;
- the selected language and user-installed language packs;
- diagnostic error logs containing timestamps, diagnostic codes, exception
  messages, stack traces, and potentially local file paths;
- a Windows startup registry value containing the executable path, if
  start-with-Windows is enabled.

The active vault is stored in `vault.json` in the selected data directory. By
default, the data directory is the directory containing the executable. If that
directory is unavailable or the user changes it, AuthenticatorDesk records a
path locator beside the application or under
`%LocalAppData%\AuthenticatorDesk\locations`.

The language preference is stored separately in
`%LocalAppData%\AuthenticatorDesk\ui-preferences.json`. User language packs can
be installed under `%LocalAppData%\AuthenticatorDesk\Languages`.

Error logs are stored in the data directory's `logs` folder. The application is
not designed to write shared secrets or generated codes to logs, but exception
text and paths should still be reviewed before a log is shared.

## Vault protection

All vault modes use an AES-256-GCM envelope, but they do not provide the same
access protection:

- **Portable mode**, the default for a new vault, stores the AES key in
  `vault.json` itself. It is easy to copy but anyone with the file can open it.
- **Master-password mode** derives the AES key from a password using
  PBKDF2-HMAC-SHA256 with a random salt and 600,000 iterations. The password is
  not stored and cannot be recovered by the project.
- **Windows-account mode** protects a random AES key with Windows DPAPI for the
  current user. The vault normally cannot be opened by a different Windows
  account or computer.

See the detailed [security model](docs/SECURITY-MODEL.md) and
[SECURITY.md](SECURITY.md) for boundaries and reporting guidance.

## Clipboard, hotkeys, and notifications

When the user copies a code or sensitive value, it is placed on the Windows
clipboard and can be read by other software with clipboard access. Verification
codes copied from the dashboard or a global copy hotkey can be cleared after
the configured delay, but only if the clipboard still contains that same code.
Shared secrets, restore codes, and OTP Auth URIs copied through their dialogs
do not use this timed-clear path.

A configured global hotkey can copy a code, show it in a Windows notification,
or send it to the currently focused window through automatic typing. These
actions are initiated by the user but expose the code to the corresponding
Windows subsystem and destination application.

## Imports, exports, and backups

Files are read or written only after the user selects an import or export
action. AuthenticatorDesk does not upload them.

- Password-protected `.authdesk` backups contain authenticator entries in an
  encrypted envelope.
- WinAuth XML can be exported with a password or as plain text.
- Exported OTP Auth URIs and QR codes contain authentication secrets.
- Changing the data directory copies the vault and intentionally keeps the old
  copy for recovery.
- Atomic saves and legacy migrations can leave `vault.json.bak`,
  `vault.json.migrated-*.bak`, or other recovery artifacts.

Users are responsible for securing exports, old copies, screenshots, removable
media, synchronization folders, and system backups.

## Data retention and deletion

Because AuthenticatorDesk does not receive user data, the project has no server
copy to retain or delete. To remove local data:

1. exit AuthenticatorDesk;
2. delete `vault.json` and any recovery artifacts from the active and previous
   data directories;
3. delete `.authdesk`, WinAuth, OTP URI, and QR exports that are no longer
   needed;
4. delete the `logs` folder if diagnostic history is no longer needed;
5. optionally delete `%LocalAppData%\AuthenticatorDesk` to remove language
   preferences, user language packs, legacy data, and path locators;
6. disable start-with-Windows in the application before removal, or delete the
   `AuthenticatorDesk` value under
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

Deleting a vault or master password is irreversible unless the user has a valid
backup or the original authentication enrollment secrets.

## Third-party and operating-system behavior

AuthenticatorDesk uses third-party libraries locally for its interface and QR
processing. Their notices are distributed with the project. Windows, antivirus
software, enterprise management, backup tools, clipboard managers, and modified
distributions are outside this project's control and may process local files or
application activity under their own policies.

## Changes and questions

Privacy-relevant changes should be reflected in this document. For questions,
open a repository issue without attaching private data. Suspected
vulnerabilities must be reported through the private process in
[SECURITY.md](SECURITY.md).
