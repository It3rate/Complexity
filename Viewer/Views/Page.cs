using Complexity.Core;
using SkiaSharp;
using Viewer.Adapters;
using Viewer.Renderer;

namespace Viewer.Views;

public interface IPageView
{
    CoreRenderer Renderer { get; }
    void Render(SKCanvas canvas);
}

/// <summary>
/// Four-page demonstration viewer with Rosé Pine dark theme.
///
/// PAGE 1  Segment Arithmetic — 1D segments as horizontal bars
/// PAGE 2  2D Grid Space      — point addition with AffineKind / Chirality annotation
/// PAGE 3  Boolean Operations — interval AND / OR / XOR
/// PAGE 4  Nested Frames Demo — values used as sub-spaces; layered tension
///
/// Navigate with ← / → arrow keys or click the on-screen buttons.
/// </summary>
public class Page : IPageView
{
    public CoreRenderer Renderer => CoreRenderer.Instance;
    private static readonly string _fontName = "Outfit";

    private int _pageIndex = 0;
    private const int PageCount = 5;

    /// <summary>Hit area of the Prev button in canvas pixels (set each frame).</summary>
    public SKRect PrevBounds { get; private set; }
    /// <summary>Hit area of the Next button in canvas pixels (set each frame).</summary>
    public SKRect NextBounds { get; private set; }

    private static readonly string[] PageTitles =
    {
        "Segment Arithmetic",
        "2D Grid Space",
        "Boolean Operations",
        "Nested Frames",
        "Twin-Square Space",
    };

    public void NextPage()     => _pageIndex = (_pageIndex + 1) % PageCount;
    public void PreviousPage() => _pageIndex = (_pageIndex + PageCount - 1) % PageCount;

    // ================================================================
    //  Top-level render
    // ================================================================

    public void Render(SKCanvas canvas)
    {
        if (Renderer.Width < 100 || Renderer.Height < 100) return;

        var pens    = Renderer.Pens;
        float w     = Renderer.Width;
        float h     = Renderer.Height;
        const float navH   = 64f;
        const float margin = 32f;

        var content = new SKRect(margin, margin, w - margin, h - navH - margin);

        switch (_pageIndex)
        {
            case 0: RenderSegmentArithmetic(canvas, pens, content); break;
            case 1: RenderGridSpace(canvas, pens, content);          break;
            case 2: RenderBoolOperations(canvas, pens, content);     break;
            case 3: RenderNestedFramesDemo(canvas, pens, content);   break;
            case 4: RenderTwinSquareDemo(canvas, pens, content);      break;
        }

        RenderNavBar(canvas, pens, w, h, navH);
    }

    // ================================================================
    //  Nav bar
    // ================================================================

    private void RenderNavBar(SKCanvas canvas, CorePens pens,
                              float w, float h, float navH)
    {
        float navTop = h - navH;

        // Panel background
        using var bg = CorePens.GetBrush(CorePens.RP_Surface);
        canvas.DrawRect(SKRect.Create(0, navTop, w, navH), bg);

        // Separator line
        using var sep = CorePens.GetPen(CorePens.RP_Muted.WithAlpha(80), 1f);
        canvas.DrawLine(0, navTop, w, navTop, sep);

        // Prev / Next buttons
        const float btnW   = 46f;
        const float btnH   = 38f;
        const float btnPad = 18f;
        float btnY = navTop + (navH - btnH) / 2f;

        PrevBounds = new SKRect(btnPad,           btnY, btnPad + btnW,       btnY + btnH);
        NextBounds = new SKRect(w - btnPad - btnW, btnY, w - btnPad,          btnY + btnH);

        DrawNavButton(canvas, PrevBounds, "←", _pageIndex > 0);
        DrawNavButton(canvas, NextBounds, "→", _pageIndex < PageCount - 1);

        // Page title — gold, centered
        using var titleFont  = CorePens.GetFont(14f, _fontName, isBold: true);
        using var titlePaint = CorePens.GetBrush(CorePens.RP_Gold);
        canvas.DrawText(PageTitles[_pageIndex],
            w / 2f, navTop + 22f,
            SKTextAlign.Center, titleFont, titlePaint);

        // Page dots
        const float dotR   = 5f;
        const float dotGap = 18f;
        float dotsW     = (PageCount - 1) * dotGap;
        float dotStartX = w / 2f - dotsW / 2f;
        float dotY      = navTop + navH - 14f;

        for (int i = 0; i < PageCount; i++)
        {
            float dx     = dotStartX + i * dotGap;
            bool  active = i == _pageIndex;
            using var dot = CorePens.GetBrush(
                active ? CorePens.RP_Teal : CorePens.RP_Muted.WithAlpha(100));
            canvas.DrawCircle(dx, dotY, active ? dotR : dotR * 0.6f, dot);
        }
    }

    private static void DrawNavButton(SKCanvas canvas, SKRect b, string label, bool enabled)
    {
        using var bg = CorePens.GetBrush(CorePens.RP_Muted.WithAlpha(40));
        canvas.DrawRoundRect(b, 8f, 8f, bg);

        SKColor color = enabled ? CorePens.RP_Text : CorePens.RP_Muted.WithAlpha(80);
        using var font  = CorePens.GetFont(20f, _fontName, isBold: true);
        using var paint = CorePens.GetBrush(color);
        canvas.DrawText(label, b.MidX, b.MidY + 8f, SKTextAlign.Center, font, paint);
    }

    // ================================================================
    //  PAGE 1: Segment Arithmetic on a 1D number line
    //
    //  Notation:  [lo, hi]  →  (−lo·i + hi)
    //    start (unot / imaginary component) = lo  →  filled dot
    //    end   (unit  / real      component) = hi  →  arrowhead
    //
    //  Examples use A = [1, 4] and B = [2, 7]:
    //    Arithmetic: A, B, A+B, 2A, A×B, −A
    //    Bool / set: A∩B, A∪B, A\B, B\A
    // ================================================================

    private static void RenderSegmentArithmetic(SKCanvas canvas, CorePens pens, SKRect bounds)
    {
        DrawPageTitle(canvas, "Segment Arithmetic", bounds);

        const float headerH = 44f;
        var area = new SKRect(bounds.Left, bounds.Top + headerH, bounds.Right, bounds.Bottom);

        // Number line occupies the top ~24% — leaves plenty of room for 10 rows below.
        float lineAreaH = Math.Min(area.Height * 0.24f, 130f);
        var lineArea    = new SKRect(area.Left, area.Top, area.Right, area.Top + lineAreaH);
        var lineSpace   = CSpace.Integer(min: -5, max: 27, ticksPerUnit: 100);
        var map         = CoreAdapter.MapSpace(lineSpace, lineArea);
        new SkiaSpace(map).Render(canvas, pens);

        // ── Build values ─────────────────────────────────────────────
        var segSpace = CSpace.Segment(min: -5, max: 27, ticksPerUnit: 100);
        var A        = segSpace.Value(1.0, 4.0);   // (-i + 4)
        var B        = segSpace.Value(2.0, 7.0);   // (-2i + 7)
        var AplusB   = A + B;                       // (-3i + 11)  [3, 11]
        var doubleA  = A + A;                       // (-2i + 8)   [2, 8]
        var AtimesB  = A * B;                       // A=4-i, B=7-2i → 26-15i → (-15i+26)
        var negA     = -A;                          // (4i - 1)   [-4, -1]

        // Raw lo/hi for set-algebra rows
        double loA = A.GetValue(0),        hiA = A.GetValue(1);        // 1, 4
        double loB = B.GetValue(0),        hiB = B.GetValue(1);        // 2, 7
        double loAnd = Math.Max(loA, loB), hiAnd = Math.Min(hiA, hiB); // [2, 4]  A ∩ B
        double loOr  = Math.Min(loA, loB), hiOr  = Math.Max(hiA, hiB); // [1, 7]  A ∪ B
        double loAmB = loA,                hiAmB = loB;                 // [1, 2]  A \ B
        double loBmA = hiA,                hiBmA = hiB;                 // [4, 7]  B \ A

        // ── Position rows ─────────────────────────────────────────────
        float lineY   = map.ToScreenY(0);
        float barsTop = lineY + 18f;
        float yStep   = Math.Max(26f, (area.Bottom - barsTop - 10f) / 10.5f);

        // ── Row data: (lo, hi, color, label) ─────────────────────────
        var rows = new (double lo, double hi, SKColor color, string label)[]
        {
            // ── Arithmetic ─────────────────────────────────────────
            (loA,                  hiA,                  CorePens.RP_Teal, $"A  =  {SegNotation(loA,  hiA)}"),
            (loB,                  hiB,                  CorePens.RP_Iris, $"B  =  {SegNotation(loB,  hiB)}"),
            (AplusB.GetValue(0),   AplusB.GetValue(1),   CorePens.RP_Pine, $"A + B  =  {SegNotation(AplusB.GetValue(0),  AplusB.GetValue(1))}"),
            (doubleA.GetValue(0),  doubleA.GetValue(1),  CorePens.RP_Gold, $"2A  =  {SegNotation(doubleA.GetValue(0), doubleA.GetValue(1))}"),
            (AtimesB.GetValue(0),  AtimesB.GetValue(1),  CorePens.RP_Rose, $"A \u00d7 B  =  {SegNotation(AtimesB.GetValue(0), AtimesB.GetValue(1))}"),
            (negA.GetValue(0),     negA.GetValue(1),     CorePens.RP_Love, $"-A  =  {SegNotation(negA.GetValue(0),  negA.GetValue(1))}"),
            // ── Bool / set ─────────────────────────────────────────
            (loAnd, hiAnd, CorePens.RP_Teal.WithAlpha(200), $"A \u2229 B  =  {SegNotation(loAnd, hiAnd)}"),
            (loOr,  hiOr,  CorePens.RP_Iris.WithAlpha(200), $"A \u222a B  =  {SegNotation(loOr,  hiOr)}"),
            (loAmB, hiAmB, CorePens.RP_Muted,               $"A \\ B  =  {SegNotation(loAmB, hiAmB)}"),
            (loBmA, hiBmA, CorePens.RP_Muted,               $"B \\ A  =  {SegNotation(loBmA, hiBmA)}"),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            var (lo, hi, color, label) = rows[i];
            float y = barsTop + yStep * (i + 0.5f);
            DrawDirectedBar(canvas, map, color, label, lo, hi, y);
        }

        // Section divider between arithmetic and bool groups
        float divY = barsTop + yStep * 6f;
        using var divPen = CorePens.GetPen(CorePens.RP_Muted.WithAlpha(60), 1f);
        canvas.DrawLine(area.Left + 4, divY, area.Right - 4, divY, divPen);

        using var divFont  = GetFont(3);
        using var divPaint = CorePens.GetBrush(CorePens.RP_Muted);
        canvas.DrawText("— set operations —", area.MidX, divY - 3f,
            SKTextAlign.Center, divFont, divPaint);
    }

    // ================================================================
    //  PAGE 2: 2D Grid Space — point addition, annotated with
    //  AffineKind, Chirality, and Metric from the space
    // ================================================================

    private static void RenderGridSpace(SKCanvas canvas, CorePens pens, SKRect bounds)
    {
        DrawPageTitle(canvas, "2D Grid Space", bounds);

        const float headerH = 44f;
        var area = new SKRect(bounds.Left, bounds.Top + headerH, bounds.Right, bounds.Bottom);

        var xAxis = Axis.Signed("x", 15, ticksPerUnit: 100);
        var yAxis = Axis.Signed("y", 15, ticksPerUnit: 100);
        var grid  = CSpace.Grid(xAxis, yAxis);
        var map   = CoreAdapter.MapSpace(grid, area);
        new SkiaSpace(map).Render(canvas, pens);

        // Space metadata annotation (top-right of area)
        using var metaFont  = CorePens.GetFont(11f, _fontName, isBold: true);
        using var metaPaint = CorePens.GetBrush(CorePens.RP_Muted);
        canvas.DrawText($"chirality={grid.Chirality:+#;-#}  {grid.Metric}",
            area.Right - 6, area.Top + 14, SKTextAlign.Right, metaFont, metaPaint);
        canvas.DrawText($"affine={grid.AffineType}",
            area.Right - 6, area.Top + 28, SKTextAlign.Right, metaFont, metaPaint);

        var ptA   = grid.Value(3.0, 5.0);
        var ptB   = grid.Value(7.0, 2.0);
        var ptSum = ptA + ptB;  // (10, 7)

        var points = new (CValue val, SKPaint pen, string label)[]
        {
            (ptA,   pens.SegPen0, "A (3, 5)"),
            (ptB,   pens.SegPen1, "B (7, 2)"),
            (ptSum, pens.SegPen2, "A+B (10, 7)"),
        };

        foreach (var (val, pen, label) in points)
        {
            var style = new ElementStyle
            {
                PointPen    = pen,
                PointRadius = 8f,
                ShowVector  = true,
                ShowLabel   = true,
                Label       = label,
            };
            CoreAdapter.AdaptValue(val, map, style).Render(canvas, pens);
        }

        // Dashed parallelogram lines
        var ptAScreen   = map.ToScreen(ptA);
        var ptBScreen   = map.ToScreen(ptB);
        var ptSumScreen = map.ToScreen(ptSum);
        using var dashEffect = SKPathEffect.CreateDash(new float[] { 8f, 5f }, 0f);

        using var dashA = CorePens.GetPen(pens.SegPen0.Color.WithAlpha(140), 2.5f);
        dashA.PathEffect = dashEffect;
        canvas.DrawLine(ptBScreen, ptSumScreen, dashA);

        using var dashB = CorePens.GetPen(pens.SegPen1.Color.WithAlpha(140), 2.5f);
        dashB.PathEffect = dashEffect;
        canvas.DrawLine(ptAScreen, ptSumScreen, dashB);
    }

    // ================================================================
    //  PAGE 3: Boolean Operations on integer intervals
    // ================================================================

    private static void RenderBoolOperations(SKCanvas canvas, CorePens pens, SKRect bounds)
    {
        DrawPageTitle(canvas, "Boolean Operations on Intervals", bounds);

        const float headerH = 44f;
        var area = new SKRect(bounds.Left, bounds.Top + headerH, bounds.Right, bounds.Bottom);

        var space = CSpace.Integer(min: -1, max: 15, ticksPerUnit: 1);
        var map   = CoreAdapter.MapSpace(space, area);
        new SkiaSpace(map).Render(canvas, pens);

        var valALo = space.ValueFromTicks(2);
        var valAHi = space.ValueFromTicks(8);
        var valBLo = space.ValueFromTicks(5);
        var valBHi = space.ValueFromTicks(12);

        var andResult = CMasked.And(space, (valALo, valAHi), (valBLo, valBHi));
        var orResult  = CMasked.Or (space, (valALo, valAHi), (valBLo, valBHi));
        var xorResult = CMasked.Xor(space, (valALo, valAHi), (valBLo, valBHi));

        float lineY = map.ToScreenY(0);
        float yStep = Math.Max(20f, (area.Height - 50f) / 9f);

        DrawDirectedBar(canvas, map, pens.SegPen0.Color, $"A  =  {SegNotation(2, 8)}",   2,  8, lineY - yStep * 3.0f);
        DrawDirectedBar(canvas, map, pens.SegPen1.Color, $"B  =  {SegNotation(5, 12)}",  5, 12, lineY - yStep * 1.8f);
        DrawMaskedResult(canvas, map, pens.SegPen2.Color, "AND", andResult, lineY + yStep * 0.5f);
        DrawMaskedResult(canvas, map, pens.SegPen3.Color, "OR",  orResult,  lineY + yStep * 1.7f);
        DrawMaskedResult(canvas, map, pens.UnotPen.Color, "XOR", xorResult, lineY + yStep * 3.0f);
    }

    // ================================================================
    //  PAGE 4: Nested Frames Demo — values used as sub-spaces
    //
    //  LEFT HALF  — one level of nesting: parent [−100..+100], sub-frame
    //               [−3..+6], and three children with different tension states.
    //
    //  RIGHT HALF — two levels of nesting: root → sub1 → sub2. One child
    //               overflows sub2 only, one overflows sub2+sub1, neither
    //               overflows root — showing the layered tension chain.
    // ================================================================

    private static void RenderNestedFramesDemo(SKCanvas canvas, CorePens pens, SKRect bounds)
    {
        DrawPageTitle(canvas, "Nested Frames: Values as Containers", bounds);

        const float headerH = 44f;
        var area = new SKRect(bounds.Left, bounds.Top + headerH, bounds.Right, bounds.Bottom);

        float midX    = area.Left + area.Width * 0.5f;
        var leftHalf  = new SKRect(area.Left,  area.Top, midX - 8f, area.Bottom);
        var rightHalf = new SKRect(midX + 8f,  area.Top, area.Right, area.Bottom);

        RenderOneLevel(canvas, pens, leftHalf);
        RenderTwoLevel(canvas, pens, rightHalf);
    }

    /// <summary>
    /// Left half: parent segment space [−100,+100]; framing value [−3,+6];
    /// three children with different tension states shown as interval bars.
    /// </summary>
    private static void RenderOneLevel(SKCanvas canvas, CorePens pens, SKRect bounds)
    {
        // Display number line: slightly wider than parent to show overflow
        var displaySpace = CSpace.Integer(-115, 165, ticksPerUnit: 1);
        var map          = CoreAdapter.MapSpace(displaySpace, bounds);
        new SkiaSpace(map).Render(canvas, pens);

        // Subtitle on top of grid
        DrawSubTitle(canvas, "One Level — parent / sub-frame / children", bounds);

        // Build the hierarchy using segment space
        var parent   = CSpace.Segment(-100, 100, ticksPerUnit: 1);
        var frameVal = parent.Value(-3, 6);           // framing value: [−3, +6]
        var sub      = frameVal.ToSubSpace("expected");

        // Three child segment values at different positions
        var child1 = sub.Value(-1,  4);   // both within sub  → sub:ok, parent:ok
        var child2 = sub.Value(-1,  8);   // unit=8 > sub max=6 → sub↑2, parent:ok
        var child3 = sub.Value(-1, 150);  // unit=150 > parent 100 → sub↑144, parent↑50

        float usableH = bounds.Height - 32f;
        float rowH    = usableH / 6f;
        float topY    = bounds.Top + 32f;

        float y0 = topY + rowH * 0.5f;
        float y1 = topY + rowH * 1.5f;
        float y2 = topY + rowH * 2.8f;
        float y3 = topY + rowH * 3.8f;
        float y4 = topY + rowH * 4.8f;

        // Parent bar (uses Love accent, full width)
        DrawDirectedBar(canvas, map, pens.SegPen3.Color,
            $"parent  {SegNotation(-100, 100)}", -100, 100, y0);

        // Sub-frame bar — Gold
        DrawDirectedBar(canvas, map, CorePens.RP_Gold,
            $"sub-frame  {SegNotation(-3, 6)}", -3, 6, y1);

        // Child bars — Teal (ok), Gold (sub overflow), Love (both overflow)
        var l1 = Tension.ComputeLayered(child1);
        var l2 = Tension.ComputeLayered(child2);
        var l3 = Tension.ComputeLayered(child3);

        string TStr(IReadOnlyList<(CSpace S, Tension T)> chain)
        {
            var parts = chain.Select(e =>
                $"{e.S.Name[..Math.Min(3, e.S.Name.Length)]}:{(e.T.IsValid ? "ok" : $"↑{e.T.Total}")}");
            return string.Join("  ", parts);
        }

        DrawDirectedBar(canvas, map, CorePens.RP_Teal,
            $"{SegNotation(child1.GetValue(0), child1.GetValue(1))}   {TStr(l1)}",
            child1.GetValue(0), child1.GetValue(1), y2);

        DrawDirectedBar(canvas, map, CorePens.RP_Gold,
            $"{SegNotation(child2.GetValue(0), child2.GetValue(1))}   {TStr(l2)}",
            child2.GetValue(0), child2.GetValue(1), y3);

        // child3 hi=150 is within the display range of 165; arrowhead shows overflow direction
        DrawDirectedBar(canvas, map, CorePens.RP_Love,
            $"{SegNotation(child3.GetValue(0), child3.GetValue(1))}   {TStr(l3)}",
            child3.GetValue(0), Math.Min(child3.GetValue(1), 163), y4);
    }

    /// <summary>
    /// Right half: two-level nesting root → sub1 → sub2.
    /// Three children show tension at 0, 1, and 2 levels respectively.
    /// </summary>
    private static void RenderTwoLevel(SKCanvas canvas, CorePens pens, SKRect bounds)
    {
        var displaySpace = CSpace.Integer(-22, 22, ticksPerUnit: 1);
        var map          = CoreAdapter.MapSpace(displaySpace, bounds);
        new SkiaSpace(map).Render(canvas, pens);

        // Subtitle on top of grid
        DrawSubTitle(canvas, "Two Levels — layered tension chain", bounds);

        // Three-level hierarchy using segment space
        var root = CSpace.Segment(-20, 20, ticksPerUnit: 1);
        var sub1 = root.Value(-10, 15).ToSubSpace("sub1");   // [−10, +15]
        var sub2 = sub1.Value(-4,   8).ToSubSpace("sub2");   //  [−4, +8]

        float usableH = bounds.Height - 32f;
        float rowH    = usableH / 7f;
        float topY    = bounds.Top + 32f;

        float y0 = topY + rowH * 0.4f;
        float y1 = topY + rowH * 1.3f;
        float y2 = topY + rowH * 2.2f;
        float y3 = topY + rowH * 3.4f;
        float y4 = topY + rowH * 4.4f;
        float y5 = topY + rowH * 5.5f;

        // Frame bars: root (Love), sub1 (Gold), sub2 (Rose)
        DrawDirectedBar(canvas, map, pens.SegPen3.Color,
            $"root  {SegNotation(-20, 20)}", -20, 20, y0);
        DrawDirectedBar(canvas, map, CorePens.RP_Gold,
            $"sub1  {SegNotation(-10, 15)}", -10, 15, y1);
        DrawDirectedBar(canvas, map, CorePens.RP_Rose,
            $"sub2  {SegNotation(-4, 8)}", -4, 8, y2);

        // Children in sub2 with progressively more tension
        var child1 = sub2.Value(0,  5);  //  5 ≤ 8: all ok
        var child2 = sub2.Value(0, 10);  // 10 > 8(sub2): sub2↑2, sub1:ok, root:ok
        var child3 = sub2.Value(0, 17);  // 17 > 8(sub2) AND 17 > 15(sub1): both↑, root:ok

        var l1 = Tension.ComputeLayered(child1);
        var l2 = Tension.ComputeLayered(child2);
        var l3 = Tension.ComputeLayered(child3);

        string TChain(IReadOnlyList<(CSpace S, Tension T)> chain)
        {
            var parts = chain.Select(e =>
                $"{e.S.Name}: {(e.T.IsValid ? "ok" : $"↑{e.T.Total}")}");
            return string.Join("   ", parts);
        }

        DrawDirectedBar(canvas, map, CorePens.RP_Teal,
            $"{SegNotation(child1.GetValue(0), child1.GetValue(1))}   {TChain(l1)}",
            child1.GetValue(0), child1.GetValue(1), y3);
        DrawDirectedBar(canvas, map, CorePens.RP_Gold,
            $"{SegNotation(child2.GetValue(0), child2.GetValue(1))}   {TChain(l2)}",
            child2.GetValue(0), child2.GetValue(1), y4);
        DrawDirectedBar(canvas, map, CorePens.RP_Love,
            $"{SegNotation(child3.GetValue(0), child3.GetValue(1))}   {TChain(l3)}",
            child3.GetValue(0), child3.GetValue(1), y5);

        // Depth annotation at bottom
        using var metaFont  = GetFont(3);
        using var metaPaint = CorePens.GetBrush(CorePens.RP_Muted);
        canvas.DrawText("depth: root=0  sub1=1  sub2=2",
            bounds.Left + 4, bounds.Bottom - 4, SKTextAlign.Left, metaFont, metaPaint);
    }

    // ================================================================
    //  SHARED HELPERS
    // ================================================================

    private static void DrawPageTitle(SKCanvas canvas, string title, SKRect bounds)
    {
        using var font  = CorePens.GetFont(17f, _fontName, isBold: true);
        using var paint = CorePens.GetBrush(CorePens.RP_Gold);
        canvas.DrawText(title, bounds.Left + 8, bounds.Top + 30,
            SKTextAlign.Left, font, paint);
    }

    private static void DrawSubTitle(SKCanvas canvas, string title, SKRect bounds)
    {
        using var font  = GetFont(2);
        using var paint = CorePens.GetBrush(CorePens.RP_Iris);
        canvas.DrawText(title, bounds.MidX, bounds.Top + 18,
            SKTextAlign.Center, font, paint);
    }

    // ── Segment notation helper ───────────────────────────────────────
    /// <summary>
    /// Format [lo, hi] in directed-segment complex notation:
    ///   value = hi − lo·i  →  (−lo·i + hi)
    /// Examples:  [1,4]→"(-i + 4)"   [2,7]→"(-2i + 7)"   [-4,-1]→"(4i - 1)"
    /// </summary>
    private static string SegNotation(double lo, double hi)
    {
        double c = -lo;   // coefficient of i in the notation
        if (Math.Abs(c) < 1e-9)
            return $"({hi:0.##})";      // degenerate: pure-real point

        string iStr;
        if      (Math.Abs(c -  1.0) < 1e-9) iStr = "i";
        else if (Math.Abs(c + 1.0) < 1e-9) iStr = "-i";
        else                                 iStr = $"{c:0.##}i";

        string hStr = hi >= -1e-9 ? $"+ {hi:0.##}" : $"- {-hi:0.##}";
        return $"({iStr} {hStr})";
    }

    // ── Directed interval bar ─────────────────────────────────────────
    /// <summary>
    /// Draw a directed segment bar with Rosé Pine style:
    ///   • Hollow ring at the <b>start</b> (lo / unot / imaginary component)
    ///   • Filled arrowhead at the <b>end</b> (hi / unit / real component)
    ///   • Label in the segment color, centered above
    /// </summary>
    private static void DrawDirectedBar(SKCanvas canvas, CoordinateMap map,
        SKColor color, string label, double lo, double hi, float y)
    {
        float sx = map.ToScreenX(lo);   // start = unot component → ring dot
        float ex = map.ToScreenX(hi);   // end   = unit  component → arrowhead

        const float thick   = 8f;
        const float dotR    = thick * 0.75f;
        const float arrowSz = 13f;
        const float capH    = dotR + 4f;

        float dir = ex >= sx ? 1f : -1f;

        // Bar — stops short of the arrowhead tip so they don't bleed together
        float barEnd = ex - dir * (arrowSz - 1f);
        using var barPen = CorePens.GetPen(color, thick, SKStrokeCap.Butt);
        canvas.DrawLine(sx + dir * dotR, y, barEnd, y, barPen);

        // Hollow ring at start (unot / lo end) — background-filled circle with
        // a colored ring, so the bar doesn't bleed through the center
        using var dotFill = CorePens.GetBrush(CorePens.RP_Background);
        using var dotRing = CorePens.GetPen(color, 2.5f);
        canvas.DrawCircle(sx, y, dotR, dotFill);
        canvas.DrawCircle(sx, y, dotR, dotRing);

        // Arrowhead at end (unit / hi end)
        float tipX   = ex + dir * 1f;          // tiny overshoot so tip is sharp
        float baseX  = ex - dir * arrowSz;
        float halfW  = arrowSz * 0.4f;
        var arrowPath = new SKPath();
        arrowPath.MoveTo(tipX, y);
        arrowPath.LineTo(baseX, y - halfW);
        arrowPath.LineTo(baseX, y + halfW);
        arrowPath.Close();
        using var arrowFill = CorePens.GetBrush(color);
        canvas.DrawPath(arrowPath, arrowFill);

        // Label centered above the bar
        using var font  = GetFont(1);
        using var paint = CorePens.GetBrush(color);
        float midX = (sx + ex) / 2f;
        canvas.DrawText(label, midX, y - capH - 5f, SKTextAlign.Center, font, paint);
    }

    private static void DrawMaskedResult(SKCanvas canvas, CoordinateMap map,
        SKColor color, string opLabel, CMasked result, float y)
    {
        bool isFirst = true;
        foreach (var (low, high) in result.Regions)
        {
            double lo = low.GetValue(0);
            double hi = high.GetValue(0);
            string label = isFirst
                ? $"{opLabel}  =  {SegNotation(lo, hi)}"
                : SegNotation(lo, hi);
            DrawDirectedBar(canvas, map, color, label, lo, hi, y);
            isFirst = false;
        }
    }

    private static SKFont GetFont(int priority) => priority switch
    {
        0 => CorePens.GetFont(18f, _fontName, isBold: true),
        1 => CorePens.GetFont(14f, _fontName, isBold: true),
        2 => CorePens.GetFont(12f, _fontName, isBold: true),
        _ => CorePens.GetFont(9f,  _fontName, isBold: true),
    };

    // ================================================================
    //  PAGE 5: Twin-Square Space — 3D isometric demo
    //
    //  Two 2D "square" planes joined at the origin.  One is the XY
    //  plane; the other is rotated 90° relative to it, sharing only
    //  the origin point (no shared edge).
    //
    //  Plane A (Gold):  a₁=(1,0,0),  a₂=(0,1,0)         — the XY plane
    //  Plane B (Iris):  b₁=(−½,−½,+1/√2), b₂=(−½,−½,−1/√2) — tilted plane
    //
    //  Key angle properties:
    //    Within-plane: a₁·a₂=0  b₁·b₂=0  (90° — genuine squares)
    //    Cross-plane:  aᵢ·bⱼ=−½           (120° — all 4 pairs)
    //    Equilibrium:  a₁+a₂+b₁+b₂=(0,0,0)
    //
    //  A value has 8 components (4 lo/hi pairs).  The 8 "extreme points"
    //  in R³ form a 16-edge polytope.  A "regular" value has all extents
    //  equal → all 8 points equidistant from the origin.
    //
    //  Visualization: Plane A shown as translucent Gold quad,
    //  Plane B as translucent Iris quad; wireframe shapes use depth cuing
    //  (front edges full opacity, back edges at 35%).
    // ================================================================

    private static void RenderTwinSquareDemo(SKCanvas canvas, CorePens pens, SKRect bounds)
    {
        DrawPageTitle(canvas, "Twin-Square Space", bounds);

        const float headerH = 44f;
        var area = new SKRect(bounds.Left, bounds.Top + headerH, bounds.Right, bounds.Bottom);

        // Split: left 64% = 3D isometric view, right 36% = annotation panel
        float splitX = area.Left + area.Width * 0.64f;
        var view3D   = new SKRect(area.Left,   area.Top, splitX - 4f, area.Bottom);
        var panel    = new SKRect(splitX + 4f, area.Top, area.Right,  area.Bottom);

        // ── Build values ─────────────────────────────────────────────
        // range=8 keeps all shapes comfortably inside the clip frame
        var ts    = CSpace.TwinSquare(range: 8.0, ticksPerUnit: 100);

        var vReg  = ts.Value(-5,  5, -5,  5, -5,   5, -5,  5);  // regular: all extents ±5
        var vStr  = ts.Value(-3,  7, -5,  5, -5,   5, -5,  5);  // a₁ stretched (lo=−3,hi=7)
        var vTens = ts.Value(-5,  5, -5,  5, -5, 9.5, -5,  5);  // hi_b1=9.5 > range 8 → tension

        var tTens = Tension.Compute(vTens);
        var inter = TwinSquareGeometry.Intersect(vReg, vStr);

        // ── Isometric projection (viewer at +X+Y+Z, right-hand standard) ─
        //   sx = cx + (wx − wz)·cos30°·sc
        //   sy = cy − wy·sc   − (wx + wz)·sin30°·sc
        float cx    = view3D.Left + view3D.Width  * 0.55f;
        float cy    = view3D.Top  + view3D.Height * 0.50f;
        float scale = Math.Min(view3D.Width, view3D.Height) / 22f;

        static SKPoint Iso(float cx, float cy, float sc, double wx, double wy, double wz)
        {
            const double C30 = 0.8660254;   // cos 30°
            const double S30 = 0.5;         // sin 30°
            return new SKPoint(
                (float)(cx + (wx - wz) * C30 * sc),
                (float)(cy - wy * sc - (wx + wz) * S30 * sc));
        }

        // ── Translucent background planes ────────────────────────────
        // Drawn first so they sit behind the wireframe shapes.
        DrawTwinPlane(canvas, cx, cy, scale, Iso, isPlaneA: true);   // Plane A: Gold XY
        DrawTwinPlane(canvas, cx, cy, scale, Iso, isPlaneA: false);  // Plane B: Iris tilted

        // ── Four labeled axis arrows ─────────────────────────────────
        DrawTwinAxes(canvas, cx, cy, scale, Iso);

        // ── Wireframe shapes (drawn front-to-back by color intent) ───
        DrawTwinSquareShape(canvas, cx, cy, scale, Iso, vReg,  CorePens.RP_Teal, showTick: false);
        DrawTwinSquareShape(canvas, cx, cy, scale, Iso, vStr,  CorePens.RP_Gold, showTick: false);
        DrawTwinSquareShape(canvas, cx, cy, scale, Iso, vTens, CorePens.RP_Love, showTick: true);

        if (inter != null)
            DrawTwinSquareShape(canvas, cx, cy, scale, Iso, inter, CorePens.RP_Pine, showTick: false);

        // ── Clipping frame ───────────────────────────────────────────
        using var clipPen = CorePens.GetPen(CorePens.RP_Muted.WithAlpha(40), 1f);
        canvas.DrawRect(view3D, clipPen);

        // ── Annotation panel ─────────────────────────────────────────
        RenderTwinSquarePanel(canvas, pens, panel, vReg, vStr, vTens, tTens, inter);
    }

    /// <summary>
    /// Draw one of the two background planes as a translucent filled quad.
    ///
    /// Plane A is the XY plane (z=0), shown as a Gold square:
    ///   corners at (±ext, ±ext, 0)
    ///
    /// Plane B is the b₁b₂ plane, shown as an Iris square.  Its four
    /// corners are ±ext·B1 and ±ext·B2 in R³:
    ///   +B1·ext = (−ext/2, −ext/2, +ext/√2)   ← back-left-up
    ///   +B2·ext = (−ext/2, −ext/2, −ext/√2)   ← back-left-down
    ///   −B1·ext = (+ext/2, +ext/2, −ext/√2)   ← front-right-down
    ///   −B2·ext = (+ext/2, +ext/2, +ext/√2)   ← front-right-up
    /// These form a square tilted 90° to Plane A, touching only at the origin.
    /// </summary>
    private static void DrawTwinPlane(SKCanvas canvas, float cx, float cy, float scale,
        IsoProjector project, bool isPlaneA)
    {
        const float  ext = 7.2f;
        const double s2  = 0.7071067811865476;   // 1/√2 = |z-component of B1 and B2|

        SKPoint[] corners = isPlaneA
            // Plane A: a square in the z=0 plane, corners (±ext, ±ext, 0)
            ? new[]
            {
                project(cx, cy, scale,  ext,  ext, 0),
                project(cx, cy, scale,  ext, -ext, 0),
                project(cx, cy, scale, -ext, -ext, 0),
                project(cx, cy, scale, -ext,  ext, 0),
            }
            // Plane B: corners are ±ext·B1 and ±ext·B2 (winding: +B1, +B2, −B1, −B2)
            : new[]
            {
                project(cx, cy, scale, -ext*0.5, -ext*0.5,  ext*s2),   // +B1·ext
                project(cx, cy, scale, -ext*0.5, -ext*0.5, -ext*s2),   // +B2·ext
                project(cx, cy, scale,  ext*0.5,  ext*0.5, -ext*s2),   // −B1·ext
                project(cx, cy, scale,  ext*0.5,  ext*0.5,  ext*s2),   // −B2·ext
            };

        SKColor fillCol = isPlaneA ? CorePens.RP_Gold.WithAlpha(20) : CorePens.RP_Iris.WithAlpha(20);
        SKColor edgeCol = isPlaneA ? CorePens.RP_Gold.WithAlpha(85) : CorePens.RP_Iris.WithAlpha(85);

        using var fill = CorePens.GetBrush(fillCol);
        using var edge = CorePens.GetPen(edgeCol, 1f);

        var path = new SKPath();
        path.MoveTo(corners[0]);
        for (int i = 1; i < corners.Length; i++) path.LineTo(corners[i]);
        path.Close();

        canvas.DrawPath(path, fill);
        canvas.DrawPath(path, edge);

        // Label near a visible corner:
        //   Plane A: corner[0] = (+ext,+ext,0) → upper-right in isometric
        //   Plane B: corner[2] = (−B1·ext) = (+ext/2,+ext/2,−ext/√2) → right of centre
        int lc   = isPlaneA ? 0 : 2;
        float lx = corners[lc].X + 4;
        float ly = corners[lc].Y - 6;
        using var font  = CorePens.GetFont(9f, "Outfit", isBold: true);
        using var paint = CorePens.GetBrush(edgeCol.WithAlpha(200));
        canvas.DrawText(isPlaneA ? "Plane A  (a₁a₂)" : "Plane B  (b₁b₂)",
            lx, ly, SKTextAlign.Left, font, paint);
    }

    /// <summary>
    /// Draw the four labeled axis arrows for the Twin-Square space.
    ///   a₁, a₂ (Gold — Plane A): along +X and +Y
    ///   b₁, b₂ (Iris — Plane B): into the (−X,−Y,±Z) octants
    /// </summary>
    private static void DrawTwinAxes(SKCanvas canvas, float cx, float cy, float scale,
        IsoProjector project)
    {
        const float  axLen = 7.0f;
        const double s2    = 0.7071067811865476;   // 1/√2

        var origin = project(cx, cy, scale, 0, 0, 0);

        // (screen endpoint, color, label, x-label-offset)
        var axes = new (SKPoint end, SKColor color, string name, float xOff)[]
        {
            (project(cx, cy, scale,  axLen,          0,          0),    CorePens.RP_Gold, "a₁",  3f),
            (project(cx, cy, scale,      0,      axLen,          0),    CorePens.RP_Gold, "a₂",  3f),
            (project(cx, cy, scale, -axLen*0.5, -axLen*0.5,  axLen*s2), CorePens.RP_Iris, "b₁", -22f),
            (project(cx, cy, scale, -axLen*0.5, -axLen*0.5, -axLen*s2), CorePens.RP_Iris, "b₂", -22f),
        };

        using var font = CorePens.GetFont(10f, "Outfit", isBold: true);
        foreach (var (end, color, name, xOff) in axes)
        {
            using var axPen    = CorePens.GetPen(color.WithAlpha(160), 1.4f);
            using var lblPaint = CorePens.GetBrush(color.WithAlpha(220));
            canvas.DrawLine(origin, end, axPen);
            canvas.DrawText(name, end.X + xOff, end.Y - 3, SKTextAlign.Left, font, lblPaint);
        }

        // Origin dot
        using var dotBrush = CorePens.GetBrush(CorePens.RP_Text.WithAlpha(120));
        canvas.DrawCircle(origin, 3f, dotBrush);
    }

    // Delegate alias so we don't repeat the lengthy signature everywhere.
    private delegate SKPoint IsoProjector(float cx, float cy, float sc,
                                           double wx, double wy, double wz);

    /// <summary>
    /// Draw the 16-edge wireframe of a Twin-Square value in isometric projection.
    ///
    /// Vertex positions come from <see cref="TwinSquareGeometry.GetExtreme8"/>.
    /// The 16 edge pairs come from <see cref="TwinSquareGeometry.Edges"/>.
    /// Depth cuing via <see cref="TwinSquareGeometry.FrontVertices"/>:
    ///   front vertices (and edges touching them) = full opacity + larger dots,
    ///   back vertices = 35% opacity + smaller dots.
    /// </summary>
    private static void DrawTwinSquareShape(SKCanvas canvas, float cx, float cy, float scale,
        IsoProjector project, CValue v, SKColor color, bool showTick)
    {
        // Project the 8 extreme points into screen space
        var pts3 = TwinSquareGeometry.GetExtreme8(v);
        var pts  = pts3.Select(t => project(cx, cy, scale, t.x, t.y, t.z)).ToArray();

        using var frontPen = CorePens.GetPen(color, 2.5f);
        using var backPen  = CorePens.GetPen(color.WithAlpha((byte)(color.Alpha * 0.35f)), 1.5f);

        foreach (var (a, b) in TwinSquareGeometry.Edges)
        {
            bool isFront = TwinSquareGeometry.FrontVertices.Contains(a)
                        || TwinSquareGeometry.FrontVertices.Contains(b);
            canvas.DrawLine(pts[a], pts[b], isFront ? frontPen : backPen);
        }

        // Vertex dots — front vertices get a slightly larger, brighter dot
        for (int i = 0; i < 8; i++)
        {
            bool isFront = TwinSquareGeometry.FrontVertices.Contains(i);
            using var dot = CorePens.GetBrush(
                isFront ? color : color.WithAlpha((byte)(color.Alpha * 0.45f)));
            canvas.DrawCircle(pts[i], isFront ? 3.5f : 2.5f, dot);
        }

        // Optional tension annotation near the overflowing vertex
        if (showTick)
        {
            var t = Tension.Compute(v);
            if (!t.IsValid)
            {
                // For the tension demo, hi_b1 is over the bound.
                // pts[4] is the hi_b1 extreme point: hi_b1 · B1 = a back-left-up vertex.
                var tipPt = pts[4];
                using var font  = CorePens.GetFont(9f, "Outfit", isBold: true);
                using var paint = CorePens.GetBrush(CorePens.RP_Love);
                canvas.DrawText($"↑{t.Total / v.Space.BasisTicks:0.#}",
                    tipPt.X + 5, tipPt.Y, SKTextAlign.Left, font, paint);
            }
        }
    }

    /// <summary>Right-side annotation panel with geometry description and value info.</summary>
    private static void RenderTwinSquarePanel(SKCanvas canvas, CorePens pens, SKRect panel,
        CValue vReg, CValue vStr, CValue vTens, Tension tTens, CValue? inter)
    {
        float x = panel.Left + 10;
        float y = panel.Top  + 18;
        const float lineH = 18f;

        void Heading(string text, SKColor color)
        {
            using var f = CorePens.GetFont(11f, "Outfit", isBold: true);
            using var p = CorePens.GetBrush(color);
            canvas.DrawText(text, x, y, SKTextAlign.Left, f, p);
            y += lineH * 1.1f;
        }
        void Line(string text, SKColor color)
        {
            using var f = CorePens.GetFont(10f, "Outfit", isBold: false);
            using var p = CorePens.GetBrush(color);
            canvas.DrawText(text, x + 8, y, SKTextAlign.Left, f, p);
            y += lineH;
        }
        void Gap() => y += lineH * 0.5f;

        // ── Geometry summary ─────────────────────────────────────────
        Heading("Twin-Square geometry", CorePens.RP_Text);
        Line("Plane A (Gold): a₁=(1,0,0)  a₂=(0,1,0)",  CorePens.RP_Gold);
        Line("Plane B (Iris): b₁=(−½,−½,+1/√2)",        CorePens.RP_Iris);
        Line("               b₂=(−½,−½,−1/√2)",         CorePens.RP_Iris);

        Gap();
        Heading("Angles", CorePens.RP_Rose);
        Line("Within planes: a₁·a₂=0  b₁·b₂=0  (90°)",  CorePens.RP_Muted);
        Line("Cross-plane:   aᵢ·bⱼ=−½           (120°)", CorePens.RP_Muted);
        Line("Equilibrium:   a₁+a₂+b₁+b₂=(0,0,0)",      CorePens.RP_Muted);

        Gap();

        // ── Regular value (Teal) ──────────────────────────────────────
        Heading("Teal — regular (all ±5)", CorePens.RP_Teal);
        var (lA1, hA1, lA2, hA2) = TwinSquareGeometry.PlaneAExtents(vReg);
        Line($"Plane A: a₁{SegNotation(lA1,hA1)}  a₂{SegNotation(lA2,hA2)}", CorePens.RP_Teal);
        var (lB1, hB1, lB2, hB2) = TwinSquareGeometry.PlaneBExtents(vReg);
        Line($"Plane B: b₁{SegNotation(lB1,hB1)}  b₂{SegNotation(lB2,hB2)}", CorePens.RP_Teal);
        Line("✓ valid — all 8 vertices equidistant",                           CorePens.RP_Teal);

        Gap();

        // ── Stretched value (Gold) ────────────────────────────────────
        Heading("Gold — a₁ stretched", CorePens.RP_Gold);
        (lA1, hA1, lA2, hA2) = TwinSquareGeometry.PlaneAExtents(vStr);
        Line($"a₁: {SegNotation(lA1,hA1)}  (asymmetric extent)", CorePens.RP_Gold);
        Line("✓ valid — within bounds",                           CorePens.RP_Gold);

        Gap();

        // ── Tension value (Love) ──────────────────────────────────────
        Heading("Love — b₁ in tension", CorePens.RP_Love);
        (lB1, hB1, lB2, hB2) = TwinSquareGeometry.PlaneBExtents(vTens);
        Line($"hi_b1 = {hB1:0.#}  (range = 8.0)",                       CorePens.RP_Love);
        Line($"tension ↑{tTens.Total / vTens.Space.BasisTicks:0.#} on b₁", CorePens.RP_Love);

        Gap();

        // ── Intersection (Pine) ───────────────────────────────────────
        Heading("Pine — Teal ∩ Gold", CorePens.RP_Pine);
        if (inter != null)
        {
            bool reg = TwinSquareGeometry.IsRegular(inter);
            Line(reg ? "perfect → regular" : "partial overlap", CorePens.RP_Pine);
            (lA1, hA1, _, _) = TwinSquareGeometry.PlaneAExtents(inter);
            Line($"a₁: {SegNotation(lA1, hA1)}", CorePens.RP_Pine);
        }
        else
        {
            Line("no intersection (tension state)", CorePens.RP_Love);
        }
    }
}
