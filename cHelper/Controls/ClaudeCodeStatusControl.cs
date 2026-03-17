using cHelper.Services;
using cHelper.Utils;

namespace cHelper.Controls;

public class ClaudeCodeStatusControl : UserControl
{
    private readonly ClaudeCodeService _claudeCodeService;
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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        // Status row
        var statusRow = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _statusLabel = new Label { AutoSize = true, Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.MiddleLeft };
        statusRow.Controls.Add(_statusLabel);
        layout.Controls.Add(statusRow, 0, 0);

        // Version row
        _versionLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.DimGray,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(_versionLabel, 0, 1);

        // Projects header
        var header = new Label
        {
            Text = "Recent Projects",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
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
            GridLines = true,
            Font = new Font("Segoe UI", 8.5f)
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
            _statusLabel.Text = "Status: Installed";
            _statusLabel.ForeColor = Color.Green;
            _versionLabel.Text = $"Version: {version}";
        }
        else
        {
            _statusLabel.Text = "Status: Not found";
            _statusLabel.ForeColor = Color.OrangeRed;
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
