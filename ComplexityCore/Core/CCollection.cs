namespace Complexity.Core;

/// <summary>
/// An ordered sequence of CValues with a declared measurement mode and closure flag.
///
/// Three structural properties live here that cannot be expressed on individual CValues:
///
///   1. Ordering — implicit in list index. Values[0] is first, Values[N-1] is last.
///      For Trajectory/TimeSeries this is time order.
///      For Ensemble this is arbitrary (no inherent order).
///
///   2. Closure — IsClosed: does the last sample connect back to the first?
///      Determined by the phenomenon being measured, not by mathematical choice.
///      Example: a dog that returns to its start position → IsClosed = true.
///
///   3. Provenance — Mode: what measurement protocol generated these values?
///      Same entity over time  → Trajectory / TimeSeries
///      Different entities     → Ensemble / Field
///      Identity transforming  → Morphogenesis
///
/// Mode determines which collection-level operations are semantically valid.
/// Mode.Unspecified skips validation and allows all operations — useful when
/// the mode is genuinely unknown or validation is not desired.
///
/// All operations work entirely with tick-precision arithmetic where possible
/// (Mean uses double accumulation to avoid overflow, then converts back to ticks).
/// Distance-based operations (PathLength, Spread) use the space's MetricKind.
/// </summary>
public class CCollection
{
    /// <summary>
    /// The ordered sequence of values. Index 0 = first/earliest, N-1 = last/latest.
    /// All values should live in the same CSpace.
    /// </summary>
    public List<CValue> Values { get; }

    /// <summary>
    /// What kind of measurement generated this collection.
    /// Trajectory = same entity over time. Ensemble = different entities at one time.
    /// Unspecified = all operations permitted without mode validation.
    /// </summary>
    public SampleMode Mode { get; set; }

    /// <summary>
    /// Whether the last sample connects back to the first (closed loop / cycle).
    /// Determined by the phenomenon being measured, not the mathematics.
    /// Example: orbit → IsClosed = true. One-way journey → IsClosed = false.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Create a collection from a sequence of values.
    /// </summary>
    public CCollection(IEnumerable<CValue> values,
        SampleMode mode = SampleMode.Unspecified,
        bool isClosed = false)
    {
        Values = new List<CValue>(values);
        Mode = mode;
        IsClosed = isClosed;
    }

    /// <summary> Number of samples in this collection. </summary>
    public int Count => Values.Count;

    /// <summary>
    /// The CSpace all values live in.
    /// Throws if the collection is empty.
    /// </summary>
    public CSpace Space => Values.Count > 0
        ? Values[0].Space
        : throw new InvalidOperationException("Collection is empty.");

    /// <summary> The first sample (index 0). </summary>
    public CValue First => Values.Count > 0
        ? Values[0]
        : throw new InvalidOperationException("Collection is empty.");

    /// <summary> The last sample (index Count-1). </summary>
    public CValue Last => Values.Count > 0
        ? Values[^1]
        : throw new InvalidOperationException("Collection is empty.");

    // ================================================================
    //  UNIVERSAL OPERATIONS — valid for any mode
    // ================================================================

    /// <summary>
    /// Component-wise minimum and maximum across all values.
    /// Returns the bounding box of the collection in value-space.
    ///
    /// For Ensemble: bounds of the population scatter.
    /// For Trajectory: bounding box of the path.
    /// </summary>
    public (CValue Min, CValue Max) Bounds()
    {
        if (Count == 0) throw new InvalidOperationException("Collection is empty.");

        var minComps = (long[])Values[0].Components.Clone();
        var maxComps = (long[])Values[0].Components.Clone();

        for (int k = 1; k < Count; k++)
        {
            for (int i = 0; i < Space.Dims; i++)
            {
                if (Values[k].Components[i] < minComps[i]) minComps[i] = Values[k].Components[i];
                if (Values[k].Components[i] > maxComps[i]) maxComps[i] = Values[k].Components[i];
            }
        }

        return (new CValue(Space, minComps), new CValue(Space, maxComps));
    }

    /// <summary>
    /// Component-wise arithmetic mean (centroid).
    ///
    /// For Ensemble: population average — the representative central value.
    /// For Trajectory: average position over the path.
    /// For Positions: this is the one valid "Position + Position" operation —
    ///   the centroid of a set of positions is itself a position.
    ///
    /// Accumulates in doubles to avoid long overflow, then converts back to ticks.
    /// </summary>
    public CValue Mean()
    {
        if (Count == 0) throw new InvalidOperationException("Collection is empty.");

        var sums = new double[Space.Dims];
        foreach (var v in Values)
            for (int i = 0; i < Space.Dims; i++)
                sums[i] += v.Components[i];

        var comps = new long[Space.Dims];
        for (int i = 0; i < Space.Dims; i++)
            comps[i] = (long)Math.Round(sums[i] / Count);

        return new CValue(Space, comps);
    }

    // ================================================================
    //  TRAJECTORY OPERATIONS
    //  Valid when Mode is Trajectory, TimeSeries, Morphogenesis, or Unspecified.
    //  "Same entity, ordered in time" — consecutive differences are meaningful.
    // ================================================================

    /// <summary>
    /// Consecutive differences between adjacent samples.
    /// Returns Count-1 displacement vectors: (Values[1]-Values[0]), (Values[2]-Values[1]), …
    ///
    /// For a trajectory of positions, each delta is the displacement vector
    /// taken during that step (Position - Position = Vector, valid).
    /// </summary>
    public List<CValue> Deltas()
    {
        RequireTrajectoryMode();
        var result = new List<CValue>(Count - 1);
        for (int i = 1; i < Count; i++)
            result.Add(Values[i] - Values[i - 1]);
        return result;
    }

    /// <summary>
    /// Total displacement from first to last sample, ignoring the path taken.
    /// Returns (Last - First): the straight-line vector from start to end.
    ///
    /// For a closed trajectory (IsClosed = true) where Last ≈ First,
    /// the displacement is near-zero even if the PathLength is large.
    /// </summary>
    public CValue Displacement()
    {
        RequireTrajectoryMode();
        if (Count < 2)
            throw new InvalidOperationException("Need at least 2 values for Displacement.");
        return Last - First;
    }

    /// <summary>
    /// Total path length: sum of metric distances between consecutive samples.
    /// Uses the space's MetricKind for each step.
    ///
    /// For a closed collection (IsClosed = true), also adds the distance
    /// from the last sample back to the first.
    /// </summary>
    public double PathLength()
    {
        RequireTrajectoryMode();
        double total = 0;
        for (int i = 1; i < Count; i++)
            total += CValue.Distance(Values[i], Values[i - 1]);
        if (IsClosed && Count > 1)
            total += CValue.Distance(Last, First);
        return total;
    }

    // ================================================================
    //  ENSEMBLE OPERATIONS
    //  Valid when Mode is Ensemble, TimeSeries, Field, or Unspecified.
    //  "Different entities" — spread and distribution are meaningful.
    // ================================================================

    /// <summary>
    /// Metric distance of each value from the collection mean.
    /// Returns Count distances, one per sample.
    ///
    /// For Ensemble: measures how spread out the population is around its centre.
    /// A tight cluster → small values. A scattered population → large values.
    /// </summary>
    public List<double> Spread()
    {
        RequireEnsembleMode();
        var mean = Mean();
        return Values.Select(v => CValue.Distance(v, mean)).ToList();
    }

    // ================================================================
    //  MODE INFERENCE
    // ================================================================

    /// <summary>
    /// Infer the most likely SampleMode from the CSpace's axis kinds.
    /// If any axis is Temporal, values are time-ordered → Trajectory.
    /// Otherwise returns Unspecified (caller must provide the mode explicitly).
    ///
    /// This is a heuristic: the measurement context is the ground truth.
    /// </summary>
    public static SampleMode InferMode(CSpace space)
    {
        foreach (var ax in space.Axes)
            if (ax.Kind == AxisKind.Temporal)
                return SampleMode.Trajectory;
        return SampleMode.Unspecified;
    }

    // ================================================================
    //  PRIVATE HELPERS
    // ================================================================

    private void RequireTrajectoryMode()
    {
        if (Mode == SampleMode.Unspecified ||
            Mode == SampleMode.Trajectory  ||
            Mode == SampleMode.TimeSeries  ||
            Mode == SampleMode.Morphogenesis)
            return;

        throw new InvalidOperationException(
            $"Operation requires Trajectory, TimeSeries, or Morphogenesis mode, " +
            $"but collection is {Mode}. Use Unspecified to skip mode validation.");
    }

    private void RequireEnsembleMode()
    {
        if (Mode == SampleMode.Unspecified ||
            Mode == SampleMode.Ensemble    ||
            Mode == SampleMode.TimeSeries  ||
            Mode == SampleMode.Field)
            return;

        throw new InvalidOperationException(
            $"Operation requires Ensemble, TimeSeries, or Field mode, " +
            $"but collection is {Mode}. Use Unspecified to skip mode validation.");
    }

    public override string ToString() =>
        $"CCollection[{Count} values, {Mode}{(IsClosed ? ", closed" : "")}]";
}
