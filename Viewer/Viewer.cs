using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Viewer.Renderer;
using Viewer.Views;

namespace Viewer;

public partial class Viewer : Form
{
    private readonly CoreRenderer _renderer;
    private readonly SKControl _control;
    public Viewer()
    {
        InitializeComponent();
        DoubleBuffered = true;
        KeyPreview = true;

        _renderer = CoreRenderer.Instance;

        _control = new SKControl();
        _control.Width = Width;
        _control.Height = Height;
        _control.Dock = DockStyle.Fill;
        _renderer.Width = Width;
        _renderer.Height = Height;
        corePanel.Controls.Add(_control);

        _control.PaintSurface += DrawOnPaintSurface;
        _control.MouseClick   += OnControlMouseClick;
        corePanel.Resize += OnResize;

        _renderer.Page = new Page();
        Bounds = new Rectangle(0, 0, 1024, 768);
    }
    private void OnResize(object? sender, EventArgs e)
    {
        _renderer.Width = corePanel.Width;
        _renderer.Height = corePanel.Height;
    }
    private void DrawOnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        _renderer.DrawOnCanvas(e.Surface.Canvas);
    }
    private void NeedsUpdate()
    {
        _control.Invalidate();
    }

    // ── Keyboard navigation ────────────────────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_renderer.Page is Page page)
        {
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down)
            {
                page.NextPage();
                _control.Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up)
            {
                page.PreviousPage();
                _control.Invalidate();
                e.Handled = true;
            }
        }
    }

    // ── Mouse navigation ───────────────────────────────────────────────

    private void OnControlMouseClick(object? sender, MouseEventArgs e)
    {
        if (_renderer.Page is Page page)
        {
            var pt = new SKPoint(e.X, e.Y);
            if (page.NextBounds.Contains(pt))
            {
                page.NextPage();
                _control.Invalidate();
            }
            else if (page.PrevBounds.Contains(pt))
            {
                page.PreviousPage();
                _control.Invalidate();
            }
        }
    }
}
