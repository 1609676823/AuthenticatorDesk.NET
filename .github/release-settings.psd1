@{
    # Default for scheduled/tag builds and the "repository-default" manual option.
    # 定时/标签构建及手动选择 repository-default 时使用此配置。
    # self-contained: bundle .NET / 附带运行时，适合面向普通用户发布（推荐）。
    # framework-dependent: require .NET 10 Windows Desktop Runtime / 需要预装桌面运行时。
    # both: publish both variants for each RID / 每种架构同时生成两种部署包。
    DeploymentMode = 'self-contained'

    # This WinForms application only supports these Windows RIDs.
    # x86 = Windows 32-bit process; arm64 is different from unsupported win-arm (32-bit).
    RuntimeIdentifiers = @('win-x86', 'win-x64', 'win-arm64')

    # No RID + UseAppHost=true: AnyCPU DLL + build-SDK-architecture EXE, framework-dependent.
    # 与 VS 可移植发布一致：主 DLL 保持 AnyCPU，EXE 对应构建 SDK 架构（当前 CI 为 x64）。
    # Windows-only; requires the .NET 10 Windows Desktop Runtime matching the process architecture.
    IncludePortable = $true
}
