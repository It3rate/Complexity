namespace Spaces.Core;

/// <summary>
/// A value within a <see cref="Space"/>: one <see cref="Focal"/> per axis.
///
/// ── WHAT A VALUE IS ─────────────────────────────────────────────────────────
///
/// For each axis k the value holds a Focal where:
///   Focal.Lo = the i-component (extent in the imaginary/left direction), in ticks
///   Focal.Hi = the r-component (extent in the real/right direction), in ticks
///
/// A segment [a, b] on axis k is stored as Focal(lo=a_ticks, hi=b_ticks), and
/// expressed in complex notation as "(−a)·i + b".
///
/// ── FLAT COMPONENT INDEXING ─────────────────────────────────────────────────
///
/// GetComponent(i) maps flat index i to a tick value:
///   i = 2k   →  Focals[k].Lo
///   i = 2k+1 →  Focals[k].Hi
///
/// This matches the indexing used in <see cref="AlgebraEntry"/>.
///
/// ── ARITHMETIC ──────────────────────────────────────────────────────────────
///
/// Addition (A + B):    component-wise in ticks.  Lo+Lo, Hi+Hi per axis.
/// Subtraction (A − B): component-wise in ticks.
/// Negation (−A):       flip all component signs.
///
/// Multiplication (A × B): uses the Space's algebra table:
///   For each AlgebraEntry(left, right, result, sign):
///     acc[result] += sign × A.GetComponent(left) × B.GetComponent(right)
///   Then divide each accumulated component by BasisTicks (unit normalisation).
///
///   This implements complex multiplication for a 1D segment space, and
///   independent complex multiplication per axis for a 2D grid space.
///
/// ── OVERFLOW / TENSION ──────────────────────────────────────────────────────
///
/// A value is "in bounds" if all components lie within their axis bounds.
/// TotalOverflow() returns the sum of excess ticks across all components.
/// Overflow is information (not an error) — it represents tension in the system.
/// </summary>
public sealed class SpaceValue
{
    public Space Space { get; }

    /// <summary>One Focal per axis, in axis order.</summary>
    public IReadOnlyList<Focal> Focals { get; }

    public SpaceValue(Space space, IReadOnlyList<Focal> focals)
    {
        if (focals.Count != space.Axes.Count)
            throw new ArgumentException(
                $"Value has {focals.Count} focals but space has {space.Axes.Count} axes.");
        Space  = space;
        Focals = focals;
    }

    // ── Component access ──────────────────────────────────────────────────────

    /// <summary>
    /// Get the tick value for flat component index i.
    ///   Even i  →  Focals[i/2].Lo
    ///   Odd  i  →  Focals[i/2].Hi
    /// </summary>
    public long GetComponent(int componentIdx)
    {
        int  a    = componentIdx >> 1;          // axisIdx = componentIdx / 2
        bool isHi = (componentIdx & 1) == 1;
        return isHi ? Focals[a].Hi : Focals[a].Lo;
    }

    /// <summary>Lo ticks for axis <paramref name="axisIdx"/>.</summary>
    public long GetLo(int axisIdx) => Focals[axisIdx].Lo;

    /// <summary>Hi ticks for axis <paramref name="axisIdx"/>.</summary>
    public long GetHi(int axisIdx) => Focals[axisIdx].Hi;

    /// <summary>Lo converted to units for axis <paramref name="axisIdx"/>.</summary>
    public double GetLoUnits(int axisIdx) =>
        Space.Axes[axisIdx].ToUnits(Focals[axisIdx].Lo);

    /// <summary>Hi converted to units for axis <paramref name="axisIdx"/>.</summary>
    public double GetHiUnits(int axisIdx) =>
        Space.Axes[axisIdx].ToUnits(Focals[axisIdx].Hi);

    // ── Arithmetic operators ──────────────────────────────────────────────────

    /// <summary>
    /// Component-wise addition.
    /// For segments: [a,b] + [c,d] = [a+c, b+d].
    /// </summary>
    public static SpaceValue operator +(SpaceValue a, SpaceValue b)
    {
        AssertSameSpace(a, b);
        int    n      = a.Space.Axes.Count;
        var    focals = new Focal[n];
        for (int k = 0; k < n; k++)
            focals[k] = new Focal(
                a.Focals[k].Lo + b.Focals[k].Lo,
                a.Focals[k].Hi + b.Focals[k].Hi);
        return new SpaceValue(a.Space, focals);
    }

    /// <summary>Component-wise subtraction.</summary>
    public static SpaceValue operator -(SpaceValue a, SpaceValue b)
    {
        AssertSameSpace(a, b);
        int    n      = a.Space.Axes.Count;
        var    focals = new Focal[n];
        for (int k = 0; k < n; k++)
            focals[k] = new Focal(
                a.Focals[k].Lo - b.Focals[k].Lo,
                a.Focals[k].Hi - b.Focals[k].Hi);
        return new SpaceValue(a.Space, focals);
    }

    /// <summary>
    /// Negate: flip all component signs.
    /// For a segment [a, b] → [−a, −b].
    /// </summary>
    public static SpaceValue operator -(SpaceValue v)
    {
        int    n      = v.Space.Axes.Count;
        var    focals = new Focal[n];
        for (int k = 0; k < n; k++)
            focals[k] = new Focal(-v.Focals[k].Lo, -v.Focals[k].Hi);
        return new SpaceValue(v.Space, focals);
    }

    /// <summary>
    /// Algebra-table multiplication, normalised by BasisTicks.
    ///
    /// For each <see cref="AlgebraEntry"/> (left, right, result, sign):
    ///   acc[result] += sign × A.component[left] × B.component[right]
    ///
    /// After accumulation, each component is divided by BasisTicks to keep
    /// the result in the same unit space as the inputs.
    ///
    /// For a 1D segment space this is identical to complex number multiplication.
    /// For a 2D grid space each axis multiplies independently (no cross terms).
    /// </summary>
    public static SpaceValue operator *(SpaceValue a, SpaceValue b)
    {
        AssertSameSpace(a, b);
        int    n   = a.Space.ComponentCount;
        long   bt  = a.Space.BasisTicks;
        var    acc = new long[n];

        foreach (var entry in a.Space.Algebra)
            acc[entry.ResultIdx] +=
                (long)entry.Sign
                * a.GetComponent(entry.LeftIdx)
                * b.GetComponent(entry.RightIdx);

        // Pack accumulator back into Focals (one per axis), normalising by BasisTicks
        int    axes   = a.Space.Axes.Count;
        var    focals = new Focal[axes];
        for (int k = 0; k < axes; k++)
            focals[k] = new Focal(acc[k * 2] / bt, acc[k * 2 + 1] / bt);
        return new SpaceValue(a.Space, focals);
    }

    // ── Overflow / tension ────────────────────────────────────────────────────

    /// <summary>
    /// Total tick overflow: sum of how much each component exceeds its axis bounds.
    /// Returns 0 when the value is entirely within all axis bounds.
    ///
    /// Overflow is not an error — it is "tension" information in the system,
    /// analogous to a value exceeding the space's defined range.
    /// </summary>
    public long TotalOverflow()
    {
        long total = 0;
        for (int k = 0; k < Space.Axes.Count; k++)
        {
            var ax = Space.Axes[k];
            total += AxisOverflow(Focals[k].Lo, ax.MinTick, ax.MaxTick);
            total += AxisOverflow(Focals[k].Hi, ax.MinTick, ax.MaxTick);
        }
        return total;
    }

    /// <summary>True when all components are within their axis bounds.</summary>
    public bool InBounds => TotalOverflow() == 0;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AssertSameSpace(SpaceValue a, SpaceValue b)
    {
        if (!ReferenceEquals(a.Space, b.Space))
            throw new InvalidOperationException(
                "Cannot combine values from different Space instances.  " +
                "Ensure both values were created from the same Space object.");
    }

    private static long AxisOverflow(long ticks, long min, long max)
    {
        if (ticks < min) return min - ticks;  // underflow: distance below min
        if (ticks > max) return ticks - max;  // overflow:  distance above max
        return 0L;
    }

    public override string ToString()
    {
        var parts = Focals.Select((f, k) =>
        {
            double lo = Space.Axes[k].ToUnits(f.Lo);
            double hi = Space.Axes[k].ToUnits(f.Hi);
            string name = Space.Axes[k].Name;
            return (name.Length > 0 ? $"{name}:" : "") + $"[{lo:G4}, {hi:G4}]";
        });
        return string.Join("  ", parts);
    }
}
