param(
    [string]$SourceRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'
$programPath = Join-Path $SourceRoot 'AuthenticatorDesk.NET/Program.cs'
$source = Get-Content -LiteralPath $programPath -Raw
$declarations = [regex]::Matches(
    $source,
    '(?m)^[\t ]*(?:(?:public|internal|private|protected|new)\s+)*const\s+string\s+AppVersion\s*=\s*"([^"\r\n]*)"\s*;'
)
if ($declarations.Count -ne 1) {
    throw 'AuthenticatorDesk.NET/Program.cs must declare exactly one const string AppVersion.'
}

$version = $declarations[0].Groups[1].Value
if ($version -cnotmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$') {
    throw 'Program.AppVersion must be a plain X.Y.Z version without leading zeros, prerelease suffixes, or build metadata.'
}
$version
