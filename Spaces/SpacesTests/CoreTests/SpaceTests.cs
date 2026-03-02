namespace Spaces.Tests.CoreTests;

[TestClass]
public class SpaceTests
{
    // ── Segment factory ────────────────────────────────────────────────────

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

    // ── Grid factory ───────────────────────────────────────────────────────

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

    // ── Value factory ──────────────────────────────────────────────────────

    [TestMethod]
    public void Value_SegmentSpace_ConvertsUnitsToTicks()
    {
        var s  = Space.Segment(10.0, 100L);
        var v  = s.Value(-2.0, 4.0);
        Assert.AreEqual(-200L, v.GetLo(0));
        Assert.AreEqual(400L,  v.GetHi(0));
    }

    [TestMethod]
    public void Value_WrongCount_Throws()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.ThrowsExactly<ArgumentException>(() => s.Value(1.0, 2.0, 3.0));
    }

    [TestMethod]
    public void Value_GridSpace_ConvertsAllFourComponents()
    {
        var xAx = Axis.Signed(10.0, 100L);
        var yAx = Axis.Signed(10.0, 100L);
        var g   = Space.Grid(xAx, yAx);
        var v   = g.Value(-1.0, 2.0, -3.0, 4.0);

        Assert.AreEqual(-100L, v.GetLo(0));   // x lo
        Assert.AreEqual(200L,  v.GetHi(0));   // x hi
        Assert.AreEqual(-300L, v.GetLo(1));   // y lo
        Assert.AreEqual(400L,  v.GetHi(1));   // y hi
    }

    // ── Static helpers ─────────────────────────────────────────────────────

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
