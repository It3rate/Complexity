namespace Spaces.Tests.CoreTests;

[TestClass]
public class ProportionTests
{
    // ── Default construction ───────────────────────────────────────────────

    [TestMethod]
    public void DefaultLocks_AreBothNone()
    {
        var p = new Proportion(3L, 7L);
        Assert.AreEqual(EndLock.None, p.P0Lock);
        Assert.AreEqual(EndLock.None, p.P1Lock);
    }

    [TestMethod]
    public void NoLock_ReturnsOwnStoredValues()
    {
        var p = new Proportion(5L, 12L);
        Assert.AreEqual(5L,  p.P0);
        Assert.AreEqual(12L, p.P1);
    }

    // ── Raw values always available ────────────────────────────────────────

    [TestMethod]
    public void RawValues_AlwaysReturnStoredValues_RegardlessOfLock()
    {
        // NegateOther on P0: effective P0 = −_p1, but RawP0 is still _p0
        var p = new Proportion(100L, 200L, EndLock.NegateOther, EndLock.None);
        Assert.AreEqual(100L, p.RawP0);
        Assert.AreEqual(200L, p.RawP1);
    }

    // ── NegateOther ────────────────────────────────────────────────────────

    [TestMethod]
    public void NegateOther_OnP0_ReturnsNegativeOfRawP1()
    {
        var p = new Proportion(100L, 100L, EndLock.NegateOther, EndLock.None);
        Assert.AreEqual(-100L, p.P0);   // −(_p1) = −100
        Assert.AreEqual(100L,  p.P1);
    }

    [TestMethod]
    public void NegateOther_OnP1_ReturnsNegativeOfRawP0()
    {
        var p = new Proportion(50L, 99L, EndLock.None, EndLock.NegateOther);
        Assert.AreEqual(50L,  p.P0);
        Assert.AreEqual(-50L, p.P1);  // −(_p0) = −50
    }

    [TestMethod]
    public void NegateOther_TracksDependentEnd_AfterWithP1()
    {
        // NegateOther on P0: P0 always reads −RawP1 (one-step, against raw)
        var p  = new Proportion(100L, 100L, EndLock.NegateOther, EndLock.None);
        var p2 = p.WithP1(200L);
        Assert.AreEqual(-200L, p2.P0);   // P0 = −_p1 = −200
        Assert.AreEqual(200L,  p2.P1);
    }

    // ── EqualOther ─────────────────────────────────────────────────────────

    [TestMethod]
    public void EqualOther_OnP0_ReturnsSameAsRawP1()
    {
        var p = new Proportion(0L, 42L, EndLock.EqualOther, EndLock.None);
        Assert.AreEqual(42L, p.P0);   // EqualOther: reads _p1 = 42
        Assert.AreEqual(42L, p.P1);
    }

    [TestMethod]
    public void EqualOther_OnP1_ReturnsSameAsRawP0()
    {
        var p = new Proportion(7L, 99L, EndLock.None, EndLock.EqualOther);
        Assert.AreEqual(7L, p.P0);
        Assert.AreEqual(7L, p.P1);   // EqualOther: reads _p0 = 7
    }

    // ── Fixed lock ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Fixed_OnP0_BlocksWithP0()
    {
        var p  = new Proportion(3L, 9L, EndLock.Fixed, EndLock.None);
        var p2 = p.WithP0(99L);
        Assert.AreSame(p, p2);   // returns this (unchanged)
        Assert.AreEqual(3L, p2.P0);
    }

    [TestMethod]
    public void Fixed_OnP1_BlocksWithP1()
    {
        var p  = new Proportion(3L, 9L, EndLock.None, EndLock.Fixed);
        var p2 = p.WithP1(99L);
        Assert.AreSame(p, p2);   // returns this (unchanged)
        Assert.AreEqual(9L, p2.P1);
    }

    // ── WithP0 / WithP1 ────────────────────────────────────────────────────

    [TestMethod]
    public void WithP0_Free_ReturnsNewProportionWithNewP0()
    {
        var p  = new Proportion(10L, 20L);
        var p2 = p.WithP0(55L);
        Assert.AreEqual(55L, p2.P0);
        Assert.AreEqual(20L, p2.P1);   // P1 unchanged
        Assert.AreEqual(10L, p.P0);    // original immutable
    }

    [TestMethod]
    public void WithP1_Free_ReturnsNewProportionWithNewP1()
    {
        var p  = new Proportion(10L, 20L);
        var p2 = p.WithP1(77L);
        Assert.AreEqual(10L, p2.P0);
        Assert.AreEqual(77L, p2.P1);
        Assert.AreEqual(20L, p.P1);    // original immutable
    }

    // ── Unlock ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Unlock_RemovesAllLocks_ExposesRawStoredValues()
    {
        // NegateOther on P0: effective P0 = −RawP1 (= −100)
        // After unlock: P0 returns RawP0 (= 50)
        var p = new Proportion(50L, 100L, EndLock.NegateOther, EndLock.None);
        Assert.AreEqual(-100L, p.P0);   // NegateOther: −RawP1 = −100

        var unlocked = p.Unlock();
        Assert.AreEqual(EndLock.None, unlocked.P0Lock);
        Assert.AreEqual(EndLock.None, unlocked.P1Lock);
        Assert.AreEqual(50L,  unlocked.P0);   // now reads raw _p0 = 50
        Assert.AreEqual(100L, unlocked.P1);
    }

    // ── GetUnit / GetMeasure — chirality-dependent access ─────────────────

    /// <summary>
    /// In positive chirality P0 is the denominator (unit) and P1 is the numerator (measure).
    /// In negative chirality the roles invert.
    /// </summary>
    [TestMethod]
    public void GetUnit_PositiveChirality_ReturnsP0()
    {
        var p = new Proportion(50L, 100L);
        Assert.AreEqual(50L, p.GetUnit(Chirality.Positive));
    }

    [TestMethod]
    public void GetMeasure_PositiveChirality_ReturnsP1()
    {
        var p = new Proportion(50L, 100L);
        Assert.AreEqual(100L, p.GetMeasure(Chirality.Positive));
    }

    [TestMethod]
    public void GetUnit_NegativeChirality_ReturnsP1()
    {
        var p = new Proportion(50L, 100L);
        Assert.AreEqual(100L, p.GetUnit(Chirality.Negative));
    }

    [TestMethod]
    public void GetMeasure_NegativeChirality_ReturnsP0()
    {
        var p = new Proportion(50L, 100L);
        Assert.AreEqual(50L, p.GetMeasure(Chirality.Negative));
    }

    [TestMethod]
    public void GetUnit_RespectsLock_EqualOther()
    {
        // When P0Lock = EqualOther, P0 reads _p1 — GetUnit(Positive) should too.
        var p = new Proportion(0L, 42L, EndLock.EqualOther, EndLock.None);
        Assert.AreEqual(42L, p.GetUnit(Chirality.Positive));   // EqualOther: reads P1 = 42
    }

    [TestMethod]
    public void GetUnitLock_PositiveChirality_ReturnsP0Lock()
    {
        var p = new Proportion(0L, 100L, EndLock.Fixed, EndLock.None);
        Assert.AreEqual(EndLock.Fixed, p.GetUnitLock(Chirality.Positive));
        Assert.AreEqual(EndLock.None,  p.GetMeasureLock(Chirality.Positive));
    }

    [TestMethod]
    public void GetUnitLock_NegativeChirality_ReturnsP1Lock()
    {
        var p = new Proportion(100L, 0L, EndLock.None, EndLock.Fixed);
        Assert.AreEqual(EndLock.Fixed, p.GetUnitLock(Chirality.Negative));
        Assert.AreEqual(EndLock.None,  p.GetMeasureLock(Chirality.Negative));
    }

    // ── Container: Measures list ──────────────────────────────────────────

    /// <summary>
    /// By default Measures contains only the primary P1 (= GetMeasure(Positive)).
    /// No additional measures are present.
    /// </summary>
    [TestMethod]
    public void Measures_DefaultsToSingleElement_EqualsP1()
    {
        var p = new Proportion(200L, 1000L);
        Assert.HasCount(1, p.Measures);
        Assert.AreEqual(1000L, p.Measures[0]);
        Assert.AreEqual(p.GetMeasure(Chirality.Positive), p.Measures[0]);
    }

    [TestMethod]
    public void WithMeasure_AppendsToMeasuresList()
    {
        var p  = new Proportion(200L, 1000L);
        var p2 = p.WithMeasure(600L);
        Assert.HasCount(2, p2.Measures);
        Assert.AreEqual(1000L, p2.Measures[0]);   // primary measure (P1)
        Assert.AreEqual(600L,  p2.Measures[1]);   // added measure
    }

    [TestMethod]
    public void WithMeasure_IsImmutable_OriginalUnchanged()
    {
        var p  = new Proportion(200L, 1000L);
        var p2 = p.WithMeasure(600L);
        Assert.HasCount(1, p.Measures);    // original untouched
        Assert.HasCount(2, p2.Measures);   // new instance has 2
    }

    [TestMethod]
    public void WithMeasure_Multiple_AllTrackedInOrder()
    {
        var p = new Proportion(200L, 1000L)
            .WithMeasure(600L)
            .WithMeasure(900L);
        Assert.HasCount(3, p.Measures);
        Assert.AreEqual(1000L, p.Measures[0]);
        Assert.AreEqual(600L,  p.Measures[1]);
        Assert.AreEqual(900L,  p.Measures[2]);
    }

    [TestMethod]
    public void WithMeasure_UnitRemainsUnchanged()
    {
        var p  = new Proportion(200L, 1000L);
        var p2 = p.WithMeasure(600L);
        Assert.AreEqual(200L, p2.GetUnit(Chirality.Positive));   // P0 unchanged
    }

    [TestMethod]
    public void WithP1_PreservesAdditionalMeasures()
    {
        var p  = new Proportion(200L, 1000L).WithMeasure(600L);
        var p2 = p.WithP1(800L);
        // primary measure changed to 800, additional measure (600) still there
        Assert.AreEqual(800L, p2.GetMeasure(Chirality.Positive));   // new primary
        Assert.HasCount(2, p2.Measures);
        Assert.AreEqual(800L, p2.Measures[0]);
        Assert.AreEqual(600L, p2.Measures[1]);
    }

    // ── Static factories ───────────────────────────────────────────────────

    [TestMethod]
    public void SymmetricUnit_P0IsEqualOther_BothPositive()
    {
        var p = Proportion.SymmetricUnit(100L);
        Assert.AreEqual(EndLock.EqualOther, p.P0Lock);
        Assert.AreEqual(EndLock.None,       p.P1Lock);
        Assert.AreEqual(100L, p.P0);   // EqualOther: reads _p1 = 100
        Assert.AreEqual(100L, p.P1);
    }

    [TestMethod]
    public void SymmetricUnit_P0TracksP1Change_ViaEqualOther()
    {
        var p  = Proportion.SymmetricUnit(100L);
        var p2 = p.WithP1(200L);
        Assert.AreEqual(200L, p2.P0);   // EqualOther: reads new _p1 = 200
        Assert.AreEqual(200L, p2.P1);
    }

    [TestMethod]
    public void ZeroImag_P0IsFixed_AtZero_P1IsFree()
    {
        var p = Proportion.ZeroImag(50L);
        Assert.AreEqual(EndLock.Fixed, p.P0Lock);
        Assert.AreEqual(EndLock.None,  p.P1Lock);
        Assert.AreEqual(0L,  p.P0);   // Fixed at zero
        Assert.AreEqual(50L, p.P1);
    }

    [TestMethod]
    public void ZeroImag_WithP1_ChangesP1_P0StaysZero()
    {
        var p  = Proportion.ZeroImag(50L);
        var p2 = p.WithP1(120L);
        Assert.AreEqual(0L,   p2.P0);   // Fixed; cannot change
        Assert.AreEqual(120L, p2.P1);
    }

    [TestMethod]
    public void FixedPoint_BothEndsFixed_SameValue()
    {
        var p = Proportion.FixedPoint(7L);
        Assert.AreEqual(EndLock.Fixed, p.P0Lock);
        Assert.AreEqual(EndLock.Fixed, p.P1Lock);
        Assert.AreEqual(7L, p.P0);
        Assert.AreEqual(7L, p.P1);
    }

    [TestMethod]
    public void FixedPoint_WithP0_And_WithP1_ReturnThis()
    {
        var p = Proportion.FixedPoint(7L);
        Assert.AreSame(p, p.WithP0(99L));
        Assert.AreSame(p, p.WithP1(99L));
    }
}
