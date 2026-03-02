namespace Spaces.Core;

/// <summary>
/// The fundamental two-long primitive: a (lo, hi) directed interval.
///
/// ── WHAT A FOCAL IS ─────────────────────────────────────────────────────────
///
/// A Focal stores two signed longs representing the endpoints of a directed
/// interval.  Within an <see cref="Axis"/> these map to:
///
///   Lo — the "imaginary" (i) component, pointing in the left/negative direction
///   Hi — the "real"      (r) component, pointing in the right/positive direction
///
/// The complex-number encoding of a segment [a, b] on a number line is:
///   Focal(lo = a, hi = b)
///   expressed in i-notation as "(−a)·i + b"  (where −a is positive when a &lt; 0)
///
///   Examples (using unit = 1 for clarity):
///     Point 5         →  Focal(0, 5)   = "0i + 5"
///     Segment [−2,+4] →  Focal(−2, 4)  = "2i + 4"
///     Segment [3, 7]  →  Focal(3, 7)   = "−3i + 7"  (starts in positive territory)
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
///   Symmetric unit (lo = −hi always):
///     Focal.SymmetricUnit(100) → _lo=−100, _hi=100, loLock=NegateOther
///     Lo always reads −_hi, so changing Hi keeps Lo=−Hi at no extra cost.
///
///   Purely-real value (imaginary part pinned to zero):
///     Focal.ZeroImag(5) → lo=0 (Fixed), hi=5 (free)
///
///   Degenerate point (both ends locked):
///     Focal.FixedPoint(7) → lo=7 (Fixed), hi=7 (Fixed)
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
    /// Note: if LoLock is NegateOther, Lo's effective value will track the new Hi.
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
    /// A symmetric unit Focal where Lo is always locked to −Hi.
    /// Changing Hi on the returned Focal (via WithHi) keeps Lo = −Hi automatically.
    /// Canonical use: the Unit Focal of a standard symmetric Axis.
    /// </summary>
    /// <param name="ticksPerUnit">
    ///   The positive unit magnitude.  Stored as _hi; _lo is set to −ticksPerUnit
    ///   (preserved but overridden by the NegateOther lock).
    /// </param>
    public static Focal SymmetricUnit(long ticksPerUnit) =>
        new Focal(-ticksPerUnit, ticksPerUnit, EndLock.NegateOther, EndLock.None);

    /// <summary>
    /// A Focal with Lo Fixed at zero, Hi free.
    /// Use to enforce a purely-real value (no i/imaginary component).
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
