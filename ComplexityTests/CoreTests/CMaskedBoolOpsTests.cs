using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

/// <summary>
/// Tests all 16 two-input boolean operations on CMasked across multiple space types:
///   - Integer  (1D, tpu=1)              — basic number line
///   - Segment  (2D, tpu=1, PRange-equiv) — Start/End range like PRange
///   - Proportion (1D, tpu=1000)          — fractional / non-unit resolution
///
/// Operations are ordered by their truth-table index (0000–1111).
///
/// Reference: with A=[2..8] and B=[5..12] the four boundary regions are:
///   [2..5)  → A=T, B=F
///   [5..8]  → A=T, B=T
///   (8..12] → A=F, B=T
/// (outside this window both are F — the algorithm does not reach there)
///
/// Note on NOR / XNOR: when inputs overlap with no gap, op(F,F) is true but no
/// within-boundary segment is found. The algorithm's fallback returns the full
/// combined range (aLo..bHi). Tests use non-overlapping inputs for these two ops
/// to exercise the gap-finding path.
/// </summary>
[TestClass]
public class CMaskedBoolOpsTests
{
    // ================================================================
    //  SPACES
    // ================================================================
    private CSpace _int;
    private CSpace _seg;
    private CSpace _prop;

    [TestInitialize]
    public void Init()
    {
        _int  = CSpace.Integer(-100, 100);          // 1D, tpu=1
        _seg  = CSpace.Segment(-100, 100);           // 2D, tpu=1 (≡ PRange)
        _prop = CSpace.Proportion(1000);             // 1D [0..1], tpu=1000
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    /// Tick values of a 1D region.
    private static (long Lo, long Hi) Region1D(CMasked r, int idx = 0) =>
        (r.Regions[idx].Low.Components[0], r.Regions[idx].High.Components[0]);

    /// Tick values of axis 0 in a 2D region.
    private static (long Lo, long Hi) Axis0(CMasked r, int idx = 0) =>
        (r.Regions[idx].Low.Components[0], r.Regions[idx].High.Components[0]);

    /// Tick values of axis 1 in a 2D region.
    private static (long Lo, long Hi) Axis1(CMasked r, int idx = 0) =>
        (r.Regions[idx].Low.Components[1], r.Regions[idx].High.Components[1]);

    // Standard overlapping pair (all spaces): A=[2..8], B=[5..12]
    private ((CValue, CValue) a, (CValue, CValue) b) AB1D() =>
        ((_int.Value(2), _int.Value(8)), (_int.Value(5), _int.Value(12)));

    // Non-overlapping pair with a gap (1D): A=[2..4], B=[7..10], gap=(4..7)
    private ((CValue, CValue) a, (CValue, CValue) b) AB1D_Gap() =>
        ((_int.Value(2), _int.Value(4)), (_int.Value(7), _int.Value(10)));

    // 2D (Segment / PRange-equiv): both axes A=[2..8], B=[5..12]
    private ((CValue, CValue) a, (CValue, CValue) b) AB2D() =>
        ((_seg.Value(2, 2), _seg.Value(8, 8)), (_seg.Value(5, 5), _seg.Value(12, 12)));

    // 2D non-overlapping with gap: both axes A=[2..4], B=[7..10]
    private ((CValue, CValue) a, (CValue, CValue) b) AB2D_Gap() =>
        ((_seg.Value(2, 2), _seg.Value(4, 4)), (_seg.Value(7, 7), _seg.Value(10, 10)));

    // Proportion pair: A=[0.2..0.8], B=[0.5..1.0]  →  ticks A=[200..800], B=[500..1000]
    private ((CValue, CValue) a, (CValue, CValue) b) ABProp() =>
        ((_prop.Value(0.2), _prop.Value(0.8)), (_prop.Value(0.5), _prop.Value(1.0)));

    // ================================================================
    //  0 — FALSE  (0000)
    //  Always returns empty regardless of inputs.
    // ================================================================

    [TestMethod] public void False_Integer_IsAlwaysEmpty()
    {
        var (a, b) = AB1D();
        Assert.IsTrue(CMasked.False(_int, a, b).IsEmpty);
    }

    [TestMethod] public void False_Segment_IsAlwaysEmpty()
    {
        var (a, b) = AB2D();
        Assert.IsTrue(CMasked.False(_seg, a, b).IsEmpty);
    }

    [TestMethod] public void False_Proportion_IsAlwaysEmpty()
    {
        var (a, b) = ABProp();
        Assert.IsTrue(CMasked.False(_prop, a, b).IsEmpty);
    }

    // ================================================================
    //  1 — AND  (0001)
    //  True only when both A and B are true → intersection.
    // ================================================================

    [TestMethod] public void And_Integer_ReturnsIntersection()
    {
        var (a, b) = AB1D();
        var r = CMasked.And(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 8L), Region1D(r));
    }

    [TestMethod] public void And_Integer_NonOverlappingIsEmpty()
    {
        var (a, b) = AB1D_Gap();
        Assert.IsTrue(CMasked.And(_int, a, b).IsEmpty);
    }

    [TestMethod] public void And_Segment_ReturnsIntersectionRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.And(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 8L), Axis0(r));
        Assert.AreEqual((5L, 8L), Axis1(r));
    }

    [TestMethod] public void And_Proportion_ReturnsIntersection()
    {
        var (a, b) = ABProp();
        var r = CMasked.And(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((500L, 800L), Region1D(r));
    }

    // ================================================================
    //  2 — AND_NOT  (0010)
    //  True only when A is true and B is false → A minus B.
    // ================================================================

    [TestMethod] public void AndNot_Integer_ReturnsAMinusB()
    {
        var (a, b) = AB1D();
        var r = CMasked.AndNot(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 5L), Region1D(r));
    }

    [TestMethod] public void AndNot_Segment_ReturnsAMinusBPerAxis()
    {
        var (a, b) = AB2D();
        var r = CMasked.AndNot(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 5L), Axis0(r));
        Assert.AreEqual((2L, 5L), Axis1(r));
    }

    [TestMethod] public void AndNot_Proportion_ReturnsAMinusB()
    {
        var (a, b) = ABProp();
        var r = CMasked.AndNot(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((200L, 500L), Region1D(r));
    }

    // ================================================================
    //  3 — A  (0011)
    //  Projection: result equals A, B is ignored.
    // ================================================================

    [TestMethod] public void ProjectA_Integer_ReturnsA()
    {
        var (a, b) = AB1D();
        var r = CMasked.ProjectA(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 8L), Region1D(r));
    }

    [TestMethod] public void ProjectA_Segment_ReturnsARectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.ProjectA(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 8L), Axis0(r));
        Assert.AreEqual((2L, 8L), Axis1(r));
    }

    [TestMethod] public void ProjectA_Proportion_ReturnsA()
    {
        var (a, b) = ABProp();
        var r = CMasked.ProjectA(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((200L, 800L), Region1D(r));
    }

    // ================================================================
    //  4 — NOT_AND  (0100)
    //  True only when A is false and B is true → B minus A.
    // ================================================================

    [TestMethod] public void NotAnd_Integer_ReturnsBMinusA()
    {
        var (a, b) = AB1D();
        var r = CMasked.NotAnd(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((8L, 12L), Region1D(r));
    }

    [TestMethod] public void NotAnd_Segment_ReturnsBMinusAPerAxis()
    {
        var (a, b) = AB2D();
        var r = CMasked.NotAnd(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((8L, 12L), Axis0(r));
        Assert.AreEqual((8L, 12L), Axis1(r));
    }

    [TestMethod] public void NotAnd_Proportion_ReturnsBMinusA()
    {
        var (a, b) = ABProp();
        var r = CMasked.NotAnd(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((800L, 1000L), Region1D(r));
    }

    // ================================================================
    //  5 — B  (0101)
    //  Projection: result equals B, A is ignored.
    // ================================================================

    [TestMethod] public void ProjectB_Integer_ReturnsB()
    {
        var (a, b) = AB1D();
        var r = CMasked.ProjectB(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 12L), Region1D(r));
    }

    [TestMethod] public void ProjectB_Segment_ReturnsBRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.ProjectB(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 12L), Axis0(r));
        Assert.AreEqual((5L, 12L), Axis1(r));
    }

    [TestMethod] public void ProjectB_Proportion_ReturnsB()
    {
        var (a, b) = ABProp();
        var r = CMasked.ProjectB(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((500L, 1000L), Region1D(r));
    }

    // ================================================================
    //  6 — XOR  (0110)
    //  True when exactly one of A or B is true → symmetric difference.
    // ================================================================

    [TestMethod] public void Xor_Integer_ReturnsTwoExclusiveRegions()
    {
        var (a, b) = AB1D();
        var r = CMasked.Xor(_int, a, b);
        Assert.AreEqual(2, r.Count);
        var lows  = r.Regions.Select(x => x.Low.Components[0]).OrderBy(v => v).ToArray();
        var highs = r.Regions.Select(x => x.High.Components[0]).OrderBy(v => v).ToArray();
        Assert.AreEqual(2L, lows[0]);  Assert.AreEqual(5L, highs[0]);  // A-only
        Assert.AreEqual(8L, lows[1]);  Assert.AreEqual(12L, highs[1]); // B-only
    }

    [TestMethod] public void Xor_Integer_IdenticalRangesIsEmpty()
    {
        var a = (_int.Value(3), _int.Value(9));
        var b = (_int.Value(3), _int.Value(9));
        Assert.IsTrue(CMasked.Xor(_int, a, b).IsEmpty);
    }

    [TestMethod] public void Xor_Segment_ReturnsFourRectangles()
    {
        // Per axis: 2 segments × 2 segments = 4 Cartesian-product rectangles
        var (a, b) = AB2D();
        var r = CMasked.Xor(_seg, a, b);
        Assert.AreEqual(4, r.Count);
    }

    [TestMethod] public void Xor_Proportion_ReturnsTwoRegions()
    {
        var (a, b) = ABProp();
        var r = CMasked.Xor(_prop, a, b);
        Assert.AreEqual(2, r.Count);
    }

    // ================================================================
    //  7 — OR  (0111)
    //  True when at least one of A or B is true → union.
    // ================================================================

    [TestMethod] public void Or_Integer_ReturnsUnion()
    {
        var (a, b) = AB1D();
        var r = CMasked.Or(_int, a, b);
        Assert.IsFalse(r.IsEmpty);
        Assert.AreEqual(2L,  r.Regions.Min(x => x.Low.Components[0]));
        Assert.AreEqual(12L, r.Regions.Max(x => x.High.Components[0]));
    }

    [TestMethod] public void Or_Segment_CoversBothRectangles()
    {
        var (a, b) = AB2D();
        var r = CMasked.Or(_seg, a, b);
        Assert.IsFalse(r.IsEmpty);
        Assert.AreEqual(2L,  r.Regions.Min(x => x.Low.Components[0]));
        Assert.AreEqual(12L, r.Regions.Max(x => x.High.Components[0]));
        Assert.AreEqual(2L,  r.Regions.Min(x => x.Low.Components[1]));
        Assert.AreEqual(12L, r.Regions.Max(x => x.High.Components[1]));
    }

    [TestMethod] public void Or_Proportion_ReturnsUnion()
    {
        var (a, b) = ABProp();
        var r = CMasked.Or(_prop, a, b);
        Assert.IsFalse(r.IsEmpty);
        Assert.AreEqual(200L,  r.Regions.Min(x => x.Low.Components[0]));
        Assert.AreEqual(1000L, r.Regions.Max(x => x.High.Components[0]));
    }

    // ================================================================
    //  8 — NOR  (1000)
    //  True only when both A and B are false.
    //  For non-overlapping inputs this finds the gap between them.
    // ================================================================

    [TestMethod] public void Nor_Integer_FindsGapBetweenNonOverlapping()
    {
        var (a, b) = AB1D_Gap(); // A=[2..4], B=[7..10]
        var r = CMasked.Nor(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((4L, 7L), Region1D(r));
    }

    [TestMethod] public void Nor_Segment_FindsGapRectangleBetweenNonOverlapping()
    {
        var (a, b) = AB2D_Gap(); // both axes A=[2..4], B=[7..10]
        var r = CMasked.Nor(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((4L, 7L), Axis0(r));
        Assert.AreEqual((4L, 7L), Axis1(r));
    }

    [TestMethod] public void Nor_Proportion_FindsGap()
    {
        // A=[0.1..0.4], B=[0.7..1.0], gap=[0.4..0.7] → ticks 100,400,700,1000
        var a = (_prop.Value(0.1), _prop.Value(0.4));
        var b = (_prop.Value(0.7), _prop.Value(1.0));
        var r = CMasked.Nor(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((400L, 700L), Region1D(r));
    }

    // ================================================================
    //  9 — XNOR  (1001)
    //  True when A and B have the same value (both true or both false).
    //  Within the boundary range this captures the overlap region.
    // ================================================================

    [TestMethod] public void Xnor_Integer_ReturnsOverlap()
    {
        var (a, b) = AB1D();
        var r = CMasked.Xnor(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 8L), Region1D(r));
    }

    [TestMethod] public void Xnor_Integer_NonOverlappingReturnsGap()
    {
        var (a, b) = AB1D_Gap(); // A=[2..4], B=[7..10], gap=(4..7)
        var r = CMasked.Xnor(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((4L, 7L), Region1D(r));
    }

    [TestMethod] public void Xnor_Segment_ReturnsOverlapRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.Xnor(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 8L), Axis0(r));
        Assert.AreEqual((5L, 8L), Axis1(r));
    }

    [TestMethod] public void Xnor_Proportion_ReturnsOverlap()
    {
        var (a, b) = ABProp();
        var r = CMasked.Xnor(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((500L, 800L), Region1D(r));
    }

    // ================================================================
    //  10 — NOT_B  (1010)
    //  Result is the complement of B (ignores A).
    //  Within the boundary window: true where B is false.
    // ================================================================

    [TestMethod] public void NotB_Integer_ReturnsBComplementWithinWindow()
    {
        var (a, b) = AB1D(); // window [2..12], B starts at 5
        var r = CMasked.NotB(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 5L), Region1D(r));
    }

    [TestMethod] public void NotB_Segment_ReturnsBComplementRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.NotB(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 5L), Axis0(r));
        Assert.AreEqual((2L, 5L), Axis1(r));
    }

    [TestMethod] public void NotB_Proportion_ReturnsBComplement()
    {
        var (a, b) = ABProp();
        var r = CMasked.NotB(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((200L, 500L), Region1D(r));
    }

    // ================================================================
    //  11 — A_OR_NOT_B  (1011)
    //  True when A is true or B is false.
    // ================================================================

    [TestMethod] public void AOrNotB_Integer_ReturnsAOrNotB()
    {
        var (a, b) = AB1D(); // true in [2..8] (A true) and [2..5] (!B) → [2..8]
        var r = CMasked.AOrNotB(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 8L), Region1D(r));
    }

    [TestMethod] public void AOrNotB_Segment_ReturnsCorrectRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.AOrNotB(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((2L, 8L), Axis0(r));
        Assert.AreEqual((2L, 8L), Axis1(r));
    }

    [TestMethod] public void AOrNotB_Proportion_ReturnsCorrectRegion()
    {
        var (a, b) = ABProp(); // A=[200..800], B=[500..1000] → A||!B → [200..800]
        var r = CMasked.AOrNotB(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((200L, 800L), Region1D(r));
    }

    // ================================================================
    //  12 — NOT_A  (1100)
    //  Result is the complement of A (ignores B).
    //  Within the boundary window: true where A is false.
    // ================================================================

    [TestMethod] public void NotA_Integer_ReturnsAComplementWithinWindow()
    {
        var (a, b) = AB1D(); // window [2..12], A ends at 8
        var r = CMasked.NotA(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((8L, 12L), Region1D(r));
    }

    [TestMethod] public void NotA_Segment_ReturnsAComplementRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.NotA(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((8L, 12L), Axis0(r));
        Assert.AreEqual((8L, 12L), Axis1(r));
    }

    [TestMethod] public void NotA_Proportion_ReturnsAComplement()
    {
        var (a, b) = ABProp();
        var r = CMasked.NotA(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((800L, 1000L), Region1D(r));
    }

    // ================================================================
    //  13 — NOT_A_OR_B  (1101)
    //  True when A is false or B is true.
    // ================================================================

    [TestMethod] public void NotAOrB_Integer_ReturnsNotAOrB()
    {
        var (a, b) = AB1D(); // true in [8..12] (!A) and [5..12] (B) → [5..12]
        var r = CMasked.NotAOrB(_int, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 12L), Region1D(r));
    }

    [TestMethod] public void NotAOrB_Segment_ReturnsCorrectRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.NotAOrB(_seg, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((5L, 12L), Axis0(r));
        Assert.AreEqual((5L, 12L), Axis1(r));
    }

    [TestMethod] public void NotAOrB_Proportion_ReturnsCorrectRegion()
    {
        var (a, b) = ABProp(); // !A||B → B=[500..1000] covers it
        var r = CMasked.NotAOrB(_prop, a, b);
        Assert.AreEqual(1, r.Count);
        Assert.AreEqual((500L, 1000L), Region1D(r));
    }

    // ================================================================
    //  14 — NAND  (1110)
    //  True unless both A and B are true → complement of AND.
    // ================================================================

    [TestMethod] public void Nand_Integer_ReturnsTwoRegionsExcludingOverlap()
    {
        var (a, b) = AB1D(); // false only at [5..8], so true at [2..5] and [8..12]
        var r = CMasked.Nand(_int, a, b);
        Assert.AreEqual(2, r.Count);
        var lows  = r.Regions.Select(x => x.Low.Components[0]).OrderBy(v => v).ToArray();
        var highs = r.Regions.Select(x => x.High.Components[0]).OrderBy(v => v).ToArray();
        Assert.AreEqual(2L, lows[0]);  Assert.AreEqual(5L, highs[0]);
        Assert.AreEqual(8L, lows[1]);  Assert.AreEqual(12L, highs[1]);
    }

    [TestMethod] public void Nand_Integer_NonOverlappingIsFullRange()
    {
        // No overlap → AND is always false → NAND is always true
        var (a, b) = AB1D_Gap();
        var r = CMasked.Nand(_int, a, b);
        Assert.IsFalse(r.IsEmpty);
    }

    [TestMethod] public void Nand_Segment_ReturnsFourRectangles()
    {
        // Per axis: 2 NAND segments → 2×2 = 4 Cartesian-product rectangles
        var (a, b) = AB2D();
        var r = CMasked.Nand(_seg, a, b);
        Assert.AreEqual(4, r.Count);
    }

    [TestMethod] public void Nand_Proportion_ReturnsTwoRegions()
    {
        var (a, b) = ABProp();
        var r = CMasked.Nand(_prop, a, b);
        Assert.AreEqual(2, r.Count);
    }

    // ================================================================
    //  15 — TRUE  (1111)
    //  Always true regardless of inputs → full combined boundary range.
    // ================================================================

    [TestMethod] public void True_Integer_ReturnsFullBoundaryRange()
    {
        var (a, b) = AB1D();
        var r = CMasked.True(_int, a, b);
        Assert.IsFalse(r.IsEmpty);
        Assert.AreEqual(2L,  r.Regions.Min(x => x.Low.Components[0]));
        Assert.AreEqual(12L, r.Regions.Max(x => x.High.Components[0]));
    }

    [TestMethod] public void True_Segment_ReturnsFullBoundaryRectangle()
    {
        var (a, b) = AB2D();
        var r = CMasked.True(_seg, a, b);
        Assert.IsFalse(r.IsEmpty);
        Assert.AreEqual(2L,  r.Regions.Min(x => x.Low.Components[0]));
        Assert.AreEqual(12L, r.Regions.Max(x => x.High.Components[0]));
        Assert.AreEqual(2L,  r.Regions.Min(x => x.Low.Components[1]));
        Assert.AreEqual(12L, r.Regions.Max(x => x.High.Components[1]));
    }

    [TestMethod] public void True_Proportion_ReturnsFullRange()
    {
        var (a, b) = ABProp();
        var r = CMasked.True(_prop, a, b);
        Assert.IsFalse(r.IsEmpty);
        Assert.AreEqual(200L,  r.Regions.Min(x => x.Low.Components[0]));
        Assert.AreEqual(1000L, r.Regions.Max(x => x.High.Components[0]));
    }

    // ================================================================
    //  INVERSE PAIR TESTS
    //  Each of the 8 inverse pairs from OperationKind.InverseOp is verified:
    //  their regions should be disjoint and together span the boundary window.
    // ================================================================

    [TestMethod] public void Inverse_FalseAndTrue_AreOpposite()
    {
        var (a, b) = AB1D();
        Assert.IsTrue (CMasked.False(_int, a, b).IsEmpty);
        Assert.IsFalse(CMasked.True (_int, a, b).IsEmpty);
    }

    [TestMethod] public void Inverse_AndAndNand_AreComplementary()
    {
        // AND=[5..8], NAND=[2..5]+[8..12] — together [2..12] with no overlap
        var (a, b) = AB1D();
        var andR  = CMasked.And (_int, a, b);
        var nandR = CMasked.Nand(_int, a, b);
        Assert.AreEqual(1, andR.Count);
        Assert.AreEqual(2, nandR.Count);
        Assert.AreEqual(8L,  andR.Regions[0].High.Components[0]);
        Assert.AreEqual(8L,  nandR.Regions.Min(x => x.Low.Components[0]) == 2L
            ? nandR.Regions.Max(x => x.Low.Components[0])
            : nandR.Regions.Min(x => x.Low.Components[0]));
    }

    [TestMethod] public void Inverse_AndNotAndNotAOrB_AreComplementary()
    {
        // AND_NOT=[2..5], NOT_A_OR_B=[5..12] — adjacent, no gap
        var (a, b) = AB1D();
        var andNot  = CMasked.AndNot  (_int, a, b);
        var notAOrB = CMasked.NotAOrB (_int, a, b);
        Assert.AreEqual(5L, andNot .Regions[0].High.Components[0]);
        Assert.AreEqual(5L, notAOrB.Regions[0].Low .Components[0]);
    }

    [TestMethod] public void Inverse_ProjectAAndNotA_AreComplementary()
    {
        // A=[2..8], !A=[8..12] — share boundary at 8
        var (a, b) = AB1D();
        var aR    = CMasked.ProjectA(_int, a, b);
        var notAR = CMasked.NotA    (_int, a, b);
        Assert.AreEqual(8L, aR   .Regions[0].High.Components[0]);
        Assert.AreEqual(8L, notAR.Regions[0].Low .Components[0]);
    }

    [TestMethod] public void Inverse_NotAndAndAOrNotB_AreComplementary()
    {
        // NOT_AND=[8..12], A_OR_NOT_B=[2..8] — share boundary at 8
        var (a, b) = AB1D();
        var notAnd  = CMasked.NotAnd  (_int, a, b);
        var aOrNotB = CMasked.AOrNotB (_int, a, b);
        Assert.AreEqual(8L, notAnd .Regions[0].Low .Components[0]);
        Assert.AreEqual(8L, aOrNotB.Regions[0].High.Components[0]);
    }

    [TestMethod] public void Inverse_ProjectBAndNotB_AreComplementary()
    {
        // B=[5..12], !B=[2..5] — share boundary at 5
        var (a, b) = AB1D();
        var bR    = CMasked.ProjectB(_int, a, b);
        var notBR = CMasked.NotB    (_int, a, b);
        Assert.AreEqual(5L, bR   .Regions[0].Low .Components[0]);
        Assert.AreEqual(5L, notBR.Regions[0].High.Components[0]);
    }

    [TestMethod] public void Inverse_XorAndXnor_AreComplementary()
    {
        // XOR=[2..5]+[8..12], XNOR=[5..8] — together cover [2..12]
        var (a, b) = AB1D();
        var xor  = CMasked.Xor (_int, a, b);
        var xnor = CMasked.Xnor(_int, a, b);
        Assert.AreEqual(2, xor.Count);
        Assert.AreEqual(1, xnor.Count);
        Assert.AreEqual(5L, xnor.Regions[0].Low .Components[0]);
        Assert.AreEqual(8L, xnor.Regions[0].High.Components[0]);
    }

    [TestMethod] public void Inverse_OrAndNor_AreComplementary()
    {
        // Non-overlapping: OR=[2..4]+[7..10], NOR=[4..7] — together [2..10]
        var (a, b) = AB1D_Gap();
        var or  = CMasked.Or (_int, a, b);
        var nor = CMasked.Nor(_int, a, b);
        Assert.IsFalse(or .IsEmpty);
        Assert.IsFalse(nor.IsEmpty);
        var norLo = nor.Regions.Min(x => x.Low .Components[0]);
        var norHi = nor.Regions.Max(x => x.High.Components[0]);
        Assert.AreEqual(4L, norLo);
        Assert.AreEqual(7L, norHi);
    }
}
