using Complexity.Core;
using SkiaSharp;
using Viewer.Renderer;

namespace Viewer.Adapters;

/// <summary>
/// Renders a CValue from a 2D segment/complex space as a horizontal bar on a 1D number line.
/// The bar spans from unot (imaginary/start) to unit (real/end) on the X axis.
/// Small vertical endcaps mark the boundaries.
/// </summary>
public class SkiaSegment : ISkiaElement
{
    public CValue Value { get; }
    public CoordinateMap Map { get; }
    public ElementStyle Style { get; }

    public SkiaSegment(CValue value, CoordinateMap map, ElementStyle? style = null)
    {
        Value = value;
        Map = map;
        Style = style ?? ElementStyle.Default;
    }

    public void Render(SKCanvas canvas, CorePens pens)
    {
        var pen = Style.PointPen ?? pens.SegPen0;
        float thickness = Style.BarThickness;

        // For segment/complex values, component[0]=unot (direction=-1), component[1]=unit (direction=+1).
        double startVal = Value.UnotValue;
        double endVal = Value.UnitValue;

        float sx = Map.ToScreenX(startVal);
        float ex = Map.ToScreenX(endVal);
        float y = Style.BarY ?? Map.ToScreenY(0);

        // Thick horizontal bar.
        using var barPen = CorePens.GetPen(pen.Color, thickness, SKStrokeCap.Butt);
        canvas.DrawLine(sx, y, ex, y, barPen);

        // Vertical endcaps.
        float capH = thickness * 1.2f;
        using var capPen = CorePens.GetPen(pen.Color, 2f);
        canvas.DrawLine(sx, y - capH, sx, y + capH, capPen);
        canvas.DrawLine(ex, y - capH, ex, y + capH, capPen);

        // Optional label.
        if (Style.ShowLabel && Style.Label != null)
        {
            float midX = (sx + ex) / 2f;
            using var labelFont = CorePens.GetFont(12f, "Arial");
            using var labelPaint = CorePens.GetBrush(pen.Color);
            canvas.DrawText(Style.Label,
                midX, y - capH - 4f,
                SKTextAlign.Center, labelFont, labelPaint);
        }
    }
}
