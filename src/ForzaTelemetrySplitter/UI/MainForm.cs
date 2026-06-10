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
        ClientSize = new Size(560, 380);
        MinimumSize = new Size(480, 320);
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
        _listenInfo.SetBounds(12, 10, ClientSize.Width - 24, 20);
        _listenInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _listenInfo.ForeColor = Color.DimGray;
        _listenInfo.Text = Strings.Main_ListenInfo(_config.ListenIp, _config.ListenPort);
        Controls.Add(_listenInfo);

        // Destination grid
        _grid.SetBounds(12, 34, ClientSize.Width - 24, 250);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.EditMode = DataGridViewEditMode.EditProgrammatically;
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = Strings.Col_On, Name = "Enabled", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Name, Name = "Name", FillWeight = 32 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Ip, Name = "Ip", FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Port, Name = "Port", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Strings.Col_Forwarded, Name = "Count", FillWeight = 22 });
        _grid.CellContentClick += OnGridCellClick;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };
        Controls.Add(_grid);

        // Buttons row
        int by = ClientSize.Height - 84;
        var add = MakeButton(Strings.Main_Add, 12, by, AnchorStyles.Bottom | AnchorStyles.Left);
        add.Click += (_, _) => AddDestination();
        var edit = MakeButton(Strings.Main_Edit, 96, by, AnchorStyles.Bottom | AnchorStyles.Left);
        edit.Click += (_, _) => EditSelected();
        var remove = MakeButton(Strings.Main_Remove, 180, by, AnchorStyles.Bottom | AnchorStyles.Left);
        remove.Click += (_, _) => RemoveSelected();

        // Speed-unit toggle (Mph / Kph), persisted to config.
        _units.DropDownStyle = ComboBoxStyle.DropDownList;
        _units.Items.AddRange(new object[] { "mph", "kph" });
        _units.SelectedIndex = _config.SpeedUnit == SpeedUnit.Kph ? 1 : 0;
        _units.SetBounds(ClientSize.Width - 210, by + 2, 64, 24);
        _units.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _units.SelectedIndexChanged += (_, _) =>
        {
            _config.SpeedUnit = _units.SelectedIndex == 1 ? SpeedUnit.Kph : SpeedUnit.Mph;
            ConfigStore.Save(_config);
        };
        Controls.Add(_units);

        // Language selector (top-right). Entries use native names so they're recognizable in any UI
        // language; "Auto" follows the Windows display language. Order maps to _languageOrder below.
        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        _language.Items.AddRange(new object[] { Strings.Lang_Auto, "English", "日本語", "Français", "Deutsch", "Español" });
        _language.SelectedIndex = Array.IndexOf(_languageOrder, _config.Language) is var i && i >= 0 ? i : 0;
        _language.SetBounds(ClientSize.Width - 132, 8, 120, 24);
        _language.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _language.SelectedIndexChanged += OnLanguageChanged;
        Controls.Add(_language);

        _startStop.SetBounds(ClientSize.Width - 132, by, 120, 28);
        _startStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _startStop.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _startStop.Click += (_, _) => ToggleRunning();
        Controls.Add(_startStop);

        // Status strip
        _status.SetBounds(12, ClientSize.Height - 44, ClientSize.Width - 24, 32);
        _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _status.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        Controls.Add(_status);

        UpdateStartStopButton();
    }

    private Button MakeButton(string text, int x, int y, AnchorStyles anchor)
    {
        var b = new Button { Text = text };
        b.SetBounds(x, y, 78, 28);
        b.Anchor = anchor;
        Controls.Add(b);
        return b;
    }

    private void RefreshGrid()
    {
        _grid.Rows.Clear();
        foreach (var d in _config.Destinations)
        {
            int idx = _grid.Rows.Add(d.Enabled, d.Name, d.Ip, d.Port, Interlocked.Read(ref d.ForwardedCount));
            _grid.Rows[idx].Tag = d;
        }
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

        // Refresh per-destination forwarded counts in the grid.
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is Destination d)
                row.Cells["Count"].Value = Interlocked.Read(ref d.ForwardedCount);
        }

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
        _status.Text = text;

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
