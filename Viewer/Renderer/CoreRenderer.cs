
using Complexity.Core;
using SkiaSharp;
using Viewer.Views;

namespace Viewer.Renderer;
public class CoreRenderer
{
    private static CoreRenderer _instance;
    public IPageView? Page { get; set; }
    public static CoreRenderer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CoreRenderer();
            }
            return _instance;
        }
    }

    public int Width { get; set; }
    public int Height { get; set; }

    public event CanvasEventHandler DrawingStart;
    public event CanvasEventHandler DrawingComplete;

    public SKCanvas Canvas;
    public CorePens Pens { get; set; }
    public SKBitmap Bitmap { get; set; }
    public bool ShowBitmap { get; set; }

    private CoreRenderer()
    {
        GeneratePens();
    }

    public virtual void BeginDraw()
    {
        Canvas.Save();
        Canvas.SetMatrix(Matrix);
        Canvas.Clear(Pens.BkgColor);
        OnDrawingBegin();
    }
    public virtual void Draw()
    {
        Page?.Render(Canvas);
    }
    public virtual void EndDraw()
    {
        Canvas.Restore();
        if (ShowBitmap && Bitmap != null)
        {
            DrawBitmap(Bitmap);
        }

        OnDrawingComplete();
        Canvas = null;
    }
    protected void OnDrawingBegin()
    {
        DrawingStart?.Invoke(this, new CanvasEventArgs(Canvas));
    }
    protected void OnDrawingComplete()
    {
        DrawingComplete?.Invoke(this, new CanvasEventArgs(Canvas));
    }



    public void DrawTextAt(SKPoint point, string text, SKPaint paint)
    {
        Canvas.DrawText(text, point.X, point.Y, paint);
    }
    public void DrawText(SKPoint center, string text, SKPaint paint, SKPaint background = null)
    {
        if (background != null)
        {
            var rect = GetTextBackgroundSize(center.X, center.Y, text, paint);
            DrawTextBackground(rect, background);
            center = new SKPoint(rect.Left + 4, center.Y);
        }
        Canvas.DrawText(text, center.X, center.Y, paint);
    }

    public void DrawTextBackground(SKRect rect, SKPaint background)
    {
        Canvas.DrawRoundRect(rect, 5, 5, background);
    }
    public void DrawBitmap(SKBitmap bitmap)
    {
        Canvas.DrawBitmap(bitmap, new SKRect(0, 0, Width, Height));
    }
    public void DrawBitmap(SKBitmap bitmap, SKRect bounds)
    {
        using (SKPaint paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
        {
            Canvas.DrawBitmap(bitmap, bounds, paint);
        }
    }

    public SKPath GetCirclePath(SKPoint center, float radius = 10)
    {
        var path = new SKPath();
        path.AddCircle(center.X, center.Y, radius);
        return path;
    }
    public SKPath GetRectPath(SKPoint topLeft, SKPoint bottomRight)
    {
        var path = new SKPath();
        path.AddRect(new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y));
        return path;
    }
    public void GeneratePens(ColorTheme colorTheme = ColorTheme.RosePine)
    {
        Pens = new CorePens(1, colorTheme);
    }
    public SKRect GetTextBackgroundSize(float x, float y, string text, SKPaint paint)
    {
        var fm = paint.FontMetrics;
        float halfTextLength = paint.MeasureText(text) / 2 + 4;
        return new SKRect((int)(x - halfTextLength), (int)(y + fm.Top + 3), (int)(x + halfTextLength), (int)(y + fm.Bottom - 1));
    }
    public SKRect GetTextBackgroundSizeExact(float x, float y, string text, SKPaint paint)
    {
        var fm = paint.FontMetrics;
        float halfTextLength = paint.MeasureText(text) / 2f + 0.1f;
        return new SKRect((int)(x - halfTextLength), (int)(y + fm.Top), (int)(x + halfTextLength), (int)(y + fm.Bottom));
    }

    #region RenderSurface

    public void DrawOnBitmapSurface()
    {
        if (Bitmap != null)
        {
            using (SKCanvas canvas = new SKCanvas(Bitmap))
            {
                DrawOnCanvas(canvas);
            }
        }
    }
    public void DrawOnCanvas(SKCanvas canvas)
    {
        Canvas = canvas;
        BeginDraw();
        Draw();
        EndDraw();
    }
    #endregion

    #region View Matrix

    private SKMatrix _matrix = SKMatrix.CreateIdentity();
    public SKMatrix Matrix
    {
        get => _matrix;
        set => _matrix = value;
    }
    public float ScreenScale { get; set; } = 1f;

    public void SetPanAndZoom(SKMatrix initalMatrix, SKPoint anchorPt, SKPoint translation, float scale)
    {
        var scaledAnchor = new SKPoint(anchorPt.X * ScreenScale, anchorPt.Y * ScreenScale);
        var scaledTranslation = new SKPoint(translation.X * ScreenScale, translation.Y * ScreenScale);

        var mTranslation = SKMatrix.CreateTranslation(scaledTranslation.X, scaledTranslation.Y);
        var mScale = SKMatrix.CreateScale(scale, scale, scaledAnchor.X, scaledAnchor.Y);
        var mIdent = SKMatrix.CreateIdentity();
        SKMatrix.Concat(ref mIdent, mTranslation, mScale);
        SKMatrix.Concat(ref _matrix, mIdent, initalMatrix);
    }

    public void ResetZoom()
    {
        Matrix = SKMatrix.CreateIdentity();
    }

    #endregion

}

public delegate void CanvasEventHandler(object? sender, CanvasEventArgs e);
public class CanvasEventArgs : EventArgs
{
    public SKCanvas Canvas { get; }

    public CanvasEventArgs(SKCanvas canvas)
    {
        Canvas = canvas;
    }
}