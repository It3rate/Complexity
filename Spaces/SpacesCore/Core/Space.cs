namespace Spaces.Core;

/// <summary>
/// A container for values: defined by a set of axes and a multiplication algebra.
///
/// ── WHAT A SPACE IS ─────────────────────────────────────────────────────────
///
/// A Space holds the rules and bounds within which <see cref="SpaceValue"/>
/// objects exist.  Every value in the space has one <see cref="Focal"/> per
/// axis — the Focal's Lo is the i-component and Hi is the r-component for
/// that axis direction.
///
/// A Space can be 1D (one axis, e.g. a segment), 2D (two axes, e.g. a grid),
/// or higher.  Spaces can also be built from other spaces (recursion), but
/// in this foundational layer we build directly from axes.
///
/// ── COMPONENT INDEXING ──────────────────────────────────────────────────────
///
/// Algebra entries operate on "flat component" indices:
///   For axis k:  lo-component = 2k,   hi-component = 2k+1
///
/// This means for a two-axis space: 0=axis0.lo, 1=axis0.hi, 2=axis1.lo, 3=axis1.hi.
/// See <see cref="AlgebraEntry"/> for full details of the algebra table.
///
/// ── MULTIPLICATION NORMALISATION ────────────────────────────────────────────
///
/// Products of tick values are divided by <see cref="BasisTicks"/> to keep
/// the result in the same unit system as the inputs.  All axes are assumed to
/// share the same resolution in this version; BasisTicks is taken from Axes[0].
///
/// ── FACTORY METHODS ─────────────────────────────────────────────────────────
///
///   Space.Segment(range, tpu)   — 1 axis; complex i²=−1 multiplication
///   Space.Grid(xAxis, yAxis)    — 2 independent axes; complex mult per axis
///
/// ── BUILDING VALUES ──────────────────────────────────────────────────────────
///
///   var s = Space.Segment(10.0, 100);
///   var v = s.Value(-2.0, 4.0);   // segment [−2, +4] in units
///
///   For multi-axis spaces supply pairs in axis order:
///   var g = Space.Grid(xAxis, yAxis);
///   var p = g.Value(x_lo, x_hi, y_lo, y_hi);
/// </summary>
public sealed class Space
{
    /// <summary>The axes defining the dimensions of this space.</summary>
    public IReadOnlyList<Axis> Axes { get; }

    /// <summary>The multiplication table (see <see cref="AlgebraEntry"/>).</summary>
    public IReadOnlyList<AlgebraEntry> Algebra { get; }

    /// <summary>Optional human-readable name.</summary>
    public string Name { get; }

    /// <summary>
    /// Ticks per unit used to normalise multiplication results.
    /// Taken from Axes[0]; all axes are assumed to share the same resolution.
    /// </summary>
    public long BasisTicks => Axes.Count > 0 ? Axes[0].TicksPerUnit : 1L;

    /// <summary>Total number of flat components (2 per axis).</summary>
    public int ComponentCount => Axes.Count * 2;

    public Space(IReadOnlyList<Axis> axes,
                 IReadOnlyList<AlgebraEntry> algebra,
                 string name = "")
    {
        if (axes.Count == 0)
            throw new ArgumentException("A Space must have at least one axis.", nameof(axes));
        Axes    = axes;
        Algebra = algebra;
        Name    = name;
    }

    // ── Value factory ─────────────────────────────────────────────────────────

    /// <summary>
    /// Create a <see cref="SpaceValue"/> from a flat list of doubles in unit space.
    /// Supply lo/hi pairs in axis order:  axis0_lo, axis0_hi, axis1_lo, axis1_hi, …
    /// Each double is converted to ticks via <see cref="Axis.ToTicks"/>.
    /// </summary>
    /// <param name="units">
    ///   Flat array of doubles.  Must have exactly <c>Axes.Count × 2</c> elements.
    /// </param>
    public SpaceValue Value(params double[] units)
    {
        int expected = Axes.Count * 2;
        if (units.Length != expected)
            throw new ArgumentException(
                $"Expected {expected} values ({Axes.Count} axis × 2), got {units.Length}.",
                nameof(units));

        var focals = new Focal[Axes.Count];
        for (int a = 0; a < Axes.Count; a++)
        {
            long lo = Axes[a].ToTicks(units[a * 2]);
            long hi = Axes[a].ToTicks(units[a * 2 + 1]);
            focals[a] = new Focal(lo, hi);
        }
        return new SpaceValue(this, focals);
    }

    // ── Static component helpers ──────────────────────────────────────────────

    /// <summary>Axis index for a flat component index (componentIdx / 2).</summary>
    public static int AxisOf(int componentIdx) => componentIdx / 2;

    /// <summary>True if a flat component index refers to the hi (r) end.</summary>
    public static bool IsHi(int componentIdx) => (componentIdx & 1) == 1;

    // ── Space factory methods ─────────────────────────────────────────────────

    /// <summary>
    /// One-dimensional segment space with complex (i²=−1) multiplication.
    ///
    /// Component layout: 0=lo (i-component), 1=hi (r-component).
    ///
    /// Algebra (implements complex number multiplication):
    ///   r×r → +r    (1,1,1,+1)
    ///   i×i → −r    (0,0,1,−1)   ← this is the i²=−1 entry
    ///   r×i → +i    (1,0,0,+1)
    ///   i×r → +i    (0,1,0,+1)
    ///
    /// This makes multiplication of two segments A=(a_i,a_r) and B=(b_i,b_r):
    ///   result_r = a_r·b_r − a_i·b_i
    ///   result_i = a_r·b_i + a_i·b_r
    /// (identical to complex number multiplication).
    /// </summary>
    /// <param name="rangeMax">Maximum extent in units (axis spans [−rangeMax, +rangeMax]).</param>
    /// <param name="ticksPerUnit">Resolution.</param>
    /// <param name="name">Optional label.</param>
    public static Space Segment(double rangeMax, long ticksPerUnit, string name = "segment")
    {
        var axis    = Axis.Signed(rangeMax, ticksPerUnit);
        var algebra = SegmentAlgebra(0);
        return new Space(new[] { axis }, algebra, name);
    }

    /// <summary>
    /// Two-dimensional grid space.  Each axis has independent complex algebra;
    /// there are no cross-axis multiplication terms (axes are orthogonal).
    ///
    /// Component layout: 0=x_lo, 1=x_hi, 2=y_lo, 3=y_hi.
    /// X multiplication uses components 0/1; Y multiplication uses 2/3.
    /// </summary>
    public static Space Grid(Axis xAxis, Axis yAxis, string name = "grid")
    {
        var algebra = SegmentAlgebra(0).Concat(SegmentAlgebra(1)).ToArray();
        return new Space(new[] { xAxis, yAxis }, algebra, name);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Build the four segment-algebra (complex) entries for axis index
    /// <paramref name="axisIdx"/>.  Component base = axisIdx * 2.
    ///   lo = base,  hi = base+1
    /// </summary>
    private static AlgebraEntry[] SegmentAlgebra(int axisIdx)
    {
        int lo = axisIdx * 2;
        int hi = axisIdx * 2 + 1;
        return new[]
        {
            new AlgebraEntry(hi, hi, hi, +1),  // r × r  →  +r
            new AlgebraEntry(lo, lo, hi, -1),  // i × i  →  −r   (i² = −1)
            new AlgebraEntry(hi, lo, lo, +1),  // r × i  →  +i
            new AlgebraEntry(lo, hi, lo, +1),  // i × r  →  +i
        };
    }

    public override string ToString() =>
        $"Space({(Name.Length > 0 ? Name : "?")}, {Axes.Count}D, {Algebra.Count} entries)";
}
