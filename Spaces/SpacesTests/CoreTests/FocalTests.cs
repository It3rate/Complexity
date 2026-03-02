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
        var f = new Focal(-5L, 12L);
        Assert.AreEqual(-5L, f.Lo);
        Assert.AreEqual(12L, f.Hi);
    }

    // ── Raw values always available ────────────────────────────────────────

    [TestMethod]
    public void RawValues_AlwaysReturnStoredValues_RegardlessOfLock()
    {
        // NegateOther lock on Lo would make effective Lo = -_hi, but RawLo is still _lo
        var f = new Focal(-100L, 100L, EndLock.NegateOther, EndLock.None);
        Assert.AreEqual(-100L, f.RawLo);
        Assert.AreEqual(100L,  f.RawHi);
    }

    // ── NegateOther ────────────────────────────────────────────────────────

    [TestMethod]
    public void NegateOther_OnLo_ReturnsNegativeOfRawHi()
    {
        var f = new Focal(-100L, 100L, EndLock.NegateOther, EndLock.None);
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
        // SymmetricUnit: lo locked to -Hi; changing Hi makes Lo track it
        var f = Focal.SymmetricUnit(100L);
        var f2 = f.WithHi(200L);
        Assert.AreEqual(-200L, f2.Lo);  // Lo = -_hi = -200
        Assert.AreEqual(200L,  f2.Hi);
    }

    // ── EqualOther ─────────────────────────────────────────────────────────

    [TestMethod]
    public void EqualOther_OnLo_ReturnsSameAsRawHi()
    {
        var f = new Focal(0L, 42L, EndLock.EqualOther, EndLock.None);
        Assert.AreEqual(42L, f.Lo);   // EqualOther: _hi = 42
        Assert.AreEqual(42L, f.Hi);
    }

    [TestMethod]
    public void EqualOther_OnHi_ReturnsSameAsRawLo()
    {
        var f = new Focal(7L, 99L, EndLock.None, EndLock.EqualOther);
        Assert.AreEqual(7L, f.Lo);
        Assert.AreEqual(7L, f.Hi);  // EqualOther: _lo = 7
    }

    // ── Fixed lock ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Fixed_OnLo_BlocksWithLo()
    {
        var f = new Focal(3L, 9L, EndLock.Fixed, EndLock.None);
        var f2 = f.WithLo(99L);
        Assert.AreSame(f, f2);  // returns this
        Assert.AreEqual(3L, f2.Lo);
    }

    [TestMethod]
    public void Fixed_OnHi_BlocksWithHi()
    {
        var f = new Focal(3L, 9L, EndLock.None, EndLock.Fixed);
        var f2 = f.WithHi(99L);
        Assert.AreSame(f, f2);  // returns this
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
        // NegateOther on Lo: effective Lo = -100; after unlock Lo = raw = -100 too,
        // but now it's because the stored _lo IS -100 (symmetric unit sets _lo=-tpu)
        var f = Focal.SymmetricUnit(100L);
        Assert.AreEqual(EndLock.NegateOther, f.LoLock);

        var unlocked = f.Unlock();
        Assert.AreEqual(EndLock.None, unlocked.LoLock);
        Assert.AreEqual(EndLock.None, unlocked.HiLock);
        // RawLo was -100, so after unlock Lo = -100 (the raw stored value)
        Assert.AreEqual(-100L, unlocked.Lo);
        Assert.AreEqual(100L,  unlocked.Hi);
    }

    // ── Static factories ───────────────────────────────────────────────────

    [TestMethod]
    public void SymmetricUnit_LoIsNegateOther_HiIsFree()
    {
        var f = Focal.SymmetricUnit(100L);
        Assert.AreEqual(EndLock.NegateOther, f.LoLock);
        Assert.AreEqual(EndLock.None,        f.HiLock);
        Assert.AreEqual(-100L, f.Lo);
        Assert.AreEqual(100L,  f.Hi);
    }

    [TestMethod]
    public void ZeroImag_LoIsFixed_HiIsFree()
    {
        var f = Focal.ZeroImag(50L);
        Assert.AreEqual(EndLock.Fixed, f.LoLock);
        Assert.AreEqual(EndLock.None,  f.HiLock);
        Assert.AreEqual(0L,  f.Lo);
        Assert.AreEqual(50L, f.Hi);
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
