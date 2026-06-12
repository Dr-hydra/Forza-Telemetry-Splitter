using System.ComponentModel;
using ForzaTelemetrySplitter.Config;
using ForzaTelemetrySplitter.Core;
using ForzaTelemetrySplitter.Resources;

namespace ForzaTelemetrySplitter.UI;

/// <summary>
/// The main configuration window: the destination grid, add/edit/remove/toggle, a Start/Stop
/// button, and a live status strip. Closing the window hides it to the tray rather than exiting.
/// </summary>
public sealed class MainForm : Form
{
    private readonly AppConfig _config;
    private readonly SplitterEngine _engine;

    private readonly DataGridView _grid = new();
    private readonly Button _startStop = new();
    private readonly Label _status = new();
    private readonly Label _listenInfo = new();
    private readonly ComboBox _units = new();
    private readonly ComboBox _language = new();
    private readonly CheckBox _startWithWindows = new();
    private readonly Button _record = new();
    private readonly TabControl _tabs = new();
    private readonly TabPage _statusPage = new();
    private readonly TabPage _activityPage = new();
    private ActivityChart? _chart;
    private readonly Dictionary<Destination, long> _prevForwarded = new();
    private readonly Dictionary<Destination, DestinationStatus> _statusCache = new();
    private readonly System.Windows.Forms.Timer _uiTimer = new();

    // Index order of the language dropdown items.
    private static readonly AppLanguage[] _languageOrder =
    {
        AppLanguage.Auto, AppLanguage.English, AppLanguage.Japanese,
        AppLanguage.French, AppLanguage.German, AppLanguage.Spanish,
    };

    private readonly OverlayForm _overlay;

    /// <summary>True once the app is genuinely exiting (vs. just hiding to tray).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ReallyExit { get; set; }

    /// <summary>Raised when the engine's running state changes, so the tray can refresh its menu.</summary>
    public event Action? RunStateChanged;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SplitterEngine Engine => _engine;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public OverlayForm Overlay => _overlay;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AppConfig Config => _config;

    public MainForm(AppConfig config, SplitterEngine engine, OverlayForm overlay)
    {
        _config = config;
        _engine = engine;
        _overlay = overlay;

        Text = Strings.Main_Title;
        Icon = AppIcon.Load();
        ClientSize = new Size(620, 440);
        MinimumSize = new Size(540, 380);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        BuildLayout();
        RefreshGrid();

        _engine.ErrorOccurred += OnEngineError;
        _engine.PortInUse += OnPortInUse;

        _uiTimer.Interval = 250; // ~4 Hz
        _uiTimer.Tick += (_, _) => UpdateStatus();
        _uiTimer.Start();
    }

    private void BuildLayout()
    {
        // Two tabs: Status (the destinations grid + controls) and Activity (the throughput chart).
        _tabs.Dock = DockStyle.Fill;
        _statusPage.Text = Strings.Tab_Status;
        _activityPage.Text = Strings.Tab_Activity;
        _tabs.TabPages.Add(_statusPage);
        _tabs.TabPages.Add(_activityPage);
        Controls.Add(_tabs);

        BuildStatusTab();
        BuildActivityTab();

        UpdateStartStopButton();
    }

    private void BuildStatusTab()
    {
        var host = _statusPage;
        int hostW = host.ClientSize.Width;
        int hostH = host.ClientSize.Height;

        _listenInfo.SetBounds(12, 10, hostW - 24, 20);
        _listenInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _listenInfo.ForeColor = Color.DimGray;
        _listenInfo.Text = Strings.Main_ListenInfo(_config.ListenIp, _config.ListenPort);
        host.Controls.Add(_listenInfo);

        // Destination grid
        _grid.SetBounds(12, 34, hostW - 24, hostH - 130);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ShowCellToolTips = true;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.EditMode = DataGridViewEditMode.EditProgrammatically;
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = Strings.Col_On, Name = "Enabled", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Status, Name = "Status", FillWeight = 24, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Name, Name = "Name", FillWeight = 28 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Ip, Name = "Ip", FillWeight = 26 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Port, Name = "Port", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Forwarded, Name = "Count", FillWeight = 18 });
        _grid.CellContentClick += OnGridCellClick;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };
        _grid.CellPainting += OnStatusCellPainting;
        host.Controls.Add(_grid);

        // Buttons row
        int by = host.ClientSize.Height - 84;
        var add = MakeButton(host, Strings.Main_Add, 12, by, AnchorStyles.Bottom | AnchorStyles.Left);
        add.Click += (_, _) => AddDestination();
        var edit = MakeButton(host, Strings.Main_Edit, 96, by, AnchorStyles.Bottom | AnchorStyles.Left);
        edit.Click += (_, _) => EditSelected();
        var remove = MakeButton(host, Strings.Main_Remove, 180, by, AnchorStyles.Bottom | AnchorStyles.Left);
        remove.Click += (_, _) => RemoveSelected();

        // Speed-unit toggle (Mph / Kph), persisted to config.
        _units.DropDownStyle = ComboBoxStyle.DropDownList;
        _units.Items.AddRange(new object[] { "mph", "kph" });
        _units.SelectedIndex = _config.SpeedUnit == SpeedUnit.Kph ? 1 : 0;
        _units.SetBounds(host.ClientSize.Width - 210, by + 2, 64, 24);
        _units.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _units.SelectedIndexChanged += (_, _) =>
        {
            _config.SpeedUnit = _units.SelectedIndex == 1 ? SpeedUnit.Kph : SpeedUnit.Mph;
            ConfigStore.Save(_config);
        };
        host.Controls.Add(_units);

        // Language selector (top-right). Entries use native names so they're recognizable in any UI
        // language; "Auto" follows the Windows display language. Order maps to _languageOrder below.
        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        _language.Items.AddRange(new object[] { Strings.Lang_Auto, "English", "日本語", "Français", "Deutsch", "Español" });
        _language.SelectedIndex = Array.IndexOf(_languageOrder, _config.Language) is var i && i >= 0 ? i : 0;
        _language.SetBounds(host.ClientSize.Width - 132, 8, 120, 24);
        _language.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _language.SelectedIndexChanged += OnLanguageChanged;
        host.Controls.Add(_language);

        _startStop.SetBounds(host.ClientSize.Width - 132, by, 120, 28);
        _startStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _startStop.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _startStop.Click += (_, _) => ToggleRunning();
        host.Controls.Add(_startStop);

        // Extra row above the buttons: "Start with Windows" (left) and Record (right).
        int er = by - 32;
        _startWithWindows.Text = Strings.Main_StartWithWindows;
        _startWithWindows.AutoSize = true;
        _startWithWindows.SetBounds(12, er + 4, 240, 22);
        _startWithWindows.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _startWithWindows.Checked = StartupRegistration.IsEnabled();
        _startWithWindows.CheckedChanged += OnStartWithWindowsChanged;
        host.Controls.Add(_startWithWindows);

        _record.Text = Strings.Main_Record;
        _record.SetBounds(host.ClientSize.Width - 132, er, 120, 26);
        _record.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _record.Click += (_, _) => ToggleRecording();
        host.Controls.Add(_record);

        // Status strip
        _status.SetBounds(12, host.ClientSize.Height - 44, host.ClientSize.Width - 24, 32);
        _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _status.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        host.Controls.Add(_status);
    }

    private void BuildActivityTab()
    {
        var host = _activityPage;
        _chart = new ActivityChart(_engine.History)
        {
            Bounds = new Rectangle(8, 8, host.ClientSize.Width - 16, host.ClientSize.Height - 48),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };
        host.Controls.Add(_chart);

        var zoomIn = new Button { Text = "+", Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
        zoomIn.SetBounds(8, host.ClientSize.Height - 36, 36, 28);
        zoomIn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        zoomIn.Click += (_, _) => _chart?.ZoomIn();
        new ToolTip().SetToolTip(zoomIn, Strings.Zoom_In);
        host.Controls.Add(zoomIn);

        var zoomOut = new Button { Text = "−", Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
        zoomOut.SetBounds(48, host.ClientSize.Height - 36, 36, 28);
        zoomOut.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        zoomOut.Click += (_, _) => _chart?.ZoomOut();
        new ToolTip().SetToolTip(zoomOut, Strings.Zoom_Out);
        host.Controls.Add(zoomOut);
    }

    private Button MakeButton(Control host, string text, int x, int y, AnchorStyles anchor)
    {
        var b = new Button { Text = text };
        b.SetBounds(x, y, 78, 28);
        b.Anchor = anchor;
        host.Controls.Add(b);
        return b;
    }

    private void RefreshGrid()
    {
        _grid.Rows.Clear();
        foreach (var d in _config.Destinations)
        {
            int idx = _grid.Rows.Add();
            var row = _grid.Rows[idx];
            row.Cells["Enabled"].Value = d.Enabled;
            row.Cells["Name"].Value = d.Name;
            row.Cells["Ip"].Value = d.Ip;
            row.Cells["Port"].Value = d.Port;
            row.Cells["Count"].Value = Interlocked.Read(ref d.ForwardedCount);
            row.Cells["Status"].Value = ""; // text drawn by OnStatusCellPainting; value used for tooltip
            row.Tag = d;
        }
    }

    /// <summary>Map a destination's status to (color, label) for the dot cell.</summary>
    private static (Color color, string label) StatusVisual(DestinationStatus s) => s switch
    {
        DestinationStatus.Forwarding => (Color.FromArgb(46, 204, 113), Strings.Dot_Forwarding),
        DestinationStatus.Idle       => (Color.FromArgb(241, 196, 15), Strings.Dot_Idle),
        DestinationStatus.Error      => (Color.FromArgb(231, 76, 60),  Strings.Dot_Error),
        _                            => (Color.Gray,                   Strings.Dot_Disabled),
    };

    /// <summary>Owner-draw the Status cell: a shaped dot + text label (color is never the only cue).</summary>
    private void OnStatusCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Status") return;
        if (_grid.Rows[e.RowIndex].Tag is not Destination d) return;

        var status = _statusCache.TryGetValue(d, out var cs) ? cs
            : (d.Enabled ? DestinationStatus.Idle : DestinationStatus.Disabled);
        var (color, label) = StatusVisual(status);

        e.PaintBackground(e.CellBounds, true);
        var b = e.CellBounds;
        int cy = b.Top + b.Height / 2;
        int dotX = b.Left + 6, dotSize = 10;
        using (var brush = new SolidBrush(color))
        {
            e.Graphics!.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Distinct shapes per state (not color alone): filled circle / hollow ring / triangle.
            switch (status)
            {
                case DestinationStatus.Forwarding:
                    e.Graphics.FillEllipse(brush, dotX, cy - dotSize / 2, dotSize, dotSize);
                    break;
                case DestinationStatus.Idle:
                    using (var pen = new Pen(color, 2))
                        e.Graphics.DrawEllipse(pen, dotX, cy - dotSize / 2, dotSize, dotSize);
                    break;
                case DestinationStatus.Error:
                    var tri = new[] {
                        new Point(dotX + dotSize / 2, cy - dotSize / 2),
                        new Point(dotX, cy + dotSize / 2),
                        new Point(dotX + dotSize, cy + dotSize / 2) };
                    e.Graphics.FillPolygon(brush, tri);
                    break;
                default: // Disabled — hollow grey ring
                    using (var pen = new Pen(color, 1.5f))
                        e.Graphics.DrawEllipse(pen, dotX, cy - dotSize / 2, dotSize, dotSize);
                    break;
            }
        }
        TextRenderer.DrawText(e.Graphics!, label, _grid.Font,
            new Rectangle(dotX + dotSize + 6, b.Top, b.Width - dotSize - 12, b.Height),
            _grid.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        e.Handled = true;
    }


    private Destination? Selected =>
        _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].Tag as Destination : null;

    private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        // Toggle the Enabled checkbox column inline.
        if (_grid.Columns[e.ColumnIndex].Name == "Enabled" &&
            _grid.Rows[e.RowIndex].Tag is Destination d)
        {
            d.Enabled = !d.Enabled;
            _grid.Rows[e.RowIndex].Cells["Enabled"].Value = d.Enabled;
            ApplyDestinations();
        }
    }

    private void AddDestination()
    {
        using var dlg = new DestinationDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (HasPortConflict(dlg.Result, null)) return;
        _config.Destinations.Add(dlg.Result);
        ApplyDestinations();
        RefreshGrid();
    }

    private void EditSelected()
    {
        var d = Selected;
        if (d is null) return;
        using var dlg = new DestinationDialog(d);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (HasPortConflict(dlg.Result, d)) return;
        d.Name = dlg.Result.Name;
        d.Ip = dlg.Result.Ip;
        d.Port = dlg.Result.Port;
        d.Enabled = dlg.Result.Enabled;
        ApplyDestinations();
        RefreshGrid();
    }

    private void RemoveSelected()
    {
        var d = Selected;
        if (d is null) return;
        if (MessageBox.Show(this, Strings.Remove_Confirm(d.Name), Strings.Remove_Title,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _config.Destinations.Remove(d);
        ApplyDestinations();
        RefreshGrid();
    }

    /// <summary>
    /// Warn if two destinations share the same IP:port (a likely mistake), or if a destination
    /// collides with the splitter's own listen port.
    /// </summary>
    private bool HasPortConflict(Destination candidate, Destination? excluding)
    {
        if (string.Equals(candidate.Ip, _config.ListenIp, StringComparison.OrdinalIgnoreCase) &&
            candidate.Port == _config.ListenPort)
        {
            MessageBox.Show(this,
                $"{candidate.Ip}:{candidate.Port} is the splitter's own listen port.\n\n" +
                "A destination must be a DIFFERENT port that your tool listens on.",
                "Port conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }

        foreach (var d in _config.Destinations)
        {
            if (ReferenceEquals(d, excluding)) continue;
            if (string.Equals(d.Ip, candidate.Ip, StringComparison.OrdinalIgnoreCase) &&
                d.Port == candidate.Port)
            {
                MessageBox.Show(this,
                    $"Another destination (\"{d.Name}\") already uses {candidate.Ip}:{candidate.Port}.",
                    "Duplicate destination", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
        }
        return false;
    }

    private void ApplyDestinations()
    {
        _engine.SetDestinations(_config.Destinations);
        ConfigStore.Save(_config);
    }

    public void ToggleRunning()
    {
        if (_engine.Running)
        {
            _engine.Stop();
        }
        else
        {
            _engine.SetDestinations(_config.Destinations);
            _engine.Start(_config.ListenIp, _config.ListenPort);
        }
        UpdateStartStopButton();
        RunStateChanged?.Invoke();
    }

    private void UpdateStartStopButton()
    {
        _startStop.Text = _engine.Running ? Strings.Tray_StopSplitting : Strings.Tray_StartSplitting;
    }

    private void OnStartWithWindowsChanged(object? sender, EventArgs e)
    {
        bool ok = _startWithWindows.Checked ? StartupRegistration.Enable() : StartupRegistration.Disable();
        if (!ok)
        {
            // Registry write failed — revert the checkbox to the real state without re-firing.
            _startWithWindows.CheckedChanged -= OnStartWithWindowsChanged;
            _startWithWindows.Checked = StartupRegistration.IsEnabled();
            _startWithWindows.CheckedChanged += OnStartWithWindowsChanged;
        }
    }

    private void ToggleRecording()
    {
        if (_engine.IsRecording)
        {
            _engine.StopRecording();
        }
        else
        {
            using var dlg = new SaveFileDialog
            {
                Title = Strings.Record_SaveTitle,
                Filter = Strings.Record_Filter,
                FileName = $"forza-session-{DateTime.Now:yyyyMMdd-HHmmss}.fts",
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            _engine.StartRecording(dlg.FileName);
        }
        UpdateRecordButton();
    }

    private void UpdateRecordButton()
    {
        _record.Text = _engine.IsRecording ? Strings.Main_StopRecording : Strings.Main_Record;
        _record.ForeColor = _engine.IsRecording ? Color.Firebrick : SystemColors.ControlText;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        int idx = _language.SelectedIndex;
        if (idx < 0 || idx >= _languageOrder.Length) return;

        _config.Language = _languageOrder[idx];
        ConfigStore.Save(_config);
        Program.ApplyCulture(_config.Language);

        // Strings already shown on built controls don't retranslate live; a relaunch applies the
        // language everywhere. Tell the user (in the newly-selected language).
        MessageBox.Show(this, Strings.Lang_RestartNote, Strings.Main_Title,
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateStatus()
    {
        var s = _engine.GetStatus();

        // Refresh per-destination counts + status dots. CurrentStatus() reads _prevForwarded, so
        // invalidate the dot first, THEN update _prevForwarded for the next tick.
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is not Destination d) continue;
            long count = Interlocked.Read(ref d.ForwardedCount);
            bool increased = _prevForwarded.TryGetValue(d, out var prev) && count > prev;
            _statusCache[d] = DestinationStatusLogic.Derive(d.Enabled, s.Receiving, increased, d.LastSendFailed);
            _prevForwarded[d] = count;

            row.Cells["Count"].Value = count;
            row.Cells["Status"].ToolTipText = Strings.Dot_Tooltip(d.Ip, d.Port);
            _grid.InvalidateCell(row.Cells["Status"]);   // repaint the dot from the cached status
        }

        // Activity chart: only repaint when its tab is visible and the window isn't minimized.
        _chart?.SetRunning(s.Running);
        if (_tabs.SelectedTab == _activityPage && WindowState != FormWindowState.Minimized)
            _chart?.Invalidate();

        // Live gear + speed readout, shown when telemetry is flowing.
        string readout = Strings.Readout_Gear(
            SpeedUnitExtensions.FormatGear(s.Gear),
            SpeedUnitExtensions.FormatSpeed(s.SpeedMps, _config.SpeedUnit));

        string dot = s.Receiving ? "🟢" : (s.Running ? "🟡" : "⚪");
        string text;
        if (!s.Running)
            text = $"{dot}  {Strings.Status_Stopped}";
        else if (s.Receiving)
            text = $"{dot}  " + Strings.Status_Connected(
                ForzaPacket.GameName(s.Format),
                s.PacketsPerSecond,
                s.IsRaceOn ? Strings.Status_RaceOn : Strings.Status_RaceOff,
                readout);
        else
            text = $"{dot}  {Strings.Status_WaitingForForza(_config.ListenIp, _config.ListenPort)}";
        if (_engine.IsRecording) text += $"   ⏺ {Strings.Main_Recording}";
        _status.Text = text;
        UpdateRecordButton();

        _overlay.SetStatus(s.Receiving, readout);
    }

    private void OnEngineError(string message)
    {
        // Marshal to UI thread.
        if (InvokeRequired) { BeginInvoke(new Action<string>(OnEngineError), message); return; }

        UpdateStartStopButton();
        RunStateChanged?.Invoke();
        MessageBox.Show(this, message, Strings.Main_Title,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void OnPortInUse(int port)
    {
        if (InvokeRequired) { BeginInvoke(new Action<int>(OnPortInUse), port); return; }

        UpdateStartStopButton();
        RunStateChanged?.Invoke();
        MessageBox.Show(this, Strings.Error_PortInUse(port), Strings.Main_Title,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Closing via the X hides to tray instead of exiting.
        if (!ReallyExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        _uiTimer.Stop();
        base.OnFormClosing(e);
    }
}
