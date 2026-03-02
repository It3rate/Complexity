namespace Spaces.Tests.CoreTests;

[TestClass]
public class SpaceValueTests
{
    private Space _seg = null!;

    [TestInitialize]
    public void SetUp()
    {
        _seg = Space.Segment(100.0, 100L);  // range [-100, +100] units, 100 ticks/unit
    }

    // ── Component access ───────────────────────────────────────────────────

    [TestMethod]
    public void GetComponent_EvenIndex_ReturnsLo()
    {
        var v = _seg.Value(-2.0, 4.0);         // lo=-200, hi=400
        Assert.AreEqual(-200L, v.GetComponent(0));   // component 0 = lo
    }

    [TestMethod]
    public void GetComponent_OddIndex_ReturnsHi()
    {
        var v = _seg.Value(-2.0, 4.0);
        Assert.AreEqual(400L, v.GetComponent(1));    // component 1 = hi
    }

    [TestMethod]
    public void GetLoUnits_GetHiUnits_ConvertBackToUnits()
    {
        var v = _seg.Value(-3.5, 7.0);
        Assert.AreEqual(-3.5, v.GetLoUnits(0), delta: 1e-10);
        Assert.AreEqual(7.0,  v.GetHiUnits(0), delta: 1e-10);
    }

    // ── Addition ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Addition_ComponentWise()
    {
        var a = _seg.Value(-1.0, 2.0);   // lo=-100, hi=200
        var b = _seg.Value(3.0, 4.0);    // lo=300,  hi=400
        var r = a + b;
        Assert.AreEqual(200L,  r.GetLo(0));   // -100 + 300 = 200
        Assert.AreEqual(600L,  r.GetHi(0));   //  200 + 400 = 600
    }

    // ── Subtraction ────────────────────────────────────────────────────────

    [TestMethod]
    public void Subtraction_ComponentWise()
    {
        var a = _seg.Value(5.0, 8.0);    // lo=500, hi=800
        var b = _seg.Value(1.0, 3.0);    // lo=100, hi=300
        var r = a - b;
        Assert.AreEqual(400L, r.GetLo(0));   // 500 - 100 = 400
        Assert.AreEqual(500L, r.GetHi(0));   // 800 - 300 = 500
    }

    // ── Negation ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Negation_FlipsAllComponentSigns()
    {
        var v = _seg.Value(-2.0, 4.0);   // lo=-200, hi=400
        var n = -v;
        Assert.AreEqual(200L,  n.GetLo(0));
        Assert.AreEqual(-400L, n.GetHi(0));
    }

    // ── Multiplication ─────────────────────────────────────────────────────

    [TestMethod]
    public void Multiply_UnitByUnit_GivesUnit()
    {
        // unit = (0, 1) in units → (0, 100) in ticks
        // (0 + 1·r) × (0 + 1·r) = 1·r  (1 × 1 − 0 × 0 = 1 for r-component)
        var one = _seg.Value(0.0, 1.0);
        var r   = one * one;
        Assert.AreEqual(0L,   r.GetLo(0));   // i-component
        Assert.AreEqual(100L, r.GetHi(0));   // r-component = 1 unit = 100 ticks
    }

    [TestMethod]
    public void Multiply_ISquaredEqualsNegativeOne()
    {
        // pure imaginary unit: (1, 0) in units → (100, 0) ticks
        // (1i) × (1i) = i² = −1  →  result_r = −1, result_i = 0
        var i = _seg.Value(1.0, 0.0);        // Focal(lo=100, hi=0)
        var r = i * i;
        Assert.AreEqual(0L,    r.GetLo(0));  // i-component = 0
        Assert.AreEqual(-100L, r.GetHi(0)); // r-component = -1 unit = -100 ticks
    }

    [TestMethod]
    public void Multiply_RealByReal_ScalesCorrectly()
    {
        // 2 × 3 = 6  (pure real values)
        var two   = _seg.Value(0.0, 2.0);   // (0, 200) ticks
        var three = _seg.Value(0.0, 3.0);   // (0, 300) ticks
        var r     = two * three;
        Assert.AreEqual(0L,   r.GetLo(0));
        Assert.AreEqual(600L, r.GetHi(0));  // 6 units = 600 ticks
    }

    [TestMethod]
    public void Multiply_ComplexProduct_IsCorrect()
    {
        // A = 2i + 3r → Focal(lo=200, hi=300)
        // B = 4i + 5r → Focal(lo=400, hi=500)
        //
        // result_r = a_r·b_r − a_i·b_i = 3·5 − 2·4 = 15 − 8 = 7
        // result_i = a_r·b_i + a_i·b_r = 3·4 + 2·5 = 12 + 10 = 22
        //
        // In ticks (tpu=100):
        //   acc[hi] = 300×500 − 200×400 = 70000  → / 100 = 700
        //   acc[lo] = 300×400 + 200×500 = 220000 → / 100 = 2200
        var a = _seg.Value(2.0, 3.0);
        var b = _seg.Value(4.0, 5.0);
        var r = a * b;
        Assert.AreEqual(2200L, r.GetLo(0));  // i-component = 22 units
        Assert.AreEqual(700L,  r.GetHi(0));  // r-component = 7 units
    }

    // ── Grid multiplication ────────────────────────────────────────────────

    [TestMethod]
    public void Multiply_GridSpace_EachAxisIndependent()
    {
        var xAx  = Axis.Signed(100.0, 100L);
        var yAx  = Axis.Signed(100.0, 100L);
        var grid = Space.Grid(xAx, yAx);

        // X: 2r × 3r = 6r  (pure real)
        // Y: 1i × 1i = −1r  (i² = −1)
        var a = grid.Value(0.0, 2.0,  1.0, 0.0);   // x=(0,200) y=(100,0)
        var b = grid.Value(0.0, 3.0,  1.0, 0.0);   // x=(0,300) y=(100,0)
        var r = a * b;

        Assert.AreEqual(0L,    r.GetLo(0));   // x i-component = 0
        Assert.AreEqual(600L,  r.GetHi(0));   // x r-component = 6 units
        Assert.AreEqual(0L,    r.GetLo(1));   // y i-component = 0
        Assert.AreEqual(-100L, r.GetHi(1));   // y r-component = -1 unit
    }

    // ── Overflow / tension ─────────────────────────────────────────────────

    [TestMethod]
    public void InBounds_WhenAllComponentsWithinRange()
    {
        var v = _seg.Value(-50.0, 50.0);  // within [-100, +100]
        Assert.IsTrue(v.InBounds);
        Assert.AreEqual(0L, v.TotalOverflow());
    }

    [TestMethod]
    public void InBounds_FalseWhenAnyComponentExceedsRange()
    {
        var v = _seg.Value(-50.0, 150.0);  // hi = 150 > 100 (out of range)
        Assert.IsFalse(v.InBounds);
    }

    [TestMethod]
    public void TotalOverflow_ReturnsExcessTicks()
    {
        // lo = -120 units = -12000 ticks, min is -10000 → underflow = 2000
        // hi =  110 units =  11000 ticks, max is  10000 → overflow  = 1000
        var farOut = Space.Segment(100.0, 100L);
        var v      = farOut.Value(-120.0, 110.0);
        Assert.AreEqual(3000L, v.TotalOverflow());
    }

    // ── Cross-space guard ──────────────────────────────────────────────────

    [TestMethod]
    public void Add_DifferentSpaces_Throws()
    {
        var s1 = Space.Segment(10.0, 100L);
        var s2 = Space.Segment(10.0, 100L);  // distinct instance
        var v1 = s1.Value(0.0, 1.0);
        var v2 = s2.Value(0.0, 1.0);
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = v1 + v2; });
    }

    [TestMethod]
    public void Multiply_DifferentSpaces_Throws()
    {
        var s1 = Space.Segment(10.0, 100L);
        var s2 = Space.Segment(10.0, 100L);
        var v1 = s1.Value(0.0, 1.0);
        var v2 = s2.Value(0.0, 1.0);
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = v1 * v2; });
    }
}
