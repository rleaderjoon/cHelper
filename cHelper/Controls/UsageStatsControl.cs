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
        BuildUI();
        Refresh();
    }

    private void BuildUI()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        Controls.Add(layout);

        // Stat boxes row
        var statsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
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
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
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
            GridLines = true,
            Font = new Font("Segoe UI", 8.5f)
        };
        _recentList.Columns.Add("Time", 80);
        _recentList.Columns.Add("Model", 140);
        _recentList.Columns.Add("Input", 60);
        _recentList.Columns.Add("Output", 60);
        _recentList.Columns.Add("Cost", 65);
        _recentList.Columns.Add("Source", 55);
        layout.Controls.Add(_recentList, 0, 3);
    }

    private static Panel MakeStatBox(string title, out Label costLabel, out Label tokensLabel)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            BorderStyle = BorderStyle.FixedSingle
        };

        var titleLbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.Gray,
            Location = new Point(8, 6),
            AutoSize = true
        };
        costLabel = new Label
        {
            Text = "$0.00",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location = new Point(8, 24),
            AutoSize = true
        };
        tokensLabel = new Label
        {
            Text = "0 tokens",
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.DimGray,
            Location = new Point(8, 52),
            AutoSize = true
        };

        panel.Controls.Add(titleLbl);
        panel.Controls.Add(costLabel);
        panel.Controls.Add(tokensLabel);
        return panel;
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
        var model = new PlotModel { Title = "Daily Usage (30 days)", TitleFontSize = 10 };

        var barSeries = new BarSeries
        {
            FillColor = OxyColor.FromRgb(99, 102, 241),
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
            FontSize = 7
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Cost (USD)",
            TitleFontSize = 8,
            FontSize = 7
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
