namespace Spaces.Tests.CoreTests;

[TestClass]
public class FocalTests
{
    // ── Default construction ───────────────────────────────────────────────

    [TestMethod]
    public void DefaultLocks_AreBothNone()
    {
        var f = new Focal(3L, 7L);
        Assert.AreEqual(EndLock.None, f.LoLock);
        Assert.AreEqual(EndLock.None, f.HiLock);
    }

    [TestMethod]
    public void NoLock_ReturnsOwnStoredValues()
    {
        var f = new Focal(5L, 12L);
        Assert.AreEqual(5L,  f.Lo);
        Assert.AreEqual(12L, f.Hi);
    }

    // ── Raw values always available ────────────────────────────────────────

    [TestMethod]
    public void RawValues_AlwaysReturnStoredValues_RegardlessOfLock()
    {
        // NegateOther on Lo: effective Lo = −_hi, but RawLo is still _lo
        var f = new Focal(100L, 200L, EndLock.NegateOther, EndLock.None);
        Assert.AreEqual(100L, f.RawLo);
        Assert.AreEqual(200L, f.RawHi);
    }

    // ── NegateOther ────────────────────────────────────────────────────────

    [TestMethod]
    public void NegateOther_OnLo_ReturnsNegativeOfRawHi()
    {
        var f = new Focal(100L, 100L, EndLock.NegateOther, EndLock.None);
        Assert.AreEqual(-100L, f.Lo);   // −(_hi) = −100
        Assert.AreEqual(100L,  f.Hi);
    }

    [TestMethod]
    public void NegateOther_OnHi_ReturnsNegativeOfRawLo()
    {
        var f = new Focal(50L, 99L, EndLock.None, EndLock.NegateOther);
        Assert.AreEqual(50L,  f.Lo);
        Assert.AreEqual(-50L, f.Hi);  // −(_lo) = −50
    }

    [TestMethod]
    public void NegateOther_TracksDependentEnd_AfterWithHi()
    {
        // NegateOther on Lo: Lo always reads −RawHi (one-step, against raw)
        var f  = new Focal(100L, 100L, EndLock.NegateOther, EndLock.None);
        var f2 = f.WithHi(200L);
        Assert.AreEqual(-200L, f2.Lo);   // Lo = −_hi = −200
        Assert.AreEqual(200L,  f2.Hi);
    }

    // ── EqualOther ─────────────────────────────────────────────────────────

    [TestMethod]
    public void EqualOther_OnLo_ReturnsSameAsRawHi()
    {
        var f = new Focal(0L, 42L, EndLock.EqualOther, EndLock.None);
        Assert.AreEqual(42L, f.Lo);   // EqualOther: reads _hi = 42
        Assert.AreEqual(42L, f.Hi);
    }

    [TestMethod]
    public void EqualOther_OnHi_ReturnsSameAsRawLo()
    {
        var f = new Focal(7L, 99L, EndLock.None, EndLock.EqualOther);
        Assert.AreEqual(7L, f.Lo);
        Assert.AreEqual(7L, f.Hi);   // EqualOther: reads _lo = 7
    }

    // ── Fixed lock ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Fixed_OnLo_BlocksWithLo()
    {
        var f  = new Focal(3L, 9L, EndLock.Fixed, EndLock.None);
        var f2 = f.WithLo(99L);
        Assert.AreSame(f, f2);   // returns this (unchanged)
        Assert.AreEqual(3L, f2.Lo);
    }

    [TestMethod]
    public void Fixed_OnHi_BlocksWithHi()
    {
        var f  = new Focal(3L, 9L, EndLock.None, EndLock.Fixed);
        var f2 = f.WithHi(99L);
        Assert.AreSame(f, f2);   // returns this (unchanged)
        Assert.AreEqual(9L, f2.Hi);
    }

    // ── WithLo / WithHi ────────────────────────────────────────────────────

    [TestMethod]
    public void WithLo_Free_ReturnsNewFocalWithNewLo()
    {
        var f  = new Focal(10L, 20L);
        var f2 = f.WithLo(55L);
        Assert.AreEqual(55L, f2.Lo);
        Assert.AreEqual(20L, f2.Hi);   // Hi unchanged
        Assert.AreEqual(10L, f.Lo);    // original immutable
    }

    [TestMethod]
    public void WithHi_Free_ReturnsNewFocalWithNewHi()
    {
        var f  = new Focal(10L, 20L);
        var f2 = f.WithHi(77L);
        Assert.AreEqual(10L, f2.Lo);
        Assert.AreEqual(77L, f2.Hi);
        Assert.AreEqual(20L, f.Hi);    // original immutable
    }

    // ── Unlock ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Unlock_RemovesAllLocks_ExposesRawStoredValues()
    {
        // NegateOther on Lo: effective Lo = −RawHi (= −100)
        // After unlock: Lo returns RawLo (= 50)
        var f = new Focal(50L, 100L, EndLock.NegateOther, EndLock.None);
        Assert.AreEqual(-100L, f.Lo);   // NegateOther: −RawHi = −100

        var unlocked = f.Unlock();
        Assert.AreEqual(EndLock.None, unlocked.LoLock);
        Assert.AreEqual(EndLock.None, unlocked.HiLock);
        Assert.AreEqual(50L,  unlocked.Lo);   // now reads raw _lo = 50
        Assert.AreEqual(100L, unlocked.Hi);
    }

    // ── Static factories ───────────────────────────────────────────────────

    /// <summary>
    /// SymmetricUnit uses EqualOther (not NegateOther) so Lo always equals Hi —
    /// both are positive magnitudes of the same unit size.
    /// Intended for COMPLEX axes where both i-unit and r-unit are equal and positive.
    /// For REAL axes, use ZeroImag (which locks Lo at 0).
    /// </summary>
    [TestMethod]
    public void SymmetricUnit_LoIsEqualOther_BothPositive()
    {
        var f = Focal.SymmetricUnit(100L);
        Assert.AreEqual(EndLock.EqualOther, f.LoLock);
        Assert.AreEqual(EndLock.None,       f.HiLock);
        Assert.AreEqual(100L, f.Lo);   // EqualOther: reads _hi = 100
        Assert.AreEqual(100L, f.Hi);
    }

    [TestMethod]
    public void SymmetricUnit_LoTracksHiChange_ViaEqualOther()
    {
        // EqualOther: Lo always reads the raw _hi value
        var f  = Focal.SymmetricUnit(100L);
        var f2 = f.WithHi(200L);
        Assert.AreEqual(200L, f2.Lo);   // EqualOther: reads new _hi = 200
        Assert.AreEqual(200L, f2.Hi);
    }

    [TestMethod]
    public void ZeroImag_LoIsFixed_AtZero_HiIsFree()
    {
        var f = Focal.ZeroImag(50L);
        Assert.AreEqual(EndLock.Fixed, f.LoLock);
        Assert.AreEqual(EndLock.None,  f.HiLock);
        Assert.AreEqual(0L,  f.Lo);   // Fixed at zero
        Assert.AreEqual(50L, f.Hi);
    }

    [TestMethod]
    public void ZeroImag_WithHi_ChangesHi_LoStaysZero()
    {
        var f  = Focal.ZeroImag(50L);
        var f2 = f.WithHi(120L);
        Assert.AreEqual(0L,   f2.Lo);   // Fixed; cannot change
        Assert.AreEqual(120L, f2.Hi);
    }

    [TestMethod]
    public void FixedPoint_BothEndsFixed_SameValue()
    {
        var f = Focal.FixedPoint(7L);
        Assert.AreEqual(EndLock.Fixed, f.LoLock);
        Assert.AreEqual(EndLock.Fixed, f.HiLock);
        Assert.AreEqual(7L, f.Lo);
        Assert.AreEqual(7L, f.Hi);
    }

    [TestMethod]
    public void FixedPoint_WithLo_And_WithHi_ReturnThis()
    {
        var f = Focal.FixedPoint(7L);
        Assert.AreSame(f, f.WithLo(99L));
        Assert.AreSame(f, f.WithHi(99L));
    }
}
