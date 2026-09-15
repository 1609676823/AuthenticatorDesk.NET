using System.Diagnostics;
using AuthenticatorDesk.Localization;

namespace AuthenticatorDesk.UI.Dialogs;

/// <summary>
/// Compiled, side-effect-free visual base used by the out-of-process
/// WinForms designer for <see cref="AboutLicensesDialog"/>.
/// </summary>
public class AboutLicensesDialogVisualBase : ResponsiveWindow
{
    protected AntdUI.PageHeader _header = null!;
    protected AntdUI.Panel _content = null!;
    protected TableLayoutPanel _layout = null!;
    protected AntdUI.Label _productLabel = null!;
    protected AntdUI.Label _versionLabel = null!;
    protected RichTextBox _legalSummary = null!;
    protected FlowLayoutPanel _actions = null!;
    protected AntdUI.Button _closeButton = null!;
    protected AntdUI.Button _repositoryButton = null!;
    protected AntdUI.Button _noticesButton = null!;
    protected AntdUI.Button _licenseButton = null!;

    protected AboutLicensesDialogVisualBase()
    {
        var palette = ThemePaletteService.Current;
        var title = L.Get("About.Title");

        Text = title;
        Name = "AboutLicensesDialog";
        ClientSize = new Size(680, 620);
        MinimumSize = new Size(420, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
        Font = LocalizationFonts.Create(9.5F);
        BackColor = palette.Canvas;

        _header = new AntdUI.PageHeader
        {
            Name = "header",
            Dock = DockStyle.Top,
            Height = 48,
            Text = title,
            ShowButton = true,
            ShowIcon = false,
            DragMove = true,
            EnableDoubleClickMaximize = false,
            MaximizeBox = false,
            MinimizeBox = false,
            FullBox = false,
            DividerShow = true,
            BackExtend = string.Empty,
            BackColor = palette.SurfaceElevated,
            ForeColor = palette.Text,
            DividerColor = palette.Border
        };

        _content = new AntdUI.Panel
        {
            Name = "content",
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 22),
            Back = palette.Surface,
            Radius = 0
        };
        _layout = new TableLayoutPanel
        {
            Name = "layout",
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 188F));

        _productLabel = new AntdUI.Label
        {
            Name = "productLabel",
            Dock = DockStyle.Fill,
            Text = Program.AppName,
            Font = LocalizationFonts.Create(21F, FontStyle.Bold),
            ForeColor = palette.Text,
            BackColor = Color.Transparent
        };
        _versionLabel = new AntdUI.Label
        {
            Name = "versionLabel",
            Dock = DockStyle.Fill,
            Text = L.Format("About.Version", Program.AppVersion),
            Font = LocalizationFonts.Create(9F),
            ForeColor = palette.TextSecondary,
            BackColor = Color.Transparent
        };
        _legalSummary = new RichTextBox
        {
            Name = "legalSummary",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = palette.Surface,
            ForeColor = palette.Text,
            Font = LocalizationFonts.Create(10F),
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Text = string.Join(
                Environment.NewLine + Environment.NewLine,
                L.Get("About.ProjectCopyright"),
                L.Get("About.ModificationNotice"),
                L.Get("About.WinAuthAttribution"),
                L.Get("About.LicenseSummary"),
                L.Get("About.NoWarranty"),
                L.Get("About.NonAffiliation"))
        };
        _legalSummary.Select(0, 0);

        _actions = new FlowLayoutPanel
        {
            Name = "actions",
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, 12, 0, 0)
        };
        _closeButton = CreateActionButton(
            L.Get("Common.Done"),
            icon: null,
            minimumWidth: 92);
        _closeButton.Name = "closeButton";
        _closeButton.DialogResult = DialogResult.Cancel;

        _repositoryButton = CreateActionButton(
            L.Get("About.ProjectRepository"),
            "GithubOutlined",
            minimumWidth: 156);
        _repositoryButton.Name = "repositoryButton";
        _repositoryButton.Click += (_, _) =>
            OpenTarget(Program.GitHubRepositoryUrl);

        _noticesButton = CreateActionButton(
            L.Get("About.ThirdPartyNotices"),
            "FileTextOutlined",
            minimumWidth: 178);
        _noticesButton.Name = "noticesButton";
        _noticesButton.Click += (_, _) =>
            OpenLocalDocument("THIRD-PARTY-NOTICES.md");

        _licenseButton = CreateActionButton(
            L.Get("About.ViewLicense"),
            "SafetyCertificateOutlined",
            minimumWidth: 160);
        _licenseButton.Name = "licenseButton";
        _licenseButton.Type = AntdUI.TTypeMini.Primary;
        _licenseButton.Click += (_, _) => OpenLocalDocument("LICENSE.txt");

        ThemePaletteService.StyleSecondaryButton(_closeButton);
        ThemePaletteService.StyleSecondaryButton(_repositoryButton);
        ThemePaletteService.StyleSecondaryButton(_noticesButton);

        _actions.Controls.Add(_closeButton);
        _actions.Controls.Add(_repositoryButton);
        _actions.Controls.Add(_noticesButton);
        _actions.Controls.Add(_licenseButton);

        _layout.Controls.Add(_productLabel, 0, 0);
        _layout.Controls.Add(_versionLabel, 0, 1);
        _layout.Controls.Add(_legalSummary, 0, 2);
        _layout.Controls.Add(_actions, 0, 3);
        _content.Controls.Add(_layout);
        Controls.Add(_content);
        Controls.Add(_header);

        CancelButton = _closeButton;
    }

    protected void ConfigureAboutLicensesDialog()
    {
        var title = L.Get("About.Title");

        Text = title;
        _header.Text = title;
        _productLabel.Text = Program.AppName;
        _versionLabel.Text = L.Format("About.Version", Program.AppVersion);
        _legalSummary.Text = string.Join(
            Environment.NewLine + Environment.NewLine,
            L.Get("About.ProjectCopyright"),
            L.Get("About.ModificationNotice"),
            L.Get("About.WinAuthAttribution"),
            L.Get("About.LicenseSummary"),
            L.Get("About.NoWarranty"),
            L.Get("About.NonAffiliation"));
        _legalSummary.Select(0, 0);

        ConfigureActionButton(
            _closeButton,
            L.Get("Common.Done"),
            minimumWidth: 92);
        ConfigureActionButton(
            _repositoryButton,
            L.Get("About.ProjectRepository"),
            minimumWidth: 156);
        ConfigureActionButton(
            _noticesButton,
            L.Get("About.ThirdPartyNotices"),
            minimumWidth: 178);
        ConfigureActionButton(
            _licenseButton,
            L.Get("About.ViewLicense"),
            minimumWidth: 160);
    }

    private AntdUI.Button CreateActionButton(
        string text,
        string? icon,
        int minimumWidth)
    {
        var button = new AntdUI.Button
        {
            IconSvg = icon,
            Radius = 9
        };
        ConfigureActionButton(button, text, minimumWidth);
        return button;
    }

    private void ConfigureActionButton(
        AntdUI.Button button,
        string text,
        int minimumWidth)
    {
        var measuredWidth =
            TextRenderer.MeasureText(text, Font).Width + ScaleLogical(44);
        button.Text = text;
        button.Size = new Size(
            Math.Clamp(
                measuredWidth,
                ScaleLogical(minimumWidth),
                ScaleLogical(232)),
            ScaleLogical(40));
        button.Margin = new Padding(
            ScaleLogical(4),
            ScaleLogical(4),
            ScaleLogical(4),
            ScaleLogical(4));
    }

    private void OpenLocalDocument(string fileName)
    {
        var path = ResolveLocalDocument(fileName);
        if (path is null)
        {
            ShowOpenError(L.Format("About.DocumentMissing", fileName));
            return;
        }

        OpenTarget(path);
    }

    private void OpenTarget(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            ShowOpenError(L.Format("About.OpenFailed", exception.Message));
        }
    }

    private void ShowOpenError(string message)
    {
        AntdUI.Message.error(this, message, autoClose: 7);
    }

    private static string? ResolveLocalDocument(string fileName)
    {
        var publishedPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(publishedPath))
        {
            return publishedPath;
        }

        // The legal files are copied beside the executable by `dotnet publish`,
        // but ordinary development builds run from bin/... without those files.
        // Walk only to a verified solution root so an unrelated parent file is
        // never opened by mistake.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; directory is not null && depth < 7; depth++)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "AuthenticatorDesk.NET.slnx")))
            {
                var repositoryPath = Path.Combine(directory.FullName, fileName);
                return File.Exists(repositoryPath) ? repositoryPath : null;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
