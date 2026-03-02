using SkiaSharp;
using Viewer.Renderer;

namespace Viewer.Adapters;

/// <summary>
/// A visual element that can draw itself onto a Skia canvas.
/// Implementations adapt Core types (CSpace, CValue, etc.) into Skia geometry.
/// </summary>
public interface ISkiaElement
{
    void Render(SKCanvas canvas, CorePens pens);
}
