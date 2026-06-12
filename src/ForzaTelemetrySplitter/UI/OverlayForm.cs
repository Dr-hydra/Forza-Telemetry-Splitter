using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ForzaTelemetrySplitter.Config;

namespace ForzaTelemetrySplitter.UI;

/// <summary>
/// A tiny always-on-top status pill rendered as a per-pixel ARGB layered window
/// (<c>UpdateLayeredWindow</c>). This gives smooth anti-aliased edges with NO fringe on any background
/// — unlike a TransparencyKey window, which hard-knocks-out a key color and halos the text/dot.
///
/// Appearance is user-configurable (Overlay tab): transparent background (dot + text only) or a
/// translucent colored panel, plus text color and a saved position. Normally topmost, non-activating
/// and click-through so it draws over borderless Forza without stealing focus; "move mode" temporarily
/// enables mouse input so the user can drag it.
/// </summary>
public sealed class OverlayForm : Form
{
    private const int WS_EX_TOPMOST     = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW  = 0x00000080;
    private const int WS_EX_LAYERED     = 0x00080000;
    private const int WS_EX_NOACTIVATE  = 0x08000000;

    private const int Inset = 12;

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
        Size = new Size(208, 34);
        Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        Reposition();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
            if (!_movable) cp.ExStyle |= WS_EX_TRANSPARENT; // click-through unless moving
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Render();
    }

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

        Render();
    }

    public void SetMovable(bool movable)
    {
        if (_movable == movable) return;
        _movable = movable;
        bool wasVisible = Visible;
        RecreateHandle();          // apply the changed WS_EX_TRANSPARENT style
        if (wasVisible && !Visible) Show();
        Render();
    }

    public void SetStatus(bool connected, string readout)
    {
        if (_connected == connected && _readout == readout) return;
        _connected = connected;
        _readout = readout;
        Render();
    }

    public void Reposition()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(area.Right - Width - Inset, area.Top + Inset);
    }

    private Point ClampToScreen(Point p)
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        return new Point(Math.Clamp(p.X, area.Left, area.Right - Width),
                         Math.Clamp(p.Y, area.Top, area.Bottom - Height));
    }

    // --- Dragging (only in move mode) ---
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (_movable && e.Button == MouseButtons.Left) { _dragging = true; _dragOffset = e.Location; }
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
        if (_dragging) { _dragging = false; Moved?.Invoke(Location); }
        base.OnMouseUp(e);
    }

    /// <summary>Render the pill into an ARGB bitmap and push it to the layered window.</summary>
    private void Render()
    {
        if (!IsHandleCreated) return;

        using var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            if (!_transparentBg)
            {
                using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10);
                int a = (int)Math.Round(_opacityPercent / 100.0 * 255);
                using var bg = new SolidBrush(Color.FromArgb(a, _bgColor.R, _bgColor.G, _bgColor.B));
                g.FillPath(bg, path);
            }

            if (_movable)
            {
                using var border = new Pen(Color.FromArgb(200, 120, 120, 120), 1) { DashStyle = DashStyle.Dash };
                g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }

            var dotColor = _connected ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);
            var dotRect = new Rectangle(12, Height / 2 - 6, 12, 12);
            using (var dot = new SolidBrush(dotColor)) g.FillEllipse(dot, dotRect);
            using (var ring = new Pen(Color.FromArgb(90, dotColor), 2)) g.DrawEllipse(ring, Rectangle.Inflate(dotRect, 2, 2));

            string label = _connected
                ? (string.IsNullOrEmpty(_readout) ? Resources.Strings.Overlay_Connected : _readout)
                : Resources.Strings.Overlay_NoData;
            using var text = new SolidBrush(_textColor);
            var textRect = new Rectangle(32, 0, Width - 36, Height);
            var fmt = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
            g.DrawString(label, Font, text, textRect, fmt);
        }

        PushLayered(bmp);
    }

    /// <summary>Blit a 32-bpp ARGB bitmap to the window with per-pixel alpha.</summary>
    private void PushLayered(Bitmap bmp)
    {
        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memDc = CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero, oldBitmap = IntPtr.Zero;
        try
        {
            hBitmap = bmp.GetHbitmap(Color.FromArgb(0)); // premultiplied alpha
            oldBitmap = SelectObject(memDc, hBitmap);

            var size = new SIZE(bmp.Width, bmp.Height);
            var src = new POINT(0, 0);
            var dst = new POINT(Left, Top);
            var blend = new BLENDFUNCTION
            {
                BlendOp = 0,            // AC_SRC_OVER
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = 1,        // AC_SRC_ALPHA
            };
            UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, 2 /*ULW_ALPHA*/);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, screenDc);
            if (hBitmap != IntPtr.Zero) { SelectObject(memDc, oldBitmap); DeleteObject(hBitmap); }
            DeleteDC(memDc);
        }
    }

    // Re-blit when the window moves (layered windows don't auto-track Left/Top for content).
    protected override void OnMove(EventArgs e) { base.OnMove(e); Render(); }

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

    // --- P/Invoke for layered window ---
    [StructLayout(LayoutKind.Sequential)] private struct SIZE { public int cx, cy; public SIZE(int x, int y){cx=x;cy=y;} }
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x, y; public POINT(int a, int b){x=a;y=b;} }
    [StructLayout(LayoutKind.Sequential)] private struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
        IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
}
