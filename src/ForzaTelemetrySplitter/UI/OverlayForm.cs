using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ForzaTelemetrySplitter.UI;

/// <summary>
/// A tiny always-on-top status pill that auto-positions in the top-right corner of the
/// primary screen. Green dot + "Connected" when Forza data is flowing, red dot + "No data"
/// otherwise.
///
/// It is made topmost AND non-activating (WS_EX_NOACTIVATE) plus click-through
/// (WS_EX_TRANSPARENT) so it draws over borderless Forza without ever stealing input focus.
/// WS_EX_TOOLWINDOW keeps it out of the Alt-Tab list.
/// </summary>
public sealed class OverlayForm : Form
{
    private const int WS_EX_TOPMOST     = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW  = 0x00000080;
    private const int WS_EX_NOACTIVATE  = 0x08000000;

    private const int Inset = 12; // px gap from the screen edges

    private bool _connected;
    private string _readout = ""; // e.g. "Gear 4   112 mph" when connected

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        Size = new Size(208, 34);

        // Rounded, semi-transparent dark pill.
        BackColor = Color.Black;
        Opacity = 0.82;

        Font = new Font("Segoe UI", 9f, FontStyle.Bold);

        Reposition();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT;
            return cp;
        }
    }

    // Never take focus when shown.
    protected override bool ShowWithoutActivation => true;

    /// <summary>
    /// Update the indicator and live readout. Cheap; called ~4x/sec from the UI timer.
    /// <paramref name="readout"/> (e.g. "Gear 4   112 mph") is shown only when connected.
    /// </summary>
    public void SetStatus(bool connected, string readout)
    {
        if (_connected == connected && _readout == readout) return;
        _connected = connected;
        _readout = readout;
        Invalidate();
    }

    /// <summary>Snap to the top-right of the primary screen's working area.</summary>
    public void Reposition()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(area.Right - Width - Inset, area.Top + Inset);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        // Rounded rectangle background.
        using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10))
        using (var bg = new SolidBrush(Color.FromArgb(28, 28, 30)))
        {
            g.FillPath(bg, path);
        }

        // Status dot.
        var dotColor = _connected ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);
        var dotRect = new Rectangle(12, Height / 2 - 6, 12, 12);
        using (var dot = new SolidBrush(dotColor))
        {
            g.FillEllipse(dot, dotRect);
        }
        // Subtle glow ring.
        using (var ring = new Pen(Color.FromArgb(90, dotColor), 2))
        {
            g.DrawEllipse(ring, Rectangle.Inflate(dotRect, 2, 2));
        }

        // Label: live readout when connected, otherwise the plain status.
        string label = _connected
            ? (string.IsNullOrEmpty(_readout) ? Resources.Strings.Overlay_Connected : _readout)
            : Resources.Strings.Overlay_NoData;
        using var text = new SolidBrush(Color.White);
        var textRect = new Rectangle(32, 0, Width - 36, Height);
        var fmt = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
        g.DrawString(label, Font, text, textRect, fmt);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
