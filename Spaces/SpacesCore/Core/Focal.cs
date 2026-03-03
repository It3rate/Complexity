namespace Spaces.Core;

/// <summary>
/// The fundamental two-long primitive: a (lo, hi) directed pair of magnitudes.
///
/// ── WHAT A FOCAL IS ─────────────────────────────────────────────────────────
///
/// A Focal stores two longs representing the Lo (imaginary / left-pointing)
/// and Hi (real / right-pointing) components of a value or a unit of measure.
///
/// ── VALUE ENCODING (for SpaceValues) ────────────────────────────────────────
///
/// When used as a value Focal inside a <see cref="Spaces.Core.SpaceValue"/>,
/// both Lo and Hi store NON-NEGATIVE MAGNITUDES:
///
///   Lo  —  magnitude in the LEFT / imaginary (i) direction  ≥ 0
///   Hi  —  magnitude in the RIGHT / real (r) direction      ≥ 0
///
/// The sign / direction is NOT stored here; it is determined by the axis
/// chirality.  Given a Focal(lo, hi) and an axis chirality:
///
///   Chirality.Positive → number-line position: [−lo/tpu, +hi/tpu]
///                         display: "{lo/tpu}i + {hi/tpu}"
///                         Example: Focal(500, 600) @ tpu=100 → [−5, +6], "5i + 6"
///
///   Chirality.Negative → number-line position: [+lo/tpu, −hi/tpu]
///                         display: "(−{lo/tpu}i − {hi/tpu})"
///                         Example: Focal(500, 600) @ tpu=100 → [+5, −6], "(−5i − 6)"
///
///   Key insight: in both chiralities the Lo magnitude is always "positive left"
///   in its own local context.  The AXIS provides the chirality; the Focal just
///   stores unsigned distances in each direction.
///
/// ── UNIT ENCODING (for Axis.Unit) ───────────────────────────────────────────
///
/// When used as a unit Focal inside an <see cref="Axis"/>:
///
///   Axis.Signed / Axis.Positive (real number axes):
///     Unit = ZeroImag(tpu) → Lo = 0 (Fixed), Hi = tpu
///     Lo = 0  means "there is no imaginary unit on this axis"
///     Hi = tpu means "one real unit = tpu ticks"
///     This prevents imaginary (left-pointing) components from accidentally
///     accumulating on a purely real axis during algebra.
///
///   Complex axes (future / custom):
///     Unit = SymmetricUnit(tpu) → Lo = tpu (EqualOther), Hi = tpu
///     Both the i-unit and r-unit have the same positive magnitude.
///     Note: BOTH magnitudes are positive; NegateOther is no longer used for units.
///
/// ── ARITHMETIC INVARIANT ────────────────────────────────────────────────────
///
/// Arithmetic operators on SpaceValue (+ − * negate) work on raw long values
/// and do NOT enforce the non-negative convention.  After arithmetic, components
/// may be negative — this represents "tension" (the measurement has gone past
/// zero in the opposite direction).  Only VALUE CREATION via Space.Value() /
/// Space.CreateValue() enforces abs() on user inputs.
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
/// The underlying stored value (_lo or _hi) is ALWAYS preserved regardless of
/// lock state — it surfaces through RawLo/RawHi and will be used again if the
/// lock is ever removed (Unlock()).
///
/// ── COMMON PATTERNS ─────────────────────────────────────────────────────────
///
///   Real unit (Axis.Signed / Axis.Positive):
///     Focal.ZeroImag(100) → Lo = 0 (Fixed), Hi = 100
///     No imaginary unit; purely real axis.
///
///   Symmetric unit (complex axes):
///     Focal.SymmetricUnit(100) → Lo = 100 (EqualOther), Hi = 100
///     Both directions have equal unit magnitude; Lo always tracks Hi.
///
///   Purely-real value (no left extent):
///     Focal.ZeroImag(5) → Lo = 0 (Fixed), Hi = 5
///
///   Degenerate point (locked at a single value):
///     Focal.FixedPoint(7) → Lo = 7 (Fixed), Hi = 7 (Fixed)
///
/// ── IMMUTABILITY ────────────────────────────────────────────────────────────
///
/// Focal is immutable after construction.  All mutation produces a new Focal
/// via WithLo / WithHi / WithLocks / Unlock.  This makes Focals safe to share
/// between axes, spaces, and values without defensive copying.
/// </summary>
public sealed class Focal
{
    // ── Raw storage ──────────────────────────────────────────────────────────
    // _lo and _hi are always preserved, even when a lock overrides the effective
    // value.  They can be retrieved via RawLo/RawHi and restored on Unlock().
    private readonly long _lo;
    private readonly long _hi;

    public EndLock LoLock { get; }
    public EndLock HiLock { get; }

    public Focal(long lo, long hi,
                 EndLock loLock = EndLock.None,
                 EndLock hiLock = EndLock.None)
    {
        _lo    = lo;
        _hi    = hi;
        LoLock = loLock;
        HiLock = hiLock;
    }

    // ── Raw stored values ─────────────────────────────────────────────────────
    // Always available regardless of lock state.
    // Use for serialisation, debugging, or post-unlock restoration.
    /// <summary>The raw stored lo value, regardless of LoLock.</summary>
    public long RawLo => _lo;
    /// <summary>The raw stored hi value, regardless of HiLock.</summary>
    public long RawHi => _hi;

    // ── Effective values ──────────────────────────────────────────────────────

    /// <summary>
    /// The effective Lo value, respecting LoLock:
    ///   None / Fixed  →  own stored _lo
    ///   EqualOther    →  _hi  (the OTHER end's RAW stored value)
    ///   NegateOther   →  −_hi
    /// </summary>
    public long Lo => LoLock switch
    {
        EndLock.EqualOther  =>  _hi,
        EndLock.NegateOther => -_hi,
        _                   =>  _lo,
    };

    /// <summary>
    /// The effective Hi value, respecting HiLock:
    ///   None / Fixed  →  own stored _hi
    ///   EqualOther    →  _lo  (the OTHER end's RAW stored value)
    ///   NegateOther   →  −_lo
    /// </summary>
    public long Hi => HiLock switch
    {
        EndLock.EqualOther  =>  _lo,
        EndLock.NegateOther => -_lo,
        _                   =>  _hi,
    };

    // ── Immutable update factories ────────────────────────────────────────────
    // Each method returns a new Focal.  Fixed locks are honoured (return this).

    /// <summary>
    /// Return a new Focal with Lo set to <paramref name="newLo"/>.
    /// If LoLock is Fixed, returns this instance unchanged.
    /// </summary>
    public Focal WithLo(long newLo) =>
        LoLock == EndLock.Fixed
            ? this
            : new Focal(newLo, _hi, LoLock, HiLock);

    /// <summary>
    /// Return a new Focal with Hi set to <paramref name="newHi"/>.
    /// If HiLock is Fixed, returns this instance unchanged.
    /// Note: if LoLock is EqualOther, Lo's effective value will track the new Hi.
    /// </summary>
    public Focal WithHi(long newHi) =>
        HiLock == EndLock.Fixed
            ? this
            : new Focal(_lo, newHi, LoLock, HiLock);

    /// <summary>Return a new Focal with the given lock states (raw values unchanged).</summary>
    public Focal WithLocks(EndLock loLock, EndLock hiLock) =>
        new Focal(_lo, _hi, loLock, hiLock);

    /// <summary>
    /// Return a new Focal with both locks removed (None/None).
    /// The raw stored values (_lo, _hi) now surface directly as Lo and Hi.
    /// </summary>
    public Focal Unlock() => new Focal(_lo, _hi);

    // ── Static factory helpers ────────────────────────────────────────────────

    /// <summary>
    /// A symmetric unit Focal where Lo always equals Hi (both positive magnitudes).
    /// Lo is locked via EqualOther so that changing Hi (via WithHi) automatically
    /// keeps Lo = Hi.
    ///
    /// Use for COMPLEX axes where both the i-unit and r-unit have the same size.
    /// For REAL (non-complex) axes, use <see cref="ZeroImag"/> instead.
    /// </summary>
    /// <param name="ticksPerUnit">
    ///   The positive unit magnitude in both directions.
    ///   Stored as both _lo and _hi; Lo reads _hi via EqualOther.
    /// </param>
    public static Focal SymmetricUnit(long ticksPerUnit) =>
        new Focal(ticksPerUnit, ticksPerUnit, EndLock.EqualOther, EndLock.None);

    /// <summary>
    /// A Focal with Lo Fixed at zero, Hi free.
    ///
    /// Two uses:
    ///   1. As a Unit Focal for <see cref="Axis.Signed"/> / <see cref="Axis.Positive"/>:
    ///      Lo = 0 (Fixed) means "no imaginary unit on this axis", preventing
    ///      imaginary components from entering calculations on a purely real axis.
    ///
    ///   2. As a value Focal representing a purely right-pointing quantity
    ///      (no left / imaginary extent).
    /// </summary>
    public static Focal ZeroImag(long hiValue) =>
        new Focal(0L, hiValue, EndLock.Fixed, EndLock.None);

    /// <summary>
    /// A degenerate point: both ends Fixed at the same value.
    /// No arithmetic can change this Focal.
    /// </summary>
    public static Focal FixedPoint(long value) =>
        new Focal(value, value, EndLock.Fixed, EndLock.Fixed);

    public override string ToString()
    {
        string loStr = LoLock != EndLock.None ? $"{Lo}[{LoLock}]" : $"{Lo}";
        string hiStr = HiLock != EndLock.None ? $"{Hi}[{HiLock}]" : $"{Hi}";
        return $"({loStr}, {hiStr})";
    }
}
