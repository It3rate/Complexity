namespace Spaces.Core;

/// <summary>
/// The fundamental two-long primitive: a (P0, P1) directed pair encoding a <b>proportion</b>.
///
/// ── WHAT A PROPORTION IS ────────────────────────────────────────────────────
///
/// A Proportion stores two positive-magnitude longs:
///
///   P0 — the "left" value.  In positive chirality: denominator / unit (bottom of fraction).
///          In negative chirality: numerator / measure (top of fraction).
///   P1 — the "right" value.  In positive chirality: numerator / measure (top of fraction).
///          In negative chirality: denominator / unit (bottom of fraction).
///
/// Both are non-negative magnitudes.  The role each plays (unit vs measure, top vs bottom)
/// is determined by the <see cref="Chirality"/> of the enclosing <see cref="Axis"/> — not by
/// any sign stored here.
///
/// ── FOUR-QUADRANT MODEL ──────────────────────────────────────────────────────
///
/// Chirality maps a Proportion onto one of the two sides of the four-quadrant plane:
///
///   Chirality.Positive (right / real side):
///     Standard fraction reading: Measure / Unit = P1 / P0.
///     The proportion sits on the RIGHT of the origin.
///     GetUnit(Positive)    = P0   (denominator)
///     GetMeasure(Positive) = P1   (numerator)
///     Display: "({P0/tpu}i+{P1/tpu})"
///     Example: Proportion(500, 600) @ tpu=100 → "(5i+6)" → fraction 6/5
///
///   Chirality.Negative (left / imaginary side):
///     Inverted fraction reading: P0 / P1  (rotate the paper 180°).
///     The proportion sits on the LEFT of the origin.
///     GetUnit(Negative)    = P1   (denominator in this view)
///     GetMeasure(Negative) = P0   (numerator in this view)
///     Display: "[-{P0/tpu}i-{P1/tpu}]"
///     Example: Proportion(500, 600) @ tpu=100 → "[-5i-6]" → fraction 5/6
///
/// Key insight: in BOTH chiralities P0 is "positive left" and P1 is "positive right"
/// in their LOCAL frame.  Chirality tells the observer which direction is "up" in the
/// fraction, not what the stored values are.
///
/// ── AXIS STRUCTURE ───────────────────────────────────────────────────────────
///
/// An <see cref="Axis"/> holds TWO Proportion instances with opposite chirality:
///
///   RealProportion  — positive chirality (real / right side)
///     P0 = real unit ticks (denominator direction)
///     P1 = real measure ticks (numerator direction / right extent)
///
///   ImagProportion  — negative chirality (imaginary / left side)
///     P0 = imaginary measure ticks (numerator in negative-chirality view / left extent)
///     P1 = imaginary unit ticks (denominator in negative-chirality view)
///
///   Range = Proportion(P0=imagMeasure, P1=realMeasure)   ← two numerators
///   Unit  = Proportion(P0=imagUnit,    P1=realUnit)       ← two denominators
///
/// ── CONTAINER BEHAVIOR ──────────────────────────────────────────────────────
///
/// A Proportion is also a container for multiple measures sharing the same unit.
///
///   GetMeasure(Positive) = P1    — the primary measure (index 0 in Measures).
///   <see cref="Measures"/>       — all measures: [P1, ...additional].
///   <see cref="WithMeasure(long)"/> — returns a new Proportion with one more measure.
///
/// Example: Proportion(unit=2, measure=10) can grow to hold [10, 6, 9],
/// representing the fractions 10/2, 6/2, 9/2 — same denominator, multiple numerators.
///
/// ── ARITHMETIC INVARIANT ────────────────────────────────────────────────────
///
/// Arithmetic operators on SpaceValue (+ − * negate) work on raw long values
/// and do NOT enforce the non-negative convention.  After arithmetic, components
/// may be negative — this represents "tension".  Only VALUE CREATION via
/// Space.Value() / Space.CreateValue() enforces abs() on user inputs.
///
/// ── LOCKING ─────────────────────────────────────────────────────────────────
///
/// Each end independently carries an <see cref="EndLock"/> controlling how it
/// is resolved when read:
///
///   None        — free; returns own stored value
///   Fixed       — immutable; cannot be changed; still returns own stored value
///   EqualOther  — derived; returns the OTHER end's RAW stored value
///   NegateOther — derived; returns −(OTHER end's RAW stored value)
///
/// Lock resolution always reads the RAW stored value of the other end
/// (not its effective value), ensuring ONE-STEP resolution with no recursion.
///
/// The underlying stored value (_p0 or _p1) is ALWAYS preserved regardless of
/// lock state — it surfaces through RawP0/RawP1 and will be used again if the
/// lock is ever removed (Unlock()).
///
/// ── COMMON PATTERNS ─────────────────────────────────────────────────────────
///
///   Real unit (Axis.Signed / Axis.Positive):
///     Proportion.ZeroImag(100) → P0 = 0 (Fixed), P1 = 100
///     P0 = 0 means "there is no unit in the imaginary/denominator direction."
///
///   Symmetric unit (complex axes):
///     Proportion.SymmetricUnit(100) → P0 = 100 (EqualOther), P1 = 100
///     Both directions have equal unit magnitude; P0 always tracks P1.
///
///   Purely-real value (no left extent):
///     Proportion.ZeroImag(5) → P0 = 0 (Fixed), P1 = 5
///
///   Degenerate point (locked at a single value):
///     Proportion.FixedPoint(7) → P0 = 7 (Fixed), P1 = 7 (Fixed)
///
/// ── IMMUTABILITY ────────────────────────────────────────────────────────────
///
/// Proportion is immutable after construction.  All mutation produces a new
/// Proportion via WithP0 / WithP1 / WithLocks / Unlock / WithMeasure.
/// This makes Proportions safe to share between axes, spaces, and values.
/// </summary>
public sealed class Proportion
{
    // ── Raw storage ──────────────────────────────────────────────────────────
    // _p0 and _p1 are always preserved, even when a lock overrides the effective
    // value.  They can be retrieved via RawP0/RawP1 and restored on Unlock().
    private readonly long _p0;
    private readonly long _p1;

    public EndLock P0Lock { get; }
    public EndLock P1Lock { get; }

    // ── Additional measures (container behavior) ──────────────────────────────
    // Measures beyond the primary P1, all sharing the same unit (P0 in +chirality).
    private readonly IReadOnlyList<long> _additionalMeasures;

    public Proportion(long p0, long p1,
                      EndLock p0Lock = EndLock.None,
                      EndLock p1Lock = EndLock.None,
                      IReadOnlyList<long>? additionalMeasures = null)
    {
        _p0                  = p0;
        _p1                  = p1;
        P0Lock               = p0Lock;
        P1Lock               = p1Lock;
        _additionalMeasures  = additionalMeasures ?? [];
    }

    // ── Raw stored values ─────────────────────────────────────────────────────
    // Always available regardless of lock state.
    // Use for serialisation, debugging, or post-unlock restoration.
    /// <summary>The raw stored P0 value, regardless of P0Lock.</summary>
    public long RawP0 => _p0;
    /// <summary>The raw stored P1 value, regardless of P1Lock.</summary>
    public long RawP1 => _p1;

    // ── Effective values ──────────────────────────────────────────────────────

    /// <summary>
    /// The effective P0 value, respecting P0Lock:
    ///   None / Fixed  →  own stored _p0
    ///   EqualOther    →  _p1  (the OTHER end's RAW stored value)
    ///   NegateOther   →  −_p1
    /// </summary>
    public long P0 => P0Lock switch
    {
        EndLock.EqualOther  =>  _p1,
        EndLock.NegateOther => -_p1,
        _                   =>  _p0,
    };

    /// <summary>
    /// The effective P1 value, respecting P1Lock:
    ///   None / Fixed  →  own stored _p1
    ///   EqualOther    →  _p0  (the OTHER end's RAW stored value)
    ///   NegateOther   →  −_p0
    /// </summary>
    public long P1 => P1Lock switch
    {
        EndLock.EqualOther  =>  _p0,
        EndLock.NegateOther => -_p0,
        _                   =>  _p1,
    };

    // ── Chirality-dependent access ─────────────────────────────────────────────

    /// <summary>
    /// Return the unit (denominator) of this proportion under the given chirality.
    ///   Positive chirality → P0  (P0 is the denominator on the real/right side)
    ///   Negative chirality → P1  (P1 is the denominator on the imaginary/left side)
    /// </summary>
    public long GetUnit(Chirality chirality) =>
        chirality == Chirality.Positive ? P0 : P1;

    /// <summary>
    /// Return the measure (numerator) of this proportion under the given chirality.
    ///   Positive chirality → P1  (P1 is the numerator on the real/right side)
    ///   Negative chirality → P0  (P0 is the numerator on the imaginary/left side)
    /// </summary>
    public long GetMeasure(Chirality chirality) =>
        chirality == Chirality.Positive ? P1 : P0;

    /// <summary>
    /// Return the lock on the unit (denominator) end for the given chirality.
    ///   Positive → P0Lock;   Negative → P1Lock.
    /// </summary>
    public EndLock GetUnitLock(Chirality chirality) =>
        chirality == Chirality.Positive ? P0Lock : P1Lock;

    /// <summary>
    /// Return the lock on the measure (numerator) end for the given chirality.
    ///   Positive → P1Lock;   Negative → P0Lock.
    /// </summary>
    public EndLock GetMeasureLock(Chirality chirality) =>
        chirality == Chirality.Positive ? P1Lock : P0Lock;

    // ── Container: multiple measures, one unit ────────────────────────────────

    /// <summary>
    /// All measures sharing this Proportion's unit (P0 in positive chirality) as denominator.
    ///
    /// Index 0 is always the primary measure (= P1 in positive chirality).
    /// Additional measures appended via <see cref="WithMeasure(long)"/> follow.
    ///
    /// Example: Proportion(p0=2, p1=10).WithMeasure(6).WithMeasure(9)
    ///   → Measures = [10, 6, 9]
    ///   → Fractions (positive chirality): 10/2, 6/2, 9/2
    /// </summary>
    public IReadOnlyList<long> Measures =>
        _additionalMeasures.Count == 0
            ? [P1]
            : [P1, .._additionalMeasures];

    /// <summary>
    /// Return a new Proportion with one additional measure appended to <see cref="Measures"/>.
    ///
    /// The unit (P0), primary measure (P1), locks, and existing additional measures
    /// are all preserved unchanged.  Immutable — this instance is not modified.
    /// </summary>
    public Proportion WithMeasure(long ticks) =>
        new Proportion(_p0, _p1, P0Lock, P1Lock, [.._additionalMeasures, ticks]);

    // ── Immutable update factories ────────────────────────────────────────────
    // Each method returns a new Proportion.  Fixed locks are honoured (return this).
    // Additional measures are preserved through all updates.

    /// <summary>
    /// Return a new Proportion with P0 set to <paramref name="newP0"/>.
    /// If P0Lock is Fixed, returns this instance unchanged.
    /// </summary>
    public Proportion WithP0(long newP0) =>
        P0Lock == EndLock.Fixed
            ? this
            : new Proportion(newP0, _p1, P0Lock, P1Lock, _additionalMeasures);

    /// <summary>
    /// Return a new Proportion with P1 set to <paramref name="newP1"/>.
    /// If P1Lock is Fixed, returns this instance unchanged.
    /// Note: if P0Lock is EqualOther, P0's effective value will track the new P1.
    /// Additional measures are preserved.
    /// </summary>
    public Proportion WithP1(long newP1) =>
        P1Lock == EndLock.Fixed
            ? this
            : new Proportion(_p0, newP1, P0Lock, P1Lock, _additionalMeasures);

    /// <summary>Return a new Proportion with the given lock states (raw values and measures unchanged).</summary>
    public Proportion WithLocks(EndLock p0Lock, EndLock p1Lock) =>
        new Proportion(_p0, _p1, p0Lock, p1Lock, _additionalMeasures);

    /// <summary>
    /// Return a new Proportion with both locks removed (None/None).
    /// The raw stored values (_p0, _p1) now surface directly as P0 and P1.
    /// Additional measures are preserved.
    /// </summary>
    public Proportion Unlock() => new Proportion(_p0, _p1, additionalMeasures: _additionalMeasures);

    // ── Static factory helpers ────────────────────────────────────────────────

    /// <summary>
    /// A symmetric unit Proportion where P0 always equals P1 (both positive magnitudes).
    /// P0 is locked via EqualOther so that changing P1 (via WithP1) automatically
    /// keeps P0 = P1.
    ///
    /// Use for COMPLEX axes where both the unit-denominator and unit-numerator
    /// have the same size.  For REAL (non-complex) axes, use <see cref="ZeroImag"/> instead.
    /// </summary>
    public static Proportion SymmetricUnit(long ticksPerUnit) =>
        new Proportion(ticksPerUnit, ticksPerUnit, EndLock.EqualOther, EndLock.None);

    /// <summary>
    /// A Proportion with P0 Fixed at zero, P1 free.
    ///
    /// Two uses:
    ///   1. As the unit Proportion for <see cref="Axis.Signed"/> / <see cref="Axis.Positive"/>:
    ///      P0 = 0 (Fixed) means "no unit in the imaginary/denominator direction."
    ///
    ///   2. As a value Proportion representing a purely right-pointing quantity
    ///      (no left / imaginary extent).
    /// </summary>
    public static Proportion ZeroImag(long p1Value) =>
        new Proportion(0L, p1Value, EndLock.Fixed, EndLock.None);

    /// <summary>
    /// A degenerate point: both ends Fixed at the same value.
    /// No arithmetic can change this Proportion.
    /// </summary>
    public static Proportion FixedPoint(long value) =>
        new Proportion(value, value, EndLock.Fixed, EndLock.Fixed);

    public override string ToString()
    {
        string p0Str = P0Lock != EndLock.None ? $"{P0}[{P0Lock}]" : $"{P0}";
        string p1Str = P1Lock != EndLock.None ? $"{P1}[{P1Lock}]" : $"{P1}";
        string measuresStr = _additionalMeasures.Count > 0
            ? $" measures=[{string.Join(", ", _additionalMeasures)}]"
            : "";
        return $"({p0Str}, {p1Str}){measuresStr}";
    }
}
