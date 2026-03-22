using System.Diagnostics;
using cHelper.Models;
using cHelper.Services;
using cHelper.Utils;

namespace cHelper.Controls;

public class ApiSettingsControl : UserControl
{
    private readonly AnthropicService _anthropic;
    private readonly SettingsCallback _onSettingsChanged;
    private TextBox _apiKeyBox = null!;
    private TossButton _toggleBtn = null!;
    private TossButton _validateBtn = null!;
    private Label _statusLabel = null!;
    private ComboBox _modelCombo = null!;
    private CheckBox _startupCheck = null!;
    private bool _keyVisible = false;
    private AppSettings _settings;

    public delegate void SettingsCallback(AppSettings settings);

    public ApiSettingsControl(AnthropicService anthropic, AppSettings settings, SettingsCallback onChanged)
    {
        _anthropic = anthropic;
        _settings = settings;
        _onSettingsChanged = onChanged;
        BuildUI();
        LoadValues();
    }

    private void BuildUI()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(16, 12, 16, 12);
        BackColor = TossTheme.Background;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            AutoSize = true,
            BackColor = TossTheme.Background
        };
        for (int i = 0; i < 8; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        Controls.Add(layout);

        // API Key section
        layout.Controls.Add(MakeSectionLabel("Anthropic API Key"), 0, 0);

        var keyRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = TossTheme.Background
        };
        _apiKeyBox = new TextBox
        {
            Width = 250,
            UseSystemPasswordChar = true,
            Font = TossTheme.Monospace(9f),
            BackColor = TossTheme.Surface,
            ForeColor = TossTheme.TextPrimary
        };
        _toggleBtn = new TossButton { Text = "Show", Width = 55, Style = TossButtonStyle.Ghost };
        _toggleBtn.Click += (_, _) => ToggleKeyVisibility();
        _validateBtn = new TossButton { Text = "Validate", Width = 70, Style = TossButtonStyle.Ghost };
        _validateBtn.Click += async (_, _) => await ValidateKey();
        var getKeyLink = new LinkLabel
        {
            Text = "Get API Key",
            AutoSize = true,
            LinkColor = TossTheme.PrimaryBlue,
            ActiveLinkColor = TossTheme.PrimaryBlue,
            VisitedLinkColor = TossTheme.PrimaryBlue,
            Font = TossTheme.Body(),
            BackColor = TossTheme.Background
        };
        getKeyLink.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo("https://console.anthropic.com/settings/keys") { UseShellExecute = true });
        keyRow.Controls.Add(_apiKeyBox);
        keyRow.Controls.Add(_toggleBtn);
        keyRow.Controls.Add(_validateBtn);
        keyRow.Controls.Add(getKeyLink);
        layout.Controls.Add(keyRow, 0, 1);

        _statusLabel = new Label
        {
            AutoSize = true,
            Font = TossTheme.Body(8.5f),
            ForeColor = TossTheme.TextTertiary,
            BackColor = TossTheme.Background,
            Text = "Enter your API key above"
        };
        layout.Controls.Add(_statusLabel, 0, 2);

        var saveKeyBtn = new TossButton { Text = "Save API Key", Width = 120, Style = TossButtonStyle.Primary };
        saveKeyBtn.Click += (_, _) => SaveApiKey();
        layout.Controls.Add(saveKeyBtn, 0, 3);

        // Model
        layout.Controls.Add(MakeSectionLabel("Default Model"), 0, 4);
        _modelCombo = new ComboBox
        {
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = TossTheme.Surface,
            ForeColor = TossTheme.TextPrimary,
            Font = TossTheme.Body()
        };
        _modelCombo.Items.AddRange([
            "claude-opus-4-6", "claude-sonnet-4-6", "claude-haiku-4-5",
            "claude-opus-4-5", "claude-sonnet-4-5", "claude-3-5-sonnet-20241022"
        ]);
        _modelCombo.SelectedValueChanged += (_, _) => SaveSettings();
        layout.Controls.Add(_modelCombo, 0, 5);

        // Startup
        layout.Controls.Add(MakeSectionLabel("System"), 0, 6);
        _startupCheck = new CheckBox
        {
            Text = "Start with Windows",
            AutoSize = true,
            ForeColor = TossTheme.TextSecondary,
            BackColor = TossTheme.Background,
            Font = TossTheme.Body()
        };
        _startupCheck.CheckedChanged += (_, _) => {
            WindowsStartup.Toggle();
            SaveSettings();
        };
        layout.Controls.Add(_startupCheck, 0, 7);
    }

    private void LoadValues()
    {
        var saved = CredentialHelper.LoadApiKey();
        if (!string.IsNullOrEmpty(saved))
        {
            _apiKeyBox.Text = saved;
            _statusLabel.Text = "API key loaded";
            _statusLabel.ForeColor = TossTheme.Success;
        }
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
        {
            _apiKeyBox.Text = "(from ANTHROPIC_API_KEY env var)";
            _apiKeyBox.ReadOnly = true;
            _statusLabel.Text = "Using environment variable";
            _statusLabel.ForeColor = TossTheme.PrimaryBlue;
        }

        var idx = _modelCombo.Items.IndexOf(_settings.DefaultModel);
        _modelCombo.SelectedIndex = idx >= 0 ? idx : 1;

        _startupCheck.Checked = WindowsStartup.IsEnabled();
    }

    private void ToggleKeyVisibility()
    {
        _keyVisible = !_keyVisible;
        _apiKeyBox.UseSystemPasswordChar = !_keyVisible;
        _toggleBtn.Text = _keyVisible ? "Hide" : "Show";
    }

    private async Task ValidateKey()
    {
        var key = _apiKeyBox.Text.Trim();
        if (string.IsNullOrEmpty(key)) return;
        _validateBtn.Enabled = false;
        _statusLabel.Text = "Validating...";
        _statusLabel.ForeColor = TossTheme.TextTertiary;
        bool ok = await _anthropic.ValidateKeyAsync(key);
        _statusLabel.Text = ok ? "Valid API key" : "Invalid API key";
        _statusLabel.ForeColor = ok ? TossTheme.Success : TossTheme.Error;
        _validateBtn.Enabled = true;
        if (ok)
        {
            _anthropic.Configure(key);
        }
    }

    private void SaveApiKey()
    {
        var key = _apiKeyBox.Text.Trim();
        if (string.IsNullOrEmpty(key)) return;
        CredentialHelper.SaveApiKey(key);
        _anthropic.Configure(key);
        _statusLabel.Text = "API key saved";
        _statusLabel.ForeColor = TossTheme.Success;
    }

    private void SaveSettings()
    {
        _settings.DefaultModel = _modelCombo.SelectedItem?.ToString() ?? "claude-sonnet-4-6";
        _settings.StartWithWindows = _startupCheck.Checked;
        _onSettingsChanged(_settings);
    }

    private static Label MakeSectionLabel(string text) => new()
    {
        Text = text,
        Font = TossTheme.SectionHeader(),
        ForeColor = TossTheme.TextSecondary,
        BackColor = TossTheme.Background,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft
    };
}
