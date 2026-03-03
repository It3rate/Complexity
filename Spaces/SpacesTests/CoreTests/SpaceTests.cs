namespace Spaces.Tests.CoreTests;

/// <summary>
/// Tests for Space: structure, value creation, IValueSpace implementation.
///
/// VALUE MAGNITUDE CONVENTION:
///   Space.Value() and Space.CreateValue() apply Math.Abs() to BOTH lo and hi
///   inputs before converting to ticks.  Passing -5.0 for lo stores 500 ticks
///   (same as +5.0).  The sign of the input is irrelevant — the caller is
///   specifying a MAGNITUDE.
///
/// Value() vs CreateValue():
///   Value()       — pure factory; created value NOT added to Space.Values
///   CreateValue() — creates AND adds to Space.Values (registered)
/// </summary>
[TestClass]
public class SpaceTests
{
    // ── Segment factory — structure ─────────────────────────────────────────

    [TestMethod]
    public void Segment_HasOneAxis()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.HasCount(1, s.Axes);
    }

    [TestMethod]
    public void Segment_HasFourAlgebraEntries()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.HasCount(4, s.Algebra);
    }

    [TestMethod]
    public void Segment_BasisTicks_MatchesTicksPerUnit()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.AreEqual(100L, s.BasisTicks);
    }

    [TestMethod]
    public void Segment_ComponentCount_IsTwo()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.AreEqual(2, s.ComponentCount);
    }

    // ── Grid factory — structure ────────────────────────────────────────────

    [TestMethod]
    public void Grid_HasTwoAxes()
    {
        var g = Space.Grid(Axis.Signed(10.0, 100L), Axis.Signed(10.0, 100L));
        Assert.HasCount(2, g.Axes);
    }

    [TestMethod]
    public void Grid_HasEightAlgebraEntries()
    {
        var g = Space.Grid(Axis.Signed(10.0, 100L), Axis.Signed(10.0, 100L));
        Assert.HasCount(8, g.Algebra);
    }

    [TestMethod]
    public void Grid_ComponentCount_IsFour()
    {
        var g = Space.Grid(Axis.Signed(10.0, 100L), Axis.Signed(10.0, 100L));
        Assert.AreEqual(4, g.ComponentCount);
    }

    // ── Value factory — magnitude convention ───────────────────────────────

    [TestMethod]
    public void Value_StoresBothInputsAsPositiveMagnitudes()
    {
        // Both lo and hi are abs'd; -2.0 → 200, +4.0 → 400
        var s = Space.Segment(10.0, 100L);
        var v = s.Value(-2.0, 4.0);
        Assert.AreEqual(200L, v.GetP0(0));   // abs(-2) × 100 = 200
        Assert.AreEqual(400L, v.GetP1(0));
    }

    [TestMethod]
    public void Value_NegativeAndPositiveLoGiveSameMagnitude()
    {
        // -5.0 and +5.0 for lo both produce 500 ticks
        var s = Space.Segment(10.0, 100L);
        Assert.AreEqual(500L, s.Value(-5.0, 0.0).GetP0(0));
        Assert.AreEqual(500L, s.Value(5.0,  0.0).GetP0(0));
    }

    [TestMethod]
    public void Value_WrongCount_Throws()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.ThrowsExactly<ArgumentException>(() => s.Value(1.0, 2.0, 3.0));
    }

    [TestMethod]
    public void Value_GridSpace_ConvertsAllFourComponents_AsMagnitudes()
    {
        var xAx = Axis.Signed(10.0, 100L);
        var yAx = Axis.Signed(10.0, 100L);
        var g   = Space.Grid(xAx, yAx);
        var v   = g.Value(-1.0, 2.0, -3.0, 4.0);

        Assert.AreEqual(100L, v.GetP0(0));   // abs(-1) × 100
        Assert.AreEqual(200L, v.GetP1(0));
        Assert.AreEqual(300L, v.GetP0(1));   // abs(-3) × 100
        Assert.AreEqual(400L, v.GetP1(1));
    }

    // ── IValueSpace — value management ─────────────────────────────────────

    [TestMethod]
    public void Space_ImplementsIValueSpace()
    {
        IValueSpace ivs = Space.Segment(10.0, 100L);
        Assert.IsNotNull(ivs);
        Assert.HasCount(1, ivs.Axes);
        Assert.AreEqual(100L, ivs.BasisTicks);
        Assert.AreEqual(2, ivs.ComponentCount);
    }

    [TestMethod]
    public void Space_InitiallyHasNoValues()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.IsEmpty(s.Values);
    }

    [TestMethod]
    public void Space_Value_PureFactory_DoesNotAddToValues()
    {
        var s = Space.Segment(10.0, 100L);
        _ = s.Value(2.0, 4.0);   // pure factory
        Assert.IsEmpty(s.Values);
    }

    [TestMethod]
    public void Space_CreateValue_AddsToValues()
    {
        var s = Space.Segment(10.0, 100L);
        var v = s.CreateValue(2.0, 4.0);
        Assert.HasCount(1, s.Values);
        Assert.AreSame(v, s.Values[0]);
    }

    [TestMethod]
    public void Space_AddValue_AppendsExistingValue()
    {
        var s = Space.Segment(10.0, 100L);
        var v = s.Value(1.0, 2.0);
        s.AddValue(v);
        Assert.HasCount(1, s.Values);
        Assert.AreSame(v, s.Values[0]);
    }

    [TestMethod]
    public void Space_AddValue_FromDifferentSpace_Throws()
    {
        var s1 = Space.Segment(10.0, 100L);
        var s2 = Space.Segment(10.0, 100L);
        var v  = s2.Value(1.0, 2.0);
        Assert.ThrowsExactly<InvalidOperationException>(() => s1.AddValue(v));
    }

    [TestMethod]
    public void Space_MultipleCreateValues_AreAllTracked_InOrder()
    {
        var s  = Space.Segment(10.0, 100L);
        var v1 = s.CreateValue(1.0, 2.0);
        var v2 = s.CreateValue(3.0, 4.0);
        var v3 = s.CreateValue(5.0, 6.0);
        Assert.HasCount(3, s.Values);
        Assert.AreSame(v1, s.Values[0]);
        Assert.AreSame(v2, s.Values[1]);
        Assert.AreSame(v3, s.Values[2]);
    }

    // ── Static helpers ──────────────────────────────────────────────────────

    [TestMethod]
    public void AxisOf_ReturnsAxisIndex()
    {
        Assert.AreEqual(0, Space.AxisOf(0));
        Assert.AreEqual(0, Space.AxisOf(1));
        Assert.AreEqual(1, Space.AxisOf(2));
        Assert.AreEqual(1, Space.AxisOf(3));
    }

    [TestMethod]
    public void IsHi_ReturnsTrueForOddIndices()
    {
        Assert.IsFalse(Space.IsHi(0));
        Assert.IsTrue(Space.IsHi(1));
        Assert.IsFalse(Space.IsHi(2));
        Assert.IsTrue(Space.IsHi(3));
    }
}

