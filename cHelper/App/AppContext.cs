using cHelper.Data;
using cHelper.Forms;
using cHelper.Models;
using cHelper.Services;
using cHelper.Utils;

namespace cHelper.App;

public class AppContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private MainForm? _mainForm;

    private readonly AnthropicService _anthropic;
    private readonly UsageTrackingService _usageService;
    private readonly ClaudeCodeService _claudeCodeService;
    private readonly SettingsRepository _settingsRepo;
    private AppSettings _settings;
    private bool _rateLimitNotified = false;

    public AppContext()
    {
        // Init services
        var usageRepo = new UsageRepository();
        _usageService = new UsageTrackingService(usageRepo);
        _anthropic = new AnthropicService(_usageService);
        _settingsRepo = new SettingsRepository();
        _settings = _settingsRepo.Load();

        var claudeCodeReader = new ClaudeCodeReader(_settings.ClaudeCodePath);
        _claudeCodeService = new ClaudeCodeService(claudeCodeReader);

        // Load saved API key
        var apiKey = CredentialHelper.LoadApiKey();
        if (!string.IsNullOrEmpty(apiKey))
            _anthropic.Configure(apiKey);

        // Tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon("tray_idle"),
            Text = "cHelper",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainForm();

        // Refresh timer
        _refreshTimer = new System.Windows.Forms.Timer
        {
            Interval = _settings.PollingIntervalSeconds * 1000
        };
        _refreshTimer.Tick += (_, _) => RefreshTrayTooltip();
        _refreshTimer.Start();

        RefreshTrayTooltip();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var titleItem = new ToolStripMenuItem("cHelper") { Enabled = false };
        titleItem.Font = new Font(titleItem.Font, FontStyle.Bold);
        menu.Items.Add(titleItem);
        menu.Items.Add(new ToolStripSeparator());

        var usageItem = new ToolStripMenuItem("Usage: loading..") { Enabled = false, Name = "usageItem" };
        menu.Items.Add(usageItem);
        menu.Items.Add(new ToolStripSeparator());

        var openItem = new ToolStripMenuItem("Open cHelper");
        openItem.Click += (_, _) => ShowMainForm();
        menu.Items.Add(openItem);

        menu.Items.Add(new ToolStripSeparator());

        var startupItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = WindowsStartup.IsEnabled(),
            CheckOnClick = true,
            Name = "startupItem"
        };
        startupItem.CheckedChanged += (_, _) =>
        {
            WindowsStartup.Toggle();
            _settings.StartWithWindows = startupItem.Checked;
            _settingsRepo.Save(_settings);
        };
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void RefreshTrayTooltip()
    {
        var today = _claudeCodeService.GetUsageToday();
        string tokens = FormatTokens(today.Total);

        string tooltipText = $"cHelper\n오늘: {tokens} tokens";
        if (tooltipText.Length > 127) tooltipText = tooltipText[..127];
        _trayIcon.Text = tooltipText;

        var usageItem = _trayIcon.ContextMenuStrip?.Items["usageItem"] as ToolStripMenuItem;
        if (usageItem != null)
            usageItem.Text = $"오늘: {tokens} tokens";

        // Rate limit check
        var rl = _claudeCodeService.GetRateLimitInfo();
        _trayIcon.Icon = LoadTrayIcon(rl.IsCurrentlyLimited ? "tray_error" : "tray_idle");

        if (rl.IsCurrentlyLimited && _settings.ShowBalloonOnRateLimit && !_rateLimitNotified)
        {
            _rateLimitNotified = true;
            _trayIcon.BalloonTipTitle = "사용량 한도 초과";
            _trayIcon.BalloonTipText = string.IsNullOrEmpty(rl.ResetText)
                ? "Claude 사용 한도에 도달했습니다."
                : rl.ResetText;
            _trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
            _trayIcon.ShowBalloonTip(5000);
        }
        else if (!rl.IsCurrentlyLimited)
        {
            _rateLimitNotified = false;
        }

        _mainForm?.RefreshUsage();
    }

    private static string FormatTokens(long tokens)
    {
        if (tokens >= 1_000_000) return $"{tokens / 1_000_000.0:F1}M";
        if (tokens >= 1_000) return $"{tokens / 1_000.0:F1}K";
        return tokens.ToString();
    }

    private void ShowMainForm()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_anthropic, _claudeCodeService, _settingsRepo, _settings);
        }
        _mainForm.Show();
        _mainForm.BringToFront();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.Activate();
    }

    private void ExitApp()
    {
        _refreshTimer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _mainForm?.Close();
        Application.Exit();
    }

    private static Icon LoadTrayIcon(string name)
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(exeDir, "Resources", "Icons", $"{name}.ico");
        if (File.Exists(path)) return new Icon(path);
        return SystemIcons.Application;
    }
}
