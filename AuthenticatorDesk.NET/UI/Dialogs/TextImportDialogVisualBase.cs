using System.Runtime.InteropServices;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.UI;

namespace AuthenticatorDesk.UI.Dialogs;

/// <summary>
/// Compiled, side-effect-free visual base used by the out-of-process
/// WinForms designer. Runtime prompts provide the initial text through
/// <see cref="TextImportDialog"/>.
/// </summary>
public class TextImportDialogVisualBase : ResponsiveWindow
{
    private readonly AntdUI.Input _input;

    protected TextImportDialogVisualBase()
        : this(string.Empty)
    {
    }

    protected TextImportDialogVisualBase(string initialText)
    {
        Text = L.Get("TextImportDialog.Title");
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(620, 450);
        MinimumSize = new Size(340, 340);
        Font = LocalizationFonts.Create(9.5F, FontStyle.Regular);
        FormBorderStyle = FormBorderStyle.None;
        BackColor = ThemePaletteService.Current.Canvas;

        var header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 54,
            Text = L.Get("TextImportDialog.Title"),
            SubText = L.Get("TextImportDialog.Subtitle"),
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
            Padding = new Padding(24, 20, 24, 20),
            BackColor = ThemePaletteService.Current.Canvas
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var hint = new AntdUI.Label
        {
            Dock = DockStyle.Fill,
            Text = L.Get("TextImportDialog.Hint"),
            ForeColor = ThemePaletteService.Current.TextSecondary
        };
        _input = new AntdUI.Input
        {
            Dock = DockStyle.Fill,
            Text = initialText,
            Multiline = true,
            AutoScroll = true,
            PlaceholderText = "otpauth://totp/Example:user@example.com?secret=...",
            Radius = 12
        };
        var status = new AntdUI.Label
        {
            Dock = DockStyle.Fill,
            ForeColor = ThemePaletteService.Current.Danger
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
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var importButton = new AntdUI.Button
        {
            Text = L.Get("TextImportDialog.Import"),
            IconSvg = "ImportOutlined",
            Type = AntdUI.TTypeMini.Primary,
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0)
        };
        var cancelButton = new AntdUI.Button
        {
            Text = L.Get("Common.Cancel"),
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 4, 0)
        };
        ThemePaletteService.StyleSecondaryButton(cancelButton);
        var pasteButton = new AntdUI.Button
        {
            Text = L.Get("TextImportDialog.Paste"),
            IconSvg = "CopyOutlined",
            Ghost = true,
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 4, 0)
        };

        importButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_input.Text))
            {
                status.Text = L.Get("TextImportDialog.Error.Empty");
                _input.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        pasteButton.Click += (_, _) =>
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    _input.Text = Clipboard.GetText();
                    status.Text = string.Empty;
                }
            }
            catch (ExternalException)
            {
                status.Text = L.Get("TextImportDialog.Error.ClipboardUnavailable");
            }
        };

        buttons.Controls.Add(pasteButton, 0, 0);
        buttons.Controls.Add(cancelButton, 1, 0);
        buttons.Controls.Add(importButton, 2, 0);
        layout.Controls.Add(hint, 0, 0);
        layout.Controls.Add(_input, 0, 1);
        layout.Controls.Add(status, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        content.Controls.Add(layout);
        Controls.Add(content);
        Controls.Add(header);

        AcceptButton = importButton;
        CancelButton = cancelButton;
        Shown += (_, _) => _input.Focus();
    }

    protected string InputTextValue => _input.Text;

}
