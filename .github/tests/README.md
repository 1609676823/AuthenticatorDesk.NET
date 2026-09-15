# Release workflow regression checks

Run the commands below from the repository root. The scripts also support being invoked from another checkout: they locate the release tools relative to their own file location. These checks mock every `gh` call and do not access or modify GitHub releases, tags, or assets. Fixtures and command logs are written under that checkout's ignored `artifacts/local-validation` directory.

```powershell
pwsh -NoProfile -File .github/tests/test-prepare-stable-release.ps1
```

To verify the exit status using the same wrapper behavior as GitHub Actions, run
this in a fresh PowerShell process (it exits that process):

```powershell
$ErrorActionPreference = 'Stop'
& ./.github/tests/test-prepare-stable-release.ps1
if (Test-Path -LiteralPath variable:\LASTEXITCODE) { exit $LASTEXITCODE }
```

The suite explicitly exits successfully only after every assertion passes.
Expected API errors must not leak their exit codes into the Actions step; an
unexpected failure still stops the suite with a nonzero exit code.

```bash
bash .github/tests/test-publish-release.sh
```

On Windows, use Git Bash for the second command. Run `actionlint` separately to validate all workflow definitions and reusable-workflow inputs.

The preparation suite covers normal skips, original-tag selection, forced current-source selection, version checks, API errors, immutable releases, and rejection of force flags on automatic events. `get-release-version.ps1` reads the single `Program.AppVersion` constant in `AuthenticatorDesk.NET/Program.cs`, with an optional `-SourceRoot` argument to select another checkout; only plain `X.Y.Z` versions are accepted. Missing/duplicate constants, leading zeros, prerelease suffixes, and build metadata fail before GitHub is queried. Application assembly and informational version checks remain in the existing .NET test project.

The publication suite covers normal stable preservation, nightly updates, forced stable replacement/creation, obsolete-asset removal, checksum errors, and failures during upload, cleanup, tag updates, or final publication. It verifies that tags are updated only after successful uploads and publication follows tag updates. Package names use `AuthenticatorDesk-<tag>-win-<target>-<publish-mode>.zip`.
