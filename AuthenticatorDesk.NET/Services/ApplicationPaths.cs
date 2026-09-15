namespace AuthenticatorDesk.Services;

public static class ApplicationPaths
{
    private static readonly object SyncRoot = new();
    private static readonly string DefaultProgramDirectory =
        StorageLocationService.NormalizeDirectory(AppContext.BaseDirectory);
    private static readonly string DefaultLegacyDataDirectory =
        StorageLocationService.NormalizeDirectory(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AuthenticatorDesk"));

    private static StorageLocationService _locationService = new(
        DefaultProgramDirectory,
        DefaultLegacyDataDirectory);
    private static StorageLocation? _location;

    public static string ProgramDirectory => DefaultProgramDirectory;

    public static string DataDirectory
    {
        get
        {
            EnsureInitialized();
            return _location!.DataDirectory;
        }
    }

    public static StorageLocationSource LocationSource
    {
        get
        {
            EnsureInitialized();
            return _location!.Source;
        }
    }

    public static string VaultFile => Path.Combine(
        DataDirectory,
        StorageLocationService.VaultFileName);

    public static string LogsDirectory => Path.Combine(DataDirectory, "logs");

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            _location = _locationService.Resolve();
        }
    }

    public static void SetDataDirectory(string dataDirectory)
    {
        var normalized = StorageLocationService.NormalizeDirectory(dataDirectory);
        Directory.CreateDirectory(normalized);
        var logsDirectory = Path.Combine(normalized, "logs");
        Directory.CreateDirectory(logsDirectory);
        VerifyDirectoryWritable(normalized);
        VerifyDirectoryWritable(logsDirectory);

        var vaultFile = Path.Combine(normalized, StorageLocationService.VaultFileName);
        if (File.Exists(vaultFile))
        {
            VerifyFileWritable(vaultFile);
        }

        lock (SyncRoot)
        {
            _location = _locationService.Persist(normalized);
        }
    }

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }

    public static void EnsureWritable()
    {
        EnsureCreated();
        VerifyDirectoryWritable(DataDirectory);
        VerifyDirectoryWritable(LogsDirectory);
        if (File.Exists(VaultFile))
        {
            VerifyFileWritable(VaultFile);
        }
    }

    private static void EnsureInitialized()
    {
        if (_location is not null)
        {
            return;
        }

        Initialize();
    }

    private static void VerifyDirectoryWritable(string directory)
    {
        var probeFile = Path.Combine(
            directory,
            $".authenticatordesk-write-probe-{Guid.NewGuid():N}.tmp");
        using var stream = new FileStream(
            probeFile,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1,
            FileOptions.DeleteOnClose);
        stream.WriteByte(0);
        stream.Flush(flushToDisk: true);
    }

    private static void VerifyFileWritable(string filePath)
    {
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.Read);
    }
}
