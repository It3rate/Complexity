namespace Complexity.Core;

/// <summary>
/// Per-axis tension: how far out of bounds a value is, and which direction to push.
/// Tension is error as first-class information — not a failure state, but an actionable signal.
/// "Where there is error/tension/surprise, there is a potential to learn."
///
/// Magnitude is in ticks — the tick distance out of bounds.
/// </summary>
public readonly struct AxisTension
{
    public int AxisIndex { get; }

    /// <summary> 0 = within bounds, greater than 0 = tick distance outside. </summary>
    public long Magnitude { get; }

    /// <summary> +1 = value too low (push up), -1 = value too high (push down), 0 = fine. </summary>
    public int Direction { get; }

    public bool IsValid => Magnitude == 0;

    public AxisTension(int axisIndex, long magnitude, int direction)
    {
        AxisIndex = axisIndex;
        Magnitude = magnitude;
        Direction = direction;
    }

    public override string ToString() =>
        Magnitude == 0
            ? $"axis[{AxisIndex}]:ok"
            : $"axis[{AxisIndex}]:{Magnitude}t({(Direction > 0 ? "↑" : "↓")})";
}

/// <summary>
/// Aggregate tension across all axes of a CValue.
/// Every value in a bounded space can be evaluated for how well it fits.
/// Out-of-bounds → tension (in ticks), with a direction that says how to fix it.
///
/// This enables distributed intelligence: each space can detect error locally
/// and push toward correction without inspecting other spaces.
/// </summary>
public class Tension
{
    public static readonly Tension None = new Tension(Array.Empty<AxisTension>());

    public AxisTension[] PerAxis { get; }

    /// <summary> Total tension in ticks (sum of magnitudes). </summary>
    public long Total { get; }

    public bool IsValid { get; }

    public Tension(AxisTension[] perAxis)
    {
        PerAxis = perAxis;
        Total = 0;
        for (int i = 0; i < perAxis.Length; i++)
            Total += Math.Abs(perAxis[i].Magnitude);
        IsValid = Total == 0;
    }

    /// <summary>
    /// The axis with the most tension (furthest out of bounds in ticks).
    /// </summary>
    public AxisTension Worst
    {
        get
        {
            if (PerAxis.Length == 0) return default;
            var worst = PerAxis[0];
            for (int i = 1; i < PerAxis.Length; i++)
            {
                if (Math.Abs(PerAxis[i].Magnitude) > Math.Abs(worst.Magnitude))
                    worst = PerAxis[i];
            }
            return worst;
        }
    }

    /// <summary>
    /// Validity as a 0..1 score. 1.0 when all in bounds, decreasing with tension.
    /// Uses doubles for the score since it's a ratio.
    /// </summary>
    public double Validity => 1.0 / (1.0 + Total);

    /// <summary>
    /// Compute tension for a CValue against its space's tick bounds.
    /// Compares long components directly against long bounds.
    /// </summary>
    public static Tension Compute(CValue val)
    {
        if (!val.IsValid) return None;

        var tensions = new AxisTension[val.Space.Dims];
        for (int i = 0; i < val.Space.Dims; i++)
        {
            var ax = val.Space.Axes[i];
            var v = val.Components[i];
            if (ax.HasMinBound && v < ax.MinBoundTicks)
                tensions[i] = new AxisTension(i, ax.MinBoundTicks - v, +1);
            else if (ax.HasMaxBound && v > ax.MaxBoundTicks)
                tensions[i] = new AxisTension(i, v - ax.MaxBoundTicks, -1);
            else
                tensions[i] = new AxisTension(i, 0, 0);
        }
        return new Tension(tensions);
    }

    public override string ToString()
    {
        if (IsValid) return "valid";
        return $"tension={Total}t [{string.Join(", ", PerAxis.Where(t => !t.IsValid))}]";
    }
}
