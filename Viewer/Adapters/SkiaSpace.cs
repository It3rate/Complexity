using SkiaSharp;
using Viewer.Renderer;

namespace Viewer.Adapters;

/// <summary>
/// Renders a CSpace as a visual coordinate system: axes, grid lines, tick labels, and
/// axis name annotations. Dispatches to a number-line or plane layout based on Dims.
/// </summary>
public class SkiaSpace : ISkiaElement
{
    public CoordinateMap Map { get; }

    public SkiaSpace(CoordinateMap map)
    {
        Map = map;
    }

    public void Render(SKCanvas canvas, CorePens pens)
    {
        if (Map.Space.Dims == 1)
            RenderNumberLine(canvas, pens);
        else
            RenderPlane(canvas, pens);
    }

    // ================================================================
    //  2D plane rendering
    // ================================================================

    private void RenderPlane(SKCanvas canvas, CorePens pens)
    {
        var map = Map;
        var space = map.Space;
        var bounds = map.ScreenBounds;

        // Subtle bounding box.
        canvas.DrawRect(bounds, pens.GrayPen);

        // Shared font for tick labels (matches LineTextPen's original size).
        using var tickFont = new SKFont(SKTypeface.Default, 12f);

        // Vertical grid lines — constant values on the X-mapped (unit/real) axis.
        double xRange = map.DataMaxX - map.DataMinX;
        double xStep = NiceStep(xRange);
        double xVal = Math.Ceiling(map.DataMinX / xStep) * xStep;

        while (xVal <= map.DataMaxX + xStep * 0.001)
        {
            float sx = map.ToScreenX(xVal);
            // When xVal == 0, this vertical line is the unot (imaginary) axis.
            bool isImagAxis = Math.Abs(xVal) < xStep * 0.01;
            canvas.DrawLine(sx, bounds.Top, sx, bounds.Bottom,
                isImagAxis ? pens.UnotPen : pens.GrayPen);

            if (!isImagAxis)
                DrawXTickLabel(canvas, pens, map, xVal, xStep, tickFont);

            xVal += xStep;
        }

        // Horizontal grid lines — constant values on the Y-mapped (unot/imaginary) axis.
        double yRange = map.DataMaxY - map.DataMinY;
        double yStep = NiceStep(yRange);
        double yVal = Math.Ceiling(map.DataMinY / yStep) * yStep;

        while (yVal <= map.DataMaxY + yStep * 0.001)
        {
            float sy = map.ToScreenY(yVal);
            // When yVal == 0, this horizontal line is the unit (real) axis.
            bool isRealAxis = Math.Abs(yVal) < yStep * 0.01;
            canvas.DrawLine(bounds.Left, sy, bounds.Right, sy,
                isRealAxis ? pens.UnitPen : pens.GrayPen);

            if (!isRealAxis)
                DrawYTickLabel(canvas, pens, map, yVal, yStep, tickFont);

            yVal += yStep;
        }

        // Small dot at origin.
        using var originBrush = CorePens.GetBrush(pens.MarkerColor);
        canvas.DrawCircle(map.Origin, 3f, originBrush);

        // Axis name labels.
        float realAxisY = Math.Clamp(map.ToScreenY(0), bounds.Top + 16, bounds.Bottom - 5);
        float imagAxisX = Math.Clamp(map.ToScreenX(0), bounds.Left + 22, bounds.Right - 40);

        using var markerFont = CorePens.GetFont(12f, "Arial");

        // Unit (real) axis label at the right end of the horizontal line.
        canvas.DrawText(space.Axes[map.XAxisIndex].Name,
            bounds.Right - 20, realAxisY - 8,
            SKTextAlign.Center, markerFont, pens.UnitMarkerText);

        // Unot (imaginary) axis label at the top of the vertical line.
        canvas.DrawText(space.Axes[map.YAxisIndex].Name,
            imagAxisX + 7, bounds.Top + 14,
            SKTextAlign.Center, markerFont, pens.UnotMarkerText);

        // Space name annotation at top-left.
        using var nameFont  = CorePens.GetFont(11f, "Arial");
        using var namePaint = CorePens.GetBrush(pens.LineTextPen.Color);
        canvas.DrawText(space.Name,
            bounds.Left + 6, bounds.Top + 13,
            SKTextAlign.Left, nameFont, namePaint);
    }

    private static void DrawXTickLabel(SKCanvas canvas, CorePens pens, CoordinateMap map,
        double xVal, double step, SKFont font)
    {
        float sx = map.ToScreenX(xVal);
        float originY = map.ToScreenY(0);
        float labelY = Math.Clamp(originY + 16, map.ScreenBounds.Top + 16, map.ScreenBounds.Bottom - 2);
        // Centre the label on the grid line.
        canvas.DrawText(FormatTick(xVal, step), sx, labelY,
            SKTextAlign.Center, font, pens.LineTextPen);
    }

    private static void DrawYTickLabel(SKCanvas canvas, CorePens pens, CoordinateMap map,
        double yVal, double step, SKFont font)
    {
        float sy = map.ToScreenY(yVal);
        float originX = map.ToScreenX(0);
        string label = FormatTick(yVal, step);
        // Measure with the font so we can keep the right edge clear of the axis.
        float textWidth = font.MeasureText(label);
        float rightX = Math.Clamp(originX - 8,
            map.ScreenBounds.Left + textWidth + 4,
            map.ScreenBounds.Right - 30);
        // Right-align the label just left of the imaginary axis.
        canvas.DrawText(label, rightX, sy + 4,
            SKTextAlign.Right, font, pens.LineTextPen);
    }

    // ================================================================
    //  1D number-line rendering
    // ================================================================

    private void RenderNumberLine(SKCanvas canvas, CorePens pens)
    {
        var map = Map;
        var space = map.Space;
        var bounds = map.ScreenBounds;
        float lineY = (bounds.Top + bounds.Bottom) / 2f;

        canvas.DrawLine(bounds.Left, lineY, bounds.Right, lineY, pens.NumberLinePen);

        double range = map.DataMaxX - map.DataMinX;
        double step = NiceStep(range);
        double xVal = Math.Ceiling(map.DataMinX / step) * step;

        using var tickFont = new SKFont(SKTypeface.Default, 12f);

        while (xVal <= map.DataMaxX + step * 0.001)
        {
            float sx = map.ToScreenX(xVal);
            bool isOrigin = Math.Abs(xVal) < step * 0.01;
            float tickH = isOrigin ? 12f : 6f;

            canvas.DrawLine(sx, lineY - tickH, sx, lineY + tickH,
                isOrigin ? pens.TickBoldPen : pens.TickPen);

            canvas.DrawText(FormatTick(xVal, step), sx, lineY + tickH + 14,
                SKTextAlign.Center, tickFont, pens.LineTextPen);

            xVal += step;
        }

        using var markerFont = CorePens.GetFont(12f, "Arial");
        canvas.DrawText(space.Axes[map.XAxisIndex].Name,
            bounds.Right - 5, lineY - 8,
            SKTextAlign.Center, markerFont, pens.UnitMarkerText);
    }

    // ================================================================
    //  Helpers
    // ================================================================

    /// <summary>Pick a round grid step that yields roughly 5–8 divisions across range.</summary>
    private static double NiceStep(double range)
    {
        if (range <= 0) return 1.0;
        double rawStep = range / 6.0;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        double normalized = rawStep / mag;

        if (normalized <= 1.0) return 1.0 * mag;
        if (normalized <= 2.0) return 2.0 * mag;
        if (normalized <= 5.0) return 5.0 * mag;
        return 10.0 * mag;
    }

    private static string FormatTick(double value, double step)
    {
        if (Math.Abs(step) >= 10) return ((int)Math.Round(value)).ToString();
        if (Math.Abs(step) >= 1)  return value.ToString("0");
        if (Math.Abs(step) >= 0.1) return value.ToString("0.0");
        return value.ToString("G3");
    }
}
