using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

[TestClass]
public class TensionTests
{
    [TestMethod]
    public void ValueWithinBoundsHasNoTension()
    {
        var space = CSpace.Integer(0, 100);
        var val = new CValue(space, new[] { 50.0 });
        var tension = Tension.Compute(val);

        Assert.IsTrue(tension.IsValid);
        Assert.AreEqual(0L, tension.Total);
        Assert.AreEqual(0, tension.PerAxis[0].Direction);
    }

    [TestMethod]
    public void ValueAboveMaxHasTensionPushingDown()
    {
        var space = CSpace.Integer(0, 100); // TPU=1, so ticks == values
        var val = new CValue(space, new[] { 120.0 });
        var tension = Tension.Compute(val);

        Assert.IsFalse(tension.IsValid);
        Assert.AreEqual(20L, tension.Total);
        Assert.AreEqual(-1, tension.PerAxis[0].Direction);
        Assert.AreEqual(20L, tension.PerAxis[0].Magnitude);
    }

    [TestMethod]
    public void ValueBelowMinHasTensionPushingUp()
    {
        var space = CSpace.Integer(0, 100);
        var val = new CValue(space, new[] { -30.0 });
        var tension = Tension.Compute(val);

        Assert.IsFalse(tension.IsValid);
        Assert.AreEqual(30L, tension.Total);
        Assert.AreEqual(+1, tension.PerAxis[0].Direction);
        Assert.AreEqual(30L, tension.PerAxis[0].Magnitude);
    }

    [TestMethod]
    public void MultidimensionalTension()
    {
        var space = CSpace.Segment(0, 10); // TPU=1
        var val = new CValue(space, new[] { -5.0, 15.0 });
        var tension = Tension.Compute(val);

        Assert.IsFalse(tension.IsValid);
        Assert.AreEqual(5L, tension.PerAxis[0].Magnitude);
        Assert.AreEqual(+1, tension.PerAxis[0].Direction);
        Assert.AreEqual(5L, tension.PerAxis[1].Magnitude);
        Assert.AreEqual(-1, tension.PerAxis[1].Direction);
        Assert.AreEqual(10L, tension.Total);
    }

    [TestMethod]
    public void ValidityScoreDecreasesWithTension()
    {
        var space = CSpace.Integer(0, 100);

        var inBounds = new CValue(space, new[] { 50.0 });
        var slightlyOut = new CValue(space, new[] { 101.0 });
        var wayOut = new CValue(space, new[] { 200.0 });

        var t1 = Tension.Compute(inBounds);
        var t2 = Tension.Compute(slightlyOut);
        var t3 = Tension.Compute(wayOut);

        Assert.AreEqual(1.0, t1.Validity, Utils.Tolerance);
        Assert.IsTrue(t2.Validity < t1.Validity);
        Assert.IsTrue(t3.Validity < t2.Validity);
        Assert.IsTrue(t3.Validity > 0);
    }

    [TestMethod]
    public void WorstAxisTest()
    {
        var space = CSpace.Segment(0, 10);
        var val = new CValue(space, new[] { -2.0, 50.0 }); // unot: 2 off, unit: 40 off
        var tension = Tension.Compute(val);

        Assert.AreEqual(1, tension.Worst.AxisIndex);
        Assert.AreEqual(40L, tension.Worst.Magnitude);
    }

    [TestMethod]
    public void CValueComputeTensionConvenience()
    {
        var space = CSpace.Integer(0, 100);
        var val = new CValue(space, new[] { 50.0 });
        var tension = val.ComputeTension();
        Assert.IsTrue(tension.IsValid);
    }

    [TestMethod]
    public void UnboundedAxesNeverHaveTension()
    {
        var space = CSpace.Segment(); // default: unbounded (long.Min/Max)
        var val = new CValue(space, new[] { 999999.0, -999999.0 });
        var tension = Tension.Compute(val);
        Assert.IsTrue(tension.IsValid);
        Assert.AreEqual(0L, tension.Total);
    }

    [TestMethod]
    public void ProportionOutOfRangeDetected()
    {
        var space = CSpace.Proportion(100); // TPU=100, bounds [0..100] ticks
        // 1.2 → 120 ticks, max = 100 ticks → 20 ticks over
        var val = new CValue(space, new[] { 1.2 });
        var tension = Tension.Compute(val);
        Assert.IsFalse(tension.IsValid);
        Assert.AreEqual(20L, tension.PerAxis[0].Magnitude);
        Assert.AreEqual(-1, tension.PerAxis[0].Direction);
    }
}
