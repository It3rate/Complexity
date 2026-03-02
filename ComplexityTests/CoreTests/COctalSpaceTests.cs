using Complexity.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ComplexityTests.CoreTests;

/// <summary>
/// Tests for the Twin-Square space (<see cref="CSpace.TwinSquare"/>) and its geometry
/// helpers (<see cref="TwinSquareGeometry"/>).
///
/// ── WHAT IS THE TWIN-SQUARE SPACE? ──────────────────────────────────────────
///
/// Two ordinary 2D "square" planes joined at a single point (the origin).
/// One plane is rotated 90° relative to the other.  The result is 4 oblique
/// axes in R³ with the following angle properties (all exact):
///
///   Plane A (XY):  a₁ = (1,0,0),  a₂ = (0,1,0)
///   Plane B (tilted): b₁ = (−½,−½,+1/√2),  b₂ = (−½,−½,−1/√2)
///
///   Within-plane:    a₁·a₂ = 0      → 90°  (genuine square)
///                    b₁·b₂ = 0      → 90°  (genuine square)
///   Cross-plane:     aᵢ·bⱼ = −½    → 120° (all 4 pairs)
///   Vector equilibrium: a₁+a₂+b₁+b₂ = (0,0,0)
///
/// A CValue has 8 components (4 lo/hi pairs — one pair per axis direction):
///   [0]=lo_a1, [1]=hi_a1, [2]=lo_a2, [3]=hi_a2,
///   [4]=lo_b1, [5]=hi_b1, [6]=lo_b2, [7]=hi_b2
///
/// The 8 "extreme points" in R³ form a 16-edge polytope (decahedron).
/// A "regular" value has all 4 hi = r and all 4 lo = −r → equidistant vertices.
/// </summary>
[TestClass]
public class CTwinSquareTests
{
    private CSpace _space = null!;

    [TestInitialize]
    public void SetUp()
    {
        _space = CSpace.TwinSquare(range: 10.0, ticksPerUnit: 100);
    }

    // ================================================================
    //  Space structure
    // ================================================================

    [TestMethod]
    public void TwinSquare_HasEightAxes()
    {
        Assert.AreEqual(8, _space.Dims);
    }

    [TestMethod]
    public void TwinSquare_AxisDirections_AlternateUnotUnit()
    {
        // Even index = unot (direction = −1), odd index = unit (direction = +1)
        Assert.AreEqual(-1, _space.Axes[0].Direction); // lo_a1 (unot)
        Assert.AreEqual(+1, _space.Axes[1].Direction); // hi_a1 (unit)
        Assert.AreEqual(-1, _space.Axes[2].Direction); // lo_a2
        Assert.AreEqual(+1, _space.Axes[3].Direction); // hi_a2
        Assert.AreEqual(-1, _space.Axes[4].Direction); // lo_b1
        Assert.AreEqual(+1, _space.Axes[5].Direction); // hi_b1
        Assert.AreEqual(-1, _space.Axes[6].Direction); // lo_b2
        Assert.AreEqual(+1, _space.Axes[7].Direction); // hi_b2
    }

    [TestMethod]
    public void TwinSquare_AxisBounds_MatchRange()
    {
        foreach (var ax in _space.Axes)
        {
            Assert.AreEqual(-10.0, ax.MinBound, 0.001);
            Assert.AreEqual( 10.0, ax.MaxBound, 0.001);
        }
    }

    [TestMethod]
    public void TwinSquare_Value_HasEightComponents()
    {
        var v = _space.Value(-3, 5, -2, 4, -1, 3, -2, 6);
        Assert.AreEqual(8, v.Components.Length);
    }

    [TestMethod]
    public void TwinSquare_Value_ComponentsMatchInput()
    {
        var v = _space.Value(-3.0, 5.0, -2.0, 4.0, -1.0, 3.0, -2.0, 6.0);
        Assert.AreEqual(-3.0, v.GetValue(TwinSquareGeometry.LoA1), 0.001);
        Assert.AreEqual( 5.0, v.GetValue(TwinSquareGeometry.HiA1), 0.001);
        Assert.AreEqual(-2.0, v.GetValue(TwinSquareGeometry.LoA2), 0.001);
        Assert.AreEqual( 4.0, v.GetValue(TwinSquareGeometry.HiA2), 0.001);
        Assert.AreEqual(-1.0, v.GetValue(TwinSquareGeometry.LoB1), 0.001);
        Assert.AreEqual( 3.0, v.GetValue(TwinSquareGeometry.HiB1), 0.001);
        Assert.AreEqual(-2.0, v.GetValue(TwinSquareGeometry.LoB2), 0.001);
        Assert.AreEqual( 6.0, v.GetValue(TwinSquareGeometry.HiB2), 0.001);
    }

    // ================================================================
    //  Basis vector angle properties
    //  These are the mathematical heart of the Twin-Square space —
    //  the 90°/120° angle structure arises entirely from these vectors.
    // ================================================================

    [TestMethod]
    public void BasisVectors_A1_A2_AreOrthogonal_90Degrees()
    {
        // Within Plane A, a₁ and a₂ are perpendicular.
        // Dot = (1)(0)+(0)(1)+(0)(0) = 0  → cos(90°) = 0 ✓
        double dot = TwinSquareGeometry.Dot(TwinSquareGeometry.A1, TwinSquareGeometry.A2);
        Assert.AreEqual(0.0, dot, 1e-9);
    }

    [TestMethod]
    public void BasisVectors_B1_B2_AreOrthogonal_90Degrees()
    {
        // Within Plane B, b₁ and b₂ are perpendicular.
        // Dot = (−½)(−½)+(−½)(−½)+(1/√2)(−1/√2) = ¼+¼−½ = 0 ✓
        double dot = TwinSquareGeometry.Dot(TwinSquareGeometry.B1, TwinSquareGeometry.B2);
        Assert.AreEqual(0.0, dot, 1e-9);
    }

    [TestMethod]
    public void BasisVectors_A1_B1_DotIsMinusHalf_120Degrees()
    {
        // Cross-plane dot product = −½ = cos(120°).
        // a₁·b₁ = (1)(−½)+(0)(−½)+(0)(1/√2) = −½ ✓
        double dot = TwinSquareGeometry.Dot(TwinSquareGeometry.A1, TwinSquareGeometry.B1);
        Assert.AreEqual(-0.5, dot, 1e-9);
    }

    [TestMethod]
    public void BasisVectors_AllCrossPlanePairs_AreAt120Degrees()
    {
        // All 4 cross-plane pairs (aᵢ·bⱼ) must equal −½.
        // This is the defining property of the Twin-Square geometry.
        double a1b1 = TwinSquareGeometry.Dot(TwinSquareGeometry.A1, TwinSquareGeometry.B1);
        double a1b2 = TwinSquareGeometry.Dot(TwinSquareGeometry.A1, TwinSquareGeometry.B2);
        double a2b1 = TwinSquareGeometry.Dot(TwinSquareGeometry.A2, TwinSquareGeometry.B1);
        double a2b2 = TwinSquareGeometry.Dot(TwinSquareGeometry.A2, TwinSquareGeometry.B2);

        Assert.AreEqual(-0.5, a1b1, 1e-9, "a₁·b₁");
        Assert.AreEqual(-0.5, a1b2, 1e-9, "a₁·b₂");
        Assert.AreEqual(-0.5, a2b1, 1e-9, "a₂·b₁");
        Assert.AreEqual(-0.5, a2b2, 1e-9, "a₂·b₂");
    }

    [TestMethod]
    public void BasisVectors_CrossPlaneAngle_Is120Degrees()
    {
        // arccos(−½) = 120°  — verified via the AngleDegrees helper
        double angle = TwinSquareGeometry.AngleDegrees(TwinSquareGeometry.A1, TwinSquareGeometry.B1);
        Assert.AreEqual(120.0, angle, 1e-6);
    }

    [TestMethod]
    public void BasisVectors_VectorEquilibrium_SumIsZero()
    {
        // a₁+a₂+b₁+b₂ = (0,0,0) — the four vectors are perfectly balanced.
        // This is a consequence of the geometry, not an input constraint.
        double sumX = TwinSquareGeometry.A1.x + TwinSquareGeometry.A2.x
                    + TwinSquareGeometry.B1.x + TwinSquareGeometry.B2.x;
        double sumY = TwinSquareGeometry.A1.y + TwinSquareGeometry.A2.y
                    + TwinSquareGeometry.B1.y + TwinSquareGeometry.B2.y;
        double sumZ = TwinSquareGeometry.A1.z + TwinSquareGeometry.A2.z
                    + TwinSquareGeometry.B1.z + TwinSquareGeometry.B2.z;

        Assert.AreEqual(0.0, sumX, 1e-9, "sumX");
        Assert.AreEqual(0.0, sumY, 1e-9, "sumY");
        Assert.AreEqual(0.0, sumZ, 1e-9, "sumZ");
    }

    [TestMethod]
    public void BasisVectors_AllUnitLength()
    {
        // Each basis vector must be a unit vector (length = 1).
        // |b₁|² = ¼+¼+½ = 1  (same for b₂)
        static double Len((double x, double y, double z) v) =>
            Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);

        Assert.AreEqual(1.0, Len(TwinSquareGeometry.A1), 1e-9, "|a₁|");
        Assert.AreEqual(1.0, Len(TwinSquareGeometry.A2), 1e-9, "|a₂|");
        Assert.AreEqual(1.0, Len(TwinSquareGeometry.B1), 1e-9, "|b₁|");
        Assert.AreEqual(1.0, Len(TwinSquareGeometry.B2), 1e-9, "|b₂|");
    }

    // ================================================================
    //  Extreme-point geometry (GetExtreme8)
    // ================================================================

    [TestMethod]
    public void GetExtreme8_RegularValue_ReturnsEightPoints()
    {
        var v   = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var pts = TwinSquareGeometry.GetExtreme8(v);
        Assert.AreEqual(8, pts.Length);
    }

    [TestMethod]
    public void GetExtreme8_RegularValue_AllPointsEquidistantFromOrigin()
    {
        // For a regular value (all hi=r, all lo=−r), every extreme point
        // lies exactly at distance r from the origin.  This is a direct
        // consequence of the unit-length basis vectors.
        var v   = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var pts = TwinSquareGeometry.GetExtreme8(v);

        foreach (var (x, y, z) in pts)
        {
            double dist = Math.Sqrt(x * x + y * y + z * z);
            Assert.AreEqual(5.0, dist, 0.001,
                $"vertex at ({x:0.##},{y:0.##},{z:0.##}) is not at distance 5");
        }
    }

    [TestMethod]
    public void GetExtreme8_RegularValue_CorrectA1Vertices()
    {
        // A1 = (1,0,0): hi_a1 tip = (+5,0,0), lo_a1 tip = (−5,0,0)
        var v   = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var pts = TwinSquareGeometry.GetExtreme8(v);

        // [0] = hi_a1 * A1
        Assert.AreEqual( 5.0, pts[0].x, 0.001); Assert.AreEqual(0.0, pts[0].y, 0.001); Assert.AreEqual(0.0, pts[0].z, 0.001);
        // [1] = lo_a1 * A1  (lo = −5, so tip is at −5 along +X)
        Assert.AreEqual(-5.0, pts[1].x, 0.001); Assert.AreEqual(0.0, pts[1].y, 0.001); Assert.AreEqual(0.0, pts[1].z, 0.001);
    }

    [TestMethod]
    public void GetExtreme8_RegularValue_CorrectA2Vertices()
    {
        // A2 = (0,1,0): hi_a2 tip = (0,+5,0), lo_a2 tip = (0,−5,0)
        var v   = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var pts = TwinSquareGeometry.GetExtreme8(v);

        // [2] = hi_a2 * A2
        Assert.AreEqual(0.0, pts[2].x, 0.001); Assert.AreEqual( 5.0, pts[2].y, 0.001); Assert.AreEqual(0.0, pts[2].z, 0.001);
        // [3] = lo_a2 * A2
        Assert.AreEqual(0.0, pts[3].x, 0.001); Assert.AreEqual(-5.0, pts[3].y, 0.001); Assert.AreEqual(0.0, pts[3].z, 0.001);
    }

    [TestMethod]
    public void GetExtreme8_RegularValue_CorrectB1Vertices()
    {
        // B1 = (−½,−½,1/√2)
        //   hi_b1=+5 → tip = 5·B1 = (−2.5, −2.5, +5/√2)
        //   lo_b1=−5 → tip = −5·B1 = (+2.5, +2.5, −5/√2)
        var v   = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var pts = TwinSquareGeometry.GetExtreme8(v);
        const double s2 = 0.7071067811865476;

        // [4] = hi_b1 * B1
        Assert.AreEqual(-2.5,  pts[4].x, 0.001);
        Assert.AreEqual(-2.5,  pts[4].y, 0.001);
        Assert.AreEqual( 5*s2, pts[4].z, 0.001);

        // [5] = lo_b1 * B1  (lo=−5, so scale = −5)
        Assert.AreEqual( 2.5,  pts[5].x, 0.001);
        Assert.AreEqual( 2.5,  pts[5].y, 0.001);
        Assert.AreEqual(-5*s2, pts[5].z, 0.001);
    }

    [TestMethod]
    public void GetExtreme8_IrregularValue_CorrectA1Vertices()
    {
        // With lo_a1=−3, hi_a1=7: the a₁ extreme points are (−3,0,0) and (7,0,0)
        var v   = _space.Value(-3, 7, -5, 5, -5, 5, -5, 5);
        var pts = TwinSquareGeometry.GetExtreme8(v);

        Assert.AreEqual( 7.0, pts[0].x, 0.001);  // [0] = hi_a1 * A1
        Assert.AreEqual(-3.0, pts[1].x, 0.001);  // [1] = lo_a1 * A1
    }

    // ================================================================
    //  Regularity
    // ================================================================

    [TestMethod]
    public void IsRegular_SymmetricEqualExtents_ReturnsTrue()
    {
        var v = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        Assert.IsTrue(TwinSquareGeometry.IsRegular(v));
    }

    [TestMethod]
    public void IsRegular_AsymmetricA1_ReturnsFalse()
    {
        // lo_a1 = −3 ≠ −hi_a1 = −5  →  not symmetric
        var v = _space.Value(-3, 5, -5, 5, -5, 5, -5, 5);
        Assert.IsFalse(TwinSquareGeometry.IsRegular(v));
    }

    [TestMethod]
    public void IsRegular_UnequalPairMagnitudes_ReturnsFalse()
    {
        // a₁ pair = ±5, a₂ pair = ±3 — different magnitudes across pairs
        var v = _space.Value(-5, 5, -3, 3, -5, 5, -5, 5);
        Assert.IsFalse(TwinSquareGeometry.IsRegular(v));
    }

    // ================================================================
    //  Plane extent extraction
    // ================================================================

    [TestMethod]
    public void PlaneAExtents_ExtractsA1AndA2Components()
    {
        var v = _space.Value(-3, 5, -2, 4, -1, 3, -2, 6);
        var (loA1, hiA1, loA2, hiA2) = TwinSquareGeometry.PlaneAExtents(v);

        Assert.AreEqual(-3.0, loA1, 0.001);
        Assert.AreEqual( 5.0, hiA1, 0.001);
        Assert.AreEqual(-2.0, loA2, 0.001);
        Assert.AreEqual( 4.0, hiA2, 0.001);
    }

    [TestMethod]
    public void PlaneBExtents_ExtractsB1AndB2Components()
    {
        var v = _space.Value(-3, 5, -2, 4, -1, 3, -2, 6);
        var (loB1, hiB1, loB2, hiB2) = TwinSquareGeometry.PlaneBExtents(v);

        Assert.AreEqual(-1.0, loB1, 0.001);
        Assert.AreEqual( 3.0, hiB1, 0.001);
        Assert.AreEqual(-2.0, loB2, 0.001);
        Assert.AreEqual( 6.0, hiB2, 0.001);
    }

    // ================================================================
    //  Tension
    // ================================================================

    [TestMethod]
    public void Tension_ValueWithinBounds_IsValid()
    {
        var v = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var t = Tension.Compute(v);
        Assert.IsTrue(t.IsValid);
        Assert.AreEqual(0L, t.Total);
    }

    [TestMethod]
    public void Tension_HiA1ExceedsBound_IsInvalid()
    {
        // hi_a1 = 12 > range 10  →  tension on the a₁ axis
        var v = _space.Value(-5, 12, -5, 5, -5, 5, -5, 5);
        var t = Tension.Compute(v);
        Assert.IsFalse(t.IsValid);
        Assert.IsTrue(t.Total > 0);
    }

    [TestMethod]
    public void Tension_MoreAxesExceedBound_HigherTotal()
    {
        var v1 = _space.Value(-5, 12, -5,  5, -5,  5, -5,  5);  // 1 axis over
        var v3 = _space.Value(-5, 12, -5, 12, -5, 12, -5,  5);  // 3 axes over
        Assert.IsTrue(Tension.Compute(v3).Total > Tension.Compute(v1).Total);
    }

    // ================================================================
    //  Intersection
    // ================================================================

    [TestMethod]
    public void Intersects_OverlappingValues_ReturnsTrue()
    {
        var a = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var b = _space.Value(-3, 7, -3, 7, -3, 7, -3, 7);
        Assert.IsTrue(TwinSquareGeometry.Intersects(a, b));
    }

    [TestMethod]
    public void Intersects_NonOverlappingOnA1_ReturnsFalse()
    {
        // hi_a1 of a = 2, lo_a1 of b = 3 → gap on a₁ → no intersection
        var a = _space.Value(-5, 2, -5, 5, -5, 5, -5, 5);
        var b = _space.Value( 3, 8, -5, 5, -5, 5, -5, 5);
        Assert.IsFalse(TwinSquareGeometry.Intersects(a, b));
    }

    [TestMethod]
    public void Intersects_TouchingAtBoundary_ReturnsTrue()
    {
        // hi_a1 of a = lo_a1 of b = 3 — touching at a single point counts as overlap
        var a = _space.Value(-5, 3, -5, 5, -5, 5, -5, 5);
        var b = _space.Value( 3, 8, -5, 5, -5, 5, -5, 5);
        Assert.IsTrue(TwinSquareGeometry.Intersects(a, b));
    }

    [TestMethod]
    public void Intersect_OverlappingValues_CorrectBounds()
    {
        var a = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var b = _space.Value(-3, 7, -3, 7, -3, 7, -3, 7);
        var r = TwinSquareGeometry.Intersect(a, b);

        Assert.IsNotNull(r);
        // Each axis: lo = max(lo_a, lo_b),  hi = min(hi_a, hi_b)
        Assert.AreEqual(-3.0, r!.GetValue(TwinSquareGeometry.LoA1), 0.001); // max(−5,−3)
        Assert.AreEqual( 5.0, r.GetValue(TwinSquareGeometry.HiA1),  0.001); // min( 5, 7)
        Assert.AreEqual(-3.0, r.GetValue(TwinSquareGeometry.LoA2),  0.001);
        Assert.AreEqual( 5.0, r.GetValue(TwinSquareGeometry.HiA2),  0.001);
        Assert.AreEqual(-3.0, r.GetValue(TwinSquareGeometry.LoB1),  0.001);
        Assert.AreEqual( 5.0, r.GetValue(TwinSquareGeometry.HiB1),  0.001);
        Assert.AreEqual(-3.0, r.GetValue(TwinSquareGeometry.LoB2),  0.001);
        Assert.AreEqual( 5.0, r.GetValue(TwinSquareGeometry.HiB2),  0.001);
    }

    [TestMethod]
    public void Intersect_PerfectOverlap_IsRegular()
    {
        // Intersecting a value with itself returns the same regular value
        var a = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var b = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var r = TwinSquareGeometry.Intersect(a, b);

        Assert.IsNotNull(r);
        Assert.IsTrue(TwinSquareGeometry.IsRegular(r!));
    }

    [TestMethod]
    public void Intersect_NonOverlappingValues_ReturnsNull()
    {
        var a = _space.Value(-5, 2, -5, 2, -5, 2, -5, 2);
        var b = _space.Value( 3, 8,  3, 8,  3, 8,  3, 8);
        Assert.IsNull(TwinSquareGeometry.Intersect(a, b));
    }

    // ================================================================
    //  Cross-tension
    // ================================================================

    [TestMethod]
    public void CrossTension_IdenticalValues_IsZero()
    {
        var a = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        Assert.AreEqual(0.0, TwinSquareGeometry.CrossTension(a, a), 0.001);
    }

    [TestMethod]
    public void CrossTension_NonOverlappingValues_IsPositive()
    {
        var a = _space.Value(-5, 2, -5, 2, -5, 2, -5, 2);
        var b = _space.Value( 3, 8,  3, 8,  3, 8,  3, 8);
        Assert.IsTrue(TwinSquareGeometry.CrossTension(a, b) > 0);
    }

    [TestMethod]
    public void CrossTension_MoreGaps_HigherTension()
    {
        // a is regular; b1 overlaps on 3 axes but gaps on a₁ only;
        // b4 gaps on all 4 axes — higher total tension.
        var a  = _space.Value(-5, 5, -5, 5, -5, 5, -5, 5);
        var b1 = _space.Value( 7, 9,  0, 5,  0, 5,  0, 5);  // 1 gap (a₁)
        var b4 = _space.Value( 7, 9,  7, 9,  7, 9,  7, 9);  // 4 gaps (all axes)
        Assert.IsTrue(TwinSquareGeometry.CrossTension(a, b4) > TwinSquareGeometry.CrossTension(a, b1));
    }
}
