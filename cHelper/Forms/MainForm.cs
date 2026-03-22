using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using cHelper.Controls;
using cHelper.Data;
using cHelper.Models;
using cHelper.Services;
using cHelper.Utils;

namespace cHelper.Forms;

public class MainForm : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private readonly AnthropicService _anthropic;
    private readonly ClaudeCodeService _claudeCodeService;
    private readonly SettingsRepository _settingsRepo;
    private AppSettings _settings;

    private ClaudeCodeStatusControl _usageControl = null!;
    private Panel[] _pages = null!;

    public MainForm(AnthropicService anthropic, ClaudeCodeService claudeCodeService,
        SettingsRepository settingsRepo, AppSettings settings)
    {
        _anthropic = anthropic;
        _claudeCodeService = claudeCodeService;
        _settingsRepo = settingsRepo;
        _settings = settings;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "cHelper";
        Size = new Size(760, 940);
        MinimumSize = new Size(620, 760);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        BackColor = TossTheme.Background;

        // ── 페이지 패널 (TabControl 대신 직접 show/hide) ─────────────────────
        var usagePage = new Panel { Dock = DockStyle.Fill, BackColor = TossTheme.Background, Visible = true };
        _usageControl = new ClaudeCodeStatusControl(_claudeCodeService);
        usagePage.Controls.Add(_usageControl);

        var settingsPage = new Panel { Dock = DockStyle.Fill, BackColor = TossTheme.Background, Visible = false };
        settingsPage.Controls.Add(new ApiSettingsControl(_anthropic, _settings, OnSettingsChanged));

        var aboutPage = new Panel { Dock = DockStyle.Fill, BackColor = TossTheme.Background, Visible = false };
        aboutPage.Controls.Add(MakeAboutPanel());

        _pages = [usagePage, settingsPage, aboutPage];

        var contentArea = new Panel { Dock = DockStyle.Fill, BackColor = TossTheme.Background };
        contentArea.Controls.AddRange(_pages);

        // ── 커스텀 탭 스트립 ─────────────────────────────────────────────────
        var strip = new TabStrip(["Usage", "Settings", "About"], idx =>
        {
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].Visible = i == idx;
            if (idx == 0) _usageControl.LoadData();
        });

        // Dock 순서: contentArea 먼저, strip 나중 (strip이 Top을 차지)
        Controls.Add(contentArea);
        Controls.Add(strip);
    }

    private Panel MakeAboutPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = TossTheme.Background };
        panel.Controls.AddRange([
            new Label { Text = "cHelper", Font = TossTheme.Body(20f, FontStyle.Bold),
                ForeColor = TossTheme.TextPrimary, AutoSize = true, Location = new Point(20, 20),
                BackColor = TossTheme.Background },
            new Label { Text = "Claude Code Usage Manager", Font = TossTheme.Body(10f),
                ForeColor = TossTheme.TextSecondary, AutoSize = true, Location = new Point(20, 58),
                BackColor = TossTheme.Background },
            new Label { Text = "Version 1.1.0", Font = TossTheme.Body(9f),
                ForeColor = TossTheme.TextTertiary, AutoSize = true, Location = new Point(20, 86),
                BackColor = TossTheme.Background },
        ]);
        return panel;
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        _settings = settings;
        _settingsRepo.Save(settings);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int dark = 1;
            DwmSetWindowAttribute(Handle, 20, ref dark, 4); // DWMWA_USE_IMMERSIVE_DARK_MODE

            // 창 테두리를 테마색(#2E2E35)으로 — DWMWA_BORDER_COLOR (Windows 11)
            int borderColor = 0x2E | (0x2E << 8) | (0x35 << 16);
            DwmSetWindowAttribute(Handle, 34, ref borderColor, 4);
        }
        catch { }
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

    public void RefreshUsage() => _usageControl.RefreshStatCards();

    // ── 커스텀 탭 스트립 (시스템 TabControl 완전 제거) ───────────────────────
    private sealed class TabStrip : Panel
    {
        private readonly Action<int> _onSelect;
        private readonly string[] _labels;
        private int _selected = 0;
        private int _hovered = -1;

        private const int BtnW   = 88;
        private const int StartX = 6;

        public TabStrip(string[] labels, Action<int> onSelect)
        {
            _labels   = labels;
            _onSelect = onSelect;
            Dock = DockStyle.Top;
            Height = 36;
            BackColor = TossTheme.Background;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;

            MouseMove  += (_, e) => UpdateHover(e.Location);
            MouseLeave += (_, _) => { _hovered = -1; Invalidate(); };
            MouseClick += (_, e) =>
            {
                int idx = HitTest(e.Location);
                if (idx >= 0) { _selected = idx; _onSelect(idx); Invalidate(); }
            };
        }

        private Rectangle BtnRect(int i) =>
            new(StartX + i * BtnW, 3, BtnW - 4, Height - 6);

        private int HitTest(Point pt)
        {
            for (int i = 0; i < _labels.Length; i++)
                if (BtnRect(i).Contains(pt)) return i;
            return -1;
        }

        private void UpdateHover(Point pt)
        {
            int h = HitTest(pt);
            if (h != _hovered) { _hovered = h; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(TossTheme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < _labels.Length; i++)
            {
                bool sel = i == _selected;
                bool hov = !sel && i == _hovered;
                var btn = BtnRect(i);

                if (sel)
                {
                    using var path = TossTheme.RoundedRect(btn, 4);
                    using var fill = new SolidBrush(TossTheme.HoverState);
                    g.FillPath(fill, path);
                }
                else if (hov)
                {
                    using var path = TossTheme.RoundedRect(btn, 4);
                    using var fill = new SolidBrush(TossTheme.Surface);
                    g.FillPath(fill, path);
                }

                var fg = sel ? TossTheme.TextPrimary
                       : hov ? TossTheme.TextSecondary
                       :       TossTheme.TextTertiary;
                using var font = TossTheme.Body(9f);
                TextRenderer.DrawText(g, _labels[i], font,
                    new Rectangle(btn.X, 0, btn.Width, Height), fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // 선택된 탭만 하단 파란 강조선
                if (sel)
                {
                    int lw = Math.Min(btn.Width - 16, 32);
                    int lx = btn.X + (btn.Width - lw) / 2;
                    using var accent = new SolidBrush(TossTheme.PrimaryBlue);
                    g.FillRectangle(accent, lx, Height - 3, lw, 2);
                }
            }
        }
    }
}
