using SkiaSharp;
using Spaces.Core;

namespace Spaces.Viewer;

/// <summary>
/// Renders a Space and its values onto a SkiaSharp canvas.
///
/// ── LAYOUT ──────────────────────────────────────────────────────────────────
///
/// The canvas is split into two vertical panels:
///
///   Top half (NumberLinePanel):
///     Draws a horizontal number line for the given Space's first axis.
///     Each SpaceValue is rendered as a horizontal bar spanning
///     [−lo/tpu, +hi/tpu] under positive chirality, or [+lo/tpu, −hi/tpu]
///     under negative chirality.
///     Each bar is labelled with its fraction: Measure/Unit (= Hi/Lo in units).
///
///   Bottom half (MultiMeasurePanel):
///     Demonstrates the Proportion container: a single Proportion with
///     multiple measures all sharing the same unit (denominator).
///     Each measure is drawn as a separate bar of different length,
///     all anchored to the same unit baseline.
///
/// ── CHIRALITY ───────────────────────────────────────────────────────────────
///
/// Chirality can be toggled via <see cref="Chirality"/> property.
/// Bars NEVER flip direction: Lo always extends LEFT, Hi always extends RIGHT.
/// Only the label changes:
///   Positive: "(loI+hi)"  — start point first with 'i', parentheses.
///   Negative: "[-loI-hi]" — same values, both negated, square brackets.
/// </summary>
public class SpaceRenderer
{
    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly SKColor Background    = SKColors.White;
    private static readonly SKColor AxisLine      = new SKColor(40,  40,  40);
    private static readonly SKColor TickMark      = new SKColor(120, 120, 120);
    private static readonly SKColor OriginLine    = new SKColor(200,  80,  80);
    private static readonly SKColor LabelText     = new SKColor(50,  50,  50);
    private static readonly SKColor PanelTitle    = new SKColor(80,  80, 180);
    private static readonly SKColor UnitBaseline  = new SKColor(100, 100, 200);

    private static readonly SKColor[] ValueColors =
    {
        new SKColor(52, 152, 219),   // blue
        new SKColor(46, 204, 113),   // green
        new SKColor(231, 76,  60),   // red
        new SKColor(230, 126, 34),   // orange
        new SKColor(155, 89, 182),   // purple
    };

    // ── State ─────────────────────────────────────────────────────────────────
    public Chirality Chirality { get; set; } = Chirality.Positive;

    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Render the full viewer canvas.
    /// </summary>
    public void Render(SKCanvas canvas, int width, int height,
                       Space space, IReadOnlyList<SpaceValue> values,
                       Proportion multiMeasureProp)
    {
        canvas.Clear(Background);

        int halfH = height / 2 - 10;

        // ── Top: Number-line panel ────────────────────────────────────────────
        using var topClip = new SKAutoCanvasRestore(canvas, true);
        canvas.ClipRect(new SKRect(0, 0, width, halfH));
        DrawNumberLinePanel(canvas, width, halfH, space, values);

        // ── Bottom: Multi-measure panel ───────────────────────────────────────
        using var botClip = new SKAutoCanvasRestore(canvas, true);
        canvas.ClipRect(new SKRect(0, halfH + 20, width, height));
        canvas.Translate(0, halfH + 20);
        DrawMultiMeasurePanel(canvas, width, height - halfH - 20, space, multiMeasureProp);

        // ── Divider ───────────────────────────────────────────────────────────
        using var divPaint = new SKPaint { Color = new SKColor(200, 200, 200), StrokeWidth = 1f };
        canvas.DrawLine(20, halfH + 10, width - 20, halfH + 10, divPaint);
    }

    // ── Number-line panel ─────────────────────────────────────────────────────

    private void DrawNumberLinePanel(SKCanvas canvas, int width, int height,
                                     Space space, IReadOnlyList<SpaceValue> values)
    {
        var ax   = space.Axes[0];
        double   maxUnits = ax.ToUnits(ax.MaxP1Tick);   // e.g. 10.0

        float margin   = 60f;
        float lineY    = height * 0.48f;
        float lineXMin = margin;
        float lineXMax = width - margin;
        float lineW    = lineXMax - lineXMin;
        float originX  = lineXMin + lineW * 0.5f;        // centre

        // Pixels per unit
        float ppu = lineW / (float)(maxUnits * 2.0);

        // Title
        DrawTitle(canvas, "Number Line — Space Values", 12f, 14f);

        // Axis line
        using var axPaint = new SKPaint
        {
            Color       = AxisLine,
            StrokeWidth = 1.5f,
            IsAntialias = true,
        };
        canvas.DrawLine(lineXMin, lineY, lineXMax, lineY, axPaint);

        // Tick marks and labels
        DrawAxisTicks(canvas, lineXMin, lineXMax, lineY, originX, ppu, maxUnits);

        // Origin marker
        using var origPaint = new SKPaint { Color = OriginLine, StrokeWidth = 1.5f };
        canvas.DrawLine(originX, lineY - 18f, originX, lineY + 18f, origPaint);

        // Chirality label
        string chiLabel = Chirality == Chirality.Positive
            ? "Positive chirality: Measure/Unit = Hi/Lo"
            : "Negative chirality: Unit/Measure = Lo/Hi  (rotate the paper)";
        DrawSmallLabel(canvas, chiLabel, lineXMin, lineY + 32f, LabelText);

        // Values
        float barH   = 14f;
        float barTopBase = lineY - 30f;
        for (int i = 0; i < values.Count; i++)
        {
            var   v     = values[i];
            var   color = ValueColors[i % ValueColors.Length];
            float yOff  = -(i + 1) * (barH + 6f);   // stack upward

            DrawValueBar(canvas, v, ax, 0, originX, ppu, lineY + yOff, barH, color);
        }
    }

    private void DrawAxisTicks(SKCanvas canvas,
                                float xMin, float xMax, float lineY,
                                float originX, float ppu, double maxUnits)
    {
        using var tickPaint = new SKPaint
        {
            Color       = TickMark,
            StrokeWidth = 1f,
            IsAntialias = false,
        };
        using var lblFont  = new SKFont(SKTypeface.Default, 10f);
        using var lblPaint = new SKPaint { Color = LabelText, IsAntialias = true };

        int tickMax = (int)Math.Floor(maxUnits);
        for (int t = -tickMax; t <= tickMax; t++)
        {
            float x      = originX + t * ppu;
            float tickH  = t == 0 ? 10f : 6f;
            canvas.DrawLine(x, lineY - tickH, x, lineY + tickH, tickPaint);
            if (t != 0)
                canvas.DrawText(t.ToString(), x, lineY + 18f, SKTextAlign.Center, lblFont, lblPaint);
        }
        canvas.DrawText("0", originX, lineY + 18f, SKTextAlign.Center, lblFont, lblPaint);
    }

    private void DrawValueBar(SKCanvas canvas, SpaceValue v, Axis ax, int axisIdx,
                               float originX, float ppu, float barY, float barH, SKColor color)
    {
        double loUnits = ax.ToUnits(v.GetP0(axisIdx));
        double hiUnits = ax.ToUnits(v.GetP1(axisIdx));

        // Bars never flip direction — Lo always extends LEFT from origin,
        // Hi always extends RIGHT.  Only the label changes with chirality.
        float leftPx  = originX - (float)(loUnits * ppu);
        float rightPx = originX + (float)(hiUnits * ppu);

        // Directional bar: rounded left cap, pointed right tip
        DrawDirectionalBar(canvas, leftPx, rightPx, barY, barH, color);

        // Label above the bar
        string label = BuildFractionLabel(loUnits, hiUnits);
        float  midX  = (leftPx + rightPx) * 0.5f;

        using var lblFont  = new SKFont(SKTypeface.Default, 11f);
        using var lblPaint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(label, midX, barY - barH * 0.5f - 3f, SKTextAlign.Center, lblFont, lblPaint);
    }

    /// <summary>
    /// Draws a bar with a rounded left cap and a slightly pointed right tip to show
    /// directionality: start (Lo, left) is soft; end (Hi, right) is an arrowhead.
    /// </summary>
    private static void DrawDirectionalBar(SKCanvas canvas,
                                           float leftPx, float rightPx,
                                           float barY,   float barH, SKColor color)
    {
        float r   = barH * 0.5f;          // radius for rounded left cap
        float tip = barH * 0.35f;         // point extension beyond rightPx
        float top = barY - r;
        float bot = barY + r;

        // Build the path clockwise: top-left → top-right → tip → bottom-right → bottom-left → arc back
        using var path = new SKPath();
        path.MoveTo(leftPx, top);
        path.LineTo(rightPx, top);
        path.LineTo(rightPx + tip, barY);   // pointed tip
        path.LineTo(rightPx, bot);
        path.LineTo(leftPx, bot);
        // Arc from (leftPx, bot) counterclockwise to (leftPx, top) — the rounded left cap
        path.ArcTo(new SKRect(leftPx - r, top, leftPx + r, bot), 90f, -180f, false);
        path.Close();

        using var fill = new SKPaint { Color = color.WithAlpha(80), IsAntialias = true };
        canvas.DrawPath(path, fill);

        using var stroke = new SKPaint
        {
            Color       = color,
            StrokeWidth = 1.5f,
            IsStroke    = true,
            IsAntialias = true,
        };
        canvas.DrawPath(path, stroke);
    }

    private string BuildFractionLabel(double loUnits, double hiUnits)
    {
        string lo = FormatUnit(loUnits);
        string hi = FormatUnit(hiUnits);
        return Chirality == Chirality.Positive
            ? $"({lo}i+{hi})"    // positive chirality: start first with 'i'
            : $"[-{lo}i-{hi}]";  // negative chirality: square brackets, both negated
    }

    private static string FormatUnit(double v) =>
        v == Math.Floor(v) ? ((int)v).ToString() : v.ToString("G4");

    // ── Multi-measure panel ───────────────────────────────────────────────────

    private void DrawMultiMeasurePanel(SKCanvas canvas, int width, int height,
                                       Space space, Proportion prop)
    {
        var ax       = space.Axes[0];
        double maxUnits = ax.ToUnits(ax.MaxP1Tick);

        float margin  = 60f;
        float lineW   = width - margin * 2f;
        float originX = margin + lineW * 0.5f;
        float ppu     = lineW / (float)(maxUnits * 2.0);

        DrawTitle(canvas, "Multiple Measures — one Unit (denominator), many Numerators", 12f, 14f);

        // Unit denominator value (P0 = unit/denominator in positive chirality)
        double unitVal = ax.ToUnits(prop.GetUnit(Chirality));

        // Baseline
        float baseY = height * 0.35f;
        using var basePaint = new SKPaint { Color = UnitBaseline, StrokeWidth = 2f };
        float baseLeft  = originX - (float)(unitVal * ppu);
        float baseRight = originX;
        canvas.DrawLine(baseLeft, baseY, baseRight, baseY, basePaint);

        using var lblFont  = new SKFont(SKTypeface.Default, 11f);
        using var lblPaint = new SKPaint { Color = UnitBaseline, IsAntialias = true };
        canvas.DrawText($"Unit = {FormatUnit(unitVal)}", baseLeft - 4f, baseY,
                        SKTextAlign.Right, lblFont, lblPaint);

        // Measures
        var measures = prop.Measures;
        float barH   = 13f;

        for (int m = 0; m < measures.Count; m++)
        {
            double measureVal = ax.ToUnits(measures[m]);
            float  barY       = baseY + (m + 1) * (barH + 8f);
            var    color      = ValueColors[m % ValueColors.Length];

            // Bar from origin to +measureVal (numerator bars always go right in this panel)
            float barRight = originX + (float)(measureVal * ppu);
            float barLeft  = originX;

            using var fill = new SKPaint { Color = color.WithAlpha(80), IsAntialias = true };
            canvas.DrawRect(barLeft, barY - barH * 0.5f, barRight - barLeft, barH, fill);

            using var stroke = new SKPaint { Color = color, StrokeWidth = 1.5f, IsStroke = true, IsAntialias = true };
            canvas.DrawRect(barLeft, barY - barH * 0.5f, barRight - barLeft, barH, stroke);

            string label = m == 0
                ? $"Measure[0] = {FormatUnit(measureVal)}  →  {FormatUnit(measureVal)}/{FormatUnit(unitVal)}"
                : $"Measure[{m}] = {FormatUnit(measureVal)}  →  {FormatUnit(measureVal)}/{FormatUnit(unitVal)}";

            using var mLblFont  = new SKFont(SKTypeface.Default, 11f);
            using var mLblPaint = new SKPaint { Color = color, IsAntialias = true };
            canvas.DrawText(label, barRight + 6f, barY + 4f, SKTextAlign.Left, mLblFont, mLblPaint);
        }

        // Caption
        using var capFont  = new SKFont(SKTypeface.Default, 10f);
        using var capPaint = new SKPaint { Color = LabelText, IsAntialias = true };
        canvas.DrawText(
            "All measures share the same Unit denominator — like fractions with a common denominator",
            margin, height - 12f, SKTextAlign.Left, capFont, capPaint);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DrawTitle(SKCanvas canvas, string text, float x, float y)
    {
        using var font  = new SKFont(SKTypeface.Default, 12f) { Embolden = true };
        using var paint = new SKPaint { Color = PanelTitle, IsAntialias = true };
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private static void DrawSmallLabel(SKCanvas canvas, string text, float x, float y, SKColor color)
    {
        using var font  = new SKFont(SKTypeface.Default, 10f);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }
}


