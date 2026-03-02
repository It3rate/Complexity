namespace Spaces.Tests.CoreTests;

[TestClass]
public class AxisTests
{
    // ── Signed factory ─────────────────────────────────────────────────────

    [TestMethod]
    public void Signed_TicksPerUnit_MatchesParameter()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(100L, ax.TicksPerUnit);
    }

    [TestMethod]
    public void Signed_Range_IsSymmetric_InTicks()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(-1000L, ax.MinTick);   // −10 × 100
        Assert.AreEqual(1000L,  ax.MaxTick);   //  +10 × 100
    }

    [TestMethod]
    public void Signed_Unit_HasNegateOtherLock_OnLo()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(EndLock.NegateOther, ax.Unit.LoLock);
    }

    [TestMethod]
    public void Signed_DefaultChirality_IsPositive()
    {
        var ax = Axis.Signed(5.0, 50L);
        Assert.AreEqual(Chirality.Positive, ax.Chirality);
    }

    // ── Positive factory ───────────────────────────────────────────────────

    [TestMethod]
    public void Positive_Range_StartsAtZero()
    {
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(0L,    ax.MinTick);
        Assert.AreEqual(1000L, ax.MaxTick);
    }

    [TestMethod]
    public void Positive_RangeLo_IsFixed()
    {
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.Range.LoLock);
    }

    // ── ToUnits / ToTicks ──────────────────────────────────────────────────

    [TestMethod]
    public void ToUnits_ConvertsTicksToDouble()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(3.5, ax.ToUnits(350L), delta: 1e-12);
        Assert.AreEqual(-2.0, ax.ToUnits(-200L), delta: 1e-12);
    }

    [TestMethod]
    public void ToTicks_ConvertsDoubleToNearestTick()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(350L, ax.ToTicks(3.5));
        Assert.AreEqual(-200L, ax.ToTicks(-2.0));
    }

    [TestMethod]
    public void InRange_ReturnsTrueWithinBounds_FalseOutside()
    {
        var ax = Axis.Signed(10.0, 100L);  // range [-1000, 1000]
        Assert.IsTrue(ax.InRange(0L));
        Assert.IsTrue(ax.InRange(1000L));
        Assert.IsTrue(ax.InRange(-1000L));
        Assert.IsFalse(ax.InRange(1001L));
        Assert.IsFalse(ax.InRange(-1001L));
    }

    // ── Chirality ──────────────────────────────────────────────────────────

    [TestMethod]
    public void FlipChirality_TogglesSign()
    {
        var ax      = Axis.Signed(10.0, 100L);                   // Positive
        var flipped = ax.FlipChirality();
        Assert.AreEqual(Chirality.Negative, flipped.Chirality);
        Assert.AreEqual(Chirality.Positive, flipped.FlipChirality().Chirality);  // round-trip
    }

    [TestMethod]
    public void ChiralitySign_IsPlus1ForPositive_Minus1ForNegative()
    {
        var pos = Axis.Signed(5.0, 10L);
        var neg = pos.WithChirality(Chirality.Negative);
        Assert.AreEqual(+1, pos.ChiralitySign);
        Assert.AreEqual(-1, neg.ChiralitySign);
    }
}
