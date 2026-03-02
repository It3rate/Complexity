using Complexity.Core;
using SkiaSharp;

namespace Viewer.Adapters;

/// <summary>
/// Factory for adapting Core types into Skia visual elements.
/// This is the entry point for the Core → Skia rendering pipeline.
///
/// Typical usage:
///   var map = CoreAdapter.MapSpace(space, screenRect);
///   new SkiaSpace(map).Render(canvas, pens);
///   CoreAdapter.AdaptValue(value, map, style).Render(canvas, pens);
/// </summary>
public static class CoreAdapter
{
    /// <summary>
    /// Build a CoordinateMap that maps this space to the given screen rectangle.
    /// viewRange controls the visible half-extent for unbounded or very large axes.
    /// </summary>
    public static CoordinateMap MapSpace(CSpace space, SKRect screenBounds, double viewRange = 5.0)
        => new CoordinateMap(space, screenBounds, viewRange);

    /// <summary>
    /// Adapt a CSpace into a renderable coordinate-system element (axes + grid).
    /// Convenience wrapper: MapSpace then wrap in SkiaSpace.
    /// </summary>
    public static SkiaSpace AdaptSpace(CSpace space, SKRect screenBounds, double viewRange = 5.0)
        => new SkiaSpace(MapSpace(space, screenBounds, viewRange));

    /// <summary>
    /// Adapt a single CValue into a renderable point element.
    /// </summary>
    public static SkiaValue AdaptValue(CValue value, CoordinateMap map, ElementStyle? style = null)
        => new SkiaValue(value, map, style);

    /// <summary>
    /// Adapt a sequence of CValues sharing the same map and style.
    /// </summary>
    public static IEnumerable<SkiaValue> AdaptValues(
        IEnumerable<CValue> values, CoordinateMap map, ElementStyle? style = null)
        => values.Select(v => new SkiaValue(v, map, style));

    /// <summary>
    /// Adapt a 2D segment/complex CValue into a renderable horizontal bar element.
    /// </summary>
    public static SkiaSegment AdaptSegment(CValue value, CoordinateMap map, ElementStyle? style = null)
        => new SkiaSegment(value, map, style);
}
