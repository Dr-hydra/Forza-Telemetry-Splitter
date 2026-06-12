using System.Drawing.Drawing2D;
using ForzaTelemetrySplitter.Config;

namespace ForzaTelemetrySplitter.UI;

/// <summary>
/// A tiny always-on-top status pill. Green dot + "Connected" when Forza data is flowing, red dot +
/// "No data" otherwise, with the live gear/speed readout.
///
/// Appearance is user-configurable (Overlay tab): transparent background (text only) or a colored
/// panel with adjustable opacity, plus text color and a saved position. It is normally topmost,
/// non-activating and click-through so it draws over borderless Forza without stealing focus; "move
/// mode" temporarily disables click-through so the user can drag it.
/// </summary>
public sealed class OverlayForm : Form
{
    private const int WS_EX_TOPMOST     = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW  = 0x00000080;
    private const int WS_EX_NOACTIVATE  = 0x08000000;

    private const int Inset = 12; // px gap from the screen edges
    // A color the user is extremely unlikely to choose; used as the transparency key so the
    // non-panel area (or the whole background in transparent mode) renders see-through.
    private static readonly Color KeyColor = Color.FromArgb(255, 1, 254, 1);

    private bool _connected;
    private string _readout = "";

    // Appearance (from config).
    private bool _transparentBg = true;
    private Color _bgColor = Color.FromArgb(28, 28, 30);
    private Color _textColor = Color.White;
    private int _opacityPercent = 82;

    // Move mode.
    private bool _movable;
    private bool _dragging;
    private Point _dragOffset;

    /// <summary>Raised when the user finishes dragging in move mode (new location to persist).</summary>
    public event Action<Point>? Moved;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        Size = new Size(208, 34);
        Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        BackColor = KeyColor;
        TransparencyKey = KeyColor;   // areas painted in KeyColor become click-through transparent
        Reposition();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            if (!_movable) cp.ExStyle |= WS_EX_TRANSPARENT; // click-through unless moving
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    /// <summary>Apply appearance + saved position from config. Safe to call live.</summary>
    public void ApplySettings(AppConfig config)
    {
        _transparentBg = config.OverlayTransparentBg;
        _bgColor = Color.FromArgb(config.OverlayBgColorArgb);
        _textColor = Color.FromArgb(config.OverlayTextColorArgb);
        _opacityPercent = Math.Clamp(config.OverlayOpacity, 10, 100);

        if (config.OverlayX is int x && config.OverlayY is int y)
            Location = ClampToScreen(new Point(x, y));
        else
            Reposition();

        Invalidate();
    }

    /// <summary>
    /// Toggle "move mode". When on, the overlay is no longer click-through and can be dragged; a
    /// border is drawn so the user sees it. When off, click-through is restored.
    /// </summary>
    public void SetMovable(bool movable)
    {
        if (_movable == movable) return;
        _movable = movable;
        // Recreate the window handle so the changed WS_EX_TRANSPARENT style takes effect.
        bool wasVisible = Visible;
        RecreateHandle();
        if (wasVisible && !Visible) Show();
        Invalidate();
    }

    public void SetStatus(bool connected, string readout)
    {
        if (_connected == connected && _readout == readout) return;
        _connected = connected;
        _readout = readout;
        Invalidate();
    }

    public void Reposition()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(area.Right - Width - Inset, area.Top + Inset);
    }

    private Point ClampToScreen(Point p)
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        int x = Math.Clamp(p.X, area.Left, area.Right - Width);
        int y = Math.Clamp(p.Y, area.Top, area.Bottom - Height);
        return new Point(x, y);
    }

    // --- Dragging (only active in move mode) ---
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (_movable && e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _dragOffset = e.Location;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
            Location = ClampToScreen(new Point(Location.X + e.X - _dragOffset.X, Location.Y + e.Y - _dragOffset.Y));
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            Moved?.Invoke(Location); // caller persists OverlayX/Y
        }
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Background: in transparent mode the whole client is the key color (invisible) except a
        // faint border when moving; otherwise a rounded colored panel at the chosen opacity.
        g.Clear(KeyColor);
        if (!_transparentBg)
        {
            using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10);
            int a = (int)Math.Round(_opacityPercent / 100.0 * 255);
            using var bg = new SolidBrush(Color.FromArgb(a, _bgColor.R, _bgColor.G, _bgColor.B));
            g.FillPath(bg, path);
        }

        if (_movable)
        {
            using var border = new Pen(Color.FromArgb(180, 100, 100, 100), 1) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }

        // Status dot.
        var dotColor = _connected ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);
        var dotRect = new Rectangle(12, Height / 2 - 6, 12, 12);
        using (var dot = new SolidBrush(dotColor)) g.FillEllipse(dot, dotRect);
        using (var ring = new Pen(Color.FromArgb(90, dotColor), 2)) g.DrawEllipse(ring, Rectangle.Inflate(dotRect, 2, 2));

        // Label.
        string label = _connected
            ? (string.IsNullOrEmpty(_readout) ? Resources.Strings.Overlay_Connected : _readout)
            : Resources.Strings.Overlay_NoData;
        using var text = new SolidBrush(_textColor);
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
