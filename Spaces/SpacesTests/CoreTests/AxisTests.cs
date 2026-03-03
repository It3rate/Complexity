namespace Spaces.Tests.CoreTests;

/// <summary>
/// Tests for Axis: unit encoding, range encoding, conversions, range-checking,
/// and chirality.
///
/// KEY CONCEPTS UNDER TEST:
///
///   1. Unit Focal (for real / Signed + Positive axes):
///      Unit = ZeroImag(tpu) → Lo = 0 (Fixed), Hi = tpu
///      This prevents imaginary components from entering purely-real calculations.
///
///   2. Range Focal (for Signed axes):
///      Range = Focal(maxTick, maxTick) — both POSITIVE magnitudes.
///      MaxLoTick = max ticks allowed in the LEFT (i) direction.
///      MaxHiTick = max ticks allowed in the RIGHT (r) direction.
///
///   3. Range Checking — LoInRange / HiInRange:
///      Valid range for any magnitude: [0, maxMagnitude].
///      Negative ticks are ALWAYS invalid (they represent arithmetic tension,
///      not a user-created value).
///
///   4. Conversions (ToTicks / ToUnits):
///      These are unchanged, signed helpers.  Space.Value() applies abs()
///      separately — the Axis itself does not enforce positivity.
/// </summary>
[TestClass]
public class AxisTests
{
    // ── Signed factory — TicksPerUnit ───────────────────────────────────────

    [TestMethod]
    public void Signed_TicksPerUnit_MatchesParameter()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(100L, ax.TicksPerUnit);
    }

    // ── Signed factory — Range (both-positive magnitudes) ──────────────────

    [TestMethod]
    public void Signed_Range_StoresBothMagnitudesAsPositive()
    {
        // Range.Lo = max left magnitude, Range.Hi = max right magnitude.
        // Both are maxTick (= rangeMax × tpu = 10 × 100 = 1000).
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(1000L, ax.MaxLoTick);
        Assert.AreEqual(1000L, ax.MaxHiTick);
        Assert.AreEqual(1000L, ax.MaxTick);   // convenience alias for MaxHiTick
    }

    [TestMethod]
    public void Signed_Range_LoAndHi_StoredInRangeFocal()
    {
        var ax = Axis.Signed(5.0, 10L);
        Assert.AreEqual(50L, ax.Range.Lo);    // max left  magnitude = 5 × 10
        Assert.AreEqual(50L, ax.Range.Hi);    // max right magnitude = 5 × 10
    }

    // ── Signed factory — Unit (ZeroImag: no imaginary component) ───────────

    [TestMethod]
    public void Signed_Unit_HasFixedLock_OnLo_AndLoIsZero()
    {
        // For real (non-complex) axes, the i-unit is 0 (Fixed).
        // This is the key change: previously NegateOther; now Fixed at 0.
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.Unit.LoLock);
        Assert.AreEqual(0L,   ax.Unit.Lo);
        Assert.AreEqual(100L, ax.Unit.Hi);
    }

    [TestMethod]
    public void Signed_Unit_TicksPerUnit_IsUnitHi()
    {
        var ax = Axis.Signed(10.0, 200L);
        Assert.AreEqual(200L, ax.TicksPerUnit);
        Assert.AreEqual(200L, ax.Unit.Hi);
    }

    // ── Signed factory — Chirality ──────────────────────────────────────────

    [TestMethod]
    public void Signed_DefaultChirality_IsPositive()
    {
        var ax = Axis.Signed(5.0, 50L);
        Assert.AreEqual(Chirality.Positive, ax.Chirality);
    }

    // ── Positive factory ────────────────────────────────────────────────────

    [TestMethod]
    public void Positive_Range_StartsAtZero_MaxHiTickIsMax()
    {
        // Positive axis has no left extent: Range = ZeroImag(maxTick).
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(0L,    ax.MaxLoTick);   // lo=0 Fixed in range focal
        Assert.AreEqual(1000L, ax.MaxHiTick);
    }

    [TestMethod]
    public void Positive_RangeLo_IsFixed_AtZero()
    {
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.Range.LoLock);
        Assert.AreEqual(0L, ax.Range.Lo);
    }

    [TestMethod]
    public void Positive_Unit_HasFixedLock_OnLo_SameAsSignedUnit()
    {
        // Both Signed and Positive use ZeroImag for their Unit focal.
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.Unit.LoLock);
        Assert.AreEqual(0L,   ax.Unit.Lo);
        Assert.AreEqual(100L, ax.Unit.Hi);
    }

    // ── Range checking — LoInRange and HiInRange ────────────────────────────

    [TestMethod]
    public void LoInRange_True_ForZeroToMaxLoTick()
    {
        var ax = Axis.Signed(10.0, 100L);   // MaxLoTick = 1000
        Assert.IsTrue(ax.LoInRange(0L));
        Assert.IsTrue(ax.LoInRange(500L));
        Assert.IsTrue(ax.LoInRange(1000L));
    }

    [TestMethod]
    public void LoInRange_False_WhenExceedsMax()
    {
        var ax = Axis.Signed(10.0, 100L);   // MaxLoTick = 1000
        Assert.IsFalse(ax.LoInRange(1001L));
        Assert.IsFalse(ax.LoInRange(10000L));
    }

    [TestMethod]
    public void LoInRange_False_ForNegativeTicks()
    {
        // Negative ticks represent arithmetic tension — always out-of-range
        // for a magnitude component created by the user.
        var ax = Axis.Signed(10.0, 100L);
        Assert.IsFalse(ax.LoInRange(-1L));
        Assert.IsFalse(ax.LoInRange(-1000L));
    }

    [TestMethod]
    public void HiInRange_True_ForZeroToMaxHiTick()
    {
        var ax = Axis.Signed(10.0, 100L);   // MaxHiTick = 1000
        Assert.IsTrue(ax.HiInRange(0L));
        Assert.IsTrue(ax.HiInRange(500L));
        Assert.IsTrue(ax.HiInRange(1000L));
    }

    [TestMethod]
    public void HiInRange_False_WhenExceedsMax()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.IsFalse(ax.HiInRange(1001L));
    }

    [TestMethod]
    public void HiInRange_False_ForNegativeTicks()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.IsFalse(ax.HiInRange(-1L));
        Assert.IsFalse(ax.HiInRange(-500L));
    }

    [TestMethod]
    public void Positive_LoInRange_OnlyZeroIsValid()
    {
        // Positive axis: MaxLoTick = 0 (no left extent)
        var ax = Axis.Positive(10.0, 100L);
        Assert.IsTrue(ax.LoInRange(0L));
        Assert.IsFalse(ax.LoInRange(1L));    // any non-zero lo is out of range
        Assert.IsFalse(ax.LoInRange(-1L));
    }

    // ── Conversions (unchanged signed helpers) ──────────────────────────────

    [TestMethod]
    public void ToUnits_ConvertsTicksToDouble()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(3.5,  ax.ToUnits(350L),  delta: 1e-12);
        Assert.AreEqual(-2.0, ax.ToUnits(-200L), delta: 1e-12);
    }

    [TestMethod]
    public void ToTicks_ConvertsDoubleToNearestTick_SignedResult()
    {
        // ToTicks does NOT apply abs() — that's Space.Value()'s responsibility.
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(350L,  ax.ToTicks(3.5));
        Assert.AreEqual(-200L, ax.ToTicks(-2.0));
    }

    // ── Chirality operations ────────────────────────────────────────────────

    [TestMethod]
    public void FlipChirality_TogglesSign()
    {
        var ax      = Axis.Signed(10.0, 100L);              // Positive
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
