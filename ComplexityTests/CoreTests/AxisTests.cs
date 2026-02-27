using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

/// <summary>
/// Tests for Axis — the basic minimum primitive of the number system.
/// An axis is one direction on the number line from zero to max range,
/// evaluated by its unit and the unit's resolution (ticks per unit).
/// </summary>
[TestClass]
public class AxisTests
{
    // ================================================================
    //  FACTORY METHOD TESTS
    // ================================================================

    [TestMethod]
    public void ForwardAxisFromZeroToMax()
    {
        // Forward: one direction from zero to a maximum, pointing right
        var axis = Axis.Forward("value", 100, ticksPerUnit: 10);
        Assert.AreEqual("value", axis.Name);
        Assert.AreEqual(+1, axis.Direction);
        Assert.AreEqual(0L, axis.MinBoundTicks);
        Assert.AreEqual(1000L, axis.MaxBoundTicks); // 100 * 10 ticks
        Assert.AreEqual(10L, axis.TicksPerUnit);
    }

    [TestMethod]
    public void BackwardAxisDirectionIsNegative()
    {
        // Backward: one direction from zero, pointing left (direction=-1)
        // Used as the unot/imaginary axis in segment/complex frames
        var axis = Axis.Backward("imag", 50, ticksPerUnit: 10);
        Assert.AreEqual(-1, axis.Direction);
        Assert.AreEqual(0L, axis.MinBoundTicks);
        Assert.AreEqual(500L, axis.MaxBoundTicks); // 50 * 10
    }

    [TestMethod]
    public void SignedAxisSymmetricRange()
    {
        // Signed: full positive and negative range, single positive unit
        // "To make a negative number we need a full positive and negative range
        //  and a single positive unit"
        var axis = Axis.Signed("value", 100, ticksPerUnit: 1);
        Assert.AreEqual(+1, axis.Direction);
        Assert.AreEqual(-100L, axis.MinBoundTicks);
        Assert.AreEqual(100L, axis.MaxBoundTicks);
    }

    [TestMethod]
    public void UnboundedAxisNoLimits()
    {
        var axis = Axis.Unbounded("x", +1, ticksPerUnit: 100);
        Assert.AreEqual(long.MinValue, axis.MinBoundTicks);
        Assert.AreEqual(long.MaxValue, axis.MaxBoundTicks);
        Assert.IsFalse(axis.HasMinBound);
        Assert.IsFalse(axis.HasMaxBound);
        Assert.AreEqual(100L, axis.TicksPerUnit);
    }

    [TestMethod]
    public void CyclicAxisWraps()
    {
        var axis = Axis.Cyclic("angle", 360, ticksPerUnit: 10);
        Assert.IsTrue(axis.IsCyclic);
        Assert.AreEqual(3600L, axis.CyclePeriodTicks); // 360 * 10
        Assert.AreEqual(360.0, axis.CyclePeriod, Utils.Tolerance);
    }

    // ================================================================
    //  RESOLUTION TESTS
    //  "There is no floating point resolution at all in this system,
    //   the resolution is always based on the number of ticks in a unit.
    //   This number can be very large and of course it can be one tick
    //   per unit creating integers."
    // ================================================================

    [TestMethod]
    public void IntegerResolution_OneTickPerUnit()
    {
        var axis = Axis.Forward("int", 10, ticksPerUnit: 1);
        Assert.AreEqual(10L, axis.MaxBoundTicks); // 10 * 1 = 10 ticks
        Assert.AreEqual(1L, axis.TicksPerUnit);
    }

    [TestMethod]
    public void TenthsResolution_TenTicksPerUnit()
    {
        var axis = Axis.Forward("dec", 10, ticksPerUnit: 10);
        Assert.AreEqual(100L, axis.MaxBoundTicks); // 10 * 10 = 100 ticks
        Assert.AreEqual(10L, axis.TicksPerUnit);
    }

    [TestMethod]
    public void ThousandthsResolution_HighTickCount()
    {
        var axis = Axis.Forward("hi", 10, ticksPerUnit: 1000);
        Assert.AreEqual(10000L, axis.MaxBoundTicks); // 10 * 1000
        Assert.AreEqual(1000L, axis.TicksPerUnit);
    }

    [TestMethod]
    public void VeryHighResolution()
    {
        // Resolution can be very large
        var axis = Axis.Forward("precise", 1, ticksPerUnit: 1000000);
        Assert.AreEqual(1000000L, axis.MaxBoundTicks);
        Assert.AreEqual(1000000L, axis.TicksPerUnit);
    }

    // ================================================================
    //  BOUND PROPERTIES
    // ================================================================

    [TestMethod]
    public void BoundsAsValues()
    {
        var axis = Axis.Forward("value", 5, ticksPerUnit: 10);
        Assert.AreEqual(0.0, axis.MinBound, Utils.Tolerance);
        Assert.AreEqual(5.0, axis.MaxBound, Utils.Tolerance);
        Assert.IsTrue(axis.HasMinBound);
        Assert.IsTrue(axis.HasMaxBound);
    }

    [TestMethod]
    public void UnboundedBoundsAreNaN()
    {
        var axis = Axis.Unbounded("x", +1);
        Assert.IsTrue(double.IsNaN(axis.MinBound));
        Assert.IsTrue(double.IsNaN(axis.MaxBound));
    }

    // ================================================================
    //  TICK-BASED CONSTRUCTOR
    // ================================================================

    [TestMethod]
    public void TickBasedConstructor()
    {
        var axis = new Axis("direct", +1, 0L, 1000L, 10L);
        Assert.AreEqual(0L, axis.MinBoundTicks);
        Assert.AreEqual(1000L, axis.MaxBoundTicks);
        Assert.AreEqual(10L, axis.TicksPerUnit);
        Assert.AreEqual(0.0, axis.MinBound, Utils.Tolerance);
        Assert.AreEqual(100.0, axis.MaxBound, Utils.Tolerance);
    }

    // ================================================================
    //  TOSTRING
    // ================================================================

    [TestMethod]
    public void ToStringContainsNameAndDirection()
    {
        var axis = Axis.Forward("unit", 100, ticksPerUnit: 10);
        var str = axis.ToString();
        Assert.IsTrue(str.Contains("unit"), $"Expected 'unit' in '{str}'");
        Assert.IsTrue(str.Contains("+"), $"Expected '+' in '{str}'");
        Assert.IsTrue(str.Contains("tpu=10"), $"Expected 'tpu=10' in '{str}'");
    }
}
