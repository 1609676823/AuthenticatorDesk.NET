using AuthenticatorDesk.Localization;

namespace AuthenticatorDesk.UI.Controls;

public sealed class ResponsiveFieldRow : Panel
{
    private readonly AntdUI.Label _label;
    private readonly Control _editor;
    private readonly int _editorHeight;
    private bool _stacked;

    public ResponsiveFieldRow(string label, Control editor, int editorHeight = 44)
    {
        _editor = editor;
        _editorHeight = Math.Max(32, editorHeight);
        _label = new AntdUI.Label
        {
            Text = label,
            Font = LocalizationFonts.Create(9F, FontStyle.Bold),
            ForeColor = ThemePaletteService.Current.TextSecondary
        };

        BackColor = Color.Transparent;
        Height = Math.Max(54, _editorHeight + 10);
        Margin = new Padding(0, 0, 0, 8);
        Controls.Add(_editor);
        Controls.Add(_label);
        Resize += (_, _) => LayoutChildren();
    }

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Control Editor => _editor;

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Stacked
    {
        get => _stacked;
        set
        {
            if (_stacked == value)
            {
                return;
            }

            _stacked = value;
            Height = value
                ? _editorHeight + 32
                : Math.Max(54, _editorHeight + 10);
            LayoutChildren();
        }
    }

    public void ApplyTheme()
    {
        _label.ForeColor = ThemePaletteService.Current.TextSecondary;
        Invalidate(true);
    }

    private void LayoutChildren()
    {
        var scale = DeviceDpi / 96F;
        int S(int value) => value == 0
            ? 0
            : Math.Max(1, (int)Math.Round(value * scale));

        if (_stacked)
        {
            var targetHeight = S(_editorHeight + 32);
            if (Height != targetHeight)
            {
                Height = targetHeight;
            }

            _label.SetBounds(0, 0, Width, S(24));
            _editor.SetBounds(0, S(26), Width, S(_editorHeight));
        }
        else
        {
            var targetHeight = S(Math.Max(54, _editorHeight + 10));
            if (Height != targetHeight)
            {
                Height = targetHeight;
            }

            var labelWidth = Math.Clamp(
                (int)(Width * 0.25F),
                S(112),
                S(168));
            _label.SetBounds(0, 0, labelWidth - S(12), Height);
            _editor.SetBounds(
                labelWidth,
                S(4),
                Math.Max(S(80), Width - labelWidth),
                S(_editorHeight));
        }
    }
}
