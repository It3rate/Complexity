using SkiaSharp;

namespace Viewer.Adapters;

/// <summary>
/// Visual styling for a rendered CValue: which pen to use, whether to draw a vector
/// from origin, the dot radius, and an optional label.
/// </summary>
public class ElementStyle
{
    public static readonly ElementStyle Default = new ElementStyle();

    /// <summary>Pen to use for the point dot and optional vector line. Null = use SegPen0.</summary>
    public SKPaint? PointPen { get; init; }

    /// <summary>Radius of the filled square drawn at the value's position.</summary>
    public float PointRadius { get; init; } = 6f;

    /// <summary>If true, draw a line from the coordinate origin to the point (vector arrow).</summary>
    public bool ShowVector { get; init; } = false;

    /// <summary>If true and Label is set, draw a text label next to the point.</summary>
    public bool ShowLabel { get; init; } = false;

    /// <summary>Text to draw next to the point when ShowLabel is true.</summary>
    public string? Label { get; init; }

    /// <summary>Thickness of the segment bar (for SkiaSegment rendering).</summary>
    public float BarThickness { get; init; } = 6f;

    /// <summary>Fixed Y screen position for segment bars. Null = use the axis zero line.</summary>
    public float? BarY { get; init; }
}
