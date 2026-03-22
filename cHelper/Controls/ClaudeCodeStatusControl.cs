using System.Runtime.InteropServices;
using cHelper.Services;
using cHelper.Utils;

namespace cHelper.Controls;

public class ClaudeCodeStatusControl : UserControl
{
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string? pszSubIdList);

    private readonly ClaudeCodeService _claudeCodeService;

    private Label _bannerDot = null!;
    private Label _bannerLabel = null!;
    private Label _todayTokensLabel = null!;
    private Label _todayAvgLabel = null!;
    private Label _monthTokensLabel = null!;
    private Label _streakLabel = null!;
    private Label _cacheRateLabel = null!;
    private HourlyChartPanel _hourlyChart = null!;
    private HeatmapPanel _heatmap = null!;
    private ProjectRankingPanel _rankingPanel = null!;
    private DarkListView _projectList = null!;

    public ClaudeCodeStatusControl(ClaudeCodeService claudeCodeService)
    {
        _claudeCodeService = claudeCodeService;
        DoubleBuffered = true;
        BuildUI();
        LoadData();
    }

    private void BuildUI()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(18, 16, 18, 16);
        BackColor = TossTheme.Background;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 1,
            BackColor = TossTheme.Background,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // 0: 배너
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));   // 1: 4-stat 카드
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // 2: 시간대 레이블
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));   // 3: 시간대 차트
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // 4: 히스토리 레이블
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));  // 5: 히트맵 (7×13 + 16 = 107)
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // 6: 랭킹 레이블
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));  // 7: 프로젝트 랭킹
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // 8: 프로젝트 레이블
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 9: 프로젝트 목록
        Controls.Add(layout);

        // ── Row 0: 배너 ───────────────────────────────────────────────────────
        var banner = new RoundedSurface
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 8,
        };
        var bannerFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(14, 0, 0, 0),
            BackColor = Color.Transparent,
        };
        _bannerDot = new Label
        {
            Text = "●",
            AutoSize = true,
            Font = new Font("Segoe UI", 7f),
            ForeColor = TossTheme.Success,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(0, 12, 0, 0),
        };
        _bannerLabel = new Label
        {
            AutoSize = true,
            Font = TossTheme.Body(8.5f),
            ForeColor = TossTheme.TextSecondary,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0),
        };
        bannerFlow.Controls.Add(_bannerDot);
        bannerFlow.Controls.Add(_bannerLabel);
        banner.Controls.Add(bannerFlow);
        layout.Controls.Add(banner, 0, 0);

        // ── Row 1: 4-stat 카드 ────────────────────────────────────────────────
        var statsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = TossTheme.Background,
            Margin = new Padding(0, 0, 0, 4),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
        };
        statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        statsPanel.Controls.Add(MakeTodayCard(out _todayTokensLabel, out _todayAvgLabel), 0, 0);
        statsPanel.Controls.Add(MakeStatCard("이번 달",  "\uE787", TossTheme.TextSecondary, out _monthTokensLabel), 1, 0);
        statsPanel.Controls.Add(MakeStatCard("연속 사용", "\uE734", TossTheme.Warning,       out _streakLabel),      2, 0);
        statsPanel.Controls.Add(MakeStatCard("캐시 적중", "\uE8D0", TossTheme.Success,        out _cacheRateLabel, lastCard: true), 3, 0);
        layout.Controls.Add(statsPanel, 0, 1);

        // ── Row 2: 레이블 ─────────────────────────────────────────────────────
        layout.Controls.Add(MakeSectionLabel("TODAY BY HOUR"), 0, 2);

        // ── Row 3: 시간대 차트 ────────────────────────────────────────────────
        _hourlyChart = new HourlyChartPanel { Dock = DockStyle.Fill };
        layout.Controls.Add(_hourlyChart, 0, 3);

        // ── Row 4: 레이블 ─────────────────────────────────────────────────────
        layout.Controls.Add(MakeSectionLabel("USAGE HISTORY"), 0, 4);

        // ── Row 5: 히트맵 ─────────────────────────────────────────────────────
        _heatmap = new HeatmapPanel { Dock = DockStyle.Fill };
        layout.Controls.Add(_heatmap, 0, 5);

        // ── Row 6: 레이블 ─────────────────────────────────────────────────────
        layout.Controls.Add(MakeSectionLabel("PROJECT RANKING"), 0, 6);

        // ── Row 7: 프로젝트 랭킹 ─────────────────────────────────────────────
        _rankingPanel = new ProjectRankingPanel { Dock = DockStyle.Fill };
        layout.Controls.Add(_rankingPanel, 0, 7);

        // ── Row 8: 레이블 ─────────────────────────────────────────────────────
        layout.Controls.Add(MakeSectionLabel("RECENT PROJECTS"), 0, 8);

        // ── Row 9: 프로젝트 목록 ─────────────────────────────────────────────
        _projectList = new DarkListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            BackColor = TossTheme.Background,
            ForeColor = TossTheme.TextSecondary,
            BorderStyle = BorderStyle.None,
            Font = TossTheme.Body(8.5f),
        };
        _projectList.Columns.Add("Project", 150);
        _projectList.Columns.Add("Sessions", 52);
        _projectList.Columns.Add("Last Active", 130);
        _projectList.Columns.Add("Path", 200);
        layout.Controls.Add(_projectList, 0, 9);

        _projectList.HandleCreated += (_, _) =>
        {
            SetWindowTheme(_projectList.Handle, "DarkMode_Explorer", null);
            BeginInvoke(FitLastColumn);
        };
        _projectList.Resize += (_, _) => FitLastColumn();

        var menu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Open in Claude Code");
        openItem.Click += (_, _) => OpenSelected();
        menu.Items.Add(openItem);
        _projectList.ContextMenuStrip = menu;
        _projectList.DoubleClick += (_, _) => OpenSelected();
    }

    // ── 카드 빌더 ─────────────────────────────────────────────────────────────

    private static RoundedCard MakeTodayCard(out Label valueLabel, out Label avgLabel)
    {
        var card = new RoundedCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            CornerRadius = 12,
        };
        // Row 1: 아이콘(11pt) + 타이틀
        card.Controls.Add(new Label
        {
            Text = "\uE945",
            Font = new Font("Segoe MDL2 Assets", 11f),
            ForeColor = TossTheme.PrimaryBlue,
            BackColor = TossTheme.CardBackground,
            Location = new Point(16, 13),
            AutoSize = true,
        });
        card.Controls.Add(new Label
        {
            Text = "오늘 사용량",
            Font = TossTheme.Body(8.5f),
            ForeColor = TossTheme.TextLabel,
            BackColor = TossTheme.CardBackground,
            Location = new Point(36, 15),
            AutoSize = true,
        });
        // Row 2: 큰 수치
        valueLabel = new Label
        {
            Text = "0",
            Font = TossTheme.StatValue(),
            ForeColor = TossTheme.TextPrimary,
            BackColor = TossTheme.CardBackground,
            Location = new Point(16, 36),
            AutoSize = true,
        };
        // Row 3: avg 보조 텍스트
        avgLabel = new Label
        {
            Text = "",
            Font = TossTheme.Body(7f),
            ForeColor = TossTheme.TextSecondary,
            BackColor = TossTheme.CardBackground,
            Location = new Point(16, 70),
            AutoSize = true,
        };
        card.Controls.Add(valueLabel);
        card.Controls.Add(avgLabel);
        return card;
    }

    private static RoundedCard MakeStatCard(string title, string glyph, Color iconColor,
        out Label valueLabel, bool lastCard = false)
    {
        var card = new RoundedCard
        {
            Dock = DockStyle.Fill,
            Margin = lastCard ? new Padding(0) : new Padding(0, 0, 8, 0),
            CornerRadius = 12,
        };
        // Row 1: 아이콘(11pt) + 타이틀
        card.Controls.Add(new Label
        {
            Text = glyph,
            Font = new Font("Segoe MDL2 Assets", 11f),
            ForeColor = iconColor,
            BackColor = TossTheme.CardBackground,
            Location = new Point(16, 13),
            AutoSize = true,
        });
        card.Controls.Add(new Label
        {
            Text = title,
            Font = TossTheme.Body(8.5f),
            ForeColor = TossTheme.TextLabel,
            BackColor = TossTheme.CardBackground,
            Location = new Point(36, 15),
            AutoSize = true,
        });
        // Row 2: 큰 수치
        valueLabel = new Label
        {
            Text = "0",
            Font = TossTheme.StatValue(),
            ForeColor = TossTheme.TextPrimary,
            BackColor = TossTheme.CardBackground,
            Location = new Point(16, 36),
            AutoSize = true,
        };
        card.Controls.Add(valueLabel);
        return card;
    }

    private static Label MakeSectionLabel(string text) => new()
    {
        Text = text,
        Font = TossTheme.SectionHeader(),
        ForeColor = TossTheme.TextLabel,
        BackColor = TossTheme.Background,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft,
        Margin = new Padding(0, 8, 0, 2),
    };

    // ── 데이터 로딩 ───────────────────────────────────────────────────────────

    public void LoadData()
    {
        _claudeCodeService.InvalidateUsageCache();
        UpdateBanner();
        UpdateStats();
        UpdateHourlyChart();
        UpdateHeatmap();
        UpdateRanking();
        UpdateProjects();
    }

    public void RefreshStatCards() => UpdateStats();

    private void UpdateBanner()
    {
        var version = _claudeCodeService.GetVersion();
        var rl = _claudeCodeService.GetRateLimitInfo();

        if (rl.IsCurrentlyLimited)
        {
            _bannerDot.ForeColor = TossTheme.Error;
            _bannerLabel.Text = string.IsNullOrEmpty(rl.ResetText)
                ? "사용량 한도 초과"
                : rl.ResetText;
        }
        else if (version != null)
        {
            _bannerDot.ForeColor = TossTheme.Success;
            _bannerLabel.Text = $"정상  ·  Claude Code {version}";
        }
        else
        {
            _bannerDot.ForeColor = TossTheme.TextTertiary;
            _bannerLabel.ForeColor = TossTheme.TextTertiary;
            _bannerLabel.Text = "Claude Code 미설치";
        }
    }

    private void UpdateStats()
    {
        var today = _claudeCodeService.GetUsageToday();
        var month = _claudeCodeService.GetUsageThisMonth();
        var avg = _claudeCodeService.GetDailyAverage(30);
        var streak = _claudeCodeService.GetStreak();
        var cacheRate = _claudeCodeService.GetCacheHitRate();

        _todayTokensLabel.Text = FormatTokens(today.Total);
        _monthTokensLabel.Text = FormatTokens(month.Total);
        _streakLabel.Text = $"{streak}일";
        _cacheRateLabel.Text = $"{cacheRate:P0}";

        if (avg > 0 && today.Total > 0)
        {
            double ratio = (double)today.Total / avg;
            string arrow = ratio >= 1.0 ? "↑" : "↓";
            _todayAvgLabel.Text = $"{arrow} 일평균 대비 {Math.Abs(ratio - 1):P0}";
            _todayAvgLabel.ForeColor = ratio >= 1.0 ? TossTheme.Success : TossTheme.TextTertiary;
        }
        else
        {
            _todayAvgLabel.Text = avg > 0 ? $"일평균 {FormatTokens(avg)}" : "";
        }
    }

    private void UpdateHourlyChart()
    {
        var hourly = _claudeCodeService.GetHourlyTokensToday();
        _hourlyChart.SetData(hourly);
    }

    private void UpdateHeatmap()
    {
        var daily = _claudeCodeService.GetDailyTokens(104 * 7);
        _heatmap.SetData(daily);
    }

    private void UpdateRanking()
    {
        var ranking = _claudeCodeService.GetProjectRanking(5);
        _rankingPanel.SetData(ranking);
    }

    private void UpdateProjects()
    {
        _projectList.Items.Clear();
        var projects = _claudeCodeService.GetProjectSummary();
        foreach (var (path, name, count, lastActive) in projects)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(count.ToString());
            item.SubItems.Add(lastActive == DateTime.MinValue
                ? "-" : lastActive.ToString("yyyy-MM-dd HH:mm"));
            item.SubItems.Add(path);
            item.Tag = path;
            _projectList.Items.Add(item);
        }
    }

    private void OpenSelected()
    {
        if (_projectList.SelectedItems.Count == 0) return;
        var path = _projectList.SelectedItems[0].Tag as string;
        if (!string.IsNullOrEmpty(path))
            _claudeCodeService.OpenInClaudeCode(path);
    }

    private bool _fittingColumn;
    private void FitLastColumn()
    {
        if (_fittingColumn || _projectList.Columns.Count == 0) return;
        _fittingColumn = true;
        int usedWidth = 0;
        for (int i = 0; i < _projectList.Columns.Count - 1; i++)
            usedWidth += _projectList.Columns[i].Width;
        int remaining = _projectList.ClientRectangle.Width - usedWidth;
        if (remaining > 40)
            _projectList.Columns[_projectList.Columns.Count - 1].Width = remaining;
        _fittingColumn = false;
    }

    private static string FormatTokens(long tokens)
    {
        if (tokens >= 1_000_000) return $"{tokens / 1_000_000.0:F1}M";
        if (tokens >= 1_000) return $"{tokens / 1_000.0:F1}K";
        return tokens.ToString();
    }

    // ── 시간대별 바 차트 ──────────────────────────────────────────────────────

    private sealed class HourlyChartPanel : Panel
    {
        private Dictionary<int, long> _data = [];
        private readonly ToolTip _tip = new() { UseAnimation = false, UseFading = false };
        private float _anim = 1f;
        private readonly System.Windows.Forms.Timer _animTimer;

        public HourlyChartPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = TossTheme.Background;
            MouseMove += OnMouseMove;
            MouseLeave += (_, _) => _tip.Hide(this);
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (_, _) =>
            {
                _anim = Math.Min(1f, _anim + 0.028f); // ~580ms
                Invalidate();
                if (_anim >= 1f) _animTimer.Stop();
            };
        }

        public void SetData(Dictionary<int, long> data)
        {
            _data = data;
            _anim = 0f;
            _animTimer.Start();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            const int labelH = 14;
            int chartH = Height - labelH - 4;
            if (chartH < 4 || Width < 24) return;

            long max = _data.Values.Any() ? _data.Values.Max() : 1;
            if (max == 0) max = 1;

            float eased = 1f - (float)Math.Pow(1f - _anim, 3); // easeOutCubic
            float barW = (float)Width / 24;
            int nowHour = DateTime.Now.Hour;

            using var labelFont = new Font("Segoe UI", 6.5f);
            using var labelBrush = new SolidBrush(TossTheme.TextTertiary);
            using var emptyBrush = new SolidBrush(ColorTranslator.FromHtml("#1C1C22"));
            using var pastBrush = new SolidBrush(TossTheme.PrimaryBlue);
            using var nowBrush = new SolidBrush(TossTheme.Success);
            using var futureBrush = new SolidBrush(ColorTranslator.FromHtml("#1C2A40"));

            for (int hour = 0; hour < 24; hour++)
            {
                float x = hour * barW;
                float bw = Math.Max(1, barW - 1);

                _data.TryGetValue(hour, out long tokens);
                float ratio = (float)tokens / max;
                float barH = tokens > 0 ? Math.Max(2, ratio * chartH * eased) : 0;
                float barY = chartH - barH;

                // slot background
                g.FillRectangle(emptyBrush, x, 0, bw, chartH);

                // bar
                if (tokens > 0)
                {
                    var brush = hour == nowHour ? nowBrush
                              : hour < nowHour ? pastBrush
                              : futureBrush;
                    g.FillRectangle(brush, x, barY, bw, barH);
                }

                // hour label every 6 hours
                if (hour % 6 == 0)
                {
                    g.DrawString(hour.ToString(), labelFont, labelBrush,
                        x + bw / 2 - 4, chartH + 2);
                }
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (Width < 24) return;
            float barW = (float)Width / 24;
            int hour = Math.Clamp((int)(e.X / barW), 0, 23);
            _data.TryGetValue(hour, out long tokens);
            string tip = tokens > 0
                ? $"{hour:D2}:00  {FormatTip(tokens)} tokens"
                : $"{hour:D2}:00  사용 없음";
            _tip.Show(tip, this, e.X + 10, e.Y + 6, 2000);
        }

        private static string FormatTip(long v)
        {
            if (v >= 1_000_000) return $"{v / 1_000_000.0:F1}M";
            if (v >= 1_000) return $"{v / 1_000.0:F1}K";
            return v.ToString();
        }
    }

    // ── 프로젝트 랭킹 (수평 바) ──────────────────────────────────────────────

    private sealed class ProjectRankingPanel : Panel
    {
        private List<(string Name, long Tokens)> _data = [];
        private float _anim = 1f;
        private readonly System.Windows.Forms.Timer _animTimer;

        public ProjectRankingPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = TossTheme.Background;
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (_, _) =>
            {
                _anim = Math.Min(1f, _anim + 0.022f); // ~730ms
                Invalidate();
                if (_anim >= 1f) _animTimer.Stop();
            };
        }

        public void SetData(List<(string Name, long Tokens)> data)
        {
            _data = data;
            _anim = 0f;
            _animTimer.Start();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_data.Count == 0) return;

            const int nameW = 110;
            const int valW = 52;
            const int rowH = 17;
            const int rowGap = 4;
            int barAreaW = Width - nameW - valW - 8;
            if (barAreaW < 8) return;

            long maxTokens = _data.Max(x => x.Tokens);
            if (maxTokens == 0) maxTokens = 1;

            float eased = 1f - (float)Math.Pow(1f - _anim, 3); // easeOutCubic

            using var nameFont = new Font("Segoe UI", 7.5f);
            using var barFillBrush = new SolidBrush(ColorTranslator.FromHtml("#1A3880"));
            using var barBgBrush = new SolidBrush(ColorTranslator.FromHtml("#1C1C22"));

            for (int i = 0; i < _data.Count; i++)
            {
                int y = i * (rowH + rowGap);
                var (name, tokens) = _data[i];

                // project name
                TextRenderer.DrawText(g, name, nameFont,
                    new Rectangle(0, y, nameW - 4, rowH),
                    TossTheme.TextSecondary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                // bar background
                g.FillRectangle(barBgBrush, nameW, y + 4, barAreaW, rowH - 8);

                // bar fill (애니메이션 적용)
                int fill = (int)(barAreaW * (double)tokens / maxTokens * eased);
                if (fill > 0)
                    g.FillRectangle(barFillBrush, nameW, y + 4, fill, rowH - 8);

                // value
                TextRenderer.DrawText(g, FormatVal(tokens), nameFont,
                    new Rectangle(nameW + barAreaW + 4, y, valW, rowH),
                    TossTheme.TextTertiary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }
        }

        private static string FormatVal(long v)
        {
            if (v >= 1_000_000) return $"{v / 1_000_000.0:F1}M";
            if (v >= 1_000) return $"{v / 1_000.0:F0}K";
            return v.ToString();
        }
    }

    // ── 히트맵 ────────────────────────────────────────────────────────────────

    private sealed class HeatmapPanel : Panel
    {
        private Dictionary<DateTime, long> _data = [];
        private readonly ToolTip _tip = new() { UseAnimation = false, UseFading = false };
        private float _anim = 1f;
        private readonly System.Windows.Forms.Timer _animTimer;

        private const int DayLabelW = 22;
        private const int MonthLabelH = 16;
        private const int Cell = 11;
        private const int Gap = 2;
        private const int Step = Cell + Gap;
        private const int MaxWeeks = 104;

        private int _paintedWeeks = 17;

        public HeatmapPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = TossTheme.Background;
            MouseMove += OnMouseMove;
            MouseLeave += (_, _) => _tip.Hide(this);
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (_, _) =>
            {
                _anim = Math.Min(1f, _anim + 0.018f); // ~890ms
                Invalidate();
                if (_anim >= 1f) _animTimer.Stop();
            };
        }

        public void SetData(Dictionary<DateTime, long> data)
        {
            _data = data;
            _anim = 0f;
            _animTimer.Start();
            Invalidate();
        }

        private int ComputeWeeks()
        {
            int available = Width - DayLabelW - 2;
            return Math.Max(4, Math.Min(available / Step, MaxWeeks));
        }

        private static DateTime GetStartDate(int weeks)
        {
            var start = DateTime.Today.AddDays(-(weeks * 7 - 1));
            return start.AddDays(-(int)start.DayOfWeek);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            _paintedWeeks = ComputeWeeks();
            var today = DateTime.Today;
            var startDate = GetStartDate(_paintedWeeks);
            long maxVal = _data.Values.Any() ? _data.Values.Max() : 1;
            if (maxVal == 0) maxVal = 1;

            using var labelFont = new Font("Segoe UI", 6.5f);
            using var labelBrush = new SolidBrush(TossTheme.TextTertiary);

            string[] dayLabels = ["", "M", "", "W", "", "F", ""];
            for (int d = 0; d < 7; d++)
            {
                if (dayLabels[d] == "") continue;
                float y = MonthLabelH + d * Step + (Cell - 7) / 2f;
                g.DrawString(dayLabels[d], labelFont, labelBrush, 2, y);
            }

            string lastMonth = "";
            for (int w = 0; w < _paintedWeeks; w++)
            {
                var weekStart = startDate.AddDays(w * 7);
                string monthName = weekStart.ToString("MMM");

                if (w == 0) lastMonth = monthName;
                else if (monthName != lastMonth)
                {
                    lastMonth = monthName;
                    g.DrawString(monthName, labelFont, labelBrush, DayLabelW + w * Step, 1);
                }

                float eased = 1f - (float)Math.Pow(1f - _anim, 2); // easeOutQuad
                for (int d = 0; d < 7; d++)
                {
                    var date = weekStart.AddDays(d);
                    if (date > today) continue;

                    _data.TryGetValue(date, out long tokens);
                    using var brush = new SolidBrush(GetColorAnimated(tokens, maxVal, eased));
                    g.FillRectangle(brush, DayLabelW + w * Step, MonthLabelH + d * Step, Cell, Cell);
                }
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            int wx = e.X - DayLabelW;
            int hy = e.Y - MonthLabelH;
            if (wx < 0 || hy < 0) { _tip.Hide(this); return; }

            int week = wx / Step;
            int day = hy / Step;
            if (week >= _paintedWeeks || day >= 7) { _tip.Hide(this); return; }

            var date = GetStartDate(_paintedWeeks).AddDays(week * 7 + day);
            if (date > DateTime.Today) { _tip.Hide(this); return; }

            _data.TryGetValue(date, out long tokens);
            string tipText = tokens > 0
                ? $"{date:yyyy-MM-dd}  {Fmt(tokens)} tokens"
                : $"{date:yyyy-MM-dd}  사용 없음";
            _tip.Show(tipText, this, e.X + 14, e.Y + 6, 2000);
        }

        private static string Fmt(long v)
        {
            if (v >= 1_000_000) return $"{v / 1_000_000.0:F1}M";
            if (v >= 1_000) return $"{v / 1_000.0:F1}K";
            return v.ToString();
        }

        private static Color GetColor(long tokens, long max)
        {
            if (tokens == 0) return ColorTranslator.FromHtml("#242428");
            double ratio = Math.Min(1.0, (double)tokens / max);
            if (ratio < 0.15) return ColorTranslator.FromHtml("#0C2340");
            if (ratio < 0.40) return ColorTranslator.FromHtml("#0A4A9E");
            if (ratio < 0.70) return ColorTranslator.FromHtml("#4D82FF");
            return ColorTranslator.FromHtml("#80AAFF");
        }

        private static Color GetColorAnimated(long tokens, long max, float progress)
        {
            var empty = ColorTranslator.FromHtml("#242428");
            var target = GetColor(tokens, max);
            if (progress >= 1f) return target;
            return Color.FromArgb(
                (int)(empty.R + (target.R - empty.R) * progress),
                (int)(empty.G + (target.G - empty.G) * progress),
                (int)(empty.B + (target.B - empty.B) * progress)
            );
        }
    }
}
