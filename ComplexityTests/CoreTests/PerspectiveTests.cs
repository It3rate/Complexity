using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

[TestClass]
public class PerspectiveTests
{
    [TestMethod]
    public void ReadFromSubstrateExtractsCorrectAxes()
    {
        var space = CSpace.Integer(0, 100); // TPU=1
        var val = new CValue(space, new[] { 50.0 });
        var perspective = new Perspective(space, val, 1.0);

        var substrate = new long[] { 42L };
        var read = perspective.ReadFromSubstrate(substrate);
        Assert.AreEqual(42L, read.Components[0]);
    }

    [TestMethod]
    public void ReadFromSubstrateWithAxisMap()
    {
        var space = CSpace.Integer(0, 100);
        var val = space.Value(50.0);
        var perspective = new Perspective(space, val, 1.0, new[] { 2 });

        var substrate = new long[] { 10L, 20L, 30L };
        var read = perspective.ReadFromSubstrate(substrate);
        Assert.AreEqual(30L, read.Components[0]);
    }

    [TestMethod]
    public void NoPressureWhenWithinBounds()
    {
        var space = CSpace.Integer(0, 100);
        var val = space.Value(50.0);
        var perspective = new Perspective(space, val, 1.0);

        var substrate = new long[] { 50L };
        var pressure = perspective.ComputePressure(substrate);
        Assert.AreEqual(0L, pressure[0]);
    }

    [TestMethod]
    public void PressurePushesUpWhenBelowMin()
    {
        var space = CSpace.Integer(0, 100); // TPU=1, bounds [0..100] ticks
        var val = space.Value(50.0);
        var perspective = new Perspective(space, val, 1.0);

        // Substrate at -20 ticks, 20 ticks below min=0
        var substrate = new long[] { -20L };
        var pressure = perspective.ComputePressure(substrate);
        Assert.IsTrue(pressure[0] > 0, "Pressure should push upward");
        Assert.AreEqual(20L, pressure[0]); // direction(+1) * magnitude(20) * confidence(1.0)
    }

    [TestMethod]
    public void PressurePushesDownWhenAboveMax()
    {
        var space = CSpace.Integer(0, 100);
        var val = space.Value(50.0);
        var perspective = new Perspective(space, val, 1.0);

        var substrate = new long[] { 130L };
        var pressure = perspective.ComputePressure(substrate);
        Assert.IsTrue(pressure[0] < 0, "Pressure should push downward");
        Assert.AreEqual(-30L, pressure[0]);
    }

    [TestMethod]
    public void ConfidenceScalesPressure()
    {
        var space = CSpace.Integer(0, 100);
        var val = space.Value(50.0);

        var weakPerspective = new Perspective(space, val, 0.25);
        var strongPerspective = new Perspective(space, val, 1.0);

        var substrate = new long[] { -20L }; // 20 ticks below min
        var weakPressure = weakPerspective.ComputePressure(substrate);
        var strongPressure = strongPerspective.ComputePressure(substrate);

        Assert.AreEqual(5L, weakPressure[0]);   // (long)(20 * 0.25) = 5
        Assert.AreEqual(20L, strongPressure[0]); // (long)(20 * 1.0) = 20
    }

    [TestMethod]
    public void ResidualTensionMatchesDirect()
    {
        var space = CSpace.Integer(0, 100);
        var val = space.Value(50.0);
        var perspective = new Perspective(space, val, 1.0);

        var substrate = new long[] { 120L };
        var residual = perspective.ResidualTension(substrate);
        Assert.IsFalse(residual.IsValid);
        Assert.AreEqual(20L, residual.Total);
    }

    [TestMethod]
    public void TwoAxisPerspectivePressure()
    {
        var space = CSpace.Segment(0, 10); // TPU=1
        var val = space.Value(5.0, 5.0);
        var perspective = new Perspective(space, val, 1.0);

        // unot=-5 (5 below min=0), unit=15 (5 above max=10)
        var substrate = new long[] { -5L, 15L };
        var pressure = perspective.ComputePressure(substrate);

        Assert.IsTrue(pressure[0] > 0, "Unot pressure should push up");
        Assert.IsTrue(pressure[1] < 0, "Unit pressure should push down");
        Assert.AreEqual(5L, pressure[0]);
        Assert.AreEqual(-5L, pressure[1]);
    }

    [TestMethod]
    public void NegotiationConvergesForCompatiblePerspectives()
    {
        var space = CSpace.Integer(0, 100);
        var p1 = new Perspective(space, space.Value(50.0), 1.0);
        var p2 = new Perspective(space, space.Value(50.0), 1.0);

        var (state, residuals) = Negotiation.Negotiate(
            new[] { p1, p2 },
            new long[] { -50L },
            maxIterations: 200,
            threshold: 1,
            damping: 0.3);

        Assert.IsTrue(state[0] >= -1, $"State should be near or within bounds, got {state[0]}");
    }

    [TestMethod]
    public void NegotiationWithConflictingPerspectives()
    {
        var space1 = CSpace.Integer(0, 10);
        var space2 = CSpace.Integer(20, 30);

        var p1 = new Perspective(space1, space1.Value(5.0), 1.0);
        var p2 = new Perspective(space2, space2.Value(25.0), 1.0);

        var (state, residuals) = Negotiation.Negotiate(
            new[] { p1, p2 },
            new long[] { 15L },
            maxIterations: 200,
            threshold: 1,
            damping: 0.3);

        Assert.IsTrue(state[0] >= 9, $"State should be >= 9, got {state[0]}");
        Assert.IsTrue(state[0] <= 21, $"State should be <= 21, got {state[0]}");
    }

    [TestMethod]
    public void NegotiationWithUnequalConfidence()
    {
        var space1 = CSpace.Integer(0, 10);
        var space2 = CSpace.Integer(20, 30);

        var p1 = new Perspective(space1, space1.Value(5.0), 0.1);
        var p2 = new Perspective(space2, space2.Value(25.0), 1.0);

        var (state, residuals) = Negotiation.Negotiate(
            new[] { p1, p2 },
            new long[] { 15L },
            maxIterations: 300,
            threshold: 1,
            damping: 0.3);

        Assert.IsTrue(state[0] > 15, $"Strong perspective should pull state higher, got {state[0]}");
    }

    [TestMethod]
    public void NegotiationResidualTensions()
    {
        var space = CSpace.Integer(0, 100);
        var p1 = new Perspective(space, space.Value(50.0), 1.0);

        var (state, residuals) = Negotiation.Negotiate(
            new[] { p1 },
            new long[] { 50L },
            maxIterations: 10);

        Assert.AreEqual(1, residuals.Length);
        Assert.IsTrue(residuals[0].IsValid);
    }

    [TestMethod]
    public void PerspectiveToStringTest()
    {
        var space = CSpace.Integer(0, 100);
        var perspective = new Perspective(space, space.Value(50.0), 0.8);
        var str = perspective.ToString();
        Assert.IsTrue(str.Contains("integer"));
        Assert.IsTrue(str.Contains("0.8"));
    }
}
