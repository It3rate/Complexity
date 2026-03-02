using Complexity.Core;
using SkiaSharp;

namespace Viewer.Adapters;

/// <summary>
/// Maps between Core value-space (CValue double components) and Skia screen-space (pixels).
///
/// Convention for 2D spaces:
///   - The first direction=+1 axis (unit/real) maps to screen X (left → right).
///   - The first direction=-1 axis (unot/imaginary) maps to screen Y (up → down, inverted).
///
/// For 1D spaces the single axis maps to screen X, with screen Y fixed at the center.
///
/// Axis bounds are read from the CSpace. If an axis is unbounded (or its bounds exceed
/// MaxReasonableRange), the viewRange parameter is used as the half-extent instead.
/// </summary>
public class CoordinateMap
{
    private const double MaxReasonableRange = 1e6;

    public CSpace Space { get; }
    public SKRect ScreenBounds { get; }

    /// <summary>Index into Space.Axes that maps to screen X.</summary>
    public int XAxisIndex { get; }

    /// <summary>Index into Space.Axes that maps to screen Y (meaningful only for Dims >= 2).</summary>
    public int YAxisIndex { get; }

    public double DataMinX { get; }
    public double DataMaxX { get; }
    public double DataMinY { get; }
    public double DataMaxY { get; }

    public CoordinateMap(CSpace space, SKRect screenBounds, double viewRange = 5.0)
    {
        Space = space;
        ScreenBounds = screenBounds;

        // Assign axes to screen directions.
        // Convention: direction=+1 (unit/real) → X, direction=-1 (unot/imaginary) → Y.
        int xIdx = -1, yIdx = -1;
        for (int i = 0; i < space.Dims; i++)
        {
            if (space.Axes[i].Direction == 1 && xIdx < 0) xIdx = i;
            else if (space.Axes[i].Direction == -1 && yIdx < 0) yIdx = i;
        }
        // Fallback for spaces where all axes share a direction (e.g., Grid with both +1).
        if (xIdx < 0 && yIdx < 0)
        {
            xIdx = 0;
            yIdx = space.Dims > 1 ? 1 : 0;
        }
        else if (xIdx < 0)
        {
            xIdx = (yIdx == 0 && space.Dims > 1) ? 1 : 0;
        }
        else if (yIdx < 0)
        {
            yIdx = (xIdx == 0 && space.Dims > 1) ? 1 : 0;
        }
        // Ensure 2D+ spaces always use different axes for X and Y.
        if (space.Dims > 1 && xIdx == yIdx)
            yIdx = (xIdx == 0) ? 1 : 0;

        XAxisIndex = xIdx;
        YAxisIndex = yIdx;

        // Determine data range from axis bounds, falling back to viewRange when unbounded/huge.
        var xAxis = space.Axes[XAxisIndex];
        var yAxis = space.Axes[YAxisIndex];

        DataMinX = (xAxis.HasMinBound && Math.Abs(xAxis.MinBound) < MaxReasonableRange)
            ? xAxis.MinBound : -viewRange;
        DataMaxX = (xAxis.HasMaxBound && Math.Abs(xAxis.MaxBound) < MaxReasonableRange)
            ? xAxis.MaxBound : viewRange;
        DataMinY = (yAxis.HasMinBound && Math.Abs(yAxis.MinBound) < MaxReasonableRange)
            ? yAxis.MinBound : -viewRange;
        DataMaxY = (yAxis.HasMaxBound && Math.Abs(yAxis.MaxBound) < MaxReasonableRange)
            ? yAxis.MaxBound : viewRange;

        // Guard against zero-width ranges.
        if (DataMaxX <= DataMinX) { DataMinX -= viewRange; DataMaxX += viewRange; }
        if (DataMaxY <= DataMinY) { DataMinY -= viewRange; DataMaxY += viewRange; }
    }

    /// <summary>Convert a data X value to screen pixels.</summary>
    public float ToScreenX(double dataX)
    {
        float t = (float)((dataX - DataMinX) / (DataMaxX - DataMinX));
        return ScreenBounds.Left + t * ScreenBounds.Width;
    }

    /// <summary>
    /// Convert a data Y value to screen pixels.
    /// Screen Y is inverted so higher data values appear higher on screen.
    /// </summary>
    public float ToScreenY(double dataY)
    {
        float t = (float)((dataY - DataMinY) / (DataMaxY - DataMinY));
        return ScreenBounds.Bottom - t * ScreenBounds.Height;
    }

    /// <summary>
    /// Convert a CValue to a screen-space point.
    /// Uses XAxisIndex for X and YAxisIndex for Y (for 2D+ spaces).
    /// </summary>
    public SKPoint ToScreen(CValue value)
    {
        double x = value.GetValue(XAxisIndex);
        double y = Space.Dims > 1 ? value.GetValue(YAxisIndex) : 0.0;
        return new SKPoint(ToScreenX(x), ToScreenY(y));
    }

    /// <summary>
    /// Convert a scalar distance in data units to screen pixels.
    /// Uses the X axis scale (assumes square data aspect for circles, etc.).
    /// </summary>
    public float DataToPixels(double dataDistance) =>
        (float)(dataDistance * ScreenBounds.Width / (DataMaxX - DataMinX));

    /// <summary>Screen position of the data origin (0, 0).</summary>
    public SKPoint Origin => new SKPoint(ToScreenX(0.0), ToScreenY(0.0));
}
