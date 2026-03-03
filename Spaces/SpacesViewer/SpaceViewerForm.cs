using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Spaces.Core;

namespace Spaces.Viewer;

/// <summary>
/// Main WinForms window for the Spaces visualizer.
///
/// ── DEMO CONTENT ────────────────────────────────────────────────────────────
///
/// Shows a pre-configured Space.Segment with several SpaceValues and a
/// Proportion with multiple measures to illustrate the container behavior.
///
///   Demo space:    Segment(rangeMax=10.0, tpu=100)
///   Demo values:   (1,5), (2,4), (0,7), (3,3)  — four proportions
///   Multi-measure: Proportion(unit=2*tpu, measure=8*tpu)
///                    .WithMeasure(5*tpu)
///                    .WithMeasure(3*tpu)
///
/// ── CONTROLS ────────────────────────────────────────────────────────────────
///
///   Flip Chirality button — toggles between Positive and Negative chirality.
///   When Negative the number line renders from the opposite perspective:
///   what was the left extent (Unit) becomes right, and vice versa.
///   Fraction labels invert: Measure/Unit ↔ Unit/Measure.
/// </summary>
public class SpaceViewerForm : Form
{
    private readonly SKGLControl _canvas;
    private readonly Button      _flipButton;
    private readonly Label       _chiralityLabel;

    private readonly SpaceRenderer _renderer = new();

    // ── Demo data ─────────────────────────────────────────────────────────────
    private readonly Space              _space;
    private readonly List<SpaceValue>   _values = new();
    private readonly Proportion         _multiProp;

    public SpaceViewerForm()
    {
        Text            = "Spaces Viewer — Proportions & Chirality";
        ClientSize      = new Size(900, 560);
        MinimumSize     = new Size(700, 400);
        BackColor       = Color.White;
        Font            = new Font("Segoe UI", 9f);

        // ── Build demo space ──────────────────────────────────────────────────
        _space = Space.Segment(10.0, 100L, "demo");

        // Four values: (lo, hi) in units
        foreach (var (lo, hi) in new[] { (1.0, 5.0), (2.0, 4.0), (0.0, 7.0), (3.0, 3.0) })
            _values.Add(_space.Value(lo, hi));

        // Multi-measure proportion: unit=2, measures=[8, 5, 3]  all in ticks
        // P0=200 (unit/denominator in +chirality), P1=800 (primary measure/numerator)
        _multiProp = new Proportion(200L, 800L)
            .WithMeasure(500L)   // 5 units
            .WithMeasure(300L);  // 3 units

        // ── Controls ──────────────────────────────────────────────────────────
        _flipButton = new Button
        {
            Text     = "Flip Chirality",
            Location = new Point(12, 8),
            Size     = new Size(120, 28),
            FlatStyle = FlatStyle.System,
        };
        _flipButton.Click += (_, _) => FlipChirality();

        _chiralityLabel = new Label
        {
            Text      = "Positive chirality",
            Location  = new Point(140, 14),
            AutoSize  = true,
            ForeColor = Color.FromArgb(52, 152, 219),
        };

        _canvas = new SKGLControl
        {
            Dock = DockStyle.Bottom,
        };
        _canvas.PaintSurface += OnPaintSurface;

        Controls.Add(_flipButton);
        Controls.Add(_chiralityLabel);
        Controls.Add(_canvas);

        // Resize to fill below toolbar
        Resize += (_, _) => _canvas.Size = new Size(ClientSize.Width, ClientSize.Height - 44);
        _canvas.Size = new Size(ClientSize.Width, ClientSize.Height - 44);
    }

    private void FlipChirality()
    {
        _renderer.Chirality = _renderer.Chirality == Chirality.Positive
            ? Chirality.Negative
            : Chirality.Positive;

        _chiralityLabel.Text = _renderer.Chirality == Chirality.Positive
            ? "Positive chirality"
            : "Negative chirality";

        _canvas.Invalidate();
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        _renderer.Render(
            e.Surface.Canvas,
            e.BackendRenderTarget.Width,
            e.BackendRenderTarget.Height,
            _space,
            _values,
            _multiProp);
    }
}
