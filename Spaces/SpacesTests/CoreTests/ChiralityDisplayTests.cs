namespace Spaces.Tests.CoreTests;

/// <summary>
/// Tests for SpaceValue.ToDisplayString() — chirality-dependent display.
///
/// CHIRALITY DISPLAY RULES:
///
///   Given Proportion(lo=loTicks, hi=hiTicks) with tpu=100:
///     loUnits = loTicks / 100   (e.g. 500 ticks → 5.0 units)
///     hiUnits = hiTicks / 100   (e.g. 600 ticks → 6.0 units)
///
///   'i' marks the start point (Lo = left extent from origin).
///   Lo is always listed first; Hi second.
///
///   Chirality.Positive — standard reading:
///     Display: "({loUnits}i+{hiUnits})"
///     Example: Proportion(500, 600) → "(5i+6)"
///
///   Chirality.Negative — rotate the paper (same data, inverted perspective):
///     Display: "[-{loUnits}i-{hiUnits}]"
///     Example: Proportion(500, 600) → "[-5i-6]"
///
/// NOTE: The stored Proportion data (500, 600) is IDENTICAL in both chiralities.
/// Chirality is purely an interpretation layer — it does not modify stored ticks.
/// </summary>
[TestClass]
public class ChiralityDisplayTests
{
    private Space _s = null!;

    [TestInitialize]
    public void SetUp() => _s = Space.Segment(100.0, 100L);

    // ── Positive chirality ─────────────────────────────────────────────────

    [TestMethod]
    public void PositiveChirality_ShowsIAndRFormat()
    {
        // Proportion(500, 600): lo=5 units left, hi=6 units right.
        var v = _s.Value(5.0, 6.0);   // lo=500, hi=600
        Assert.AreEqual("(5i+6)", v.ToDisplayString(Chirality.Positive, 0));
    }

    [TestMethod]
    public void PositiveChirality_ZeroLo_ShowsZeroI()
    {
        // A purely right-going value: 0 left, 6 right.
        var v = _s.Value(0.0, 6.0);
        Assert.AreEqual("(0i+6)", v.ToDisplayString(Chirality.Positive, 0));
    }

    [TestMethod]
    public void PositiveChirality_ZeroHi_ShowsZeroR()
    {
        // A purely left-going value: 5 left, 0 right.
        var v = _s.Value(5.0, 0.0);
        Assert.AreEqual("(5i+0)", v.ToDisplayString(Chirality.Positive, 0));
    }

    [TestMethod]
    public void PositiveChirality_FractionalUnits_UsesG4Format()
    {
        // 250 ticks / 100 tpu = 2.5 units; 375 ticks / 100 tpu = 3.75 units
        var v = _s.Value(2.5, 3.75);
        Assert.AreEqual("(2.5i+3.75)", v.ToDisplayString(Chirality.Positive, 0));
    }

    [TestMethod]
    public void PositiveChirality_NumberLineInterpretation()
    {
        // Proportion(300, 700) under Positive chirality → number-line [−3, +7]
        var v = _s.Value(3.0, 7.0);   // lo=300, hi=700
        Assert.AreEqual(300L, v.GetP0(0));   // 3 units LEFT from origin
        Assert.AreEqual(700L, v.GetP1(0));   // 7 units RIGHT from origin
        Assert.AreEqual("(3i+7)", v.ToDisplayString(Chirality.Positive, 0));
        Assert.AreEqual(-3.0, -v.GetP0Units(0), delta: 1e-10);
        Assert.AreEqual(+7.0, +v.GetP1Units(0), delta: 1e-10);
    }

    // ── Negative chirality ─────────────────────────────────────────────────

    [TestMethod]
    public void NegativeChirality_ShowsSquareBracketsWithNegatedValues()
    {
        // Proportion(500, 600) under Negative chirality (rotate the paper):
        // Display: "[-5i-6]"
        var v = _s.Value(5.0, 6.0);
        Assert.AreEqual("[-5i-6]", v.ToDisplayString(Chirality.Negative, 0));
    }

    [TestMethod]
    public void NegativeChirality_ZeroLo_ShowsSquareBracketsWithZeroI()
    {
        var v = _s.Value(0.0, 6.0);
        Assert.AreEqual("[-0i-6]", v.ToDisplayString(Chirality.Negative, 0));
    }

    [TestMethod]
    public void NegativeChirality_FractionalUnits_UsesG4Format()
    {
        var v = _s.Value(2.5, 3.75);
        Assert.AreEqual("[-2.5i-3.75]", v.ToDisplayString(Chirality.Negative, 0));
    }

    [TestMethod]
    public void NegativeChirality_NumberLineInterpretation()
    {
        // Proportion(300, 700) under Negative chirality → number-line [+3, −7]
        var v = _s.Value(3.0, 7.0);
        Assert.AreEqual("[-3i-7]", v.ToDisplayString(Chirality.Negative, 0));
        // Raw ticks unchanged:
        Assert.AreEqual(+3.0, +v.GetP0Units(0), delta: 1e-10);
        Assert.AreEqual(-7.0, -v.GetP1Units(0), delta: 1e-10);
    }

    // ── Same data, different chirality ─────────────────────────────────────

    [TestMethod]
    public void StoredDataIsIdentical_OnlyDisplayChanges()
    {
        // The Proportion stores the same ticks regardless of chirality.
        // Chirality is purely an interpretation layer.
        var v = _s.Value(5.0, 6.0);   // Proportion(500, 600) — ALWAYS stored this way

        string posDisplay = v.ToDisplayString(Chirality.Positive, 0);
        string negDisplay = v.ToDisplayString(Chirality.Negative, 0);

        Assert.AreEqual("(5i+6)",   posDisplay);
        Assert.AreEqual("[-5i-6]",  negDisplay);

        // Raw ticks unchanged regardless of which display was called:
        Assert.AreEqual(500L, v.GetP0(0));
        Assert.AreEqual(600L, v.GetP1(0));
    }

    // ── Grid space (multi-axis display) ────────────────────────────────────

    [TestMethod]
    public void GridSpace_CanDisplayEachAxisIndependently()
    {
        var xAx = Axis.Signed(100.0, 100L);
        var yAx = Axis.Signed(100.0, 100L);
        var g   = Space.Grid(xAx, yAx);
        var v   = g.Value(3.0, 7.0, 1.0, 4.0);   // x: lo=300, hi=700; y: lo=100, hi=400

        Assert.AreEqual("(3i+7)", v.ToDisplayString(Chirality.Positive, 0));   // x axis
        Assert.AreEqual("(1i+4)", v.ToDisplayString(Chirality.Positive, 1));   // y axis
        Assert.AreEqual("[-3i-7]", v.ToDisplayString(Chirality.Negative, 0));
        Assert.AreEqual("[-1i-4]", v.ToDisplayString(Chirality.Negative, 1));
    }
}

