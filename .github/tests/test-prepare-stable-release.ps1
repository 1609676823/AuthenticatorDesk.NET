$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskScript = Join-Path $taskRepositoryRoot '.github/scripts/prepare-stable-release.ps1'
$taskVersionScript = Join-Path $taskRepositoryRoot '.github/scripts/get-release-version.ps1'
$taskFixtureRoot = Join-Path $taskRepositoryRoot ('artifacts/local-validation/prepare-' + [guid]::NewGuid().ToString('N'))
$taskHead = git -C $taskRepositoryRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine test repository commit.' }
function global:gh {
    $global:LASTEXITCODE = 0
    $taskCommand = $args -join ' '
    $taskCommand >> $env:MOCK_LOG
    if ($taskCommand -like 'api --paginate *') {
        switch ($env:MOCK_CASE) {
            'published' { return "false`tfalse`tfalse" }
            'draft' { return "true`tfalse`tfalse" }
            'wrong-channel' { return "false`ttrue`tfalse" }
            'force-published' { return "false`tfalse`tfalse" }
            'force-draft' { return "true`tfalse`tfalse" }
            'force-immutable' { return "false`tfalse`ttrue" }
            'force-wrong-channel' { return "false`ttrue`tfalse" }
            'force-api-failure' { $global:LASTEXITCODE = 22; return }
            'api-failure' { $global:LASTEXITCODE = 22; return }
        }
    } elseif ($taskCommand -like 'api */git/matching-refs/*') {
        if ($env:MOCK_CASE -eq 'tag-api-failure') { $global:LASTEXITCODE = 22; return }
        if ($env:MOCK_CASE -in @('draft', 'existing-tag', 'commit-api-failure')) { return 'refs/tags/v0.1.0' }
    } elseif ($taskCommand -like 'api */commits/*') {
        if ($env:MOCK_CASE -eq 'commit-api-failure') { $global:LASTEXITCODE = 22; return }
        return '1111111111111111111111111111111111111111'
    } else {
        throw "Unexpected gh call: $taskCommand"
    }
}

# gh is mocked and preparation is read-only, including all force-publication scenarios.
foreach ($taskCase in @('new', 'published', 'draft', 'existing-tag', 'beta', 'metadata', 'tag-match', 'tag-mismatch', 'invalid-version', 'leading-zero', 'four-part', 'missing-version', 'duplicate-version', 'missing-program', 'api-failure', 'tag-api-failure', 'commit-api-failure', 'wrong-channel', 'upgrade', 'force-published', 'force-draft', 'force-new', 'force-immutable', 'force-wrong-channel', 'force-api-failure', 'force-beta', 'force-schedule', 'force-push', 'invalid-force')) {
    $taskFixture = Join-Path $taskFixtureRoot $taskCase
    New-Item -ItemType Directory -Path $taskFixture -Force | Out-Null
    $env:MOCK_CASE = $taskCase
    $env:MOCK_LOG = Join-Path $taskFixture 'gh.log'
    $env:GITHUB_OUTPUT = Join-Path $taskFixture 'output.txt'
    $env:GITHUB_STEP_SUMMARY = Join-Path $taskFixture 'summary.txt'
    $env:GITHUB_REF = 'refs/heads/master'
    $env:GH_REPO = 'example/repository'
    $env:FORCE_RELEASE = if ($taskCase.StartsWith('force-')) { 'true' } else { 'false' }
    $env:GITHUB_EVENT_NAME = 'workflow_dispatch'
    $taskVersion = '0.1.0'
    switch ($taskCase) {
        'beta' { $taskVersion = '0.2.0-beta.1' }
        'metadata' { $taskVersion = '0.2.0+build.1' }
        'tag-match' { $env:GITHUB_REF = 'refs/tags/v0.1.0' }
        'tag-mismatch' { $env:GITHUB_REF = 'refs/tags/v0.2.0' }
        'invalid-version' { $taskVersion = 'invalid' }
        'leading-zero' { $taskVersion = '01.0.0' }
        'four-part' { $taskVersion = '1.0.0.0' }
        'upgrade' { $taskVersion = '0.2.0' }
        'force-beta' { $taskVersion = '0.2.0-beta.1' }
        'force-schedule' { $env:GITHUB_EVENT_NAME = 'schedule' }
        'force-push' { $env:GITHUB_EVENT_NAME = 'push' }
        'invalid-force' { $env:FORCE_RELEASE = 'yes' }
    }
    $taskProgramDirectory = Join-Path $taskFixture 'AuthenticatorDesk.NET'
    New-Item -ItemType Directory -Path $taskProgramDirectory -Force | Out-Null
    $taskDeclaration = "    internal const string AppVersion = `"$taskVersion`";"
    if ($taskCase -eq 'missing-version') { $taskDeclaration = '    internal const string AppName = "AuthenticatorDesk";' }
    if ($taskCase -eq 'duplicate-version') { $taskDeclaration += "`n    private const string AppVersion = `"1.2.3`";" }
    if ($taskCase -ne 'missing-program') {
        Set-Content -LiteralPath (Join-Path $taskProgramDirectory 'Program.cs') -Value "namespace AuthenticatorDesk;`ninternal static class Program`n{`n$taskDeclaration`n}"
    }
    $taskFailure = $null
    Push-Location $taskFixture
    try { & $taskScript } catch { $taskFailure = $_ } finally { Pop-Location }
    $taskInvalidSource = $taskCase -in @('beta', 'metadata', 'invalid-version', 'leading-zero', 'four-part', 'missing-version', 'duplicate-version', 'missing-program', 'force-beta')
    $taskExpectedFailure = $taskInvalidSource -or $taskCase -in @('tag-mismatch', 'api-failure', 'tag-api-failure', 'commit-api-failure', 'wrong-channel', 'force-immutable', 'force-wrong-channel', 'force-api-failure', 'force-schedule', 'force-push', 'invalid-force')
    if ([bool]$taskFailure -ne $taskExpectedFailure) { throw "Unexpected result for ${taskCase}: $taskFailure" }
    if (!$taskExpectedFailure) {
        $taskOutput = Get-Content -LiteralPath $env:GITHUB_OUTPUT
        $taskExplicitVersion = & $taskVersionScript -SourceRoot $taskFixture
        if ($taskExplicitVersion -cne $taskVersion) { throw 'Explicit SourceRoot must select that checkout version.' }
        $taskSkip = $taskCase -eq 'published'
        if ($taskSkip) {
            if ($taskOutput -notcontains 'should-build=false') { throw "Expected skip for $taskCase" }
        } else {
            $taskExpectedCommit = if ($taskCase -in @('draft', 'existing-tag')) { '1111111111111111111111111111111111111111' } else { $taskHead }
            if ($taskOutput -notcontains 'should-build=true' -or $taskOutput -notcontains "commit=$taskExpectedCommit" -or $taskOutput -notcontains "tag=v$taskVersion") { throw "Wrong build metadata for $taskCase" }
            if ($taskCase.StartsWith('force-') -and ($taskOutput -match '^published-tag=')) { throw 'Force build must not enter the skip/update-title branch.' }
        }
    }
    if (Test-Path -LiteralPath $env:MOCK_LOG) {
        if ($taskInvalidSource) { throw 'Invalid source metadata must fail before any GitHub call.' }
        if (Select-String -LiteralPath $env:MOCK_LOG -Pattern '--method|^release ') { throw 'Preflight must be read-only' }
        if ($taskCase.StartsWith('force-') -and (Select-String -LiteralPath $env:MOCK_LOG -Pattern '/commits/|/git/matching-refs/')) { throw 'Force build must use current source, not the existing tag.' }
    }
    Write-Output "PASS prepare-$taskCase"
}

# Expected failure cases can leave LASTEXITCODE nonzero even after their assertions pass.
# GitHub's pwsh wrapper propagates that value; report success only after the entire suite passes.
exit 0
