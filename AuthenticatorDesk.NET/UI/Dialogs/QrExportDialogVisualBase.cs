using System.Runtime.InteropServices;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;
using AuthenticatorDesk.Services;

namespace AuthenticatorDesk.UI.Dialogs;

/// <summary>
/// Compiled, side-effect-free visual base for the inherited-form designer.
/// The runtime dialog still supplies the authenticator entry through its
/// public constructor, while the designer can safely instantiate this base.
/// </summary>
public class QrExportDialogVisualBase : ResponsiveWindow
{
    protected readonly Bitmap _qrBitmap;
    protected readonly AntdUI.Input _uriInput;

    protected QrExportDialogVisualBase()
        : this(CreateDesignerPreviewEntry(), useDesignerPreview: true)
    {
    }

    protected QrExportDialogVisualBase(AuthenticatorEntry entry)
        : this(entry, useDesignerPreview: false)
    {
    }

    private QrExportDialogVisualBase(
        AuthenticatorEntry entry,
        bool useDesignerPreview)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Name = "QrExportDialog";
        Text = L.Get("QrExportDialog.Title");
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(600, 680);
        MinimumSize = new Size(340, 500);
        Font = LocalizationFonts.Create(9.5F, FontStyle.Regular);
        FormBorderStyle = FormBorderStyle.None;
        BackColor = ThemePaletteService.Current.Canvas;

        var uri = useDesignerPreview
            ? "otpauth://totp/Example:designer@example.com" +
              "?secret=JBSWY3DPEHPK3PXP&issuer=Example"
            : OtpAuthUriService.Build(entry, includeProviderData: true);
        _qrBitmap = useDesignerPreview
            ? CreateDesignerPreviewBitmap()
            : QrCodeService.Create(uri, 8);

        var header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 54,
            Text = L.Get("QrExportDialog.Title"),
            SubText = $"{entry.DisplayIssuer} · {entry.DisplayName}",
            ShowButton = true,
            DragMove = true,
            DividerShow = true,
            BackColor = ThemePaletteService.Current.SurfaceElevated,
            ForeColor = ThemePaletteService.Current.Text,
            DividerColor = ThemePaletteService.Current.Border
        };
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            BackColor = ThemePaletteService.Current.Canvas
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var pictureHost = new AntdUI.Panel
        {
            Dock = DockStyle.Fill,
            Back = Color.White,
            BorderWidth = 1,
            BorderColor = ThemePaletteService.Current.Border,
            Radius = 20,
            Shadow = 8,
            ShadowColor = Color.Black,
            Margin = new Padding(48, 4, 48, 16),
            Padding = new Padding(18)
        };
        layout.Resize += (_, _) =>
        {
            var horizontalMargin = layout.ClientSize.Width < ScaleLogical(420)
                ? ScaleLogical(12)
                : ScaleLogical(48);
            pictureHost.Margin = new Padding(
                horizontalMargin,
                ScaleLogical(4),
                horizontalMargin,
                ScaleLogical(16));
        };
        var picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = _qrBitmap,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };
        pictureHost.Controls.Add(picture);

        var uriTitle = new AntdUI.Label
        {
            Dock = DockStyle.Fill,
            Text = L.Get("QrExportDialog.UriLabel"),
            Font = LocalizationFonts.Create(9F, FontStyle.Bold),
            ForeColor = ThemePaletteService.Current.Text
        };
        _uriInput = new AntdUI.Input
        {
            Dock = DockStyle.Fill,
            Text = uri,
            ReadOnly = true,
            Multiline = true,
            Radius = 10
        };
        var warning = new AntdUI.Label
        {
            Dock = DockStyle.Fill,
            Text = L.Get("QrExportDialog.SecretWarning"),
            ForeColor = ThemePaletteService.Current.Warning
        };
        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var closeButton = new AntdUI.Button
        {
            Text = L.Get("Common.Done"),
            Type = AntdUI.TTypeMini.Primary,
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0)
        };
        var saveButton = new AntdUI.Button
        {
            Text = L.Get("QrExportDialog.SaveQr"),
            IconSvg = "SaveOutlined",
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 4, 0)
        };
        var copyButton = new AntdUI.Button
        {
            Text = L.Get("QrExportDialog.CopyUri"),
            IconSvg = "CopyOutlined",
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 4, 0)
        };
        ThemePaletteService.StyleSecondaryButton(saveButton);
        ThemePaletteService.StyleSecondaryButton(copyButton);

        closeButton.Click += (_, _) => Close();
        copyButton.Click += (_, _) => CopyUri();
        saveButton.Click += (_, _) => SaveQr(entry);

        buttons.Controls.Add(copyButton, 0, 0);
        buttons.Controls.Add(saveButton, 1, 0);
        buttons.Controls.Add(closeButton, 2, 0);
        layout.Controls.Add(pictureHost, 0, 0);
        layout.Controls.Add(uriTitle, 0, 1);
        layout.Controls.Add(_uriInput, 0, 2);
        layout.Controls.Add(warning, 0, 3);
        layout.Controls.Add(buttons, 0, 4);
        content.Controls.Add(layout);
        Controls.Add(content);
        Controls.Add(header);

        CancelButton = closeButton;
        Shown += (_, _) => DialogSizing.FitToOwner(
            this,
            Owner,
            new Size(600, 680),
            new Size(340, 500));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _qrBitmap.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CopyUri()
    {
        try
        {
            Clipboard.SetText(_uriInput.Text);
            AntdUI.Message.success(
                this,
                L.Get("QrExportDialog.Notification.UriCopied"),
                autoClose: 2);
        }
        catch (ExternalException)
        {
            AntdUI.Message.error(
                this,
                L.Get("Clipboard.Unavailable"),
                autoClose: 3);
        }
    }

    private void SaveQr(AuthenticatorEntry entry)
    {
        using var dialog = new SaveFileDialog
        {
            Title = L.Get("QrExportDialog.FileDialog.Title"),
            Filter = L.Get("QrExportDialog.FileDialog.Filter"),
            FileName = MakeSafeFileName($"{entry.DisplayIssuer}-{entry.DisplayName}.png"),
            AddExtension = true,
            DefaultExt = "png"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _qrBitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            AntdUI.Message.success(
                this,
                L.Get("QrExportDialog.Notification.Saved"),
                autoClose: 2);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ExternalException)
        {
            AntdUI.Message.error(
                this,
                L.Format("QrExportDialog.Error.SaveFailed", exception.Message),
                autoClose: 4);
        }
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(character, '_');
        }

        return value;
    }

    private static AuthenticatorEntry CreateDesignerPreviewEntry()
    {
        return new AuthenticatorEntry
        {
            Name = "designer@example.com",
            Issuer = "Example",
            Secret = "JBSWY3DPEHPK3PXP",
            Kind = AuthenticatorKind.Totp,
            Digits = 6,
            Period = 30
        };
    }

    private static Bitmap CreateDesignerPreviewBitmap()
    {
        const int size = 256;
        const int cell = 16;
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var brush = new SolidBrush(Color.Black);

        DrawFinderPattern(graphics, brush, cell, cell, cell);
        DrawFinderPattern(graphics, brush, size - (cell * 6), cell, cell);
        DrawFinderPattern(graphics, brush, cell, size - (cell * 6), cell);

        for (var row = 1; row < 15; row++)
        {
            for (var column = 1; column < 15; column++)
            {
                var inFinderArea =
                    column <= 5 && row <= 5 ||
                    column >= 10 && row <= 5 ||
                    column <= 5 && row >= 10;
                if (!inFinderArea &&
                    ((row * 11) + (column * 7) + (row * column)) % 5 <= 1)
                {
                    graphics.FillRectangle(
                        brush,
                        column * cell,
                        row * cell,
                        cell,
                        cell);
                }
            }
        }

        return bitmap;
    }

    private static void DrawFinderPattern(
        Graphics graphics,
        Brush brush,
        int left,
        int top,
        int cell)
    {
        graphics.FillRectangle(brush, left, top, cell * 5, cell * 5);
        graphics.FillRectangle(
            Brushes.White,
            left + cell,
            top + cell,
            cell * 3,
            cell * 3);
        graphics.FillRectangle(
            brush,
            left + (cell * 2),
            top + (cell * 2),
            cell,
            cell);
    }
}
