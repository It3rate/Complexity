namespace Spaces.Core;

/// <summary>
/// The observation perspective for an <see cref="Axis"/> or space.
///
/// Every axis can be viewed from two sides.  Positive chirality is the
/// default canonical view (numbers grow "rightward" / "upward").
/// Negative chirality is the mirror view — conceptually turning the
/// number line end-for-end.
///
/// ── EXAMPLE ─────────────────────────────────────────────────────────────────
///
///   Height above ground  →  Positive chirality (0 at ground, values grow up)
///   Depth below ground   →  Negative chirality (same physical axis, viewed
///                           from the opposite end)
///
///   A segment [−2, +4] in positive chirality stored as Focal(lo=−200, hi=400).
///   Viewed in negative chirality, the encoding flips sign:
///   Focal(lo=+200, hi=−400) — the segment is physically unchanged, only the
///   perspective has reversed.
///
/// ── IMPORTANT ───────────────────────────────────────────────────────────────
///
///   Chirality does NOT change how values are stored internally.  It records
///   which perspective WAS USED when the value was created / interpreted.
///   Converting between chiralities negates both the lo and hi components.
/// </summary>
public enum Chirality
{
    /// <summary>Default, canonical positive perspective.  ChiralitySign = +1.</summary>
    Positive = +1,

    /// <summary>Mirrored negative perspective.  ChiralitySign = −1.</summary>
    Negative = -1,
}
