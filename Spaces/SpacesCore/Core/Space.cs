namespace Spaces.Core;

/// <summary>
/// Root coordinate context: a set of axes, a multiplication algebra, and a
/// collection of registered <see cref="SpaceValue"/> objects.
///
/// ── WHAT A SPACE IS ─────────────────────────────────────────────────────────
///
/// A Space is the top-level <see cref="IValueSpace"/>.  It has NO parent — it
/// defines the absolute coordinate frame.  All SpaceValues created from this
/// Space express their measurements in terms of this Space's axes.
///
/// A Space can be 1D (Segment), 2D (Grid), or higher.
///
/// ── VALUE MAGNITUDE CONVENTION ──────────────────────────────────────────────
///
/// SpaceValues created via <see cref="Value"/> or <see cref="CreateValue"/> store
/// NON-NEGATIVE MAGNITUDES in their Focal components:
///
///   Lo (i-component) = absolute distance in the LEFT direction  (≥ 0)
///   Hi (r-component) = absolute distance in the RIGHT direction (≥ 0)
///
/// Both factory methods apply Math.Abs() to all user inputs before converting to
/// ticks, so passing -5.0 and +5.0 for lo are equivalent — both store 500 ticks
/// (assuming tpu=100).
///
/// Arithmetic results (from SpaceValue operators) are NOT abs'd; they may be
/// negative, representing tension or out-of-range extents.
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
/// ── VALUE MANAGEMENT ────────────────────────────────────────────────────────
///
/// Space implements <see cref="IValueSpace"/>.  Two paths for creating values:
///
///   Value(units)        — PURE factory.  Creates a SpaceValue but does NOT add
///                         it to the Values list.  Use for arithmetic temporaries
///                         and values you do not want tracked.
///
///   CreateValue(units)  — REGISTERING factory.  Creates the value AND adds it
///                         to the Values list.  Use when you want the space to
///                         own and enumerate the value.
///
///   AddValue(value)     — Registers an existing SpaceValue (created via Value())
///                         into the Values list.  The value must belong to this
///                         Space (by reference equality of the Space property).
///
/// ── FACTORY METHODS ─────────────────────────────────────────────────────────
///
///   Space.Segment(range, tpu)   — 1 axis; complex i²=−1 multiplication
///   Space.Grid(xAxis, yAxis)    — 2 independent axes; complex mult per axis
/// </summary>
public sealed class Space : IValueSpace
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

    // ── Value registry ────────────────────────────────────────────────────────
    private readonly List<SpaceValue> _values = new();

    /// <summary>
    /// All SpaceValues registered with this Space, in registration order.
    /// Values created via <see cref="Value"/> (pure factory) do NOT appear here
    /// unless explicitly passed to <see cref="AddValue"/>.
    /// </summary>
    public IReadOnlyList<SpaceValue> Values => _values;

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

    // ── Pure value factory ────────────────────────────────────────────────────

    /// <summary>
    /// Create a <see cref="SpaceValue"/> from a flat list of doubles in unit space.
    /// Supply lo/hi pairs in axis order:  axis0_lo, axis0_hi, axis1_lo, axis1_hi, …
    ///
    /// Both lo and hi are treated as MAGNITUDES: Math.Abs() is applied before
    /// converting to ticks.  Passing −5.0 for lo is equivalent to +5.0.
    ///
    /// This is a PURE factory — the created value is NOT added to <see cref="Values"/>.
    /// Use <see cref="CreateValue"/> to create-and-register.
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
            // Both lo and hi are stored as non-negative magnitudes.
            long lo = Math.Abs(Axes[a].ToTicks(units[a * 2]));
            long hi = Math.Abs(Axes[a].ToTicks(units[a * 2 + 1]));
            focals[a] = new Focal(lo, hi);
        }
        return new SpaceValue(this, focals);
    }

    // ── IValueSpace implementation ────────────────────────────────────────────

    /// <summary>
    /// Create a <see cref="SpaceValue"/> and register it in <see cref="Values"/>.
    /// Identical to <see cref="Value"/> in how it converts inputs to magnitudes,
    /// but additionally appends the result to the tracked Values list.
    /// </summary>
    public SpaceValue CreateValue(params double[] units)
    {
        var v = Value(units);
        _values.Add(v);
        return v;
    }

    /// <summary>
    /// Register an externally-created <see cref="SpaceValue"/> in <see cref="Values"/>.
    /// The value must have been created from this Space instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="value"/> belongs to a different Space.
    /// </exception>
    public void AddValue(SpaceValue value)
    {
        if (!ReferenceEquals(value.Space, this))
            throw new InvalidOperationException(
                "Cannot add a value that belongs to a different Space instance.");
        _values.Add(value);
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
    /// Uses <see cref="Axis.Signed"/>, which enforces:
    ///   Unit.Lo = 0 (Fixed) — no imaginary unit; real axis only
    ///   Unit.Hi = ticksPerUnit
    ///
    /// Algebra (implements complex number multiplication):
    ///   r×r → +r    (1,1,1,+1)
    ///   i×i → −r    (0,0,1,−1)   ← i²=−1 entry
    ///   r×i → +i    (1,0,0,+1)
    ///   i×r → +i    (0,1,0,+1)
    ///
    /// For value Focals with lo=imaginary magnitude, hi=real magnitude:
    ///   A=(a_i, a_r), B=(b_i, b_r):
    ///     result_r = a_r·b_r − a_i·b_i
    ///     result_i = a_r·b_i + a_i·b_r
    /// (identical to complex number multiplication)
    /// </summary>
    /// <param name="rangeMax">Maximum extent in units (axis spans ±rangeMax).</param>
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
