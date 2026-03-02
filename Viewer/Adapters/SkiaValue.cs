using Complexity.Core;
using SkiaSharp;
using Viewer.Renderer;

namespace Viewer.Adapters;

/// <summary>
/// Renders a CValue onto a CoordinateMap as a filled dot, with an optional vector
/// line from the origin and an optional text label.
/// </summary>
public class SkiaValue : ISkiaElement
{
    public CValue Value { get; }
    public CoordinateMap Map { get; }
    public ElementStyle Style { get; }

    public SkiaValue(CValue value, CoordinateMap map, ElementStyle? style = null)
    {
        Value = value;
        Map = map;
        Style = style ?? ElementStyle.Default;
    }

    public void Render(SKCanvas canvas, CorePens pens)
    {
        var pt = Map.ToScreen(Value);
        var pen = Style.PointPen ?? pens.SegPen0;

        // Optional: line from coordinate origin to the point.
        if (Style.ShowVector)
            canvas.DrawLine(Map.Origin.X, Map.Origin.Y, pt.X, pt.Y, pen);

        // Filled square.
        float r = Style.PointRadius;
        var rect = new SKRect(pt.X - r, pt.Y - r, pt.X + r, pt.Y + r);
        using var fill = CorePens.GetBrush(pen.Color);
        canvas.DrawRect(rect, fill);

        // White outline for contrast against the grid.
        using var ring = CorePens.GetPen(new SKColor(255, 255, 255, 200), 1.5f);
        canvas.DrawRect(rect, ring);

        // Optional label.
        if (Style.ShowLabel && Style.Label != null)
        {
            using var labelFont = CorePens.GetFont(12f, "Arial");
            using var labelPaint = CorePens.GetBrush(pen.Color);
            canvas.DrawText(Style.Label,
                pt.X + Style.PointRadius + 3f, pt.Y + 4f,
                SKTextAlign.Left, labelFont, labelPaint);
        }
    }
}
