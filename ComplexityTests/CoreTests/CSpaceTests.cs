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
}
