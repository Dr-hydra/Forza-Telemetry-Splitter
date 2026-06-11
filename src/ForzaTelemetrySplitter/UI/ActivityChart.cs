using System.Drawing.Drawing2D;
using ForzaTelemetrySplitter.Core;
using ForzaTelemetrySplitter.Resources;

namespace ForzaTelemetrySplitter.UI;

/// <summary>
/// A hand-drawn (GDI+) rolling packets/sec chart for the Activity tab. Reads from the engine's
/// in-memory <see cref="ActivityHistory"/>. The view window defaults to the last 15 minutes and can
/// be zoomed between 1 minute and 1 hour via the +/- buttons or the mouse wheel; it always shows the
/// most recent data on the right ("now").
///
/// No charting dependency - same GDI+ approach as OverlayForm. Repaint is driven externally (the
/// MainForm timer calls Invalidate only while this tab is visible), so it costs nothing when hidden.
/// </summary>
public sealed class ActivityChart : Control
{
    private const int MinView = 60;        // 1 minute
    private const int MaxView = ActivityHistory.OneHourSeconds; // 1 hour
    private const int DefaultView = 900;   // 15 minutes

    private readonly ActivityHistory _history;
    private readonly float[] _snap;        // reused snapshot buffer (oldest to newest)
    private readonly PointF[] _pts;        // reused polyline buffer

    private int _viewSeconds = DefaultView;
    private bool _running;

    // Cached GDI+ objects (created once, disposed in Dispose).
    private readonly Pen _gridPen = new(Color.FromArgb(40, 255, 255, 255));
    private readonly Pen _linePen = new(Color.FromArgb(46, 204, 113), 1.6f);
    private readonly Font _titleFont = new("Segoe UI", 9f, FontStyle.Bold);
    private readonly Font _axisFont = new("Segoe UI", 8f);
    private readonly Font _bigFont = new("Segoe UI", 20f, FontStyle.Bold);
    private readonly SolidBrush _textBrush = new(Color.Gainsboro);
    private readonly SolidBrush _dimBrush = new(Color.Gray);

    public ActivityChart(ActivityHistory history)
    {
        _history = history;
        _snap = new float[history.Capacity];
        _pts = new PointF[history.Capacity];

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Color.FromArgb(24, 24, 26);
    }

    /// <summary>The current view window in seconds (for the title/labels).</summary>
    public int ViewSeconds => _viewSeconds;

    /// <summary>Tell the chart whether the engine is running (affects the empty/waiting message).</summary>
    public void SetRunning(bool running)
    {
        if (_running != running) { _running = running; Invalidate(); }
    }

    public void ZoomIn() => SetView((int)Math.Round(_viewSeconds / 1.5));
    public void ZoomOut() => SetView((int)Math.Round(_viewSeconds * 1.5));

    private void SetView(int seconds)
    {
        int v = Math.Clamp(seconds, MinView, MaxView);
        if (v != _viewSeconds) { _viewSeconds = v; Invalidate(); }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Delta > 0) ZoomIn(); else ZoomOut(); // wheel up = zoom in (fewer seconds)
    }

    private int Dpi => DeviceDpi <= 0 ? 96 : DeviceDpi;
    private float DpiScale => Dpi / 96f;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        int n = _history.Snapshot(_snap);

        float pad = 8 * DpiScale;
        float leftPad = 38 * DpiScale;
        float bottomPad = 18 * DpiScale;
        float topPad = 22 * DpiScale;
        var plot = new RectangleF(leftPad, topPad,
            Math.Max(1, Width - leftPad - pad), Math.Max(1, Height - topPad - bottomPad));

        // Title.
        g.DrawString(Strings.Chart_Title(_viewSeconds / 60), _titleFont, _textBrush, pad, pad * 0.5f);

        // Determine the visible slice: the last _viewSeconds samples (1 sample/sec).
        int visible = Math.Min(_viewSeconds, n);

        // Empty / waiting states.
        if (n == 0 || visible == 0)
        {
            string msg = _running ? Strings.Chart_Waiting : Strings.Chart_Empty;
            DrawCentered(g, msg, plot);
            return;
        }

        int startIdx = n - visible;

        // Auto-scale Y (min anchored at 0). Ignore NaN gaps.
        float yMax = 1f;
        for (int i = startIdx; i < n; i++)
            if (!float.IsNaN(_snap[i]) && _snap[i] > yMax) yMax = _snap[i];
        yMax = NiceCeil(yMax);

        // Gridlines + Y labels (4 steps).
        for (int s = 0; s <= 4; s++)
        {
            float frac = s / 4f;
            float y = plot.Bottom - frac * plot.Height;
            g.DrawLine(_gridPen, plot.Left, y, plot.Right, y);
            g.DrawString(((int)(yMax * frac)).ToString(), _axisFont, _dimBrush, 2 * DpiScale, y - 7 * DpiScale);
        }

        // X labels: "-Nm" .. "now".
        g.DrawString($"-{_viewSeconds / 60}m", _axisFont, _dimBrush, plot.Left, plot.Bottom + 2 * DpiScale);
        string nowLabel = "now";
        var nowSize = g.MeasureString(nowLabel, _axisFont);
        g.DrawString(nowLabel, _axisFont, _dimBrush, plot.Right - nowSize.Width, plot.Bottom + 2 * DpiScale);

        float xStep = visible > 1 ? plot.Width / (visible - 1) : plot.Width;

        // Build runs split at NaN gaps; fill area + draw line per run.
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int run = 0;
        float lastVal = 0;
        for (int i = 0; i < visible; i++)
        {
            float v = _snap[startIdx + i];
            if (float.IsNaN(v))
            {
                FlushRun(g, plot, run, yMax);
                run = 0;
                continue;
            }
            float x = plot.Left + i * xStep;
            float y = plot.Bottom - (v / yMax) * plot.Height;
            _pts[run++] = new PointF(x, y);
            lastVal = v;
        }
        FlushRun(g, plot, run, yMax);
        g.SmoothingMode = SmoothingMode.Default;

        // Big current-rate readout (top-right).
        string big = $"{(int)lastVal}";
        var bigSize = g.MeasureString(big, _bigFont);
        g.DrawString(big, _bigFont, _textBrush, plot.Right - bigSize.Width, topPad);
        g.DrawString(Strings.Chart_Unit, _axisFont, _dimBrush,
            plot.Right - bigSize.Width, topPad + bigSize.Height - 6 * DpiScale);
    }

    /// <summary>Draw one contiguous run of points: gradient area fill then the line.</summary>
    private void FlushRun(Graphics g, RectangleF plot, int count, float yMax)
    {
        if (count < 1) return;
        if (count == 1)
        {
            g.FillEllipse(_linePen.Brush, _pts[0].X - 1, _pts[0].Y - 1, 2, 2);
            return;
        }

        using var path = new GraphicsPath();
        path.AddLines(_pts.AsSpan(0, count).ToArray());
        // Close to baseline for the fill.
        using var fill = new GraphicsPath();
        fill.AddLines(_pts.AsSpan(0, count).ToArray());
        fill.AddLine(_pts[count - 1].X, _pts[count - 1].Y, _pts[count - 1].X, plot.Bottom);
        fill.AddLine(_pts[count - 1].X, plot.Bottom, _pts[0].X, plot.Bottom);
        fill.CloseFigure();
        using var grad = new LinearGradientBrush(
            new RectangleF(plot.Left, plot.Top, plot.Width, plot.Height),
            Color.FromArgb(80, 46, 204, 113), Color.FromArgb(8, 46, 204, 113), LinearGradientMode.Vertical);
        g.FillPath(grad, fill);
        g.DrawLines(_linePen, _pts.AsSpan(0, count).ToArray());
    }

    private void DrawCentered(Graphics g, string text, RectangleF area)
    {
        using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, _axisFont, _dimBrush, area, fmt);
    }

    private static float NiceCeil(float v)
    {
        if (v <= 10) return 10;
        if (v <= 30) return 30;
        if (v <= 60) return 60;
        if (v <= 120) return 120;
        if (v <= 250) return 250;
        return (float)(Math.Ceiling(v / 100.0) * 100);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gridPen.Dispose(); _linePen.Dispose();
            _titleFont.Dispose(); _axisFont.Dispose(); _bigFont.Dispose();
            _textBrush.Dispose(); _dimBrush.Dispose();
        }
        base.Dispose(disposing);
    }
}
