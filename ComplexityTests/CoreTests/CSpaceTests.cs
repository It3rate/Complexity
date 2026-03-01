using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

[TestClass]
public class CSpaceTests
{
    [TestMethod]
    public void IntegerSpaceCreation()
    {
        var space = CSpace.Integer(-100, 100);
        Assert.AreEqual(1, space.Dims);
        Assert.AreEqual("integer", space.Name);
        Assert.AreEqual(+1, space.Axes[0].Direction);
        Assert.AreEqual(-100.0, space.Axes[0].MinBound, Utils.Tolerance);
        Assert.AreEqual(100.0, space.Axes[0].MaxBound, Utils.Tolerance);
        Assert.AreEqual(-100L, space.Axes[0].MinBoundTicks);
        Assert.AreEqual(100L, space.Axes[0].MaxBoundTicks);
        Assert.AreEqual(1L, space.Axes[0].TicksPerUnit);
        Assert.AreEqual(1, space.Algebra.Length);
    }

    [TestMethod]
    public void SegmentSpaceCreation()
    {
        var space = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        Assert.AreEqual(2, space.Dims);
        Assert.AreEqual("segment", space.Name);
        Assert.AreEqual(10L, space.BasisTicks);

        Assert.AreEqual("unot", space.Axes[0].Name);
        Assert.AreEqual(-1, space.Axes[0].Direction);
        Assert.AreEqual("unit", space.Axes[1].Name);
        Assert.AreEqual(+1, space.Axes[1].Direction);
        Assert.AreEqual(4, space.Algebra.Length);
    }

    [TestMethod]
    public void ProportionSpaceCreation()
    {
        var space = CSpace.Proportion(100);
        Assert.AreEqual(1, space.Dims);
        Assert.AreEqual(0.0, space.Axes[0].MinBound, Utils.Tolerance);
        Assert.AreEqual(1.0, space.Axes[0].MaxBound, Utils.Tolerance);
        Assert.AreEqual(0L, space.Axes[0].MinBoundTicks);
        Assert.AreEqual(100L, space.Axes[0].MaxBoundTicks);
        Assert.AreEqual(100L, space.Axes[0].TicksPerUnit);
    }

    [TestMethod]
    public void ModularSpaceCreation()
    {
        var space = CSpace.Modular(12);
        Assert.AreEqual(1, space.Dims);
        Assert.IsTrue(space.Axes[0].IsCyclic);
        Assert.AreEqual(12.0, space.Axes[0].CyclePeriod, Utils.Tolerance);
        Assert.AreEqual(12L, space.Axes[0].CyclePeriodTicks);
        Assert.AreEqual(0.0, space.Axes[0].MinBound, Utils.Tolerance);
        Assert.AreEqual(11.0, space.Axes[0].MaxBound, Utils.Tolerance);
    }

    [TestMethod]
    public void QuaternionSpaceCreation()
    {
        var space = CSpace.Quaternion();
        Assert.AreEqual(4, space.Dims);
        Assert.AreEqual("quaternion", space.Name);
        Assert.AreEqual(+1, space.Axes[0].Direction);
        Assert.AreEqual(-1, space.Axes[1].Direction);
        Assert.AreEqual(-1, space.Axes[2].Direction);
        Assert.AreEqual(-1, space.Axes[3].Direction);
        Assert.AreEqual(16, space.Algebra.Length);
    }

    [TestMethod]
    public void SplitComplexSpaceCreation()
    {
        var space = CSpace.SplitComplex();
        Assert.AreEqual(2, space.Dims);
        Assert.AreEqual(4, space.Algebra.Length);
        var jj = space.Algebra.First(e => e.Left == 0 && e.Right == 0);
        Assert.AreEqual(+1, jj.Sign);
        Assert.AreEqual(1, jj.Target);
    }

    [TestMethod]
    public void DualNumberSpaceCreation()
    {
        var space = CSpace.DualNumber();
        Assert.AreEqual(2, space.Dims);
        var ee = space.Algebra.First(e => e.Left == 0 && e.Right == 0);
        Assert.AreEqual(0, ee.Sign);
    }

    [TestMethod]
    public void ZeroAndOneValues()
    {
        var space = CSpace.Segment();
        var zero = space.Zero();
        var one = space.One();

        Assert.AreEqual(0L, zero.Components[0]);
        Assert.AreEqual(0L, zero.Components[1]);
        Assert.IsTrue(zero.IsZero);

        Assert.AreEqual(0L, one.Components[0]); // unot = 0
        Assert.AreEqual(space.BasisTicks, one.Components[1]); // unit = TicksPerUnit ticks
        Assert.AreEqual(1.0, one.GetValue(1), Utils.Tolerance); // unit value = 1.0
        Assert.IsFalse(one.IsZero);
    }

    [TestMethod]
    public void DegreesOfFreedomTests()
    {
        var space = CSpace.Segment();
        Assert.AreEqual(2, space.DegreesOfFreedom());
        Assert.AreEqual(1, space.DegreesOfFreedom(new HashSet<int> { 0 }));
        Assert.AreEqual(0, space.DegreesOfFreedom(new HashSet<int> { 0, 1 }));
    }

    [TestMethod]
    public void MergeSpacesTest()
    {
        var xSpace = CSpace.Integer(-100, 100);
        var ySpace = CSpace.Integer(-100, 100);
        var merged = CSpace.Merge(xSpace, ySpace);
        Assert.AreEqual(2, merged.Dims);
        Assert.AreEqual(2, merged.Algebra.Length);
        Assert.AreEqual("integer×integer", merged.Name);
    }

    [TestMethod]
    public void MergeSpacesWithCrossAlgebra()
    {
        var a = CSpace.Integer();
        var b = CSpace.Integer();
        var cross = new[] { new AlgebraEntry(0, 1, 0, +1) };
        var merged = CSpace.Merge(a, b, cross);
        Assert.AreEqual(3, merged.Algebra.Length);
    }

    [TestMethod]
    public void ProjectAndEmbedTest()
    {
        var space2D = CSpace.Segment();
        var space1D = CSpace.Integer();

        var intVal = new CValue(space1D, new[] { 5.0 });
        var embedded = CSpace.Embed(intVal, space2D, new[] { 1 });
        Assert.AreEqual(0.0, embedded.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(5.0, embedded.GetValue(1), Utils.Tolerance);

        var segVal = new CValue(space2D, new[] { 3.0, 7.0 });
        var projected = CSpace.Project(segVal, space1D, new[] { 1 });
        Assert.AreEqual(7.0, projected.GetValue(0), Utils.Tolerance);
    }

    [TestMethod]
    public void ValueFactoryMethodTest()
    {
        var space = CSpace.Segment();
        var val = space.Value(2.0, 6.0);
        Assert.AreEqual(2.0, val.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(6.0, val.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ValueFactoryWrongDimsThrows()
    {
        var space = CSpace.Segment();
        space.Value(1.0, 2.0, 3.0);
    }

    [TestMethod]
    public void ValueFromTicksTest()
    {
        var space = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        var val = space.ValueFromTicks(30, 50);
        Assert.AreEqual(30L, val.Components[0]);
        Assert.AreEqual(50L, val.Components[1]);
        Assert.AreEqual(3.0, val.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(5.0, val.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void BasisTicksProperty()
    {
        var space = CSpace.Segment(-100, 100, ticksPerUnit: 100);
        Assert.AreEqual(100L, space.BasisTicks);
    }

    // ================================================================
    //  AFFINE KIND — Position vs Vector
    // ================================================================

    [TestMethod]
    public void CSpace_DefaultAffineTypeIsVector()
    {
        // All factory methods default to Vector — most frames work with displacements
        Assert.AreEqual(AffineKind.Vector, CSpace.Integer().AffineType);
        Assert.AreEqual(AffineKind.Vector, CSpace.Segment().AffineType);
        Assert.AreEqual(AffineKind.Vector, CSpace.Modular(12).AffineType);
        Assert.AreEqual(AffineKind.Vector, CSpace.Proportion().AffineType);
    }

    [TestMethod]
    public void CSpace_AffineTypeCanBeSetOnConstructor()
    {
        var space = new CSpace("xy", new[] { Axis.Signed("x", 10), Axis.Signed("y", 10) },
            new[] { new AlgebraEntry(0, 0, 0, +1), new AlgebraEntry(1, 1, 1, +1) },
            affineType: AffineKind.Position);
        Assert.AreEqual(AffineKind.Position, space.AffineType);
    }

    [TestMethod]
    public void ResultAffineKind_VectorPlusVector_IsVector()
    {
        var result = CSpace.ResultAffineKind(AffineKind.Vector, AffineKind.Vector, isSubtraction: false);
        Assert.AreEqual(AffineKind.Vector, result);
    }

    [TestMethod]
    public void ResultAffineKind_PositionPlusVector_IsPosition()
    {
        var result = CSpace.ResultAffineKind(AffineKind.Position, AffineKind.Vector, isSubtraction: false);
        Assert.AreEqual(AffineKind.Position, result);
    }

    [TestMethod]
    public void ResultAffineKind_VectorPlusPosition_IsPosition()
    {
        var result = CSpace.ResultAffineKind(AffineKind.Vector, AffineKind.Position, isSubtraction: false);
        Assert.AreEqual(AffineKind.Position, result);
    }

    [TestMethod]
    public void ResultAffineKind_PositionPlusPosition_IsInvalid()
    {
        // Adding two absolute positions has no geometric meaning
        var result = CSpace.ResultAffineKind(AffineKind.Position, AffineKind.Position, isSubtraction: false);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ResultAffineKind_PositionMinusPosition_IsVector()
    {
        // The difference between two positions is a displacement vector
        var result = CSpace.ResultAffineKind(AffineKind.Position, AffineKind.Position, isSubtraction: true);
        Assert.AreEqual(AffineKind.Vector, result);
    }

    [TestMethod]
    public void ResultAffineKind_PositionMinusVector_IsPosition()
    {
        var result = CSpace.ResultAffineKind(AffineKind.Position, AffineKind.Vector, isSubtraction: true);
        Assert.AreEqual(AffineKind.Position, result);
    }

    [TestMethod]
    public void ResultAffineKind_VectorMinusPosition_IsInvalid()
    {
        // Subtracting a position from a vector has no geometric meaning
        var result = CSpace.ResultAffineKind(AffineKind.Vector, AffineKind.Position, isSubtraction: true);
        Assert.IsNull(result);
    }

    // ================================================================
    //  CHIRALITY
    // ================================================================

    [TestMethod]
    public void CSpace_DefaultChiralityIsPositive()
    {
        Assert.AreEqual(+1, CSpace.Integer().Chirality);
        Assert.AreEqual(+1, CSpace.Segment().Chirality);
        Assert.AreEqual(+1, CSpace.Quaternion().Chirality);
    }

    [TestMethod]
    public void DeriveChirality_1D_IsAlwaysPositive()
    {
        // 1D spaces have no chirality concept — always returns +1
        var algebra = new[] { new AlgebraEntry(0, 0, 0, +1) };
        Assert.AreEqual(+1, CSpace.DeriveChirality(algebra, 1));
    }

    [TestMethod]
    public void DeriveChirality_ComplexSpace_IsRightHanded()
    {
        // Complex multiplication: unit*unot → +unot (first cross-term has positive sign)
        var complexAlgebra = CSpace.Segment().Algebra;
        Assert.AreEqual(+1, CSpace.DeriveChirality(complexAlgebra, 2));
    }

    [TestMethod]
    public void Mirror_FlipsChirality()
    {
        var space = CSpace.Segment();
        Assert.AreEqual(+1, space.Chirality);

        var mirrored = space.Mirror(0); // flip the unot axis
        Assert.AreEqual(-1, mirrored.Chirality);

        var doubleMirrored = mirrored.Mirror(0);
        Assert.AreEqual(+1, doubleMirrored.Chirality);
    }

    [TestMethod]
    public void Mirror_PreservesArithmetic_ForEvenFlips()
    {
        // Mirroring twice should restore arithmetic to the original
        var space = CSpace.Segment(-10, 10, ticksPerUnit: 10);
        var orig = space.Value(2.0, 5.0);

        var twice = space.Mirror(0).Mirror(0);
        var copy = new CValue(twice, orig.Components);
        Assert.AreEqual(orig.GetValue(0), copy.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(orig.GetValue(1), copy.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void Mirror_ChangesAxisDirection()
    {
        var space = CSpace.Integer(-10, 10);
        Assert.AreEqual(+1, space.Axes[0].Direction);

        var mirrored = space.Mirror(0);
        Assert.AreEqual(-1, mirrored.Axes[0].Direction);
    }

    // ================================================================
    //  METRIC KIND
    // ================================================================

    [TestMethod]
    public void CSpace_DefaultMetricIsEuclidean()
    {
        Assert.AreEqual(MetricKind.Euclidean, CSpace.Integer().Metric);
        Assert.AreEqual(MetricKind.Euclidean, CSpace.Segment().Metric);
    }

    [TestMethod]
    public void CSpace_MetricCanBeSetOnConstructor()
    {
        var space = new CSpace("grid",
            new[] { Axis.Signed("x", 10), Axis.Signed("y", 10) },
            new[] { new AlgebraEntry(0, 0, 0, +1), new AlgebraEntry(1, 1, 1, +1) },
            metric: MetricKind.Manhattan);
        Assert.AreEqual(MetricKind.Manhattan, space.Metric);
    }

    // ================================================================
    //  TOSTRING — includes new properties
    // ================================================================

    [TestMethod]
    public void CSpace_ToStringIncludesNewProperties()
    {
        var space = CSpace.Segment();
        var str = space.ToString();
        Assert.IsTrue(str.Contains("Vector"),   $"Expected 'Vector' in '{str}'");
        Assert.IsTrue(str.Contains("Euclidean"), $"Expected 'Euclidean' in '{str}'");
    }

    // ================================================================
    //  AXIS KIND PRESERVED THROUGH COMPLEX / GRID FACTORIES
    // ================================================================

    [TestMethod]
    public void Complex_PreservesAxisKindFromInput()
    {
        var realAxis = Axis.Signed("real", 10, kind: AxisKind.Spatial);
        var imagAxis = Axis.Signed("imag", 10, kind: AxisKind.Intensity);
        var space = CSpace.Complex(realAxis, imagAxis);

        // Both axes should retain their Kind values
        var unitAxis = space.Axes.First(a => a.Direction == +1);
        var unotAxis = space.Axes.First(a => a.Direction == -1);
        Assert.AreEqual(AxisKind.Spatial,   unitAxis.Kind);
        Assert.AreEqual(AxisKind.Intensity, unotAxis.Kind);
    }

    [TestMethod]
    public void Grid_PreservesAxisKindFromInput()
    {
        var xAxis = Axis.Signed("x", 10, kind: AxisKind.Spatial);
        var yAxis = Axis.Signed("y", 10, kind: AxisKind.Temporal);
        var space = CSpace.Grid(xAxis, yAxis);

        Assert.AreEqual(AxisKind.Spatial,  space.Axes[0].Kind);
        Assert.AreEqual(AxisKind.Temporal, space.Axes[1].Kind);
    }
}
