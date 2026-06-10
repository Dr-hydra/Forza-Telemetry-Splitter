using System.Net;
using ForzaTelemetrySplitter.Config;
using ForzaTelemetrySplitter.Core;
using ForzaTelemetrySplitter.Resources;

namespace ForzaTelemetrySplitter.UI;

/// <summary>
/// Add/edit dialog for a single forwarding destination. A preset dropdown auto-fills the name
/// and a sensible default port for known telemetry tools; all fields remain editable.
/// </summary>
public sealed class DestinationDialog : Form
{
    private readonly ComboBox _preset = new();
    private readonly TextBox _name = new();
    private readonly TextBox _ip = new();
    private readonly NumericUpDown _port = new();
    private readonly CheckBox _enabled = new();
    private readonly Label _note = new();

    /// <summary>The edited destination, valid only when DialogResult == OK.</summary>
    public Destination Result { get; private set; } = new();

    public DestinationDialog(Destination? existing = null)
    {
        Text = existing is null ? Strings.Dest_AddTitle : Strings.Dest_EditTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(380, 250);
        Font = new Font("Segoe UI", 9f);

        int labelX = 14, fieldX = 110, fieldW = 250, y = 16, rowH = 34;

        AddLabel(Strings.Dest_Preset, labelX, y);
        _preset.DropDownStyle = ComboBoxStyle.DropDownList;
        _preset.SetBounds(fieldX, y - 3, fieldW, 24);
        foreach (var p in TunerPresets.All) _preset.Items.Add(p.Name);
        _preset.SelectedIndexChanged += OnPresetChanged;
        Controls.Add(_preset);
        y += rowH;

        AddLabel(Strings.Dest_Name, labelX, y);
        _name.SetBounds(fieldX, y - 3, fieldW, 24);
        Controls.Add(_name);
        y += rowH;

        AddLabel(Strings.Dest_Ip, labelX, y);
        _ip.SetBounds(fieldX, y - 3, fieldW, 24);
        Controls.Add(_ip);
        y += rowH;

        AddLabel(Strings.Dest_PortLabel, labelX, y);
        _port.SetBounds(fieldX, y - 3, 90, 24);
        _port.Minimum = 1;
        _port.Maximum = 65535;
        Controls.Add(_port);
        y += rowH;

        _enabled.Text = Strings.Dest_Enabled;
        _enabled.SetBounds(fieldX, y, 120, 24);
        Controls.Add(_enabled);
        y += rowH;

        _note.SetBounds(labelX, y, ClientSize.Width - 28, 36);
        _note.ForeColor = Color.DimGray;
        _note.Font = new Font("Segoe UI", 8f, FontStyle.Italic);
        Controls.Add(_note);

        var ok = new Button { Text = Strings.Dest_Ok, DialogResult = DialogResult.None };
        ok.SetBounds(ClientSize.Width - 180, ClientSize.Height - 36, 80, 26);
        ok.Click += OnOk;
        Controls.Add(ok);

        var cancel = new Button { Text = Strings.Dest_Cancel, DialogResult = DialogResult.Cancel };
        cancel.SetBounds(ClientSize.Width - 92, ClientSize.Height - 36, 80, 26);
        Controls.Add(cancel);

        AcceptButton = ok;
        CancelButton = cancel;

        if (existing is not null)
        {
            _name.Text = existing.Name;
            _ip.Text = existing.Ip;
            _port.Value = Math.Clamp(existing.Port, 1, 65535);
            _enabled.Checked = existing.Enabled;
            // Try to reflect a matching preset by name; otherwise leave unselected.
            int idx = _preset.Items.IndexOf(existing.Name);
            if (idx >= 0) _preset.SelectedIndex = idx;
        }
        else
        {
            _ip.Text = "127.0.0.1";
            _enabled.Checked = true;
            _port.Value = 5556;
        }
    }

    private void AddLabel(string text, int x, int y)
    {
        var l = new Label { Text = text, AutoSize = true };
        l.SetBounds(x, y, 90, 20);
        Controls.Add(l);
    }

    private void OnPresetChanged(object? sender, EventArgs e)
    {
        if (_preset.SelectedIndex < 0) return;
        var preset = TunerPresets.All[_preset.SelectedIndex];
        _note.Text = preset.Note;

        // Custom… leaves the fields for the user to fill in.
        if (preset.Name == TunerPresets.Custom.Name)
        {
            if (string.IsNullOrWhiteSpace(_name.Text) || IsKnownPresetName(_name.Text))
                _name.Text = string.Empty;
            return;
        }

        _name.Text = preset.Name;
        _port.Value = Math.Clamp(preset.DefaultPort, 1, 65535);
    }

    private static bool IsKnownPresetName(string name) =>
        TunerPresets.All.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    private void OnOk(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show(this, Strings.Dest_MissingName, Strings.Main_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!IPAddress.TryParse(_ip.Text.Trim(), out _))
        {
            MessageBox.Show(this, Strings.Dest_InvalidIp, Strings.Main_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Result = new Destination
        {
            Name = _name.Text.Trim(),
            Ip = _ip.Text.Trim(),
            Port = (int)_port.Value,
            Enabled = _enabled.Checked,
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
