namespace Spaces.Core;

/// <summary>
/// Shared abstraction for any coordinate context that holds <see cref="SpaceValue"/>
/// objects and can produce new ones.
///
/// ── WHY THIS INTERFACE EXISTS ────────────────────────────────────────────────
///
/// Both <see cref="Space"/> (a global root frame) and <see cref="SpaceValue"/>
/// (a sub-frame within a parent space) are capable of:
///   • owning a list of values
///   • creating new values within their coordinate system
///   • accepting externally-created values
///
/// IValueSpace captures this shared capability, enabling calibration layers,
/// display systems, and hierarchical value management to treat root and
/// sub-frames uniformly.
///
/// ── ROOT vs CHILD ────────────────────────────────────────────────────────────
///
///   Space        — root; has NO parent; Axes are defined directly.
///                  Values added here exist in absolute tick coordinates.
///
///   SpaceValue   — child; HAS a parent <see cref="Space"/>.
///                  Sub-values added here are semantically "within" this value
///                  but physically stored in the same parent Space tick space.
///
///   In a future calibration-aware version, a SpaceValue acting as IValueSpace
///   would carry its own <see cref="Calibration"/>, allowing sub-values to be
///   expressed in different units (e.g. feet inside a miles-based Space).
///   See <see cref="Calibration"/> for the planned architecture.
///
/// ── CREATING vs ADDING VALUES ────────────────────────────────────────────────
///
///   CreateValue  — creates a new SpaceValue AND registers it in Values.
///                  Both lo and hi magnitudes are abs'd (see value-semantics docs).
///
///   AddValue     — registers an already-created SpaceValue in Values.
///                  Implementations may validate that the value belongs to the
///                  same logical space before accepting it.
///
///   Space.Value()     (not part of this interface) — pure factory: creates
///                  a SpaceValue WITHOUT registering it; intended for arithmetic
///                  and temporary values.
/// </summary>
public interface IValueSpace
{
    /// <summary>Human-readable label.</summary>
    string Name { get; }

    /// <summary>The axes that define the dimensions of this coordinate context.</summary>
    IReadOnlyList<Axis> Axes { get; }

    /// <summary>
    /// Ticks per unit used to normalise multiplication results.
    /// Derived from Axes[0].TicksPerUnit.
    /// </summary>
    long BasisTicks { get; }

    /// <summary>Total number of flat components (2 per axis: one Lo, one Hi).</summary>
    int ComponentCount { get; }

    /// <summary>
    /// All <see cref="SpaceValue"/> objects currently registered in this context,
    /// in registration order.
    /// </summary>
    IReadOnlyList<SpaceValue> Values { get; }

    /// <summary>
    /// Create a new <see cref="SpaceValue"/> from flat unit-space magnitudes and
    /// register it in <see cref="Values"/>.
    ///
    /// Supply lo/hi pairs in axis order: axis0_lo, axis0_hi, axis1_lo, axis1_hi, …
    /// Both lo and hi are interpreted as magnitudes (absolute values are taken).
    /// </summary>
    SpaceValue CreateValue(params double[] units);

    /// <summary>
    /// Register an externally-created <see cref="SpaceValue"/> in <see cref="Values"/>.
    /// Implementations should verify the value is compatible with this context.
    /// </summary>
    void AddValue(SpaceValue value);
}
