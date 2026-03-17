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

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
        Controls.Add(_tabs);

        // Usage Tab
        var usageTab = new TabPage("Usage");
        _usageControl = new UsageStatsControl(_usageService);
        usageTab.Controls.Add(_usageControl);
        _tabs.TabPages.Add(usageTab);

        // Claude Code Tab
        var codeTab = new TabPage("Claude Code");
        _claudeCodeControl = new ClaudeCodeStatusControl(_claudeCodeService);
        codeTab.Controls.Add(_claudeCodeControl);
        _tabs.TabPages.Add(codeTab);

        // Settings Tab
        var settingsTab = new TabPage("Settings");
        var settingsControl = new ApiSettingsControl(_anthropic, _settings, OnSettingsChanged);
        settingsTab.Controls.Add(settingsControl);
        _tabs.TabPages.Add(settingsTab);

        // About Tab
        var aboutTab = new TabPage("About");
        aboutTab.Controls.Add(MakeAboutPanel());
        _tabs.TabPages.Add(aboutTab);

        // Refresh button in Usage tab
        _tabs.Selected += (_, e) =>
        {
            if (e.TabPage == usageTab) _usageControl.Refresh();
            if (e.TabPage == codeTab) _claudeCodeControl.LoadData();
        };
    }

    private Panel MakeAboutPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var title = new Label
        {
            Text = "cHelper",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var subtitle = new Label
        {
            Text = "Claude API Usage Manager",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(20, 55)
        };

        var versionLabel = new Label
        {
            Text = "Version 1.0.0",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.DimGray,
            AutoSize = true,
            Location = new Point(20, 82)
        };

        var docsBtn = new Button
        {
            Text = "Anthropic Documentation",
            Location = new Point(20, 120),
            Width = 180,
            Height = 28
        };
        docsBtn.Click += (_, _) => ProcessHelper.OpenUrl("https://docs.anthropic.com");

        var consoleBtn = new Button
        {
            Text = "Anthropic Console",
            Location = new Point(20, 155),
            Width = 150,
            Height = 28
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
