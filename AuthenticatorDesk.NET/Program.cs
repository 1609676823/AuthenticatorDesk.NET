using System.Diagnostics;
using System.Text;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;
using AuthenticatorDesk.Services;
using AuthenticatorDesk.UI;
using AuthenticatorDesk.UI.Dialogs;

namespace AuthenticatorDesk;

internal static class Program
{
    // Release metadata: update AppVersion here for each application release.
    internal const string AppName = "AuthenticatorDesk";
    internal const string AppVersion = "1.0.4";
    internal const string GitHubRepositoryUrl =
        "https://github.com/1609676823/AuthenticatorDesk.NET";

    private const string InstanceMutexName = @"Local\AuthenticatorDesk.NET.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        var storedLanguage = LanguagePreferenceStore.Load();
        L.Initialize(storedLanguage);

        ApplicationConfiguration.Initialize();
        using var instanceMutex = new Mutex(
            initiallyOwned: true,
            InstanceMutexName,
            out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                L.Get("App.AlreadyRunning"),
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ThemePaletteService.Apply(AppThemeMode.Dark);
        if (!PrepareApplicationPaths())
        {
            return;
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, eventArgs) =>
            HandleFatalException(
                "UI_THREAD_UNHANDLED",
                "Diagnostics.Context.UiThreadException",
                eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            HandleFatalException(
                "APPDOMAIN_UNHANDLED",
                "Diagnostics.Context.UnhandledException",
                eventArgs.ExceptionObject as Exception ??
                new InvalidOperationException(L.Get("Error.UnknownUnhandled")));

        using var vault = new VaultService(ApplicationPaths.VaultFile);
        var payload = OpenVault(vault);
        if (payload is null)
        {
            return;
        }

        try
        {
            ThemePaletteService.Apply(payload.Settings.Theme);
            if (!vault.Exists)
            {
                vault.Save(payload);
            }

            var startMinimized = args.Any(argument =>
                argument.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                argument.Equals("/minimized", StringComparison.OrdinalIgnoreCase));
            using var mainForm = new MainForm(vault, payload, startMinimized);
            Application.Run(mainForm);
            if (mainForm.RestartRequested)
            {
                instanceMutex.ReleaseMutex();
                RestartProcess();
            }
        }
        catch (Exception exception)
        {
            HandleFatalException(
                "APPLICATION_START_FAILED",
                "Diagnostics.Context.StartupFailure",
                exception);
        }
    }

    private static void RestartProcess()
    {
        try
        {
            var executable = Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    L.Get("Error.Restart.ExecutablePathUnknown"));
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                L.Format("Restart.ManualRequired.Message", exception.Message),
                L.Get("Restart.ManualRequired.Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static bool PrepareApplicationPaths()
    {
        var initialized = false;
        try
        {
            ApplicationPaths.Initialize();
            initialized = true;
            ApplicationPaths.EnsureWritable();
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (initialized &&
                ApplicationPaths.LocationSource == StorageLocationSource.ProgramDirectory &&
                !File.Exists(ApplicationPaths.VaultFile))
            {
                return SelectDataDirectory(exception, allowCreate: true);
            }

            return SelectDataDirectory(exception, allowCreate: false);
        }
    }

    private static bool SelectDataDirectory(
        Exception originalException,
        bool allowCreate)
    {
        while (true)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = allowCreate
                    ? L.Get("Startup.DataDirectory.SelectProgramWritable")
                    : L.Get("Startup.DataDirectory.SelectExisting"),
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true,
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                InitialDirectory =
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dialog.ShowDialog() != DialogResult.OK ||
                string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                ShowDataDirectoryError(originalException);
                return false;
            }

            try
            {
                var targetDirectory =
                    StorageLocationService.NormalizeDirectory(dialog.SelectedPath);
                Directory.CreateDirectory(targetDirectory);
                var targetVault = Path.Combine(
                    targetDirectory,
                    StorageLocationService.VaultFileName);
                if (!File.Exists(targetVault))
                {
                    if (!allowCreate)
                    {
                        MessageBox.Show(
                            L.Get("Startup.DataDirectory.VaultNotFound.Message"),
                            L.Get("Startup.DataDirectory.VaultNotFound.Title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        continue;
                    }

                    using var initialVault = new VaultService(targetVault);
                    var initialPayload = initialVault.Open();
                    initialVault.Save(initialPayload);
                }
                else if (!ValidateExistingVault(targetVault))
                {
                    continue;
                }

                ApplicationPaths.SetDataDirectory(targetDirectory);
                ApplicationPaths.EnsureWritable();
                return true;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or
                    InvalidDataException or VaultAuthenticationException or
                    ArgumentException or NotSupportedException)
            {
                MessageBox.Show(
                    L.Format(
                        "Startup.DataDirectory.InvalidSelection.Message",
                        exception.Message),
                    L.Get("Startup.DataDirectory.InvalidSelection.Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    private static bool ValidateExistingVault(string filePath)
    {
        using var candidate = new VaultService(filePath);
        string? password = null;
        if (candidate.InspectProtectionMode() == VaultProtectionMode.Password)
        {
            password = PasswordDialog.Prompt(
                owner: null,
                L.Get("Startup.Vault.ValidateExisting.Title"),
                L.Get("Startup.Vault.ValidateExisting.Description"),
                submitText: L.Get("Common.Verify"));
            if (password is null)
            {
                return false;
            }
        }

        var payload = candidate.Open(password);
        payload.Entries.Clear();
        return true;
    }

    private static void ShowDataDirectoryError(Exception exception)
    {
        MessageBox.Show(
            L.Format(
                "Startup.DataDirectory.Unavailable.Message",
                exception.Message),
            L.Get("Startup.DataDirectory.Unavailable.Title"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static VaultPayload? OpenVault(VaultService vault)
    {
        while (true)
        {
            VaultProtectionMode? inspectedMode = null;
            try
            {
                string? password = null;
                if (vault.Exists)
                {
                    inspectedMode = vault.InspectProtectionMode();
                    if (inspectedMode == VaultProtectionMode.Password)
                    {
                        password = PasswordDialog.Prompt(
                            owner: null,
                            L.Get("Startup.Vault.Unlock.Title"),
                            L.Get("Startup.Vault.Unlock.Description"),
                            submitText: L.Get("Common.Unlock"));
                        if (password is null)
                        {
                            return null;
                        }
                    }
                }

                return vault.Open(password);
            }
            catch (VaultAuthenticationException exception)
            {
                MessageBox.Show(
                    exception.Message,
                    L.Get("Startup.Vault.Unlock.ErrorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                if (!vault.Exists ||
                    inspectedMode != VaultProtectionMode.Password)
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                HandleFatalException(
                    "VAULT_READ_FAILED",
                    "Diagnostics.Context.VaultReadFailure",
                    exception);
                return null;
            }
        }
    }

    private static void HandleFatalException(
        string diagnosticCode,
        string contextKey,
        Exception exception)
    {
        try
        {
            ApplicationPaths.EnsureCreated();
            var logFile = Path.Combine(
                ApplicationPaths.LogsDirectory,
                $"error-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(
                logFile,
                $"[{DateTimeOffset.Now:O}] {diagnosticCode}{Environment.NewLine}" +
                exception +
                Environment.NewLine +
                Environment.NewLine,
                new UTF8Encoding(false));
        }
        catch
        {
            // Error reporting must not trigger another crash.
        }

        MessageBox.Show(
            L.Format(
                "Fatal.ErrorWithLogLocation",
                L.Get(contextKey),
                exception.Message),
            AppName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
