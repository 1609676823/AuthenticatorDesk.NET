using AuthenticatorDesk.Localization;
using AntButton = AntdUI.Button;
using AntInput = AntdUI.Input;
using AntLabel = AntdUI.Label;

namespace AuthenticatorDesk.UI.Dialogs;

/// <summary>
/// Compiled, side-effect-free visual base used by the out-of-process
/// WinForms designer. Runtime prompts supply their text and layout mode
/// through <see cref="PasswordDialog"/>.
/// </summary>
public class PasswordDialogVisualBase : ResponsiveWindow
{
    private readonly AntInput _passwordInput;
    private readonly AntInput? _confirmInput;
    private readonly AntLabel _errorLabel;

    protected PasswordDialogVisualBase()
        : this(
            L.Get("Settings.VaultProtection.SetPassword"),
            L.Get("Settings.VaultProtection.PasswordPrompt"),
            requireConfirmation: true,
            L.Get("Common.Confirm"))
    {
    }

    protected PasswordDialogVisualBase(
        string title,
        string description,
        bool requireConfirmation,
        string submitText)
    {
        Text = title;
        ClientSize = new Size(440, requireConfirmation ? 350 : 296);
        MinimumSize = new Size(340, requireConfirmation ? 330 : 276);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
        Font = LocalizationFonts.Create(9.5F, FontStyle.Regular);
        BackColor = ThemePaletteService.Current.Canvas;

        var header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 46,
            Text = title,
            ShowButton = true,
            ShowIcon = false,
            DragMove = true,
            EnableDoubleClickMaximize = false,
            MaximizeBox = false,
            MinimizeBox = false,
            FullBox = false,
            DividerShow = false,
            BackExtend = string.Empty,
            BackColor = ThemePaletteService.Current.SurfaceElevated,
            ForeColor = ThemePaletteService.Current.Text,
            DividerColor = ThemePaletteService.Current.Border
        };

        var content = new AntdUI.Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 18, 28, 24),
            Back = ThemePaletteService.Current.Surface,
            Radius = 0
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = requireConfirmation ? 7 : 5,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        if (requireConfirmation)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        }

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        var descriptionLabel = new AntLabel
        {
            Dock = DockStyle.Fill,
            Text = description,
            TextMultiLine = true,
            ForeColor = ThemePaletteService.Current.TextSecondary,
            Font = LocalizationFonts.Create(9.5F, FontStyle.Regular)
        };
        _passwordInput = new AntInput
        {
            Dock = DockStyle.Fill,
            PlaceholderText = L.Get("PasswordDialog.PasswordPlaceholder"),
            PrefixSvg = "LockOutlined",
            UseSystemPasswordChar = true,
            PasswordCopy = false,
            PasswordPaste = true,
            Radius = 10,
            MaxLength = 256
        };
        _passwordInput.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Enter)
            {
                Submit();
            }
        };

        _errorLabel = new AntLabel
        {
            Dock = DockStyle.Fill,
            ForeColor = ThemePaletteService.Current.Danger,
            Font = LocalizationFonts.Create(8.5F, FontStyle.Regular),
            Text = string.Empty
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        var submitButton = new AntButton
        {
            Text = submitText,
            Type = AntdUI.TTypeMini.Primary,
            Radius = 9,
            Size = new Size(104, 38)
        };
        var cancelButton = new AntButton
        {
            Text = L.Get("Common.Cancel"),
            Radius = 9,
            Size = new Size(88, 38),
            DialogResult = DialogResult.Cancel
        };
        ThemePaletteService.StyleSecondaryButton(cancelButton);
        submitButton.Click += (_, _) => Submit();
        buttonPanel.Controls.Add(submitButton);
        buttonPanel.Controls.Add(cancelButton);

        layout.Controls.Add(descriptionLabel, 0, 0);
        layout.Controls.Add(_passwordInput, 0, 1);

        var nextRow = 2;
        if (requireConfirmation)
        {
            _confirmInput = new AntInput
            {
                Dock = DockStyle.Fill,
                PlaceholderText = L.Get("PasswordDialog.ConfirmPasswordPlaceholder"),
                PrefixSvg = "SafetyCertificateOutlined",
                UseSystemPasswordChar = true,
                PasswordCopy = false,
                PasswordPaste = true,
                Radius = 10,
                MaxLength = 256
            };
            _confirmInput.KeyDown += (_, eventArgs) =>
            {
                if (eventArgs.KeyCode == Keys.Enter)
                {
                    Submit();
                }
            };
            layout.Controls.Add(_confirmInput, 0, 3);
            nextRow = 4;
        }

        layout.Controls.Add(_errorLabel, 0, nextRow);
        layout.Controls.Add(buttonPanel, 0, nextRow + 2);
        content.Controls.Add(layout);
        Controls.Add(content);
        Controls.Add(header);

        AcceptButton = submitButton;
        CancelButton = cancelButton;
        Shown += (_, _) => _passwordInput.Focus();
    }

    protected string PasswordValue => _passwordInput.Text;

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(_passwordInput.Text))
        {
            _errorLabel.Text = L.Get("PasswordDialog.Error.PasswordRequired");
            _passwordInput.Focus();
            return;
        }

        if (_confirmInput is not null &&
            !string.Equals(_passwordInput.Text, _confirmInput.Text, StringComparison.Ordinal))
        {
            _errorLabel.Text = L.Get("PasswordDialog.Error.PasswordMismatch");
            _confirmInput.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
