using cHelper.Models;
using cHelper.Services;
using cHelper.Utils;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace cHelper.Controls;

public class UsageStatsControl : UserControl
{
    private readonly UsageTrackingService _usageService;
    private Label _todayCostLabel = null!;
    private Label _todayTokensLabel = null!;
    private Label _monthCostLabel = null!;
    private Label _monthTokensLabel = null!;
    private Label _allCostLabel = null!;
    private Label _allTokensLabel = null!;
    private ListView _recentList = null!;
    private PlotView _chart = null!;

    public UsageStatsControl(UsageTrackingService usageService)
    {
        _usageService = usageService;
        DoubleBuffered = true;
        BuildUI();
        Refresh();
    }

    private void BuildUI()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(12);
        BackColor = TossTheme.Background;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(0),
            BackColor = TossTheme.Background
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        Controls.Add(layout);

        // Stat boxes row
        var statsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = TossTheme.Background
        };
        statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));

        statsPanel.Controls.Add(MakeStatBox("Today", out _todayCostLabel, out _todayTokensLabel), 0, 0);
        statsPanel.Controls.Add(MakeStatBox("This Month", out _monthCostLabel, out _monthTokensLabel), 1, 0);
        statsPanel.Controls.Add(MakeStatBox("All Time", out _allCostLabel, out _allTokensLabel), 2, 0);
        layout.Controls.Add(statsPanel, 0, 0);

        // Chart
        _chart = new PlotView { Dock = DockStyle.Fill, Model = new PlotModel() };
        layout.Controls.Add(_chart, 0, 1);

        // Recent label
        var recentLabel = new Label
        {
            Text = "Recent Activity",
            Font = TossTheme.SectionHeader(),
            ForeColor = TossTheme.TextSecondary,
            BackColor = TossTheme.Background,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };
        layout.Controls.Add(recentLabel, 0, 2);

        // Recent list
        _recentList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BackColor = TossTheme.Surface,
            ForeColor = TossTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = TossTheme.Body(8.5f)
        };
        _recentList.Columns.Add("Time", 80);
        _recentList.Columns.Add("Model", 140);
        _recentList.Columns.Add("Input", 60);
        _recentList.Columns.Add("Output", 60);
        _recentList.Columns.Add("Cost", 65);
        _recentList.Columns.Add("Source", 55);
        layout.Controls.Add(_recentList, 0, 3);
    }

    private static RoundedCard MakeStatBox(string title, out Label costLabel, out Label tokensLabel)
    {
        var card = new RoundedCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(12, 10, 12, 10)
        };

        var titleLbl = new Label
        {
            Text = title,
            Font = TossTheme.StatLabel(),
            ForeColor = TossTheme.TextTertiary,
            BackColor = TossTheme.CardBackground,
            Location = new Point(12, 10),
            AutoSize = true
        };
        costLabel = new Label
        {
            Text = "$0.00",
            Font = TossTheme.StatValue(),
            ForeColor = TossTheme.TextPrimary,
            BackColor = TossTheme.CardBackground,
            Location = new Point(12, 26),
            AutoSize = true
        };
        tokensLabel = new Label
        {
            Text = "0 tokens",
            Font = TossTheme.StatLabel(),
            ForeColor = TossTheme.TextTertiary,
            BackColor = TossTheme.CardBackground,
            Location = new Point(12, 52),
            AutoSize = true
        };

        card.Controls.Add(titleLbl);
        card.Controls.Add(costLabel);
        card.Controls.Add(tokensLabel);
        return card;
    }

    public new void Refresh()
    {
        var today = _usageService.GetToday();
        var month = _usageService.GetThisMonth();
        var all = _usageService.GetAllTime();

        _todayCostLabel.Text = TokenCostCalculator.FormatCost(today.TotalCostUsd);
        _todayTokensLabel.Text = TokenCostCalculator.FormatTokens(today.TotalTokens) + " tok";
        _monthCostLabel.Text = TokenCostCalculator.FormatCost(month.TotalCostUsd);
        _monthTokensLabel.Text = TokenCostCalculator.FormatTokens(month.TotalTokens) + " tok";
        _allCostLabel.Text = TokenCostCalculator.FormatCost(all.TotalCostUsd);
        _allTokensLabel.Text = TokenCostCalculator.FormatTokens(all.TotalTokens) + " tok";

        RefreshChart();
        RefreshRecentList();
    }

    private void RefreshChart()
    {
        var daily = _usageService.GetDailyBreakdown(30);
        var model = new PlotModel
        {
            Title = "Daily Usage (30 days)",
            TitleFontSize = 10,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            TextColor = OxyColor.FromRgb(78, 89, 104)
        };

        var barSeries = new BarSeries
        {
            FillColor = OxyColor.FromRgb(49, 130, 246),
            Title = "Cost ($)"
        };

        var startDate = DateTime.UtcNow.Date.AddDays(-29);
        var labels = new List<string>();
        for (int i = 0; i < 30; i++)
        {
            var d = startDate.AddDays(i);
            daily.TryGetValue(d, out var s);
            barSeries.Items.Add(new BarItem((double)(s?.TotalCostUsd ?? 0)));
            labels.Add(i % 5 == 0 ? d.ToString("M/d") : "");
        }

        model.Series.Add(barSeries);
        model.Axes.Add(new CategoryAxis
        {
            Position = AxisPosition.Left,
            ItemsSource = labels,
            FontSize = 7,
            AxislineColor = OxyColor.FromRgb(229, 232, 235),
            TicklineColor = OxyColor.FromRgb(229, 232, 235)
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Cost (USD)",
            TitleFontSize = 8,
            FontSize = 7,
            AxislineColor = OxyColor.FromRgb(229, 232, 235),
            TicklineColor = OxyColor.FromRgb(229, 232, 235)
        });

        _chart.Model = model;
    }

    private void RefreshRecentList()
    {
        _recentList.Items.Clear();
        var records = _usageService.GetRecent(30);
        foreach (var r in records)
        {
            var item = new ListViewItem(r.Timestamp.ToLocalTime().ToString("HH:mm:ss"));
            item.SubItems.Add(r.Model.Replace("claude-", "").Replace("-20", " "));
            item.SubItems.Add(TokenCostCalculator.FormatTokens(r.InputTokens));
            item.SubItems.Add(TokenCostCalculator.FormatTokens(r.OutputTokens));
            item.SubItems.Add(TokenCostCalculator.FormatCost(r.CostUsd));
            item.SubItems.Add(r.Source);
            _recentList.Items.Add(item);
        }
    }
}
