using cHelper.Controls;
using cHelper.Data;
using cHelper.Models;
using cHelper.Services;
using cHelper.Utils;

namespace cHelper.Forms;

public class MainForm : Form
{
    private readonly AnthropicService _anthropic;
    private readonly UsageTrackingService _usageService;
    private readonly ClaudeCodeService _claudeCodeService;
    private readonly SettingsRepository _settingsRepo;
    private AppSettings _settings;

    private TabControl _tabs = null!;
    private UsageStatsControl _usageControl = null!;
    private ClaudeCodeStatusControl _claudeCodeControl = null!;

    public MainForm(AnthropicService anthropic, UsageTrackingService usageService,
        ClaudeCodeService claudeCodeService, SettingsRepository settingsRepo, AppSettings settings)
    {
        _anthropic = anthropic;
        _usageService = usageService;
        _claudeCodeService = claudeCodeService;
        _settingsRepo = settingsRepo;
        _settings = settings;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "cHelper";
        Size = new Size(520, 580);
        MinimumSize = new Size(480, 500);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        BackColor = TossTheme.Background;

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = TossTheme.Body(9f),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(110, 32),
            BackColor = TossTheme.Background
        };
        _tabs.DrawItem += DrawTab;
        Controls.Add(_tabs);

        // Usage Tab
        var usageTab = new TabPage("Usage") { BackColor = TossTheme.Background };
        _usageControl = new UsageStatsControl(_usageService);
        usageTab.Controls.Add(_usageControl);
        _tabs.TabPages.Add(usageTab);

        // Claude Code Tab
        var codeTab = new TabPage("Claude Code") { BackColor = TossTheme.Background };
        _claudeCodeControl = new ClaudeCodeStatusControl(_claudeCodeService);
        codeTab.Controls.Add(_claudeCodeControl);
        _tabs.TabPages.Add(codeTab);

        // Settings Tab
        var settingsTab = new TabPage("Settings") { BackColor = TossTheme.Background };
        var settingsControl = new ApiSettingsControl(_anthropic, _settings, OnSettingsChanged);
        settingsTab.Controls.Add(settingsControl);
        _tabs.TabPages.Add(settingsTab);

        // About Tab
        var aboutTab = new TabPage("About") { BackColor = TossTheme.Background };
        aboutTab.Controls.Add(MakeAboutPanel());
        _tabs.TabPages.Add(aboutTab);

        // Refresh on tab switch
        _tabs.Selected += (_, e) =>
        {
            if (e.TabPage == usageTab) _usageControl.Refresh();
            if (e.TabPage == codeTab) _claudeCodeControl.LoadData();
        };
    }

    private void DrawTab(object? sender, DrawItemEventArgs e)
    {
        var tab = _tabs.TabPages[e.Index];
        bool selected = e.Index == _tabs.SelectedIndex;

        using var bgBrush = new SolidBrush(TossTheme.Background);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        if (selected)
        {
            using var accentBrush = new SolidBrush(TossTheme.PrimaryBlue);
            e.Graphics.FillRectangle(accentBrush, e.Bounds.X, e.Bounds.Bottom - 2, e.Bounds.Width, 2);
        }

        var textColor = selected ? TossTheme.TextPrimary : TossTheme.TextTertiary;
        using var font = selected ? TossTheme.SectionHeader() : TossTheme.Body(9f);
        TextRenderer.DrawText(e.Graphics, tab.Text, font, e.Bounds, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private Panel MakeAboutPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = TossTheme.Background };

        var title = new Label
        {
            Text = "cHelper",
            Font = TossTheme.Body(20f, FontStyle.Bold),
            ForeColor = TossTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var subtitle = new Label
        {
            Text = "Claude API Usage Manager",
            Font = TossTheme.Body(10f),
            ForeColor = TossTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(20, 58)
        };

        var versionLabel = new Label
        {
            Text = "Version 1.0.0",
            Font = TossTheme.Body(9f),
            ForeColor = TossTheme.TextTertiary,
            AutoSize = true,
            Location = new Point(20, 86)
        };

        var docsBtn = new TossButton
        {
            Text = "Anthropic Documentation",
            Location = new Point(20, 124),
            Width = 200,
            Style = TossButtonStyle.Ghost
        };
        docsBtn.Click += (_, _) => ProcessHelper.OpenUrl("https://docs.anthropic.com");

        var consoleBtn = new TossButton
        {
            Text = "Anthropic Console",
            Location = new Point(20, 162),
            Width = 160,
            Style = TossButtonStyle.Ghost
        };
        consoleBtn.Click += (_, _) => ProcessHelper.OpenUrl("https://console.anthropic.com");

        panel.Controls.AddRange([title, subtitle, versionLabel, docsBtn, consoleBtn]);
        return panel;
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        _settings = settings;
        _settingsRepo.Save(settings);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    public void RefreshUsage() => _usageControl.Refresh();
}
