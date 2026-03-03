namespace Spaces.Core;

/// <summary>
/// A value within a <see cref="Space"/>: one <see cref="Proportion"/> per axis,
/// plus an optional collection of sub-values.
///
/// ── VALUE ENCODING ──────────────────────────────────────────────────────────
///
/// For each axis k the value holds a Proportion where:
///   Proportion.P0 = magnitude in the LEFT / imaginary direction, in ticks (≥ 0)
///   Proportion.P1 = magnitude in the RIGHT / real direction, in ticks (≥ 0)
///
/// Both components are NON-NEGATIVE MAGNITUDES when created via
/// <see cref="Space.Value"/> or <see cref="Space.CreateValue"/> (abs is applied
/// to inputs).  After arithmetic the components may become negative, representing
/// "tension" — extension beyond the axis range in the wrong direction.
///
/// ── CHIRALITY AND DISPLAY ───────────────────────────────────────────────────
///
/// The fraction read from each Proportion and the direction on the number line
/// depends on the axis chirality:
///
///   Chirality.Positive (right / real side):
///     Number-line position: [−p0/tpu, +p1/tpu]
///     Display: "({p0/tpu}i+{p1/tpu})"
///     Example: Proportion(500, 600) @ tpu=100 → "(5i+6)"  fraction 6/5
///
///   Chirality.Negative (left / imaginary side — rotate the paper):
///     Number-line position: [+p0/tpu, −p1/tpu]
///     Display: "[-{p0/tpu}i-{p1/tpu}]"
///     Example: Proportion(500, 600) @ tpu=100 → "[-5i-6]"  fraction 5/6
///
/// Use <see cref="ToDisplayString(Chirality, int)"/> to render axis-level display.
///
/// ── FLAT COMPONENT INDEXING ─────────────────────────────────────────────────
///
/// GetComponent(i) maps flat index i to a tick value:
///   i = 2k   →  Proportions[k].P0
///   i = 2k+1 →  Proportions[k].P1
///
/// This matches the indexing used in <see cref="AlgebraEntry"/>.
///
/// ── ARITHMETIC ──────────────────────────────────────────────────────────────
///
/// Addition (A + B):    component-wise in ticks.  P0+P0, P1+P1 per axis.
/// Subtraction (A − B): component-wise in ticks.  May produce negative components.
/// Negation (−A):       flip all component signs.  Produces negative components.
///
/// Multiplication (A × B): uses the Space's algebra table:
///   For each AlgebraEntry(left, right, result, sign):
///     acc[result] += sign × A.GetComponent(left) × B.GetComponent(right)
///   Then divide each accumulated component by BasisTicks (unit normalisation).
///
/// ── OVERFLOW / TENSION ──────────────────────────────────────────────────────
///
/// A magnitude component is "in bounds" when it lies in [0, maxMagnitude].
/// Negative ticks (from arithmetic tension) and ticks above the axis maximum
/// both contribute to TotalOverflow().  Overflow is not an error — it is
/// diagnostic information about how far a value has extended beyond its
/// intended range.
///
/// ── SUB-VALUES AND HIERARCHY ─────────────────────────────────────────────────
///
/// A SpaceValue implements <see cref="IValueSpace"/>, allowing it to act as a
/// sub-frame that holds additional SpaceValues.  By default, sub-values use
/// the same Space (same axes, same ticks-per-unit).
///
/// In a future calibration-aware design (see <see cref="Calibration"/>), a
/// SpaceValue could carry a <see cref="Calibration"/> object that maps its
/// local coordinate frame to the parent Space, enabling sub-values with
/// different units, scales, or origins.  The IValueSpace interface is
/// specifically designed to accommodate this extension without API changes.
/// </summary>
public sealed class SpaceValue : IValueSpace
{
    /// <summary>The Space this value belongs to.</summary>
    public Space Space { get; }

    /// <summary>One Proportion per axis, in axis order.</summary>
    public IReadOnlyList<Proportion> Proportions { get; }

    // ── Sub-value registry ────────────────────────────────────────────────────
    private readonly List<SpaceValue> _subValues = new();

    public SpaceValue(Space space, IReadOnlyList<Proportion> proportions)
    {
        if (proportions.Count != space.Axes.Count)
            throw new ArgumentException(
                $"Value has {proportions.Count} proportions but space has {space.Axes.Count} axes.");
        Space       = space;
        Proportions = proportions;
    }

    // ── IValueSpace implementation ────────────────────────────────────────────
    //
    // A SpaceValue acts as a sub-frame: its coordinate system is the same as
    // its parent Space (same axes, same ticks-per-unit).  Sub-values live in
    // this same tick space.
    //
    // Future: when Calibration is implemented, this class will carry a
    // Calibration object and CreateValue / AddValue will transform coordinates
    // accordingly.

    /// <summary>Name of the parent Space (sub-frame uses the same axes).</summary>
    public string Name => Space.Name;

    /// <summary>Axes from the parent Space.</summary>
    public IReadOnlyList<Axis> Axes => Space.Axes;

    /// <summary>BasisTicks from the parent Space.</summary>
    public long BasisTicks => Space.BasisTicks;

    /// <summary>ComponentCount from the parent Space.</summary>
    public int ComponentCount => Space.ComponentCount;

    /// <summary>
    /// Sub-values registered within this value's sub-frame.
    /// These are additional SpaceValues grouped under this value, expressed
    /// in the same parent-Space coordinate system.
    ///
    /// TODO (Calibration): when a Calibration is attached, sub-values will be
    /// expressed in the child coordinate system and transformed to parent ticks
    /// transparently.
    /// </summary>
    public IReadOnlyList<SpaceValue> Values => _subValues;

    /// <summary>
    /// Create a new SpaceValue in the parent Space and register it as a sub-value.
    /// Inputs are treated as magnitudes (abs applied).
    ///
    /// TODO (Calibration): when a Calibration is attached, units will be
    /// interpreted in the child coordinate frame and transformed to parent ticks.
    /// </summary>
    public SpaceValue CreateValue(params double[] units)
    {
        var v = Space.Value(units);   // pure factory — doesn't add to Space.Values
        _subValues.Add(v);
        return v;
    }

    /// <summary>
    /// Register an existing SpaceValue as a sub-value of this value.
    /// The supplied value must belong to the same Space as this value.
    /// </summary>
    public void AddValue(SpaceValue value)
    {
        if (!ReferenceEquals(value.Space, Space))
            throw new InvalidOperationException(
                "Cannot add a sub-value that belongs to a different Space instance.");
        _subValues.Add(value);
    }

    // ── Component access ──────────────────────────────────────────────────────

    /// <summary>
    /// Get the tick value for flat component index i.
    ///   Even i  →  Proportions[i/2].P0  (left / imaginary magnitude)
    ///   Odd  i  →  Proportions[i/2].P1  (right / real magnitude)
    /// </summary>
    public long GetComponent(int componentIdx)
    {
        int  a    = componentIdx >> 1;
        bool isP1 = (componentIdx & 1) == 1;
        return isP1 ? Proportions[a].P1 : Proportions[a].P0;
    }

    /// <summary>P0 ticks (left / imaginary magnitude) for axis <paramref name="axisIdx"/>.</summary>
    public long GetP0(int axisIdx) => Proportions[axisIdx].P0;

    /// <summary>P1 ticks (right / real magnitude) for axis <paramref name="axisIdx"/>.</summary>
    public long GetP1(int axisIdx) => Proportions[axisIdx].P1;

    /// <summary>
    /// P0 converted to units for axis <paramref name="axisIdx"/>.
    /// Divides by TicksPerUnit (= Unit.P1 for the axis).
    /// Returns a positive value for user-created values; may be negative
    /// after arithmetic tension.
    /// </summary>
    public double GetP0Units(int axisIdx) =>
        Space.Axes[axisIdx].ToUnits(Proportions[axisIdx].P0);

    /// <summary>
    /// P1 converted to units for axis <paramref name="axisIdx"/>.
    /// Divides by TicksPerUnit.
    /// </summary>
    public double GetP1Units(int axisIdx) =>
        Space.Axes[axisIdx].ToUnits(Proportions[axisIdx].P1);

    // ── Chirality display ─────────────────────────────────────────────────────

    /// <summary>
    /// Render the value for <paramref name="axisIdx"/> as a human-readable string,
    /// applying the axis's <paramref name="chirality"/> to determine sign convention.
    ///
    /// 'i' marks the start point (P0 = left extent from origin).
    /// P0 is always listed first, P1 second.
    ///
    /// Chirality.Positive — standard reading (real / right side):
    ///   Display: "({p0_units}i+{p1_units})"
    ///   Example: Proportion(500, 600) @ tpu=100 → "(5i+6)"
    ///
    /// Chirality.Negative  (rotate the paper — imaginary / left side):
    ///   Display: "[-{p0_units}i-{p1_units}]"
    ///   Example: Proportion(500, 600) @ tpu=100 → "[-5i-6]"
    /// </summary>
    public string ToDisplayString(Chirality chirality, int axisIdx)
    {
        var    ax      = Space.Axes[axisIdx];
        double p0Units = ax.ToUnits(Proportions[axisIdx].P0);
        double p1Units = ax.ToUnits(Proportions[axisIdx].P1);

        return chirality == Chirality.Positive
            ? $"({p0Units:G4}i+{p1Units:G4})"
            : $"[-{p0Units:G4}i-{p1Units:G4}]";
    }

    // ── Arithmetic operators ──────────────────────────────────────────────────

    /// <summary>
    /// Component-wise addition.
    /// Adds the P0 and P1 magnitudes independently per axis.
    /// Result may exceed axis range (tension) but is never clamped.
    /// </summary>
    public static SpaceValue operator +(SpaceValue a, SpaceValue b)
    {
        AssertSameSpace(a, b);
        int    n           = a.Space.Axes.Count;
        var    proportions = new Proportion[n];
        for (int k = 0; k < n; k++)
            proportions[k] = new Proportion(
                a.Proportions[k].P0 + b.Proportions[k].P0,
                a.Proportions[k].P1 + b.Proportions[k].P1);
        return new SpaceValue(a.Space, proportions);
    }

    /// <summary>
    /// Component-wise subtraction.
    /// May produce negative components (tension) when a component is smaller
    /// than the subtracted amount.
    /// </summary>
    public static SpaceValue operator -(SpaceValue a, SpaceValue b)
    {
        AssertSameSpace(a, b);
        int    n           = a.Space.Axes.Count;
        var    proportions = new Proportion[n];
        for (int k = 0; k < n; k++)
            proportions[k] = new Proportion(
                a.Proportions[k].P0 - b.Proportions[k].P0,
                a.Proportions[k].P1 - b.Proportions[k].P1);
        return new SpaceValue(a.Space, proportions);
    }

    /// <summary>
    /// Negate: flip all component signs.
    /// The result has negative P0 and P1 — this is valid "tension" arithmetic.
    /// </summary>
    public static SpaceValue operator -(SpaceValue v)
    {
        int    n           = v.Space.Axes.Count;
        var    proportions = new Proportion[n];
        for (int k = 0; k < n; k++)
            proportions[k] = new Proportion(-v.Proportions[k].P0, -v.Proportions[k].P1);
        return new SpaceValue(v.Space, proportions);
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
    /// For a 1D segment space this is identical to complex number multiplication:
    ///   result_r = a_r·b_r − a_i·b_i
    ///   result_i = a_r·b_i + a_i·b_r
    ///
    /// Note: the result may have negative components (e.g. when i²=−1 dominates).
    /// Negative r means the product went "left" of the axis origin.
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

        // Pack accumulator back into Proportions (one per axis), normalising by BasisTicks
        int    axes        = a.Space.Axes.Count;
        var    proportions = new Proportion[axes];
        for (int k = 0; k < axes; k++)
            proportions[k] = new Proportion(acc[k * 2] / bt, acc[k * 2 + 1] / bt);
        return new SpaceValue(a.Space, proportions);
    }

    // ── Overflow / tension ────────────────────────────────────────────────────

    /// <summary>
    /// Total tick overflow: the sum of how much each magnitude component
    /// deviates from the valid range [0, maxMagnitude].
    ///
    /// Two sources of overflow:
    ///   1. Magnitude too large: ticks > maxMagnitude → excess = ticks − max
    ///   2. Negative ticks (arithmetic tension): ticks &lt; 0 → excess = |ticks|
    ///
    /// Returns 0 when all components are within bounds.
    /// Overflow is not an error — it is diagnostic "tension" information.
    /// </summary>
    public long TotalOverflow()
    {
        long total = 0;
        for (int k = 0; k < Space.Axes.Count; k++)
        {
            var ax = Space.Axes[k];
            total += MagnitudeOverflow(Proportions[k].P0, ax.MaxP0Tick);
            total += MagnitudeOverflow(Proportions[k].P1, ax.MaxP1Tick);
        }
        return total;
    }

    /// <summary>True when all components lie within their axis bounds.</summary>
    public bool InBounds => TotalOverflow() == 0;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AssertSameSpace(SpaceValue a, SpaceValue b)
    {
        if (!ReferenceEquals(a.Space, b.Space))
            throw new InvalidOperationException(
                "Cannot combine values from different Space instances.  " +
                "Ensure both values were created from the same Space object.");
    }

    /// <summary>
    /// Compute the overflow for a single magnitude component.
    ///   ticks &lt; 0:          return |ticks| (negative magnitude = full tension)
    ///   ticks &gt; maxMagnitude: return excess above max
    ///   otherwise:          return 0
    /// </summary>
    private static long MagnitudeOverflow(long ticks, long maxMagnitude)
    {
        if (ticks < 0)            return -ticks;
        if (ticks > maxMagnitude) return ticks - maxMagnitude;
        return 0L;
    }

    public override string ToString()
    {
        var parts = Proportions.Select((p, k) =>
        {
            var    ax   = Space.Axes[k];
            double p0   = ax.ToUnits(p.P0);
            double p1   = ax.ToUnits(p.P1);
            string name = ax.Name;
            return (name.Length > 0 ? $"{name}:" : "") + $"[{p0:G4}i+{p1:G4}]";
        });
        return string.Join("  ", parts);
    }
}
