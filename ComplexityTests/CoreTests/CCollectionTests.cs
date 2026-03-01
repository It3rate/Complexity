using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

/// <summary>
/// Tests for CCollection — ordered sequences of CValues with declared mode and closure.
///
/// Three things CCollection expresses that individual CValues cannot:
///   1. Ordering — implicit in list index
///   2. Closure — IsClosed: does the sequence loop?
///   3. Provenance — Mode: same entity over time vs different entities at one time
/// </summary>
[TestClass]
public class CCollectionTests
{
    // A 2D signed grid space for all 2D tests
    private CSpace _grid = null!;

    // A 1D signed integer space for 1D tests
    private CSpace _int = null!;

    [TestInitialize]
    public void Init()
    {
        _grid = CSpace.Grid(
            Axis.Signed("x", 20, ticksPerUnit: 10),
            Axis.Signed("y", 20, ticksPerUnit: 10));
        _int = CSpace.Integer(-100, 100, ticksPerUnit: 10);
    }

    // ================================================================
    //  CONSTRUCTION AND BASIC PROPERTIES
    // ================================================================

    [TestMethod]
    public void Construction_DefaultModeIsUnspecified()
    {
        var col = new CCollection(new[] { _grid.Value(1.0, 2.0), _grid.Value(3.0, 4.0) });
        Assert.AreEqual(SampleMode.Unspecified, col.Mode);
        Assert.IsFalse(col.IsClosed);
    }

    [TestMethod]
    public void Construction_ModeAndClosureRoundTrip()
    {
        var col = new CCollection(
            new[] { _grid.Value(0.0, 0.0), _grid.Value(5.0, 5.0) },
            mode: SampleMode.Trajectory,
            isClosed: true);

        Assert.AreEqual(SampleMode.Trajectory, col.Mode);
        Assert.IsTrue(col.IsClosed);
    }

    [TestMethod]
    public void Construction_CountFirstLast()
    {
        var v0 = _grid.Value(1.0, 2.0);
        var v1 = _grid.Value(3.0, 4.0);
        var v2 = _grid.Value(5.0, 6.0);
        var col = new CCollection(new[] { v0, v1, v2 }, SampleMode.Trajectory);

        Assert.AreEqual(3, col.Count);
        Assert.AreEqual(v0, col.First);
        Assert.AreEqual(v2, col.Last);
    }

    [TestMethod]
    public void Construction_SpaceComesFromFirstValue()
    {
        var col = new CCollection(new[] { _grid.Value(1.0, 2.0), _grid.Value(3.0, 4.0) });
        Assert.AreSame(_grid, col.Space);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void EmptyCollection_SpaceThrows()
    {
        var col = new CCollection(Array.Empty<CValue>());
        _ = col.Space;
    }

    // ================================================================
    //  BOUNDS — universal (all modes)
    // ================================================================

    [TestMethod]
    public void Bounds_SingleValue()
    {
        var pt = _grid.Value(3.0, 7.0);
        var col = new CCollection(new[] { pt });
        var (min, max) = col.Bounds();

        Assert.AreEqual(3.0, min.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(7.0, min.GetValue(1), Utils.Tolerance);
        Assert.AreEqual(3.0, max.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(7.0, max.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void Bounds_MultipleValues_ComponentWiseMinMax()
    {
        var pts = new[]
        {
            _grid.Value( 1.0,  8.0),
            _grid.Value( 5.0,  2.0),
            _grid.Value(-3.0,  6.0),
            _grid.Value( 4.0, -1.0),
        };
        var col = new CCollection(pts);
        var (min, max) = col.Bounds();

        Assert.AreEqual(-3.0, min.GetValue(0), Utils.Tolerance); // min X
        Assert.AreEqual(-1.0, min.GetValue(1), Utils.Tolerance); // min Y
        Assert.AreEqual( 5.0, max.GetValue(0), Utils.Tolerance); // max X
        Assert.AreEqual( 8.0, max.GetValue(1), Utils.Tolerance); // max Y
    }

    // ================================================================
    //  MEAN — universal (all modes)
    // ================================================================

    [TestMethod]
    public void Mean_OneDimensional_Integer()
    {
        // Simple average of 4 values: (2 + 4 + 6 + 8) / 4 = 5
        var col = new CCollection(new[]
        {
            _int.Value(2.0), _int.Value(4.0), _int.Value(6.0), _int.Value(8.0)
        });
        var mean = col.Mean();
        Assert.AreEqual(5.0, mean.GetValue(0), Utils.Tolerance);
    }

    [TestMethod]
    public void Mean_TwoDimensional_Grid()
    {
        // Centroid of four corners of a rectangle
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(4.0, 0.0),
            _grid.Value(4.0, 6.0),
            _grid.Value(0.0, 6.0),
        });
        var mean = col.Mean();
        Assert.AreEqual(2.0, mean.GetValue(0), Utils.Tolerance); // centre X = 2
        Assert.AreEqual(3.0, mean.GetValue(1), Utils.Tolerance); // centre Y = 3
    }

    [TestMethod]
    public void Mean_SingleValue_IsThatValue()
    {
        var pt = _grid.Value(3.5, -1.5);
        var col = new CCollection(new[] { pt });
        var mean = col.Mean();
        Assert.AreEqual(pt.GetValue(0), mean.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(pt.GetValue(1), mean.GetValue(1), Utils.Tolerance);
    }

    // ================================================================
    //  TRAJECTORY OPERATIONS: Deltas, Displacement, PathLength
    // ================================================================

    [TestMethod]
    public void Trajectory_Deltas_ConsecutiveDifferences()
    {
        // Straight-line path: (0,0) → (1,0) → (3,0) → (6,0)
        // Deltas: (1,0), (2,0), (3,0)
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(1.0, 0.0),
            _grid.Value(3.0, 0.0),
            _grid.Value(6.0, 0.0),
        }, SampleMode.Trajectory);

        var deltas = col.Deltas();

        Assert.AreEqual(3, deltas.Count);
        Assert.AreEqual(1.0, deltas[0].GetValue(0), Utils.Tolerance);
        Assert.AreEqual(2.0, deltas[1].GetValue(0), Utils.Tolerance);
        Assert.AreEqual(3.0, deltas[2].GetValue(0), Utils.Tolerance);
        // Y stays zero throughout
        Assert.AreEqual(0.0, deltas[0].GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void Trajectory_Displacement_FirstToLast()
    {
        // Non-straight path: (0,0) → (3,4) → (5,0)
        // Displacement = Last - First = (5,0) - (0,0) = (5,0)
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(3.0, 4.0),
            _grid.Value(5.0, 0.0),
        }, SampleMode.Trajectory);

        var disp = col.Displacement();
        Assert.AreEqual(5.0, disp.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, disp.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void Trajectory_Displacement_ClosedPath_IsNearZero()
    {
        // A closed loop returns to its start: displacement ≈ zero
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(5.0, 0.0),
            _grid.Value(5.0, 5.0),
            _grid.Value(0.0, 5.0),
            _grid.Value(0.0, 0.0), // same as first
        }, SampleMode.Trajectory, isClosed: true);

        var disp = col.Displacement();
        Assert.AreEqual(0.0, disp.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, disp.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void Trajectory_PathLength_StraightLine()
    {
        // Straight path from (0,0) to (3,4): Euclidean length = 5
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(3.0, 4.0),
        }, SampleMode.Trajectory);

        Assert.AreEqual(5.0, col.PathLength(), Utils.Tolerance);
    }

    [TestMethod]
    public void Trajectory_PathLength_MultipleSteps()
    {
        // Two steps each of length 5: (0,0)→(3,4)→(6,8)
        // Total = 5 + 5 = 10
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(3.0, 4.0),
            _grid.Value(6.0, 8.0),
        }, SampleMode.Trajectory);

        Assert.AreEqual(10.0, col.PathLength(), Utils.Tolerance);
    }

    [TestMethod]
    public void Trajectory_PathLength_ClosedAddsReturnSegment()
    {
        // Square: (0,0)→(4,0)→(4,4)→(0,4)→back to (0,0) automatically
        // Each side = 4, so total perimeter = 16
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(4.0, 0.0),
            _grid.Value(4.0, 4.0),
            _grid.Value(0.0, 4.0),
        }, SampleMode.Trajectory, isClosed: true);

        Assert.AreEqual(16.0, col.PathLength(), Utils.Tolerance);
    }

    // ================================================================
    //  ENSEMBLE OPERATIONS: Spread
    // ================================================================

    [TestMethod]
    public void Ensemble_Spread_TightCluster_IsSmall()
    {
        // Points very close together: spread should be near zero
        var col = new CCollection(new[]
        {
            _grid.Value(5.0, 5.0),
            _grid.Value(5.1, 5.0),
            _grid.Value(5.0, 5.1),
            _grid.Value(4.9, 5.0),
        }, SampleMode.Ensemble);

        var spread = col.Spread();
        Assert.AreEqual(4, spread.Count);
        foreach (var s in spread)
            Assert.IsTrue(s < 0.5, $"Expected small spread, got {s}");
    }

    [TestMethod]
    public void Ensemble_Spread_EqualDistanceFromMean()
    {
        // Four points at distance 5 from origin along axes — mean is (0,0)
        // Each point is exactly 5 from the mean (via Euclidean)
        var col = new CCollection(new[]
        {
            _grid.Value( 5.0,  0.0),
            _grid.Value(-5.0,  0.0),
            _grid.Value( 0.0,  5.0),
            _grid.Value( 0.0, -5.0),
        }, SampleMode.Ensemble);

        var mean = col.Mean();
        Assert.AreEqual(0.0, mean.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, mean.GetValue(1), Utils.Tolerance);

        var spread = col.Spread();
        foreach (var s in spread)
            Assert.AreEqual(5.0, s, Utils.Tolerance);
    }

    // ================================================================
    //  MODE ENFORCEMENT
    // ================================================================

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ModeEnforcement_TrajectoryOp_OnEnsemble_Throws()
    {
        // Deltas() requires Trajectory or Unspecified — Ensemble should throw
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(1.0, 1.0),
        }, SampleMode.Ensemble);

        _ = col.Deltas();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ModeEnforcement_EnsembleOp_OnTrajectory_Throws()
    {
        // Spread() requires Ensemble or Unspecified — Trajectory should throw
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(1.0, 1.0),
        }, SampleMode.Trajectory);

        _ = col.Spread();
    }

    [TestMethod]
    public void ModeEnforcement_Unspecified_AllowsAllOps()
    {
        // Unspecified mode skips validation — both trajectory and ensemble ops work
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(3.0, 4.0),
        }, SampleMode.Unspecified);

        // Should not throw
        var deltas = col.Deltas();
        var spread = col.Spread();
        var disp   = col.Displacement();

        // 2 values → 1 delta, 2 spread values (one per element), 1 displacement
        Assert.AreEqual(1, deltas.Count);
        Assert.AreEqual(2, spread.Count); // one distance-from-mean per collection value
        Assert.AreEqual(3.0, disp.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(4.0, disp.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void ModeEnforcement_TimeSeries_AllowsBothTrajectoryAndEnsemble()
    {
        // TimeSeries supports both trajectory ops (Deltas) and ensemble ops (Spread)
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0),
            _grid.Value(1.0, 1.0),
            _grid.Value(2.0, 2.0),
        }, SampleMode.TimeSeries);

        var deltas = col.Deltas();
        var spread = col.Spread();

        Assert.AreEqual(2, deltas.Count);
        Assert.AreEqual(3, spread.Count);
    }

    // ================================================================
    //  MODE INFERENCE
    // ================================================================

    [TestMethod]
    public void InferMode_TemporalAxis_ReturnsTrajectory()
    {
        // A space with a Temporal axis signals time-ordered samples
        var tAxis = Axis.Signed("t", 100, kind: AxisKind.Temporal);
        var space = CSpace.FromAxis(tAxis);
        Assert.AreEqual(SampleMode.Trajectory, CCollection.InferMode(space));
    }

    [TestMethod]
    public void InferMode_SpatialAxisOnly_ReturnsUnspecified()
    {
        // A purely spatial space doesn't determine ordering automatically
        Assert.AreEqual(SampleMode.Unspecified, CCollection.InferMode(_grid));
        Assert.AreEqual(SampleMode.Unspecified, CCollection.InferMode(_int));
    }

    [TestMethod]
    public void InferMode_MixedSpatialTemporal_ReturnsTrajectory()
    {
        // If any axis is Temporal, the space implies time-ordering
        var space = CSpace.Grid(
            Axis.Signed("x", 10, kind: AxisKind.Spatial),
            Axis.Signed("t", 10, kind: AxisKind.Temporal));
        Assert.AreEqual(SampleMode.Trajectory, CCollection.InferMode(space));
    }

    // ================================================================
    //  TOSTRING
    // ================================================================

    [TestMethod]
    public void ToString_IncludesModeAndCount()
    {
        var col = new CCollection(new[]
        {
            _grid.Value(0.0, 0.0), _grid.Value(1.0, 1.0)
        }, SampleMode.Trajectory, isClosed: true);

        var str = col.ToString();
        Assert.IsTrue(str.Contains("Trajectory"), $"Expected 'Trajectory' in '{str}'");
        Assert.IsTrue(str.Contains("closed"),     $"Expected 'closed' in '{str}'");
        Assert.IsTrue(str.Contains("2"),          $"Expected count '2' in '{str}'");
    }
}
