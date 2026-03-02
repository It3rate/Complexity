namespace Spaces.Core;

/// <summary>
/// Governs how one end of a <see cref="Focal"/> is resolved when read.
///
/// Locks are always resolved in a SINGLE STEP against the OTHER end's
/// raw stored value — never recursively through the other end's lock.
/// This avoids infinite recursion and keeps the semantic simple:
/// "what does THIS end actually evaluate to, given what is STORED in the other?"
///
/// ── PATTERNS ────────────────────────────────────────────────────────────────
///
///   Symmetric unit:    lo locked to NegateOther  →  Lo always = −RawHi
///   Purely-real value: lo locked to Fixed(0)      →  Lo is always 0
///   Degenerate point:  both Fixed to same value   →  Lo == Hi, immutable
/// </summary>
public enum EndLock
{
    /// <summary>
    /// Unconstrained.  The value is freely settable and returns its own
    /// stored raw value.
    /// </summary>
    None,

    /// <summary>
    /// Immutable.  The stored value cannot be changed via factory methods,
    /// but is still returned normally when read.
    /// </summary>
    Fixed,

    /// <summary>
    /// Derived: always returns the OTHER end's raw stored value (not its
    /// effective value — one-step resolution only).
    /// Useful for degenerate "point" values where lo must equal hi.
    /// </summary>
    EqualOther,

    /// <summary>
    /// Derived: always returns the NEGATION of the other end's raw stored value.
    /// Canonical use: enforcing symmetric resolution (lo = −hi) in an Axis unit,
    /// ensuring that the positive and negative unit sizes are always identical.
    /// Also used to lock a rational number's imaginary part to zero via negation
    /// of a known-zero denominator.
    /// </summary>
    NegateOther,
}
