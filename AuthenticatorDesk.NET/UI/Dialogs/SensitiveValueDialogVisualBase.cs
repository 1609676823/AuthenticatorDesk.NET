using System.Runtime.InteropServices;
using AuthenticatorDesk.Localization;

namespace AuthenticatorDesk.UI.Dialogs;

/// <summary>
/// Compiled, side-effect-free visual base used by the out-of-process
/// WinForms designer for <see cref="SensitiveValueDialog"/>.
/// </summary>
public class SensitiveValueDialogVisualBase : ResponsiveWindow
{
    protected string _value = "SENSITIVE-VALUE";
    protected bool _revealed;

    protected AntdUI.PageHeader _header = null!;
    protected Panel _content = null!;
    protected TableLayoutPanel _layout = null!;
    protected AntdUI.Label _descriptionLabel = null!;
    protected AntdUI.Panel _valuePanel = null!;
    protected AntdUI.Label _valueLabel = null!;
    protected TableLayoutPanel _buttons = null!;
    protected AntdUI.Button _closeButton = null!;
    protected AntdUI.Button _copyButton = null!;
    protected AntdUI.Button _revealButton = null!;

    protected SensitiveValueDialogVisualBase()
    {
        var palette = ThemePaletteService.Current;
        var title = L.Get("Secret.Title");

        Text = title;
        Name = "SensitiveValueDialog";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(540, 310);
        MinimumSize = new Size(340, 280);
        Font = LocalizationFonts.Create(9.5F, FontStyle.Regular);
        FormBorderStyle = FormBorderStyle.None;
        BackColor = palette.Canvas;

        _header = new AntdUI.PageHeader
        {
            Name = "header",
            Dock = DockStyle.Top,
            Height = 54,
            Text = title,
            SubText = L.Get("SensitiveValueDialog.Subtitle"),
            ShowButton = true,
            DragMove = true,
            DividerShow = true,
            BackColor = palette.SurfaceElevated,
            ForeColor = palette.Text,
            DividerColor = palette.Border
        };
        _content = new Panel
        {
            Name = "content",
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            BackColor = palette.Canvas
        };
        _layout = new TableLayoutPanel
        {
            Name = "layout",
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        _descriptionLabel = new AntdUI.Label
        {
            Name = "descriptionLabel",
            Dock = DockStyle.Fill,
            Text = L.Get("Secret.Warning"),
            TextMultiLine = true,
            ForeColor = palette.TextSecondary
        };
        _valuePanel = new AntdUI.Panel
        {
            Name = "valuePanel",
            Dock = DockStyle.Fill,
            Back = palette.Surface,
            BorderWidth = 1,
            BorderColor = palette.Border,
            Radius = 14,
            Padding = new Padding(18, 10, 18, 10),
            Margin = new Padding(0, 0, 0, 4)
        };
        _valueLabel = new AntdUI.Label
        {
            Name = "valueLabel",
            Dock = DockStyle.Fill,
            Font = new Font("Cascadia Mono", 15F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            TextMultiLine = true,
            ForeColor = palette.Text
        };
        _valueLabel.Resize += (_, _) => UpdateValue();
        _valuePanel.Controls.Add(_valueLabel);

        _buttons = new TableLayoutPanel
        {
            Name = "buttons",
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        _buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        _buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        _buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
        _buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _closeButton = new AntdUI.Button
        {
            Name = "closeButton",
            Text = L.Get("Common.Done"),
            Type = AntdUI.TTypeMini.Primary,
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0)
        };
        _copyButton = new AntdUI.Button
        {
            Name = "copyButton",
            Text = L.Get("Common.Copy"),
            IconSvg = "CopyOutlined",
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 4, 0)
        };
        _revealButton = new AntdUI.Button
        {
            Name = "revealButton",
            Text = L.Get("Common.Show"),
            IconSvg = "EyeOutlined",
            Radius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 4, 0)
        };
        ThemePaletteService.StyleSecondaryButton(_copyButton);
        ThemePaletteService.StyleSecondaryButton(_revealButton);

        _closeButton.Click += (_, _) => Close();
        _copyButton.Click += (_, _) => CopyValue();
        _revealButton.Click += (_, _) =>
        {
            _revealed = !_revealed;
            UpdateRevealButton();
            UpdateValue();
        };

        _buttons.Controls.Add(_revealButton, 0, 0);
        _buttons.Controls.Add(_copyButton, 1, 0);
        _buttons.Controls.Add(_closeButton, 2, 0);
        _layout.Controls.Add(_descriptionLabel, 0, 0);
        _layout.Controls.Add(_valuePanel, 0, 1);
        _layout.Controls.Add(_buttons, 0, 2);
        _content.Controls.Add(_layout);
        Controls.Add(_content);
        Controls.Add(_header);

        CancelButton = _closeButton;
        UpdateValue();
        Shown += (_, _) => DialogSizing.FitToOwner(
            this,
            Owner,
            new Size(540, 310),
            new Size(340, 280));
    }

    protected void ConfigureSensitiveValueDialog(
        string title,
        string description,
        string value,
        bool initiallyRevealed)
    {
        _value = value;
        _revealed = initiallyRevealed;

        Text = title;
        _header.Text = title;
        _header.SubText = L.Get("SensitiveValueDialog.Subtitle");
        _descriptionLabel.Text = description;
        _closeButton.Text = L.Get("Common.Done");
        _copyButton.Text = L.Get("Common.Copy");
        UpdateRevealButton();
        UpdateValue();
    }

    protected void UpdateValue()
    {
        var displayValue = _revealed
            ? _value
            : new string('\u25CF', Math.Clamp(_value.Length, 8, 24));
        _valueLabel.Text = WrapValue(displayValue);
    }

    protected string WrapValue(string value)
    {
        var availableWidth = _valueLabel.ClientSize.Width;
        if (availableWidth <= 0 || string.IsNullOrEmpty(value))
        {
            return value;
        }

        var characterWidth = Math.Max(
            1,
            TextRenderer.MeasureText(
                "M",
                _valueLabel.Font,
                Size.Empty,
                TextFormatFlags.NoPadding).Width);
        var charactersPerLine = Math.Max(1, availableWidth / characterWidth);
        if (value.Length <= charactersPerLine)
        {
            return value;
        }

        var lines = new List<string>(
            (value.Length + charactersPerLine - 1) / charactersPerLine);
        for (var offset = 0; offset < value.Length; offset += charactersPerLine)
        {
            lines.Add(value.Substring(
                offset,
                Math.Min(charactersPerLine, value.Length - offset)));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void UpdateRevealButton()
    {
        _revealButton.Text = _revealed
            ? L.Get("Common.Hide")
            : L.Get("Common.Show");
        _revealButton.IconSvg = _revealed
            ? "EyeInvisibleOutlined"
            : "EyeOutlined";
    }

    private void CopyValue()
    {
        try
        {
            Clipboard.SetText(_value);
            AntdUI.Message.success(
                this,
                L.Get("Clipboard.Copied"),
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
}
