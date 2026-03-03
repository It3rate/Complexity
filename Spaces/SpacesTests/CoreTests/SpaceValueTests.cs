namespace Spaces.Tests.CoreTests;

/// <summary>
/// Tests for SpaceValue arithmetic, overflow, and component access.
///
/// VALUE MAGNITUDE CONVENTION:
///   All values created via s.Value() store NON-NEGATIVE MAGNITUDES:
///     Lo = distance in LEFT  (i) direction  ≥ 0
///     Hi = distance in RIGHT (r) direction  ≥ 0
///
///   Space.Value() abs() both inputs before converting to ticks.
///
/// ARITHMETIC TENSION:
///   After arithmetic operations, components MAY be negative — this is valid.
///   Negative Lo means "the left extent went past zero into the right direction."
///   These negative values contribute to TotalOverflow() as tension information.
///
/// OVERFLOW SEMANTICS:
///   Valid range for a magnitude: [0, maxMagnitude].
///   MagnitudeOverflow(ticks, max):
///     ticks &lt; 0:          excess = |ticks| (tension)
///     ticks &gt; max:        excess = ticks − max
///     otherwise:          0
/// </summary>
[TestClass]
public class SpaceValueTests
{
    private Space _seg = null!;

    [TestInitialize]
    public void SetUp()
    {
        _seg = Space.Segment(100.0, 100L);  // MaxLoTick = MaxHiTick = 10000 (100 units)
    }

    // ── Component access ───────────────────────────────────────────────────

    [TestMethod]
    public void GetComponent_EvenIndex_ReturnsLo()
    {
        // Value() abs both inputs; lo input is treated as a magnitude
        var v = _seg.Value(2.0, 4.0);          // lo=200, hi=400
        Assert.AreEqual(200L, v.GetComponent(0));
    }

    [TestMethod]
    public void GetComponent_OddIndex_ReturnsHi()
    {
        var v = _seg.Value(2.0, 4.0);          // lo=200, hi=400
        Assert.AreEqual(400L, v.GetComponent(1));
    }

    [TestMethod]
    public void GetLoUnits_GetHiUnits_ConvertMagnitudeBackToUnits()
    {
        // Both lo and hi are stored as positive magnitudes.
        // GetLoUnits returns lo_ticks / tpu = 350/100 = 3.5 (positive).
        var v = _seg.Value(3.5, 7.0);          // lo=350, hi=700 ticks
        Assert.AreEqual(3.5, v.GetLoUnits(0), delta: 1e-10);
        Assert.AreEqual(7.0, v.GetHiUnits(0), delta: 1e-10);
    }

    [TestMethod]
    public void GetLoUnits_NegativeInput_StoredAsPositiveMagnitude()
    {
        // -3.5 for lo → abs → stored as 350 ticks → GetLoUnits returns +3.5
        var v = _seg.Value(-3.5, 7.0);
        Assert.AreEqual(3.5, v.GetLoUnits(0), delta: 1e-10);
    }

    // ── Addition ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Addition_ComponentWise_OnMagnitudes()
    {
        // Adding two magnitude focals: each component sums independently.
        var a = _seg.Value(1.0, 2.0);   // lo=100, hi=200
        var b = _seg.Value(3.0, 4.0);   // lo=300, hi=400
        var r = a + b;
        Assert.AreEqual(400L, r.GetLo(0));   // 100 + 300 = 400
        Assert.AreEqual(600L, r.GetHi(0));   // 200 + 400 = 600
    }

    // ── Subtraction ────────────────────────────────────────────────────────

    [TestMethod]
    public void Subtraction_ComponentWise()
    {
        var a = _seg.Value(5.0, 8.0);   // lo=500, hi=800
        var b = _seg.Value(1.0, 3.0);   // lo=100, hi=300
        var r = a - b;
        Assert.AreEqual(400L, r.GetLo(0));   // 500 - 100 = 400
        Assert.AreEqual(500L, r.GetHi(0));   // 800 - 300 = 500
    }

    [TestMethod]
    public void Subtraction_LargerFromSmaller_ProducesNegativeLo_Tension()
    {
        // When b.lo > a.lo, result.lo < 0 — this is "tension."
        var a = _seg.Value(3.0, 0.0);   // lo=300
        var b = _seg.Value(8.0, 0.0);   // lo=800
        var r = a - b;
        Assert.AreEqual(-500L, r.GetLo(0));   // 300 - 800 = -500 (tension)
    }

    // ── Negation ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Negation_FlipsAllComponentSigns()
    {
        // Negating a positive-magnitude focal gives negative lo and hi.
        // This is valid arithmetic tension.
        var v = _seg.Value(2.0, 4.0);   // lo=200, hi=400 (magnitudes)
        var n = -v;
        Assert.AreEqual(-200L, n.GetLo(0));
        Assert.AreEqual(-400L, n.GetHi(0));
    }

    // ── Multiplication ─────────────────────────────────────────────────────

    [TestMethod]
    public void Multiply_UnitByUnit_GivesUnit()
    {
        // (0i + 1r) × (0i + 1r) = 0i + 1r
        var one = _seg.Value(0.0, 1.0);     // lo=0, hi=100 ticks
        var r   = one * one;
        Assert.AreEqual(0L,   r.GetLo(0));
        Assert.AreEqual(100L, r.GetHi(0));  // 1 unit in r direction
    }

    [TestMethod]
    public void Multiply_ISquaredEqualsNegativeOne()
    {
        // (1i + 0r) × (1i + 0r) = i² = −1r (negative real)
        // lo=100, hi=0:  acc[hi] = 0×0 − 100×100 = −10000 → / 100 = −100 ticks = −1r
        var i = _seg.Value(1.0, 0.0);       // lo=100, hi=0
        var r = i * i;
        Assert.AreEqual(0L,    r.GetLo(0));
        Assert.AreEqual(-100L, r.GetHi(0)); // r = −1 unit (goes left of origin)
    }

    [TestMethod]
    public void Multiply_RealByReal_ScalesCorrectly()
    {
        // (0i + 2r) × (0i + 3r) = 0i + 6r
        var two   = _seg.Value(0.0, 2.0);   // lo=0, hi=200
        var three = _seg.Value(0.0, 3.0);   // lo=0, hi=300
        var r     = two * three;
        Assert.AreEqual(0L,   r.GetLo(0));
        Assert.AreEqual(600L, r.GetHi(0));  // 6 units
    }

    [TestMethod]
    public void Multiply_ComplexProduct_IsCorrect()
    {
        // A = (2i + 3r) → lo=200, hi=300 ticks
        // B = (4i + 5r) → lo=400, hi=500 ticks
        //
        // Complex multiplication:
        //   result_r = a_r×b_r − a_i×b_i = 3×5 − 2×4 = 15 − 8 = 7
        //   result_i = a_r×b_i + a_i×b_r = 3×4 + 2×5 = 12 + 10 = 22
        //
        // In ticks (tpu=100):
        //   acc[hi] = 300×500 − 200×400 = 70000  → / 100 =  700 ticks = 7r
        //   acc[lo] = 300×400 + 200×500 = 220000 → / 100 = 2200 ticks = 22i
        var a = _seg.Value(2.0, 3.0);
        var b = _seg.Value(4.0, 5.0);
        var r = a * b;
        Assert.AreEqual(2200L, r.GetLo(0));   // i-component = 22 units
        Assert.AreEqual(700L,  r.GetHi(0));   // r-component = 7 units
    }

    // ── Grid multiplication ────────────────────────────────────────────────

    [TestMethod]
    public void Multiply_GridSpace_EachAxisIndependent()
    {
        var xAx  = Axis.Signed(100.0, 100L);
        var yAx  = Axis.Signed(100.0, 100L);
        var grid = Space.Grid(xAx, yAx);

        // X: (0i + 2r) × (0i + 3r) = 0i + 6r
        // Y: (1i + 0r) × (1i + 0r) = i² = −1r
        var a = grid.Value(0.0, 2.0,  1.0, 0.0);   // x=(0,200) y=(100,0)
        var b = grid.Value(0.0, 3.0,  1.0, 0.0);   // x=(0,300) y=(100,0)
        var r = a * b;

        Assert.AreEqual(0L,    r.GetLo(0));   // x i-component = 0
        Assert.AreEqual(600L,  r.GetHi(0));   // x r-component = 6 units
        Assert.AreEqual(0L,    r.GetLo(1));   // y i-component = 0
        Assert.AreEqual(-100L, r.GetHi(1));   // y r-component = −1 unit (i² = −1)
    }

    // ── Overflow / tension ─────────────────────────────────────────────────

    [TestMethod]
    public void InBounds_WhenAllMagnitudesWithinRange()
    {
        // MaxLoTick = MaxHiTick = 10000 (100 units × 100 ticks)
        var v = _seg.Value(50.0, 50.0);  // lo=5000, hi=5000
        Assert.IsTrue(v.InBounds);
        Assert.AreEqual(0L, v.TotalOverflow());
    }

    [TestMethod]
    public void InBounds_False_WhenHiMagnitudeExceedsMax()
    {
        var v = _seg.Value(50.0, 150.0);  // hi=15000 > MaxHiTick=10000
        Assert.IsFalse(v.InBounds);
    }

    [TestMethod]
    public void InBounds_False_WhenLoMagnitudeExceedsMax()
    {
        var v = _seg.Value(150.0, 50.0);  // lo=15000 > MaxLoTick=10000
        Assert.IsFalse(v.InBounds);
    }

    [TestMethod]
    public void TotalOverflow_ReturnsExcessMagnitudeTicks()
    {
        // lo magnitude 120 > MaxLoTick 100 → lo overflow = 2000 ticks
        // hi magnitude 110 > MaxHiTick 100 → hi overflow = 1000 ticks
        var farOut = Space.Segment(100.0, 100L);
        var v      = farOut.Value(120.0, 110.0);
        Assert.AreEqual(3000L, v.TotalOverflow());
    }

    [TestMethod]
    public void TotalOverflow_NegativeTicksFromArithmetic_CountFullyAsOverflow()
    {
        // Arithmetic can produce negative lo (tension).
        // MagnitudeOverflow(-300, 10000) = 300 (entire negative value is excess).
        var s  = Space.Segment(100.0, 100L);
        var v1 = s.Value(5.0, 0.0);   // lo=500
        var v2 = s.Value(8.0, 0.0);   // lo=800
        var r  = v1 - v2;             // lo = 500 - 800 = -300 (tension)
        Assert.AreEqual(300L, r.TotalOverflow());
    }

    [TestMethod]
    public void TotalOverflow_NegatedValue_IsAllOverflow()
    {
        // Negating a positive-magnitude value makes all ticks negative.
        var v = _seg.Value(3.0, 4.0);    // lo=300, hi=400
        var n = -v;                       // lo=-300, hi=-400 (all tension)
        Assert.AreEqual(700L, n.TotalOverflow());   // |−300| + |−400| = 700
    }

    // ── Cross-space guard ──────────────────────────────────────────────────

    [TestMethod]
    public void Add_DifferentSpaces_Throws()
    {
        var s1 = Space.Segment(10.0, 100L);
        var s2 = Space.Segment(10.0, 100L);
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
