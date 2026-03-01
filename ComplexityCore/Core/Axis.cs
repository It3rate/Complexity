namespace Complexity.Core;

/// <summary>
/// The basic minimum primitive of the number system.
/// An axis is one direction on the number line from zero to max range,
/// evaluated by its unit and the unit's resolution (ticks per unit).
///
/// The four ways infinity is limited in this system:
/// 1. Range bounds (MinBoundTicks / MaxBoundTicks) - limits how far numbers extend
/// 2. Resolution (TicksPerUnit) - limits how finely numbers are divided
/// 3. Repeats/overflow rules - global properties (tracked but not enforced yet)
/// 4. Values live within this framework and are evaluated based on the unit
///
/// There is no floating point resolution at all in this system.
/// Resolution is always based on the number of ticks in a unit.
/// This number can be very large (fine resolution) or 1 tick per unit (integers).
///
/// All bounds are stored in ticks (longs). Doubles are derived on demand
/// by dividing by TicksPerUnit — this is the "evaluation" step.
///
/// Different number types are built by combining axes:
/// - Single forward axis → positive numbers (unsigned integers)
/// - Single signed axis → integers (positive and negative range, single positive unit)
/// - Two axes (forward + backward) → segments/complex numbers ("football field" math)
/// - Two orthogonal axes → XY grid / 2D coordinates
/// - Four axes → quaternions
///
/// Semantic metadata (Kind) lives here alongside the mathematical structure.
/// Kind does not affect arithmetic — it guides interpretation and collection operations.
/// </summary>
public struct Axis
{
    public string Name { get; }

    /// <summary> +1 = forward/right/positive, -1 = backward/left/negative </summary>
    public int Direction { get; }

    /// <summary> Lower bound in ticks. long.MinValue = unbounded. </summary>
    public long MinBoundTicks { get; set; }

    /// <summary> Upper bound in ticks. long.MaxValue = unbounded. </summary>
    public long MaxBoundTicks { get; set; }

    /// <summary>
    /// Number of ticks per unit value. This IS the resolution.
    /// 1 = integer resolution. 10 = tenths. 1000 = thousandths.
    /// Higher values = finer resolution. No floating point needed.
    /// </summary>
    public long TicksPerUnit { get; set; }

    public bool IsCyclic { get; set; }

    /// <summary> Cycle period in ticks. 0 = not cyclic. </summary>
    public long CyclePeriodTicks { get; set; }

    /// <summary>
    /// What physical or conceptual quantity this axis measures.
    /// Does not affect arithmetic — semantic metadata used by CCollection inference.
    /// Spatial = position/length (default). Temporal = time/sequence.
    /// Intensity = scalar quality. Ordinal = rank/category.
    /// </summary>
    public AxisKind Kind { get; set; }

    public bool HasMinBound => MinBoundTicks != long.MinValue;
    public bool HasMaxBound => MaxBoundTicks != long.MaxValue;

    /// <summary> Lower bound as a value (ticks / ticksPerUnit). Only valid when HasMinBound. </summary>
    public double MinBound => HasMinBound ? (double)MinBoundTicks / TicksPerUnit : double.NaN;

    /// <summary> Upper bound as a value (ticks / ticksPerUnit). Only valid when HasMaxBound. </summary>
    public double MaxBound => HasMaxBound ? (double)MaxBoundTicks / TicksPerUnit : double.NaN;

    /// <summary> Cycle period as a value (ticks / ticksPerUnit). </summary>
    public double CyclePeriod => (double)CyclePeriodTicks / TicksPerUnit;

    // ================================================================
    //  CONSTRUCTORS
    // ================================================================

    /// <summary>
    /// Create an axis from value-space bounds (doubles converted to ticks internally).
    /// Use double.NaN for unbounded.
    /// </summary>
    public Axis(string name, int direction,
        double minBound = double.NaN, double maxBound = double.NaN,
        long ticksPerUnit = 1, bool isCyclic = false, double cyclePeriod = 0.0,
        AxisKind kind = AxisKind.Spatial)
    {
        Name = name;
        Direction = direction;
        TicksPerUnit = ticksPerUnit;
        MinBoundTicks = double.IsNaN(minBound) ? long.MinValue : (long)(minBound * ticksPerUnit);
        MaxBoundTicks = double.IsNaN(maxBound) ? long.MaxValue : (long)(maxBound * ticksPerUnit);
        IsCyclic = isCyclic;
        CyclePeriodTicks = (long)(cyclePeriod * ticksPerUnit);
        Kind = kind;
    }

    /// <summary>
    /// Create an axis directly from tick values (no conversion needed).
    /// This is the primary constructor — everything is already in ticks.
    /// </summary>
    public Axis(string name, int direction,
        long minBoundTicks, long maxBoundTicks,
        long ticksPerUnit, bool isCyclic = false, long cyclePeriodTicks = 0,
        AxisKind kind = AxisKind.Spatial)
    {
        Name = name;
        Direction = direction;
        TicksPerUnit = ticksPerUnit;
        MinBoundTicks = minBoundTicks;
        MaxBoundTicks = maxBoundTicks;
        IsCyclic = isCyclic;
        CyclePeriodTicks = cyclePeriodTicks;
        Kind = kind;
    }

    // ================================================================
    //  FACTORY METHODS — common axis types
    // ================================================================

    /// <summary>
    /// A forward-pointing axis from zero to max with given resolution.
    /// This is the simplest axis: one direction from zero to a maximum.
    /// Example: Forward("value", 100, 10) creates 0..1000 ticks (0..100 value-space)
    /// </summary>
    public static Axis Forward(string name, double maxValue, long ticksPerUnit = 1,
        AxisKind kind = AxisKind.Spatial) =>
        new Axis(name, +1, 0, maxValue, ticksPerUnit: ticksPerUnit, kind: kind);

    /// <summary>
    /// A backward/left-facing axis (direction = -1) from zero to max with given resolution.
    /// Used as the imaginary/unot axis in segment/complex number frames.
    /// "Football field" math uses one forward and one backward unit at endpoints of a range.
    /// </summary>
    public static Axis Backward(string name, double maxValue, long ticksPerUnit = 1,
        AxisKind kind = AxisKind.Spatial) =>
        new Axis(name, -1, 0, maxValue, ticksPerUnit: ticksPerUnit, kind: kind);

    /// <summary>
    /// A signed axis spanning -maxValue to +maxValue.
    /// Full positive and negative range with a single positive unit direction.
    /// "To make a negative number we need a full positive and negative range
    ///  and a single positive unit."
    /// </summary>
    public static Axis Signed(string name, double maxValue, long ticksPerUnit = 1,
        AxisKind kind = AxisKind.Spatial) =>
        new Axis(name, +1, -maxValue, maxValue, ticksPerUnit: ticksPerUnit, kind: kind);

    /// <summary>
    /// An unbounded axis (no limits) with given resolution and direction.
    /// MinBound = long.MinValue, MaxBound = long.MaxValue.
    /// </summary>
    public static Axis Unbounded(string name, int direction, long ticksPerUnit = 1,
        AxisKind kind = AxisKind.Spatial) =>
        new Axis(name, direction, long.MinValue, long.MaxValue, ticksPerUnit, false, 0, kind);

    /// <summary>
    /// A cyclic axis that wraps at the given period.
    /// For clock arithmetic, angles, modular math.
    /// </summary>
    public static Axis Cyclic(string name, double period, long ticksPerUnit = 1,
        AxisKind kind = AxisKind.Spatial) =>
        new Axis(name, +1, 0, period, ticksPerUnit: ticksPerUnit,
            isCyclic: true, cyclePeriod: period, kind: kind);

    public override string ToString() =>
        $"{Name}({(Direction == 1 ? "+" : "-")}, [{(HasMinBound ? MinBound.ToString("0.##") : "?")}..{(HasMaxBound ? MaxBound.ToString("0.##") : "?")}], tpu={TicksPerUnit}, {Kind})";
}
