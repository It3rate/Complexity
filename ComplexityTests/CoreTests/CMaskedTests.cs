using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

[TestClass]
public class CMaskedTests
{
    private CSpace _space;

    [TestInitialize]
    public void Init()
    {
        _space = CSpace.Integer(-100, 100); // TPU=1, ticks == values
    }

    [TestMethod]
    public void AndOverlappingIntervals()
    {
        var a = (_space.Value(2.0), _space.Value(8.0));
        var b = (_space.Value(5.0), _space.Value(12.0));

        var result = CMasked.And(_space, a, b);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(5L, result.Regions[0].Low.Components[0]);
        Assert.AreEqual(8L, result.Regions[0].High.Components[0]);
    }

    [TestMethod]
    public void AndNonOverlappingIsEmpty()
    {
        var a = (_space.Value(2.0), _space.Value(4.0));
        var b = (_space.Value(6.0), _space.Value(8.0));

        var result = CMasked.And(_space, a, b);
        Assert.IsTrue(result.IsEmpty);
    }

    [TestMethod]
    public void OrOverlappingIntervals()
    {
        var a = (_space.Value(2.0), _space.Value(8.0));
        var b = (_space.Value(5.0), _space.Value(12.0));

        var result = CMasked.Or(_space, a, b);
        Assert.IsFalse(result.IsEmpty);
        var lo = result.Regions.Min(r => r.Low.Components[0]);
        var hi = result.Regions.Max(r => r.High.Components[0]);
        Assert.AreEqual(2L, lo);
        Assert.AreEqual(12L, hi);
    }

    [TestMethod]
    public void And2DOverlappingRectangles()
    {
        var space = CSpace.Segment(-100, 100); // TPU=1
        var aLow = space.Value(0.0, 0.0);
        var aHigh = space.Value(6.0, 6.0);
        var bLow = space.Value(3.0, 3.0);
        var bHigh = space.Value(9.0, 9.0);

        var result = CMasked.And(space, (aLow, aHigh), (bLow, bHigh));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(3L, result.Regions[0].Low.Components[0]);
        Assert.AreEqual(6L, result.Regions[0].High.Components[0]);
        Assert.AreEqual(3L, result.Regions[0].Low.Components[1]);
        Assert.AreEqual(6L, result.Regions[0].High.Components[1]);
    }

    [TestMethod]
    public void And2DNonOverlappingOnOneAxisIsEmpty()
    {
        var space = CSpace.Segment(-100, 100);
        var aLow = space.Value(0.0, 0.0);
        var aHigh = space.Value(6.0, 4.0);
        var bLow = space.Value(3.0, 6.0);
        var bHigh = space.Value(9.0, 9.0);

        var result = CMasked.And(space, (aLow, aHigh), (bLow, bHigh));
        Assert.IsTrue(result.IsEmpty);
    }

    [TestMethod]
    public void EmptyResultTest()
    {
        var empty = CMasked.Empty(_space);
        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual(0, empty.Count);
    }

    [TestMethod]
    public void XorProducesTwoRegions()
    {
        var a = (_space.Value(0.0), _space.Value(10.0));
        var b = (_space.Value(5.0), _space.Value(15.0));

        var result = CMasked.Xor(_space, a, b);
        Assert.IsFalse(result.IsEmpty);
        Assert.IsTrue(result.Count >= 1);
    }
}
