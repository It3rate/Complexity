using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

[TestClass]
public class CValueTests
{
    private CSpace _segmentSpace;
    private CSpace _integerSpace;

    [TestInitialize]
    public void Init()
    {
        _segmentSpace = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        _integerSpace = CSpace.Integer(-1000, 1000);
    }

    // ================================================================
    //  SEGMENT SPACE ←→ PRANGE EQUIVALENCE
    //  The segment algebra table must produce the same results as PRange.
    //  All comparisons via GetValue() (ticks → double).
    // ================================================================

    [TestMethod]
    public void SegmentAdditionMatchesPRange()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var b = new CValue(_segmentSpace, new[] { 1.0, 3.0 });

        var pr_a = new PRange(2.0, 6.0);
        var pr_b = new PRange(1.0, 3.0);
        var pr_result = pr_a + pr_b;

        var result = a + b;
        Assert.AreEqual(pr_result.Start, result.GetValue(0), Utils.Tolerance); // unot
        Assert.AreEqual(pr_result.End, result.GetValue(1), Utils.Tolerance);   // unit
    }

    [TestMethod]
    public void SegmentMultiplicationMatchesPRange()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var b = new CValue(_segmentSpace, new[] { 1.0, 3.0 });

        var pr_a = new PRange(2.0, 6.0);
        var pr_b = new PRange(1.0, 3.0);
        var pr_result = pr_a * pr_b;

        var result = a * b;
        Assert.AreEqual(pr_result.Start, result.GetValue(0), Utils.Tolerance); // unot = ad+bc
        Assert.AreEqual(pr_result.End, result.GetValue(1), Utils.Tolerance);   // unit = ac-bd
    }

    [TestMethod]
    public void SegmentSquareMatchesPRange()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var pr_a = new PRange(2.0, 6.0);
        var pr_sq = PRange.Square(pr_a);

        var sq = a.Square();
        Assert.AreEqual(pr_sq.Start, sq.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(pr_sq.End, sq.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void SegmentDivisionMatchesPRange()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var b = new CValue(_segmentSpace, new[] { 1.0, 3.0 });

        var pr_a = new PRange(2.0, 6.0);
        var pr_b = new PRange(1.0, 3.0);
        var pr_result = pr_a / pr_b;

        var result = a / b;
        Assert.AreEqual(pr_result.Start, result.GetValue(0), 0.2);
        Assert.AreEqual(pr_result.End, result.GetValue(1), 0.2);
    }

    [TestMethod]
    public void SegmentNegationMatchesPRange()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var pr_a = new PRange(2.0, 6.0);
        var pr_neg = -pr_a;

        var neg = -a;
        Assert.AreEqual(pr_neg.Start, neg.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(pr_neg.End, neg.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void SegmentConjugateTest()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var conj = a.Conjugate();
        Assert.AreEqual(-2.0, conj.GetValue(0), Utils.Tolerance); // unot negated
        Assert.AreEqual(6.0, conj.GetValue(1), Utils.Tolerance);  // unit unchanged
    }

    [TestMethod]
    public void SegmentMagnitudeSquaredTest()
    {
        // |a+bi|² = a² + b² for complex. |6+2i|² = 36+4 = 40
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var magSq = a.MagnitudeSquared();
        Assert.AreEqual(40.0, magSq, 0.5);
    }

    [TestMethod]
    public void SegmentMagnitudeMatchesPRangeAbs()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var pr_a = new PRange(2.0, 6.0);
        Assert.AreEqual(PRange.Abs(pr_a), a.Magnitude(), 0.5);
    }

    [TestMethod]
    public void SegmentInverseTimesOriginalIsOne()
    {
        // Inverse requires higher resolution to avoid tick-rounding loss
        var hiRes = CSpace.Segment(-100, 100, ticksPerUnit: 1000);
        var a = new CValue(hiRes, new[] { 2.0, 6.0 });
        var inv = a.Inverse();
        var product = a * inv;

        Assert.AreEqual(0.0, product.GetValue(0), 0.01); // unot ≈ 0
        Assert.AreEqual(1.0, product.GetValue(1), 0.01); // unit ≈ 1
    }

    [TestMethod]
    public void SegmentPow2MatchesPRange()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var pr_a = new PRange(2.0, 6.0);
        var pr_pow = PRange.Pow(pr_a, 2.0);

        var pow = CValue.Pow(a, 2.0);
        Assert.AreEqual(pr_pow.Start, pow.GetValue(0), 0.2);
        Assert.AreEqual(pr_pow.End, pow.GetValue(1), 0.2);
    }

    [TestMethod]
    public void SegmentPow3MatchesPRange()
    {
        var a = new CValue(_segmentSpace, new[] { 1.0, 2.0 });
        var pr_a = new PRange(1.0, 2.0);
        var pr_pow = PRange.Pow(pr_a, 3.0);

        var pow = CValue.Pow(a, 3.0);
        Assert.AreEqual(pr_pow.Start, pow.GetValue(0), 0.2);
        Assert.AreEqual(pr_pow.End, pow.GetValue(1), 0.2);
    }

    [TestMethod]
    public void SegmentScalarMultiply()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var result = a * 3.0;
        Assert.AreEqual(6.0, result.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(18.0, result.GetValue(1), Utils.Tolerance);
    }

    // ================================================================
    //  INTEGER SPACE TESTS (ticksPerUnit=1, so ticks == values)
    // ================================================================

    [TestMethod]
    public void IntegerAddition()
    {
        var a = new CValue(_integerSpace, new[] { 7.0 });
        var b = new CValue(_integerSpace, new[] { 3.0 });
        var result = a + b;
        Assert.AreEqual(10L, result.Components[0]);
    }

    [TestMethod]
    public void IntegerMultiplication()
    {
        var a = new CValue(_integerSpace, new[] { 7.0 });
        var b = new CValue(_integerSpace, new[] { 3.0 });
        var result = a * b;
        Assert.AreEqual(21L, result.Components[0]);
    }

    [TestMethod]
    public void IntegerDivision()
    {
        var a = new CValue(_integerSpace, new[] { 21.0 });
        var b = new CValue(_integerSpace, new[] { 3.0 });
        var result = a / b;
        Assert.AreEqual(7L, result.Components[0]);
    }

    [TestMethod]
    public void IntegerPower()
    {
        var a = new CValue(_integerSpace, new[] { 3.0 });
        var result = CValue.Pow(a, 4.0);
        Assert.AreEqual(81L, result.Components[0]);
    }

    // ================================================================
    //  QUATERNION SPACE TESTS (ticksPerUnit=1000)
    // ================================================================

    [TestMethod]
    public void QuaternionISquaredIsNegativeOne()
    {
        var space = CSpace.Quaternion();
        var i = new CValue(space, new[] { 0.0, 1.0, 0.0, 0.0 });
        var iSq = i * i;
        Assert.AreEqual(-1.0, iSq.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, iSq.GetValue(1), Utils.Tolerance);
        Assert.AreEqual(0.0, iSq.GetValue(2), Utils.Tolerance);
        Assert.AreEqual(0.0, iSq.GetValue(3), Utils.Tolerance);
    }

    [TestMethod]
    public void QuaternionJSquaredIsNegativeOne()
    {
        var space = CSpace.Quaternion();
        var j = new CValue(space, new[] { 0.0, 0.0, 1.0, 0.0 });
        var jSq = j * j;
        Assert.AreEqual(-1.0, jSq.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, jSq.GetValue(1), Utils.Tolerance);
        Assert.AreEqual(0.0, jSq.GetValue(2), Utils.Tolerance);
        Assert.AreEqual(0.0, jSq.GetValue(3), Utils.Tolerance);
    }

    [TestMethod]
    public void QuaternionITimesJIsK()
    {
        var space = CSpace.Quaternion();
        var i = new CValue(space, new[] { 0.0, 1.0, 0.0, 0.0 });
        var j = new CValue(space, new[] { 0.0, 0.0, 1.0, 0.0 });
        var ij = i * j;
        Assert.AreEqual(0.0, ij.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, ij.GetValue(1), Utils.Tolerance);
        Assert.AreEqual(0.0, ij.GetValue(2), Utils.Tolerance);
        Assert.AreEqual(1.0, ij.GetValue(3), Utils.Tolerance);
    }

    [TestMethod]
    public void QuaternionJTimesIIsNegativeK()
    {
        var space = CSpace.Quaternion();
        var i = new CValue(space, new[] { 0.0, 1.0, 0.0, 0.0 });
        var j = new CValue(space, new[] { 0.0, 0.0, 1.0, 0.0 });
        var ji = j * i;
        Assert.AreEqual(0.0, ji.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, ji.GetValue(1), Utils.Tolerance);
        Assert.AreEqual(0.0, ji.GetValue(2), Utils.Tolerance);
        Assert.AreEqual(-1.0, ji.GetValue(3), Utils.Tolerance);
    }

    [TestMethod]
    public void QuaternionInverseTimesOriginalIsOne()
    {
        var space = CSpace.Quaternion();
        var q = new CValue(space, new[] { 1.0, 2.0, 3.0, 4.0 });
        var inv = q.Inverse();
        var product = q * inv;

        Assert.AreEqual(1.0, product.GetValue(0), 0.01);
        Assert.AreEqual(0.0, product.GetValue(1), 0.01);
        Assert.AreEqual(0.0, product.GetValue(2), 0.01);
        Assert.AreEqual(0.0, product.GetValue(3), 0.01);
    }

    // ================================================================
    //  SPLIT-COMPLEX TESTS
    // ================================================================

    [TestMethod]
    public void SplitComplexJSquaredIsPlusOne()
    {
        var space = CSpace.SplitComplex();
        var j = new CValue(space, new[] { 1.0, 0.0 });
        var jSq = j * j;
        Assert.AreEqual(0.0, jSq.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(1.0, jSq.GetValue(1), Utils.Tolerance);
    }

    // ================================================================
    //  DUAL NUMBER TESTS
    // ================================================================

    [TestMethod]
    public void DualNumberEpsilonSquaredIsZero()
    {
        var space = CSpace.DualNumber();
        var eps = new CValue(space, new[] { 1.0, 0.0 });
        var epsSq = eps * eps;
        Assert.AreEqual(0.0, epsSq.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, epsSq.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void DualNumberMultiplication()
    {
        var space = CSpace.DualNumber();
        // (2 + 3ε) * (4 + 5ε) = 8 + (10+12)ε + 15ε² = 8 + 22ε
        var a = new CValue(space, new[] { 3.0, 2.0 }); // 3ε + 2
        var b = new CValue(space, new[] { 5.0, 4.0 }); // 5ε + 4
        var result = a * b;
        Assert.AreEqual(22.0, result.GetValue(0), Utils.Tolerance); // ε: 2*5 + 3*4 = 22
        Assert.AreEqual(8.0, result.GetValue(1), Utils.Tolerance);  // real: 2*4 = 8
    }

    // ================================================================
    //  MODULAR SPACE TESTS
    // ================================================================

    [TestMethod]
    public void ModularAdditionWraps()
    {
        var space = CSpace.Modular(12);
        var a = new CValue(space, new[] { 10.0 });
        var b = new CValue(space, new[] { 5.0 });
        var result = a + b;
        Assert.AreEqual(3L, result.Components[0]); // 15 mod 12 = 3
    }

    // ================================================================
    //  TICK CONVERSION
    // ================================================================

    [TestMethod]
    public void TickConversionRoundtrip()
    {
        var space = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        var original = new CValue(space, new[] { 2.5, 6.3 });
        var ticks = original.ToTicks();
        var restored = CValue.FromTicks(space, ticks);

        Assert.AreEqual(25L, ticks[0]); // 2.5 * 10
        Assert.AreEqual(63L, ticks[1]); // 6.3 * 10
        Assert.AreEqual(original.Components[0], restored.Components[0]);
        Assert.AreEqual(original.Components[1], restored.Components[1]);
    }

    [TestMethod]
    public void TicksAreStorageDirectly()
    {
        // With tick-based system, Components ARE ticks
        var space = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        var val = new CValue(space, new[] { 3.0, 7.0 });
        Assert.AreEqual(30L, val.Components[0]); // 3.0 * 10
        Assert.AreEqual(70L, val.Components[1]); // 7.0 * 10
    }

    // ================================================================
    //  UNIT/UNOT ACCESSORS (return doubles)
    // ================================================================

    [TestMethod]
    public void UnitUnotAccessors()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        Assert.AreEqual(6.0, a.UnitValue, Utils.Tolerance);
        Assert.AreEqual(2.0, a.UnotValue, Utils.Tolerance);
    }

    // ================================================================
    //  SNAP
    // ================================================================

    [TestMethod]
    public void SnapClampsToBounds()
    {
        var space = CSpace.Integer(0, 100);
        var val = new CValue(space, new[] { 150.0 });
        var snapped = val.Snap();
        Assert.AreEqual(100L, snapped.Components[0]);

        var val2 = new CValue(space, new[] { -50.0 });
        var snapped2 = val2.Snap();
        Assert.AreEqual(0L, snapped2.Components[0]);
    }

    [TestMethod]
    public void SnapQuantizesToResolution()
    {
        var space = CSpace.Proportion(4); // 4 ticks per unit
        // 0.3 * 4 = 1.2 → rounds to 1 tick → 1/4 = 0.25
        var val = new CValue(space, new[] { 0.3 });
        // Components should be 1 tick (0.3 * 4 = 1.2, rounded to 1)
        Assert.AreEqual(1L, val.Components[0]);
        Assert.AreEqual(0.25, val.GetValue(0), Utils.Tolerance);
    }

    // ================================================================
    //  NORMALIZE
    // ================================================================

    [TestMethod]
    public void NormalizeProducesUnitMagnitude()
    {
        var a = new CValue(_segmentSpace, new[] { 3.0, 4.0 });
        var norm = a.Normalize();
        Assert.AreEqual(1.0, norm.Magnitude(), 0.2);
    }

    // ================================================================
    //  DISTANCE
    // ================================================================

    [TestMethod]
    public void DistanceTest()
    {
        var a = new CValue(_segmentSpace, new[] { 0.0, 0.0 });
        var b = new CValue(_segmentSpace, new[] { 3.0, 4.0 });
        Assert.AreEqual(5.0, CValue.Distance(a, b), Utils.Tolerance);
    }

    // ================================================================
    //  EQUALITY (direct long comparison)
    // ================================================================

    [TestMethod]
    public void EqualityTest()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var b = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var c = new CValue(_segmentSpace, new[] { 2.0, 7.0 });

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
        Assert.IsTrue(a == b);
        Assert.IsTrue(a != c);
    }

    [TestMethod]
    public void CloneTest()
    {
        var a = new CValue(_segmentSpace, new[] { 2.0, 6.0 });
        var b = a.Clone();
        Assert.AreEqual(a, b);
        // Modifying clone shouldn't affect original
        b.Components[0] = 99L;
        Assert.AreNotEqual(a.Components[0], b.Components[0]);
    }

    // ================================================================
    //  GetValues / GetValue
    // ================================================================

    [TestMethod]
    public void GetValuesReturnsDoubles()
    {
        var space = CSpace.Segment(-100, 100, ticksPerUnit: 100);
        var val = new CValue(space, new[] { 3.5, 7.2 });
        var values = val.GetValues();
        Assert.AreEqual(3.5, values[0], Utils.Tolerance);
        Assert.AreEqual(7.2, values[1], Utils.Tolerance);
    }

    // ================================================================
    //  METRIC-AWARE DISTANCE — Euclidean / Manhattan / Chebyshev
    //  Distance() reads MetricKind from the space.
    // ================================================================

    private static CSpace MakeGridSpace(MetricKind metric)
    {
        return new CSpace("grid",
            new[] { Axis.Signed("x", 100, ticksPerUnit: 10), Axis.Signed("y", 100, ticksPerUnit: 10) },
            new[] { new AlgebraEntry(0, 0, 0, +1), new AlgebraEntry(1, 1, 1, +1) },
            metric: metric);
    }

    [TestMethod]
    public void Distance_Euclidean_TwoDimensions()
    {
        // Point A=(0,0), B=(3,4) → Euclidean distance = 5 (the classic 3-4-5 triangle)
        var space = MakeGridSpace(MetricKind.Euclidean);
        var a = space.Value(0.0, 0.0);
        var b = space.Value(3.0, 4.0);
        Assert.AreEqual(5.0, CValue.Distance(a, b), Utils.Tolerance);
    }

    [TestMethod]
    public void Distance_Manhattan_TwoDimensions()
    {
        // Point A=(0,0), B=(3,4) → Manhattan distance = |3| + |4| = 7
        var space = MakeGridSpace(MetricKind.Manhattan);
        var a = space.Value(0.0, 0.0);
        var b = space.Value(3.0, 4.0);
        Assert.AreEqual(7.0, CValue.Distance(a, b), Utils.Tolerance);
    }

    [TestMethod]
    public void Distance_Chebyshev_TwoDimensions()
    {
        // Point A=(0,0), B=(3,4) → Chebyshev distance = max(|3|, |4|) = 4
        var space = MakeGridSpace(MetricKind.Chebyshev);
        var a = space.Value(0.0, 0.0);
        var b = space.Value(3.0, 4.0);
        Assert.AreEqual(4.0, CValue.Distance(a, b), Utils.Tolerance);
    }

    [TestMethod]
    public void Distance_AllMetrics_AgreeWhenEqual()
    {
        // All three metrics must return 0 for a point vs itself
        var eu = MakeGridSpace(MetricKind.Euclidean).Value(5.0, 7.0);
        var mn = MakeGridSpace(MetricKind.Manhattan).Value(5.0, 7.0);
        var ch = MakeGridSpace(MetricKind.Chebyshev).Value(5.0, 7.0);

        Assert.AreEqual(0.0, CValue.Distance(eu, MakeGridSpace(MetricKind.Euclidean).Value(5.0, 7.0)), Utils.Tolerance);
        Assert.AreEqual(0.0, CValue.Distance(mn, MakeGridSpace(MetricKind.Manhattan).Value(5.0, 7.0)), Utils.Tolerance);
        Assert.AreEqual(0.0, CValue.Distance(ch, MakeGridSpace(MetricKind.Chebyshev).Value(5.0, 7.0)), Utils.Tolerance);
    }

    [TestMethod]
    public void Distance_Symmetric()
    {
        // Distance(a, b) == Distance(b, a) for all metrics
        var space = MakeGridSpace(MetricKind.Euclidean);
        var a = space.Value(1.0, 2.0);
        var b = space.Value(5.0, 6.0);
        Assert.AreEqual(CValue.Distance(a, b), CValue.Distance(b, a), Utils.Tolerance);
    }

    // ================================================================
    //  CYCLIC AXIS DISTANCE — shortest-arc
    // ================================================================

    [TestMethod]
    public void Distance_CyclicAxis_ShortestArcGoesShortWay()
    {
        // Angle space: 0..360 cyclic
        // 350° to 10° should be 20° (short way), not 340° (long way)
        var angleSpace = CSpace.FromAxis(Axis.Cyclic("angle", 360.0, ticksPerUnit: 10));
        var a = angleSpace.Value(350.0);
        var b = angleSpace.Value(10.0);
        Assert.AreEqual(20.0, CValue.Distance(a, b), 0.1);
    }

    [TestMethod]
    public void Distance_CyclicAxis_ZeroToHalfPeriod()
    {
        // At exactly half a cycle, both directions are equal — distance = period/2
        var angleSpace = CSpace.FromAxis(Axis.Cyclic("angle", 360.0, ticksPerUnit: 10));
        var zero = angleSpace.Value(0.0);
        var half = angleSpace.Value(180.0);
        Assert.AreEqual(180.0, CValue.Distance(zero, half), 0.1);
    }

    [TestMethod]
    public void Distance_CyclicAxis_SamePoint_IsZero()
    {
        var modSpace = CSpace.Modular(12);
        var a = modSpace.Value(7.0);
        var b = modSpace.Value(7.0);
        Assert.AreEqual(0.0, CValue.Distance(a, b), Utils.Tolerance);
    }
}
