namespace Complexity.Core;

/// <summary>
/// One axis of a complexity space: a bounded path with direction and resolution.
/// Generalizes what BasisFocal + MinMaxFocal do for a single dimension.
/// direction = +1 is "normal" (like the unit/real axis in PRange).
/// direction = -1 is "inverted" (like the unot/imaginary axis in PRange).
///
/// All bounds and cycle periods are stored in ticks (longs).
/// Doubles are derived on demand by dividing by TicksPerUnit.
/// </summary>
public struct Axis
{
    public string Name { get; }
    public int Direction { get; }

    /// <summary> Lower bound in ticks. long.MinValue = unbounded. </summary>
    public long MinBoundTicks { get; set; }

    /// <summary> Upper bound in ticks. long.MaxValue = unbounded. </summary>
    public long MaxBoundTicks { get; set; }

    /// <summary> Number of ticks per unit value. Higher = more resolution. </summary>
    public long TicksPerUnit { get; set; }

    public bool IsCyclic { get; set; }

    /// <summary> Cycle period in ticks. 0 = not cyclic. </summary>
    public long CyclePeriodTicks { get; set; }

    public bool HasMinBound => MinBoundTicks != long.MinValue;
    public bool HasMaxBound => MaxBoundTicks != long.MaxValue;

    /// <summary> Lower bound as a value (ticks / ticksPerUnit). </summary>
    public double MinBound => HasMinBound ? (double)MinBoundTicks / TicksPerUnit : double.NaN;

    /// <summary> Upper bound as a value (ticks / ticksPerUnit). </summary>
    public double MaxBound => HasMaxBound ? (double)MaxBoundTicks / TicksPerUnit : double.NaN;

    /// <summary> Cycle period as a value (ticks / ticksPerUnit). </summary>
    public double CyclePeriod => (double)CyclePeriodTicks / TicksPerUnit;

    /// <summary>
    /// Create an axis. Bounds are specified as values and converted to ticks internally.
    /// Use double.NaN for unbounded.
    /// </summary>
    public Axis(string name, int direction,
        double minBound = double.NaN, double maxBound = double.NaN,
        long ticksPerUnit = 1, bool isCyclic = false, double cyclePeriod = 0.0)
    {
        Name = name;
        Direction = direction;
        TicksPerUnit = ticksPerUnit;
        MinBoundTicks = double.IsNaN(minBound) ? long.MinValue : (long)(minBound * ticksPerUnit);
        MaxBoundTicks = double.IsNaN(maxBound) ? long.MaxValue : (long)(maxBound * ticksPerUnit);
        IsCyclic = isCyclic;
        CyclePeriodTicks = (long)(cyclePeriod * ticksPerUnit);
    }

    /// <summary>
    /// Create an axis directly from tick values (no conversion needed).
    /// </summary>
    public Axis(string name, int direction,
        long minBoundTicks, long maxBoundTicks,
        long ticksPerUnit, bool isCyclic, long cyclePeriodTicks)
    {
        Name = name;
        Direction = direction;
        TicksPerUnit = ticksPerUnit;
        MinBoundTicks = minBoundTicks;
        MaxBoundTicks = maxBoundTicks;
        IsCyclic = isCyclic;
        CyclePeriodTicks = cyclePeriodTicks;
    }

    public override string ToString() =>
        $"{Name}({(Direction == 1 ? "+" : "-")}, [{(HasMinBound ? MinBound.ToString("0.##") : "?")}..{(HasMaxBound ? MaxBound.ToString("0.##") : "?")}], tpu={TicksPerUnit})";
}
