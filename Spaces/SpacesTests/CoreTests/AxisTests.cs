using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Spaces.Tests.CoreTests;

/// <summary>
/// Tests for Axis: unit encoding, range encoding, conversions, range-checking,
/// and chirality.
///
/// KEY CONCEPTS UNDER TEST:
///
///   1. Two-proportion structure:
///      RealProportion  — positive chirality (real / right side)
///        P0 = real unit ticks (denominator),  P1 = real extent ticks (numerator)
///      ImagProportion  — negative chirality (imaginary / left side)
///        P0 = imaginary extent ticks (numerator),  P1 = imaginary unit ticks (denominator)
///
///   2. Derived Range and Unit:
///      Range.P0 = ImagProportion.GetMeasure(Negative) = max LEFT  magnitude
///      Range.P1 = RealProportion.GetMeasure(Positive) = max RIGHT magnitude
///      Unit.P0  = ImagProportion.GetUnit(Negative)    = imaginary tpu (0 Fixed for real axes)
///      Unit.P1  = RealProportion.GetUnit(Positive)    = real tpu
///
///   3. Range Checking — P0InRange / P1InRange:
///      Valid range for any magnitude: [0, maxMagnitude].
///      Negative ticks are ALWAYS invalid (arithmetic tension).
///
///   4. Conversions (ToTicks / ToUnits):
///      Signed helpers.  Space.Value() applies abs() separately.
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
        // Range.P0 = max left magnitude, Range.P1 = max right magnitude.
        // Both are maxTick (= rangeMax × tpu = 10 × 100 = 1000).
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(1000L, ax.MaxP0Tick);
        Assert.AreEqual(1000L, ax.MaxP1Tick);
        Assert.AreEqual(1000L, ax.MaxTick);   // convenience alias for MaxP1Tick
    }

    [TestMethod]
    public void Signed_Range_P0AndP1_StoredInRangeProportion()
    {
        var ax = Axis.Signed(5.0, 10L);
        Assert.AreEqual(50L, ax.Range.P0);    // max left  magnitude = 5 × 10
        Assert.AreEqual(50L, ax.Range.P1);    // max right magnitude = 5 × 10
    }

    // ── Signed factory — Unit (ZeroImag: no imaginary component) ───────────

    [TestMethod]
    public void Signed_Unit_HasFixedLock_OnP0_AndP0IsZero()
    {
        // For real (non-complex) axes, the imaginary unit is 0 (Fixed).
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.Unit.P0Lock);
        Assert.AreEqual(0L,   ax.Unit.P0);
        Assert.AreEqual(100L, ax.Unit.P1);
        // Chirality-dependent access:
        Assert.AreEqual(0L,   ax.Unit.GetUnit(Chirality.Positive));    // denominator direction = 0
        Assert.AreEqual(100L, ax.Unit.GetMeasure(Chirality.Positive)); // numerator direction = 100 ticks
    }

    [TestMethod]
    public void Signed_Unit_TicksPerUnit_IsUnitP1()
    {
        var ax = Axis.Signed(10.0, 200L);
        Assert.AreEqual(200L, ax.TicksPerUnit);
        Assert.AreEqual(200L, ax.Unit.P1);
    }

    // ── Signed factory — two-proportion structure ───────────────────────────

    [TestMethod]
    public void Signed_RealProportion_HasUnitAndMeasure()
    {
        // RealProportion.P0 = tpu (unit/denominator in positive chirality)
        // RealProportion.P1 = maxTick (measure/numerator in positive chirality)
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(100L,  ax.RealProportion.P0);   // real tpu
        Assert.AreEqual(1000L, ax.RealProportion.P1);   // real extent
        Assert.AreEqual(100L,  ax.RealProportion.GetUnit(Chirality.Positive));
        Assert.AreEqual(1000L, ax.RealProportion.GetMeasure(Chirality.Positive));
    }

    [TestMethod]
    public void Signed_ImagProportion_HasMeasureAndZeroUnit()
    {
        // ImagProportion.P0 = maxTick (measure in negative chirality)
        // ImagProportion.P1 = 0 Fixed (unit in negative chirality — no imaginary unit for real axis)
        var ax = Axis.Signed(10.0, 100L);
        Assert.AreEqual(1000L,       ax.ImagProportion.P0);
        Assert.AreEqual(0L,          ax.ImagProportion.P1);
        Assert.AreEqual(EndLock.Fixed, ax.ImagProportion.P1Lock);
        Assert.AreEqual(0L,          ax.ImagProportion.GetUnit(Chirality.Negative));
        Assert.AreEqual(1000L,       ax.ImagProportion.GetMeasure(Chirality.Negative));
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
    public void Positive_Range_StartsAtZero_MaxP1TickIsMax()
    {
        // Positive axis has no left extent: Range.P0 = 0 (Fixed).
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(0L,    ax.MaxP0Tick);   // P0 fixed at 0 in range
        Assert.AreEqual(1000L, ax.MaxP1Tick);
    }

    [TestMethod]
    public void Positive_RangeP0_IsFixed_AtZero()
    {
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.Range.P0Lock);
        Assert.AreEqual(0L, ax.Range.P0);
    }

    [TestMethod]
    public void Positive_Unit_HasFixedLock_OnP0_SameAsSignedUnit()
    {
        // Both Signed and Positive produce Unit.P0 = 0 (Fixed).
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.Unit.P0Lock);
        Assert.AreEqual(0L,   ax.Unit.P0);
        Assert.AreEqual(100L, ax.Unit.P1);
    }

    [TestMethod]
    public void Positive_ImagProportion_BothFixed_AtZero()
    {
        // Positive axis: ImagProportion has P0=0 (Fixed) and P1=0 (Fixed).
        var ax = Axis.Positive(10.0, 100L);
        Assert.AreEqual(EndLock.Fixed, ax.ImagProportion.P0Lock);
        Assert.AreEqual(EndLock.Fixed, ax.ImagProportion.P1Lock);
        Assert.AreEqual(0L, ax.ImagProportion.P0);
        Assert.AreEqual(0L, ax.ImagProportion.P1);
    }

    // ── Range checking — P0InRange and P1InRange ────────────────────────────

    [TestMethod]
    public void P0InRange_True_ForZeroToMaxP0Tick()
    {
        var ax = Axis.Signed(10.0, 100L);   // MaxP0Tick = 1000
        Assert.IsTrue(ax.P0InRange(0L));
        Assert.IsTrue(ax.P0InRange(500L));
        Assert.IsTrue(ax.P0InRange(1000L));
    }

    [TestMethod]
    public void P0InRange_False_WhenExceedsMax()
    {
        var ax = Axis.Signed(10.0, 100L);   // MaxP0Tick = 1000
        Assert.IsFalse(ax.P0InRange(1001L));
        Assert.IsFalse(ax.P0InRange(10000L));
    }

    [TestMethod]
    public void P0InRange_False_ForNegativeTicks()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.IsFalse(ax.P0InRange(-1L));
        Assert.IsFalse(ax.P0InRange(-1000L));
    }

    [TestMethod]
    public void P1InRange_True_ForZeroToMaxP1Tick()
    {
        var ax = Axis.Signed(10.0, 100L);   // MaxP1Tick = 1000
        Assert.IsTrue(ax.P1InRange(0L));
        Assert.IsTrue(ax.P1InRange(500L));
        Assert.IsTrue(ax.P1InRange(1000L));
    }

    [TestMethod]
    public void P1InRange_False_WhenExceedsMax()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.IsFalse(ax.P1InRange(1001L));
    }

    [TestMethod]
    public void P1InRange_False_ForNegativeTicks()
    {
        var ax = Axis.Signed(10.0, 100L);
        Assert.IsFalse(ax.P1InRange(-1L));
        Assert.IsFalse(ax.P1InRange(-500L));
    }

    [TestMethod]
    public void Positive_P0InRange_OnlyZeroIsValid()
    {
        // Positive axis: MaxP0Tick = 0 (no left extent)
        var ax = Axis.Positive(10.0, 100L);
        Assert.IsTrue(ax.P0InRange(0L));
        Assert.IsFalse(ax.P0InRange(1L));
        Assert.IsFalse(ax.P0InRange(-1L));
    }

    // ── Conversions ─────────────────────────────────────────────────────────

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

    [TestMethod]
    public void WithChirality_PreservesProportions()
    {
        // Flipping chirality should not change the stored proportions.
        var ax  = Axis.Signed(10.0, 100L);
        var neg = ax.WithChirality(Chirality.Negative);
        Assert.AreEqual(ax.RealProportion.P0, neg.RealProportion.P0);
        Assert.AreEqual(ax.RealProportion.P1, neg.RealProportion.P1);
        Assert.AreEqual(ax.ImagProportion.P0, neg.ImagProportion.P0);
        Assert.AreEqual(ax.ImagProportion.P1, neg.ImagProportion.P1);
    }
}
