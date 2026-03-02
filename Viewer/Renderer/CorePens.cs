using SkiaSharp;

namespace Viewer.Renderer;

public enum ColorTheme
{
    Normal,
    Dark,
    RosePine
}
public class CorePens
{
    // ── Rosé Pine palette ─────────────────────────────────────────────
    public static readonly SKColor RP_Background = SKColor.Parse("#191724");
    public static readonly SKColor RP_Surface    = SKColor.Parse("#1f1d2e");
    public static readonly SKColor RP_Text       = SKColor.Parse("#e0def4");
    public static readonly SKColor RP_Muted      = SKColor.Parse("#6e6a86");
    public static readonly SKColor RP_Gold       = SKColor.Parse("#f6c177");
    public static readonly SKColor RP_Teal       = SKColor.Parse("#9ccfd8");
    public static readonly SKColor RP_Iris       = SKColor.Parse("#c4a7e7");
    public static readonly SKColor RP_Pine       = SKColor.Parse("#31748f");
    public static readonly SKColor RP_Rose       = SKColor.Parse("#ebbcba");
    public static readonly SKColor RP_Love       = SKColor.Parse("#eb6f92");

    public List<SKPaint> Pens = new List<SKPaint>();

    public float DefaultWidth { get; }
    public bool IsHoverMap { get; set; }

    public SKPaint BkgBrush { get; set; }
    public SKPaint DrawPen { get; set; }
    public SKPaint NumberLinePen { get; set; }
    public SKPaint NumberLineGradient { get; set; }
    public SKPaint TickBoldPen { get; set; }
    public SKPaint TickPen { get; set; }
    public SKPaint UnitPen { get; set; }
    public SKPaint UnotPen { get; set; }
    public SKPaint UnitPenLight { get; set; }
    public SKPaint UnotPenLight { get; set; }
    public SKPaint UnitStrokePen { get; set; }
    public SKPaint MarkerBrush { get; set; }

    public SKPaint GrayPen { get; set; }
    public SKPaint BackHatch { get; set; }
    public SKPaint ForeHatch { get; set; }

    public SKPaint HoverPen { get; set; }
    public SKPaint HighlightPen { get; set; }
    public SKPaint ThickHighlightPen { get; set; }

    public SKPaint UnotInlinePen { get; set; }
    public SKPaint UnitInlinePen { get; set; }
    public SKPaint SegPen0 { get; set; }
    public SKPaint SegPen1 { get; set; }
    public SKPaint SegPen2 { get; set; }
    public SKPaint SegPen3 { get; set; }
    public List<SKPaint> SegPens;
    public SKPaint SegPenHighlight { get; set; }
    public SKPaint DomainPenHighlight { get; set; }

    public SKPaint TextBrush { get; set; }
    public SKPaint LabelBrush { get; set; }
    public SKPaint Seg0TextBrush { get; set; }
    public SKPaint Seg1TextBrush { get; set; }
    public SKPaint Seg2TextBrush { get; set; }
    public SKPaint Seg3TextBrush { get; set; }
    public List<SKPaint> SegTextBrushes;

    public SKPaint LineTextPen { get; set; }
    public SKPaint TextBackgroundPen { get; set; }
    public SKPaint TextFractionPen { get; set; }
    public SKPaint SlugTextPen { get; set; }

    public SKColor BkgColor { get; set; }
    public SKColor UnitColor { get; set; }
    public SKColor UnitLineColor { get; set; }
    public SKColor UnotColor { get; set; }
    public SKColor UnotLineColor { get; set; }
    public SKColor UnitStrokeColor { get; set; }
    public SKColor TickColor { get; set; }
    public SKColor MarkerColor { get; set; }

    public SKColor UnitTextColor { get; set; }
    public SKColor UnotTextColor { get; set; }
    public SKPaint UnitMarkerText { get; set; }
    public SKPaint UnotMarkerText { get; set; }

    public static byte lighterAlpha = 160;
    public CorePens(float defaultWidth = 1f, ColorTheme colorTheme = ColorTheme.Normal)
    {
        DefaultWidth = defaultWidth;
        GenTheme(colorTheme);
    }

    private void GenNormalTheme()
    {
        BkgColor = SKColor.Parse("#FFFFFF");
        UnitColor = SKColor.Parse("#00FAFF");
        UnitLineColor = SKColor.Parse("#AFDFF8");
        UnotColor = SKColor.Parse("#FF0098");
        UnotLineColor = SKColor.Parse("#DDAAAE");
        UnitStrokeColor = SKColor.Parse("#404040");
        TickColor = SKColors.Black;
        MarkerColor = SKColors.DarkGray;

        UnitTextColor = SKColor.Parse("#006070");
        UnotTextColor = SKColor.Parse("#600040");
        UnitMarkerText = GetText(UnitTextColor, 12, "Arial", false, true);
        UnotMarkerText = GetText(UnotTextColor, 12, "Arial", false, true);

        BkgBrush = GetBrush(BkgColor);

        NumberLinePen = GetPen(TickColor, DefaultWidth * 0.75f);
        NumberLineGradient = GetPen(TickColor, DefaultWidth * 0.75f);
        UnitPen = GetPen(UnitColor, DefaultWidth * 4f, SKStrokeCap.Butt);
        UnotPen = GetPen(UnotColor, DefaultWidth * 4f, SKStrokeCap.Butt);
        UnitPenLight = GetPen(UnitColor.WithAlpha(lighterAlpha), DefaultWidth * 4f, SKStrokeCap.Butt);
        UnotPenLight = GetPen(UnotColor.WithAlpha(lighterAlpha), DefaultWidth * 4f, SKStrokeCap.Butt);
        UnitStrokePen = GetPen(UnitStrokeColor, UnitPen.StrokeWidth * 1.4f, SKStrokeCap.Butt);
        TickPen = GetPen(TickColor, DefaultWidth * .25f);
        TickBoldPen = GetPen(TickColor, DefaultWidth * 2f);
        MarkerBrush = GetBrush(MarkerColor);

        BackHatch = GetHatch(SKColors.Black, false);
        ForeHatch = GetHatch(SKColors.Black, true);
        HoverPen = GetPen(SKColor.Parse("#80D02020"), DefaultWidth * 2);
        HighlightPen = GetPen(SKColors.LightYellow, DefaultWidth * 1f);
        ThickHighlightPen = GetPen(SKColor.Parse("#80E0D000"), DefaultWidth * 5f);

        UnitInlinePen = GetPen(UnitColor, DefaultWidth * 2f);
        UnotInlinePen = GetPen(UnotColor, DefaultWidth * 2f);
        SegPen0 = GetPen(SKColor.Parse("#106010"), DefaultWidth * 5f);
        SegPen1 = GetPen(SKColor.Parse("#101060"), DefaultWidth * 5f);
        SegPen2 = GetPen(SKColor.Parse("#601000"), DefaultWidth * 5f);
        SegPen3 = GetPen(SKColor.Parse("#106060"), DefaultWidth * 5f);
        SegPens = new List<SKPaint>() { SegPen0, SegPen1, SegPen2, SegPen3 };
        SegPenHighlight = GetPen(SKColor.Parse("#101040"), DefaultWidth * 8f);
        DomainPenHighlight = GetPen(SKColor.Parse("#80EEEE22"), DefaultWidth * 8f);
        DomainPenHighlight.StrokeCap = SKStrokeCap.Butt;

        TextBrush = GetText(SKColor.Parse("#A0A0F0"), 20);
        LabelBrush = GetText(SKColor.Parse("#406060"), 14);
        Seg0TextBrush = GetText(SegPen0.Color, 20);
        Seg1TextBrush = GetText(SegPen1.Color, 20);
        Seg2TextBrush = GetText(SegPen2.Color, 20);
        Seg3TextBrush = GetText(SegPen3.Color, 20);
        SegTextBrushes = new List<SKPaint>() { Seg0TextBrush, Seg1TextBrush, Seg2TextBrush, Seg3TextBrush };

        DrawPen = GetPen(SKColors.LightBlue, DefaultWidth * 4);
        GrayPen = GetPen(SKColors.LightGray, DefaultWidth * 0.75f);

        LineTextPen = new SKPaint(new SKFont(SKTypeface.Default, 12f));
        LineTextPen.IsAntialias = true;
        LineTextPen.Color = new SKColor(0x40, 0x40, 0x60);
        LineTextPen.TextAlign = SKTextAlign.Center;

        TextBackgroundPen = GetPen(new SKColor(244, 244, 244, 220), 0);
        TextBackgroundPen.Style = SKPaintStyle.Fill;

        TextFractionPen = new SKPaint(new SKFont(SKTypeface.Default, 10f));
        TextFractionPen.IsAntialias = true;
        TextFractionPen.Color = new SKColor(0x40, 0x40, 0x60);
        TextFractionPen.TextAlign = SKTextAlign.Right;

        SlugTextPen = new SKPaint(new SKFont(SKTypeface.Default, 8f));
        SlugTextPen.IsAntialias = true;
        SlugTextPen.Color = new SKColor(0x80, 0x40, 0x40);
        SlugTextPen.TextAlign = SKTextAlign.Center;
    }
    private void GenDarkTheme()
    {
        BkgColor = SKColor.FromHsl(200f, 14f, 8f);
        UnitColor = SKColor.Parse("#008A8F");
        UnitLineColor = SKColor.Parse("#203070");
        UnotColor = SKColor.Parse("#8F0058");
        UnotLineColor = SKColor.Parse("#703020");

        UnitTextColor = SKColor.Parse("#80C0D0");
        UnotTextColor = SKColor.Parse("#D080A0");
        UnitMarkerText = GetText(UnitTextColor, 12, "Arial", false, true);
        UnotMarkerText = GetText(UnotTextColor, 12, "Arial", false, true);

        TickColor = SKColors.Gray;
        UnitPen = GetPen(UnitColor, DefaultWidth * 4f, SKStrokeCap.Butt);
        UnotPen = GetPen(UnotColor, DefaultWidth * 4f, SKStrokeCap.Butt);
        UnitPenLight = GetPen(UnitColor.WithAlpha(lighterAlpha), DefaultWidth * 4f, SKStrokeCap.Butt);
        UnotPenLight = GetPen(UnotColor.WithAlpha(lighterAlpha), DefaultWidth * 4f, SKStrokeCap.Butt);
        UnitStrokePen = null;
        MarkerColor = SKColors.Gray;

        BkgBrush = GetBrush(BkgColor);
        BackHatch = GetHatch(SKColors.Black, false);
        ForeHatch = GetHatch(SKColors.Black, true);

        NumberLinePen = GetPen(TickColor, DefaultWidth * 0.75f);
        NumberLineGradient = GetPen(TickColor, DefaultWidth * 0.75f);
        TickPen = GetPen(TickColor, DefaultWidth * 0.25f);
        TickBoldPen = GetPen(TickColor, DefaultWidth * 2f);
        MarkerBrush = GetBrush(MarkerColor);

        HoverPen = GetPen(new SKColor(240, 220, 220), DefaultWidth * 2);
        HighlightPen = GetPen(SKColors.LightYellow, DefaultWidth * 1f);
        ThickHighlightPen = GetPen(SKColor.Parse("#80E0D000"), DefaultWidth * 5f);

        UnitInlinePen = GetPen(UnitColor, DefaultWidth * 2.5f);
        UnotInlinePen = GetPen(UnotColor, DefaultWidth * 2.5f);
        SegPen0 = GetPen(SKColors.Black, DefaultWidth * 4f);
        SegPen1 = GetPen(new SKColor(50, 250, 210, 255), DefaultWidth * 4f);
        SegPen2 = GetPen(new SKColor(50, 50, 250, 255), DefaultWidth * 4f);
        SegPen3 = GetPen(new SKColor(50, 250, 50, 255), DefaultWidth * 4f);
        SegPens = new List<SKPaint>() { SegPen0, SegPen1, SegPen2, SegPen3 };
        SegPenHighlight = GetPen(SKColor.Parse("#B0B0FF"), DefaultWidth * 8f);
        DomainPenHighlight = GetPen(SKColor.Parse("#80EEEE22"), DefaultWidth * 8f);
        DomainPenHighlight.StrokeCap = SKStrokeCap.Butt;

        TextBrush = GetText(SKColor.Parse("#A0404060"), 20);
        LabelBrush = GetText(SKColor.Parse("#406060"), 14);
        Seg0TextBrush = GetText(SegPen0.Color, 20);
        Seg1TextBrush = GetText(SegPen1.Color, 20);
        Seg2TextBrush = GetText(SegPen2.Color, 20);
        Seg3TextBrush = GetText(SegPen3.Color, 20);
        SegTextBrushes = new List<SKPaint>() { Seg0TextBrush, Seg1TextBrush, Seg2TextBrush, Seg3TextBrush };

        DrawPen = GetPen(SKColors.LightBlue, DefaultWidth * 4);
        GrayPen = GetPen(SKColors.LightGray, DefaultWidth * 0.75f);

        LineTextPen = new SKPaint(new SKFont(SKTypeface.Default, 12f));
        LineTextPen.IsAntialias = true;
        LineTextPen.Color = new SKColor(0x40, 0x40, 0x60);
        LineTextPen.TextAlign = SKTextAlign.Center;
        TextBackgroundPen = GetPen(SKColor.Parse("#80303040"), 0);
        TextBackgroundPen.Style = SKPaintStyle.Fill;

        TextFractionPen = new SKPaint(new SKFont(SKTypeface.Default, 10f));
        TextFractionPen.IsAntialias = true;
        TextFractionPen.Color = new SKColor(0xB0, 0xB0, 0xB0);
        TextFractionPen.TextAlign = SKTextAlign.Left;

        SlugTextPen = new SKPaint(new SKFont(SKTypeface.Default, 8f));
        SlugTextPen.IsAntialias = true;
        SlugTextPen.Color = new SKColor(0x80, 0x40, 0x40);
        SlugTextPen.TextAlign = SKTextAlign.Center;
    }
    private void GenRosePineTheme()
    {
        const float W = 7f;   // bold stroke width multiplier throughout

        BkgColor        = RP_Background;
        UnitColor       = RP_Teal;
        UnitLineColor   = new SKColor(0x31, 0x74, 0x8f, 80);
        UnotColor       = RP_Iris;
        UnotLineColor   = new SKColor(0xc4, 0xa7, 0xe7, 80);
        UnitStrokeColor = RP_Pine;
        TickColor       = RP_Muted;
        MarkerColor     = RP_Muted;

        UnitTextColor  = RP_Teal;
        UnotTextColor  = RP_Iris;
        UnitMarkerText = GetText(UnitTextColor, 12, "Outfit", false, true);
        UnotMarkerText = GetText(UnotTextColor, 12, "Outfit", false, true);

        BkgBrush = GetBrush(BkgColor);

        NumberLinePen      = GetPen(RP_Muted, DefaultWidth * 1.0f);
        NumberLineGradient = GetPen(RP_Muted, DefaultWidth * 1.0f);
        UnitPen       = GetPen(UnitColor, DefaultWidth * W, SKStrokeCap.Butt);
        UnotPen       = GetPen(UnotColor, DefaultWidth * W, SKStrokeCap.Butt);
        UnitPenLight  = GetPen(UnitColor.WithAlpha(lighterAlpha), DefaultWidth * W, SKStrokeCap.Butt);
        UnotPenLight  = GetPen(UnotColor.WithAlpha(lighterAlpha), DefaultWidth * W, SKStrokeCap.Butt);
        UnitStrokePen = GetPen(UnitStrokeColor, DefaultWidth * W * 1.4f, SKStrokeCap.Butt);
        TickPen       = GetPen(TickColor, DefaultWidth * 0.5f);
        TickBoldPen   = GetPen(TickColor, DefaultWidth * 2.5f);
        MarkerBrush   = GetBrush(MarkerColor);

        BackHatch = GetHatch(RP_Muted, false);
        ForeHatch = GetHatch(RP_Muted, true);
        HoverPen  = GetPen(RP_Love.WithAlpha(160), DefaultWidth * 2f);
        HighlightPen      = GetPen(RP_Gold.WithAlpha(120), DefaultWidth * 1f);
        ThickHighlightPen = GetPen(RP_Gold.WithAlpha(100), DefaultWidth * 5f);

        UnitInlinePen = GetPen(UnitColor, DefaultWidth * 3f);
        UnotInlinePen = GetPen(UnotColor, DefaultWidth * 3f);

        // Segment pens — Rosé Pine accent palette, bold strokes
        SegPen0 = GetPen(RP_Teal, DefaultWidth * W);
        SegPen1 = GetPen(RP_Iris, DefaultWidth * W);
        SegPen2 = GetPen(RP_Rose, DefaultWidth * W);
        SegPen3 = GetPen(RP_Love, DefaultWidth * W);
        SegPens = new List<SKPaint>() { SegPen0, SegPen1, SegPen2, SegPen3 };
        SegPenHighlight    = GetPen(RP_Gold, DefaultWidth * 10f);
        DomainPenHighlight = GetPen(RP_Gold.WithAlpha(120), DefaultWidth * 10f);
        DomainPenHighlight.StrokeCap = SKStrokeCap.Butt;

        TextBrush     = GetText(RP_Text,  20, "Outfit");
        LabelBrush    = GetText(RP_Muted, 14, "Outfit");
        Seg0TextBrush = GetText(SegPen0.Color, 20, "Outfit");
        Seg1TextBrush = GetText(SegPen1.Color, 20, "Outfit");
        Seg2TextBrush = GetText(SegPen2.Color, 20, "Outfit");
        Seg3TextBrush = GetText(SegPen3.Color, 20, "Outfit");
        SegTextBrushes = new List<SKPaint>() { Seg0TextBrush, Seg1TextBrush, Seg2TextBrush, Seg3TextBrush };

        DrawPen = GetPen(RP_Teal, DefaultWidth * 4f);
        GrayPen = GetPen(RP_Muted.WithAlpha(60), DefaultWidth * 0.75f);

        var outfitTypeface = SKTypeface.FromFamilyName("Outfit", SKFontStyle.Normal);

        LineTextPen = new SKPaint(new SKFont(outfitTypeface, 12f));
        LineTextPen.IsAntialias = true;
        LineTextPen.Color       = RP_Muted;
        LineTextPen.TextAlign   = SKTextAlign.Center;

        TextBackgroundPen = GetPen(RP_Surface.WithAlpha(220), 0);
        TextBackgroundPen.Style = SKPaintStyle.Fill;

        TextFractionPen = new SKPaint(new SKFont(outfitTypeface, 10f));
        TextFractionPen.IsAntialias = true;
        TextFractionPen.Color     = RP_Muted;
        TextFractionPen.TextAlign = SKTextAlign.Right;

        SlugTextPen = new SKPaint(new SKFont(outfitTypeface, 9f));
        SlugTextPen.IsAntialias = true;
        SlugTextPen.Color     = RP_Muted;
        SlugTextPen.TextAlign = SKTextAlign.Center;
    }

    public void GenTheme(ColorTheme theme)
    {
        switch (theme)
        {
            case ColorTheme.RosePine:
                GenRosePineTheme();
                break;
            case ColorTheme.Dark:
                GenDarkTheme();
                break;
            case ColorTheme.Normal:
            default:
                GenNormalTheme();
                break;
        }

        Pens.Clear();
        Pens.Add(GetPen(SKColors.Black, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkRed, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkOrange, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkGoldenrod, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkOliveGreen, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkGreen, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkCyan, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkBlue, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkOrchid, DefaultWidth));
        Pens.Add(GetPen(SKColors.DarkMagenta, DefaultWidth));
        Pens.Add(GetPen(SKColors.Red, DefaultWidth));
        Pens.Add(GetPen(SKColors.Orange, DefaultWidth));
        Pens.Add(GetPen(SKColors.Yellow, DefaultWidth));
        Pens.Add(GetPen(SKColors.Chartreuse, DefaultWidth));
        Pens.Add(GetPen(SKColors.Green, DefaultWidth));
        Pens.Add(GetPen(SKColors.Cyan, DefaultWidth));
        Pens.Add(GetPen(SKColors.Blue, DefaultWidth));
        Pens.Add(GetPen(SKColors.Orchid, DefaultWidth));
        Pens.Add(GetPen(SKColors.Magenta, DefaultWidth));
        Pens.Add(GetPen(SKColors.White, DefaultWidth));
        Pens.Add(GetPen(SKColors.White, DefaultWidth)); // filler
        Pens.Add(GetPen(SKColors.White, DefaultWidth));
        Pens.Add(GetPen(SKColors.White, DefaultWidth));
        Pens.Add(GetPen(SKColors.White, DefaultWidth));
        Pens.Add(GetPen(SKColors.White, DefaultWidth));
        Pens.Add(GetPen(SKColors.White, DefaultWidth));
        Pens.Add(GetPen(SKColors.White, DefaultWidth));

    }


    public SKPaint this[int i] => GetPenForIndex(i);
    private SKPaint GetPenForIndex(int index)
    {
        SKPaint result;
        index = index < 0 ? 0 : index;
        if (index < Pens.Count)
        {
            result = Pens[index];
        }
        else
        {
            throw new OverflowException("Pen not found with index:" + index);
            //result = GetPenByOrder(index - Pens.Count);
        }

        return result;
    }
    public SKPaint GetPenByOrder(int index, float widthScale = 1)
    {
        //uint col = (uint)((index + 3) | 0xFF000000);
        uint col = (uint)((index + 3) * 0x110D05) | 0xFF000000;
        if (IndexOfColor.ContainsKey(col))
        {
            IndexOfColor[col] = index;
        }
        else
        {
            IndexOfColor.Add(col, index);
        }

        var color = new SKColor(col);
        return GetPen(color, DefaultWidth * widthScale);
    }
    public int IndexOfPen(SKPaint pen) => Pens.IndexOf(pen);

    public Dictionary<uint, int> IndexOfColor { get; } = new Dictionary<uint, int>();

    public static SKPaint GetBrush(SKColor color)
    {
        SKPaint result = new SKPaint()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = color
        };
        return result;
    }
    public static SKPaint GetPen(SKColor color, float width, SKStrokeCap cap = SKStrokeCap.Round)
    {
        SKPaint pen = new SKPaint()
        {
            Style = SKPaintStyle.Stroke,
            Color = color,
            StrokeWidth = width,
            IsAntialias = true,
            StrokeCap = cap,
        };
        return pen;
    }

    public static SKPaint GetGradientPen(SKPoint p0, SKPoint p1, SKColor color0, SKColor color1, float width)
    {
        var shader = SKShader.CreateLinearGradient(
            p0,
            p1,
            new SKColor[] { color0, color1 },
            new float[] { 0, 1 },
            SKShaderTileMode.Clamp);

        SKPaint result = new SKPaint()
        {
            Style = SKPaintStyle.Stroke,
            Color = color0,
            StrokeWidth = width,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Butt,
            Shader = shader
        };
        return result;
    }
    public static SKPaint GetGradientBrush(SKPoint p0, SKPoint p1, SKColor color0, SKColor color1)
    {
        var shader = SKShader.CreateLinearGradient(
            p0,
            p1,
            new SKColor[] { color0, color1 },
            new float[] { 0, 1 },
            SKShaderTileMode.Clamp);

        SKPaint result = new SKPaint()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Shader = shader
        };
        return result;
    }
    public static SKPaint GetHatch(SKColor color, bool isForward = true)
    {
        var hatch = new SKPath();
        if (isForward)
        {
            hatch.AddPoly(new SKPoint[] { new SKPoint(-2, 2), new SKPoint(2, -2) }, false);
        }
        else
        {
            hatch.AddPoly(new SKPoint[] { new SKPoint(2, 2), new SKPoint(-2, -2) }, false);
        }

        var result = new SKPaint
        {
            PathEffect = SKPathEffect.Create2DPath(SKMatrix.CreateScale(8, 8), hatch),
            Color = color,// SKColor.FromHsl(200f, 14f, 18f),
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 1f
        };
        return result;
    }

    public static SKPaint GetText(SKColor color, float fontSize, string fontName = "Arial", bool isBold = false, bool isCentered = false)
    {
        var result = new SKPaint { TextSize = fontSize, Color = color };
        result.IsAntialias = true;
        result.TextAlign = isCentered ? SKTextAlign.Center : SKTextAlign.Left;
        result.Typeface = SKTypeface.FromFamilyName(
            fontName,
            isBold ? SKFontStyle.Bold : SKFontStyle.Normal);
        return result;
    }

    /// <summary>
    /// Create an SKFont for use with the non-deprecated DrawText overloads.
    /// Pair with GetBrush(color) for the paint argument.
    /// </summary>
    public static SKFont GetFont(float fontSize, string fontName = "Arial", bool isBold = false)
    {
        var typeface = SKTypeface.FromFamilyName(
            fontName,
            isBold ? SKFontStyle.Bold : SKFontStyle.Normal);
        return new SKFont(typeface, fontSize);
    }
}
