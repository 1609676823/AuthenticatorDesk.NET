param(
    [ValidateSet('repository-default', 'self-contained', 'framework-dependent', 'both')]
    [string]$DeploymentMode = 'repository-default',
    [string]$SourceRoot = (Get-Location).Path,
    [string]$OutputDirectory = 'artifacts/packages',
    [string]$SettingsPath = (Join-Path $PSScriptRoot '../release-settings.psd1'),
    [Parameter(Mandatory)][string]$ReleaseTag,
    [Parameter(Mandatory)][string]$Version,
    [ValidateSet('nightly', 'stable')][string]$Channel = 'nightly',
    [string]$SourceCommit = $env:RELEASE_COMMIT
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$settings = Import-PowerShellDataFile -LiteralPath $SettingsPath
if ($DeploymentMode -eq 'repository-default') { $DeploymentMode = $settings.DeploymentMode }
if ($DeploymentMode -notin @('self-contained', 'framework-dependent', 'both')) {
    throw 'Invalid DeploymentMode in release-settings.psd1.'
}
$runtimes = @($settings.RuntimeIdentifiers)
if ($runtimes.Count -ne @($runtimes | Select-Object -Unique).Count) { throw 'Duplicate runtime identifiers.' }
foreach ($runtime in $runtimes) {
    if ($runtime -notin @('win-x86', 'win-x64', 'win-arm64')) { throw "Unsupported WinForms runtime: $runtime" }
}
if ($settings.IncludePortable -isnot [bool]) { throw 'IncludePortable must be a Boolean.' }
if (!$runtimes.Count -and !$settings.IncludePortable) { throw 'At least one package must be enabled.' }
if ($ReleaseTag -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+(-nightly)?$') { throw 'Invalid release tag.' }

$sourcePath = (Resolve-Path -LiteralPath $SourceRoot).Path
$appVersion = & (Join-Path $PSScriptRoot 'get-release-version.ps1') -SourceRoot $sourcePath
$expectedTag = if ($Channel -eq 'nightly') { "v$appVersion-nightly" } else { "v$appVersion" }
if ($ReleaseTag -ne $expectedTag) { throw 'ReleaseTag must match the source AppVersion and channel.' }
if (($Version -replace '[-+].*$', '') -ne $appVersion -or
    ($Channel -eq 'stable' -and $Version -ne $appVersion) -or
    ($Channel -eq 'nightly' -and $Version -notmatch ('^' + [regex]::Escape($appVersion) + '-nightly\.[0-9]+\.[0-9]+$'))) {
    throw 'Version must match AppVersion (stable) or AppVersion-nightly.RUN.ATTEMPT (nightly).'
}
if (!$SourceCommit) {
    $SourceCommit = git -C $sourcePath rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot determine the source commit.' }
}
if ($SourceCommit -notmatch '^[0-9a-fA-F]{40}$') { throw 'SourceCommit must be a full Git commit SHA.' }
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$scratchRoot = Join-Path $outputPath ('publish-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratchRoot | Out-Null
$modes = if ($DeploymentMode -eq 'both') { @('self-contained', 'framework-dependent') } else { @($DeploymentMode) }
$packages = @(
    foreach ($runtime in $runtimes) {
        foreach ($mode in $modes) { @{ Runtime = $runtime; Mode = $mode } }
    }
    if ($settings.IncludePortable) { @{ Runtime = 'win-portable'; Mode = 'framework-dependent' } }
)
$checksumLines = @()
$tableRows = @()

foreach ($package in $packages) {
    $runtime = $package.Runtime
    $mode = $package.Mode
    $portable = $runtime -eq 'win-portable'
    $selfContained = $mode -eq 'self-contained'
    $archiveName = "AuthenticatorDesk-$ReleaseTag-$runtime-$mode.zip"
    $packageDirectoryName = 'AuthenticatorDesk'
    $publishDirectory = Join-Path (Join-Path $scratchRoot "$runtime-$mode") $packageDirectoryName
    # Each combination has a fresh output directory; never mix runtimes or deployment modes.
    # Like Visual Studio's portable publish, no-RID output keeps AnyCPU DLLs
    # and includes an EXE for the build SDK's platform and architecture.
    $arguments = @(
        'publish', (Join-Path $sourcePath 'AuthenticatorDesk.NET/AuthenticatorDesk.NET.csproj'),
        '-c', 'Release', '--self-contained', $selfContained.ToString().ToLowerInvariant(),
        '-o', $publishDirectory, "-p:Version=$Version", '-p:ContinuousIntegrationBuild=true',
        '-p:PublishTrimmed=false', '-p:PublishSingleFile=false', '-p:PublishReadyToRun=false',
        '-p:DebugType=None', '-p:DebugSymbols=false', '-p:UseAppHost=true'
    )
    if ($portable) {
        $arguments += @('-p:RuntimeIdentifier=', '-p:PlatformTarget=AnyCPU')
    } else {
        $arguments += @('-r', $runtime)
    }
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Publishing $runtime / $mode failed: $LASTEXITCODE" }

    $requiredFiles = @(
        'AuthenticatorDesk.exe', 'AuthenticatorDesk.dll', 'AuthenticatorDesk.deps.json', 'AuthenticatorDesk.runtimeconfig.json',
        'AntdUI.dll', 'QRCoder.dll', 'zxing.dll', 'ZXing.Windows.Compatibility.dll',
        'Languages/en.json', 'Languages/zh-Hans.json', 'Languages/zh-Hant.json',
        'Languages/de.json', 'Languages/es.json', 'Languages/fr.json', 'Languages/ja.json',
        'Languages/ko.json', 'Languages/pt-BR.json', 'Languages/ru.json',
        'Languages/language-pack.schema.json', 'Languages/README.md', 'Languages/README.zh-CN.md',
        'LICENSE.txt', 'THIRD-PARTY-NOTICES.md', 'WINAUTH-ATTRIBUTION.md', 'WINAUTH-ATTRIBUTION.zh-CN.md',
        'THIRD-PARTY-LICENSES/Apache-2.0.txt', 'THIRD-PARTY-LICENSES/Microsoft-Public-License.txt'
    )
    if ($selfContained) { $requiredFiles += @('coreclr.dll', 'hostfxr.dll', 'System.Windows.Forms.dll') }
    foreach ($file in $requiredFiles) {
        if (!(Test-Path -LiteralPath (Join-Path $publishDirectory $file) -PathType Leaf)) { throw "Missing $runtime/$mode file: $file" }
    }
    if ($selfContained) {
        foreach ($pack in @('microsoft.netcore.app.runtime', 'microsoft.windowsdesktop.app.runtime')) {
            $legalDirectories = @(Get-ChildItem -LiteralPath (Join-Path $publishDirectory 'THIRD-PARTY-LICENSES/dotnet-runtime') -Directory |
                Where-Object { $_.Name -like "$pack.$runtime-*" })
            if ($legalDirectories.Count -ne 1) { throw "Missing or ambiguous runtime legal files for $pack.$runtime" }
            $legalPath = $legalDirectories[0].FullName
            # The Windows Desktop runtime pack ships LICENSE only; the core runtime also ships notices.
            if ((!(Test-Path -LiteralPath (Join-Path $legalPath 'LICENSE.TXT')) -and
                 !(Test-Path -LiteralPath (Join-Path $legalPath 'LICENSE'))) -or
                ($pack -eq 'microsoft.netcore.app.runtime' -and
                 !(Test-Path -LiteralPath (Join-Path $legalPath 'THIRD-PARTY-NOTICES.TXT')))) {
                throw "Incomplete runtime legal files for $pack.$runtime"
            }
        }
    }
    if (!$selfContained -and (Test-Path -LiteralPath (Join-Path $publishDirectory 'coreclr.dll'))) {
        throw 'A framework-dependent package unexpectedly contains the runtime.'
    }
    $launch = if ($portable) { 'AuthenticatorDesk.exe (or Start-AuthenticatorDesk.cmd / dotnet AuthenticatorDesk.dll)' } else { 'AuthenticatorDesk.exe' }
    $requirement = if ($selfContained) { 'Included / 已附带 .NET 10 Windows Desktop Runtime' } else { 'Install / 需安装 .NET 10 Windows Desktop Runtime for the selected dotnet/process architecture' }
    if ($portable) {
        # Alternative for running the AnyCPU DLL with the desired dotnet architecture on PATH.
        $launcher = @'
@echo off
dotnet "%~dp0AuthenticatorDesk.dll"
if errorlevel 1 pause
'@
        [System.IO.File]::WriteAllText((Join-Path $publishDirectory 'Start-AuthenticatorDesk.cmd'), ($launcher -replace '\r?\n', "`r`n") + "`r`n", [System.Text.Encoding]::ASCII)
    }
    @"
$ReleaseTag — $runtime — $mode

Windows only / 仅支持 Windows。Extract every file / 请完整解压全部文件。
Open the extracted $packageDirectoryName folder to start the application.
解压后进入 $packageDirectoryName 文件夹启动程序。
Start / 启动: $launch
Runtime / 运行时: $requirement
Application version / 程序版本: $appVersion
Build version / 构建版本: $Version
Source commit / 源码提交: $SourceCommit
Source / 对应源码: https://github.com/1609676823/AuthenticatorDesk.NET/tree/$SourceCommit

Portable DLLs do not make WinForms cross-platform. Linux, macOS and Windows ARM32 are not supported.
可移植 DLL 仍依赖 Windows；Linux、macOS、Windows ARM32 不受支持。
The portable EXE matches the build SDK's architecture; it requires the matching Desktop Runtime.
可移植包中的 EXE 与构建 SDK 的架构一致，需要安装对应架构的桌面运行时。
To use the portable DLL with another Windows architecture, use Start-AuthenticatorDesk.cmd or dotnet AuthenticatorDesk.dll with that architecture's dotnet.
如需用其他 Windows 架构运行可移植 DLL，请通过相应架构的 dotnet 使用 Start-AuthenticatorDesk.cmd 或 dotnet AuthenticatorDesk.dll。
Use a Windows version supported by .NET 10. / 请使用 .NET 10 支持的 Windows 版本。
All ZIPs are folder deployments and are unsigned. Keep Languages and all license/attribution files.
所有 ZIP 都是文件夹部署，未进行代码签名。请保留 Languages 及全部许可证与归属声明。
Back up your vault and preserve your selected data directory and AuthenticatorDesk.paths.json when upgrading.
升级前请备份保险库，保留所选数据目录和 AuthenticatorDesk.paths.json 路径配置。
"@ | Set-Content -LiteralPath (Join-Path $publishDirectory 'PACKAGE-README.txt') -Encoding utf8NoBOM

    $archivePath = Join-Path $outputPath $archiveName
    # Every archive extracts to AuthenticatorDesk; the parent staging directories keep targets isolated.
    Compress-Archive -LiteralPath $publishDirectory -DestinationPath $archivePath -CompressionLevel Optimal -Force
    $hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $checksumLines += "$hash  $archiveName"
    $tableRows += "| $archiveName | $requirement | $launch |"
}

# This file is the authoritative asset list; publish-release.sh uploads only the verified entries.
# Explicit LF keeps sha256sum on the Linux publishing runner compatible with Windows packaging.
Set-Content -LiteralPath (Join-Path $outputPath 'SHA256SUMS.txt') -Value (($checksumLines -join "`n") + "`n") -Encoding utf8NoBOM -NoNewline
$builtAt = [DateTimeOffset]::UtcNow.ToString('u')
$buildLink = if ($env:GITHUB_RUN_ID) { "https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID" } else { 'Local build / 本地构建' }
@"
$ReleaseTag ($Channel)

- Application version / 程序版本: $appVersion
- Build version / 构建版本: $Version
- Built at / 构建时间: $builtAt
- Source commit / 源码提交: $SourceCommit
- Build / 构建记录: $buildLink
- Deployment mode / 部署模式: $DeploymentMode
- Corresponding source / 对应源码: https://github.com/1609676823/AuthenticatorDesk.NET/tree/$SourceCommit
- Source ZIP / 源码压缩包: https://github.com/1609676823/AuthenticatorDesk.NET/archive/$SourceCommit.zip
- Signing / 签名状态: Unsigned / 未进行代码签名

| Package / 下载包 | Runtime / 运行时要求 | Start / 启动方式 |
| --- | --- | --- |
$($tableRows -join "`n")

Extract the complete ZIP. Self-contained packages include .NET; framework-dependent and portable packages require .NET 10 Windows Desktop Runtime.
请完整解压：self-contained 包附带运行时；framework-dependent 和可移植包需要安装 .NET 10 Windows Desktop Runtime。
Each ZIP contains one top-level folder named AuthenticatorDesk. Open that folder to start the application.
每个 ZIP 内都只有一个名为 AuthenticatorDesk 的顶级文件夹；解压后进入该文件夹启动程序。
Windows only. Portable packages contain an AnyCPU DLL and an EXE matching the build SDK's architecture; the EXE requires the matching Desktop Runtime.
仅支持 Windows；可移植包包含 AnyCPU DLL 及与构建 SDK 架构一致的 EXE，运行 EXE 需要对应架构的桌面运行时。
For another Windows architecture, use the portable CMD/DLL with that architecture's dotnet. Keep Languages and all license/attribution files.
其他 Windows 架构可使用相应架构的 dotnet 启动可移植包中的 CMD/DLL。请保留 Languages 及全部许可证与归属声明。
Back up your vault and preserve your selected data directory and AuthenticatorDesk.paths.json when upgrading.
升级前请备份保险库，保留所选数据目录和 AuthenticatorDesk.paths.json 路径配置。
SHA256SUMS.txt verifies all packages. Normal runs preserve published stable assets; manual force publication replaces assets, build notes, and the source tag.
SHA256SUMS.txt 可校验全部安装包；普通任务保留已发布的正式版，手动强制发布会更新附件、构建说明及源码标签。
"@ | Set-Content -LiteralPath (Join-Path $outputPath 'release-notes.md') -Encoding utf8NoBOM
Write-Output "PACKAGES_OK: $($packages.Count) packages in $outputPath"
