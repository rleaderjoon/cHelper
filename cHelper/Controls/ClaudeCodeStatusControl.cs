using cHelper.Services;
using cHelper.Utils;

namespace cHelper.Controls;

public class ClaudeCodeStatusControl : UserControl
{
    private readonly ClaudeCodeService _claudeCodeService;
    private Label _statusDot = null!;
    private Label _statusLabel = null!;
    private Label _versionLabel = null!;
    private ListView _projectList = null!;

    public ClaudeCodeStatusControl(ClaudeCodeService claudeCodeService)
    {
        _claudeCodeService = claudeCodeService;
        BuildUI();
        LoadData();
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
            BackColor = TossTheme.Background
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        // Status row with colored dot
        var statusRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = TossTheme.Background,
            FlowDirection = FlowDirection.LeftToRight
        };
        _statusDot = new Label
        {
            Text = "●",
            AutoSize = true,
            Font = new Font("Segoe UI", 8f),
            ForeColor = TossTheme.TextTertiary,
            BackColor = TossTheme.Background,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusLabel = new Label
        {
            AutoSize = true,
            Font = TossTheme.Body(9f),
            ForeColor = TossTheme.TextPrimary,
            BackColor = TossTheme.Background,
            TextAlign = ContentAlignment.MiddleLeft
        };
        statusRow.Controls.Add(_statusDot);
        statusRow.Controls.Add(_statusLabel);
        layout.Controls.Add(statusRow, 0, 0);

        // Version row
        _versionLabel = new Label
        {
            AutoSize = true,
            Font = TossTheme.Body(8.5f),
            ForeColor = TossTheme.TextTertiary,
            BackColor = TossTheme.Background,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(_versionLabel, 0, 1);

        // Projects header
        var header = new Label
        {
            Text = "Recent Projects",
            Font = TossTheme.SectionHeader(),
            ForeColor = TossTheme.TextSecondary,
            BackColor = TossTheme.Background,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };
        layout.Controls.Add(header, 0, 2);

        // Project list
        _projectList = new ListView
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
        _projectList.Columns.Add("Project", 160);
        _projectList.Columns.Add("Sessions", 65);
        _projectList.Columns.Add("Last Active", 140);
        _projectList.Columns.Add("Path", 240);
        layout.Controls.Add(_projectList, 0, 3);

        // Context menu for open action
        var menu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Open in Claude Code");
        openItem.Click += (_, _) => OpenSelected();
        menu.Items.Add(openItem);
        _projectList.ContextMenuStrip = menu;
        _projectList.DoubleClick += (_, _) => OpenSelected();
    }

    public void LoadData()
    {
        var version = _claudeCodeService.GetVersion();
        if (version != null)
        {
            _statusDot.ForeColor = TossTheme.Success;
            _statusLabel.Text = "Status: Installed";
            _versionLabel.Text = $"Version: {version}";
        }
        else
        {
            _statusDot.ForeColor = TossTheme.Error;
            _statusLabel.Text = "Status: Not found";
            _versionLabel.Text = "claude CLI not found in PATH";
        }

        _projectList.Items.Clear();
        var projects = _claudeCodeService.GetProjectSummary();
        foreach (var (path, name, count, lastActive) in projects)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(count.ToString());
            item.SubItems.Add(lastActive == DateTime.MinValue ? "-" : lastActive.ToString("yyyy-MM-dd HH:mm"));
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
}
