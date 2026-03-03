namespace Spaces.Core;

/// <summary>
/// A single named dimension with defined resolution, extent, and chirality.
///
/// ── TWO-PROPORTION STRUCTURE ─────────────────────────────────────────────────
///
/// An Axis holds TWO <see cref="Proportion"/> instances with opposite chirality,
/// representing the real (right) and imaginary (left) sides of the axis:
///
///   RealProportion  — positive chirality (real / right side)
///     P0 = real unit ticks     (denominator, GetUnit(Positive))
///     P1 = real extent ticks   (numerator,   GetMeasure(Positive))
///
///   ImagProportion  — negative chirality (imaginary / left side)
///     P0 = imaginary extent ticks (numerator,   GetMeasure(Negative))
///     P1 = imaginary unit ticks   (denominator, GetUnit(Negative))
///
/// Range and Unit are computed from these two proportions:
///
///   Range.P0 = ImagProportion.GetMeasure(Negative) = max LEFT  (i) magnitude
///   Range.P1 = RealProportion.GetMeasure(Positive) = max RIGHT (r) magnitude
///
///   Unit.P0  = ImagProportion.GetUnit(Negative)    = imaginary ticks per unit
///   Unit.P1  = RealProportion.GetUnit(Positive)    = real      ticks per unit
///
/// For a real axis (Axis.Signed / Axis.Positive):
///   ImagProportion has P1 Fixed at 0 — no imaginary unit.
///   This prevents imaginary components from accumulating during algebra.
///
/// ── RANGE AND UNIT ───────────────────────────────────────────────────────────
///
///   Range    — max extents as positive magnitudes.
///              Range.P0 = max ticks in the LEFT (i) direction.
///              Range.P1 = max ticks in the RIGHT (r) direction.
///
///              Example for Axis.Signed(10.0, 100):
///                Range = Proportion(1000, 1000) — axis spans ±10 units from origin
///
///   Unit     — resolution: ticks per unit.
///              Unit.P0 = imaginary tpu (0 for real axes, Fixed).
///              Unit.P1 = real tpu.
///              TicksPerUnit = |Unit.P1| = real tpu.
///
///   Chirality — the observer perspective.  Does not change stored Proportion data;
///               it is a label used at display / interpretation time.
///
/// ── VALUE MAGNITUDES vs NUMBER-LINE POSITIONS ────────────────────────────────
///
/// A SpaceValue Proportion on this axis stores NON-NEGATIVE MAGNITUDES:
///   p0_ticks = distance in the LEFT direction from the axis origin
///   p1_ticks = distance in the RIGHT direction from the axis origin
///
/// To get number-line positions apply chirality:
///
///   Chirality.Positive:
///     Left endpoint  = −p0_ticks / TicksPerUnit
///     Right endpoint = +p1_ticks / TicksPerUnit
///     Display: "({p0/tpu}i+{p1/tpu})"
///
///   Chirality.Negative  (axis "viewed upside-down"):
///     Left endpoint  = +p0_ticks / TicksPerUnit
///     Right endpoint = −p1_ticks / TicksPerUnit
///     Display: "[-{p0/tpu}i-{p1/tpu}]"
///
/// ── OVERFLOW / TENSION ──────────────────────────────────────────────────────
///
/// A SpaceValue is "in bounds" when all component magnitudes lie within the axis range.
///
///   P0InRange(ticks): valid when 0 ≤ ticks ≤ MaxP0Tick
///   P1InRange(ticks): valid when 0 ≤ ticks ≤ MaxP1Tick
/// </summary>
public sealed class Axis
{
    // ── Primary storage: two proportions with opposite chirality ──────────────

    /// <summary>
    /// The real (right / positive-chirality) proportion.
    ///   P0 = real unit ticks     (denominator under positive chirality)
    ///   P1 = real extent ticks   (numerator   under positive chirality)
    /// </summary>
    public Proportion RealProportion { get; }

    /// <summary>
    /// The imaginary (left / negative-chirality) proportion.
    ///   P0 = imaginary extent ticks (numerator   under negative chirality)
    ///   P1 = imaginary unit ticks   (denominator under negative chirality)
    /// For real axes: P1 is Fixed at 0 (no imaginary unit).
    /// </summary>
    public Proportion ImagProportion { get; }

    // ── Derived from the two proportions ─────────────────────────────────────

    /// <summary>
    /// Maximum extents of this axis, stored as positive magnitudes.
    /// Range.P0 = max ticks in the LEFT  (i) direction  = ImagProportion.GetMeasure(Negative)
    /// Range.P1 = max ticks in the RIGHT (r) direction  = RealProportion.GetMeasure(Positive)
    /// </summary>
    public Proportion Range { get; }

    /// <summary>
    /// Resolution: ticks per unit.
    /// Unit.P0 = imaginary tpu = ImagProportion.GetUnit(Negative)   [0 and Fixed for real axes]
    /// Unit.P1 = real      tpu = RealProportion.GetUnit(Positive)
    /// </summary>
    public Proportion Unit { get; }

    /// <summary>Which perspective this axis is observed from.</summary>
    public Chirality Chirality { get; }

    /// <summary>Optional human-readable label.</summary>
    public string Name { get; }

    public Axis(Proportion realProportion, Proportion imagProportion,
                Chirality chirality = Chirality.Positive,
                string name = "")
    {
        RealProportion = realProportion;
        ImagProportion = imagProportion;
        Chirality      = chirality;
        Name           = name;

        // Range: P0 = imaginary measure (left extent), P1 = real measure (right extent)
        Range = new Proportion(
            imagProportion.GetMeasure(Chirality.Negative),
            realProportion.GetMeasure(Chirality.Positive),
            imagProportion.GetMeasureLock(Chirality.Negative),
            realProportion.GetMeasureLock(Chirality.Positive));

        // Unit: P0 = imaginary tpu, P1 = real tpu
        Unit = new Proportion(
            imagProportion.GetUnit(Chirality.Negative),
            realProportion.GetUnit(Chirality.Positive),
            imagProportion.GetUnitLock(Chirality.Negative),
            realProportion.GetUnitLock(Chirality.Positive));
    }

    // ── Derived properties ────────────────────────────────────────────────────

    /// <summary>Ticks per unit (real axis resolution; always positive; uses |Unit.P1|).</summary>
    public long TicksPerUnit => Math.Abs(Unit.P1);

    /// <summary>
    /// Maximum tick MAGNITUDE in the P0 (left / imaginary) direction.
    /// Equals Range.P0.  For Axis.Positive this is 0 (no left extent).
    /// </summary>
    public long MaxP0Tick => Range.P0;

    /// <summary>
    /// Maximum tick MAGNITUDE in the P1 (right / real) direction.
    /// Equals Range.P1.
    /// </summary>
    public long MaxP1Tick => Range.P1;

    /// <summary>Convenience alias for MaxP1Tick.</summary>
    public long MaxTick => Range.P1;

    /// <summary>+1 for Positive chirality, −1 for Negative.</summary>
    public int ChiralitySign => (int)Chirality;

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Convert a tick count to a double unit value.
    /// Works for both P0 and P1 components: divide by TicksPerUnit (= Unit.P1).
    /// Returns 0 if TicksPerUnit is 0 (degenerate axis).
    /// </summary>
    public double ToUnits(long ticks) =>
        TicksPerUnit == 0 ? 0.0 : (double)ticks / TicksPerUnit;

    /// <summary>
    /// Convert a double unit value to the nearest tick (round-half-to-even).
    /// Note: this method does NOT apply abs(); it returns a signed result.
    /// Callers that require positive magnitudes (e.g. Space.Value()) must abs
    /// the result themselves.
    /// </summary>
    public long ToTicks(double units) =>
        (long)Math.Round(units * TicksPerUnit, MidpointRounding.ToEven);

    // ── Range checking ────────────────────────────────────────────────────────

    /// <summary>
    /// True when <paramref name="ticks"/> is a valid P0 (i / left) magnitude:
    /// must be in [0, MaxP0Tick].
    /// Negative ticks are always out-of-range (they represent tension from arithmetic).
    /// </summary>
    public bool P0InRange(long ticks) => ticks >= 0 && ticks <= MaxP0Tick;

    /// <summary>
    /// True when <paramref name="ticks"/> is a valid P1 (r / right) magnitude:
    /// must be in [0, MaxP1Tick].
    /// Negative ticks are always out-of-range (they represent tension from arithmetic).
    /// </summary>
    public bool P1InRange(long ticks) => ticks >= 0 && ticks <= MaxP1Tick;

    // ── Chirality operations ──────────────────────────────────────────────────

    /// <summary>Return a copy of this axis with the given chirality.</summary>
    public Axis WithChirality(Chirality chirality) =>
        new Axis(RealProportion, ImagProportion, chirality, Name);

    /// <summary>Return a copy with the opposite chirality.</summary>
    public Axis FlipChirality() =>
        WithChirality(Chirality == Chirality.Positive ? Chirality.Negative : Chirality.Positive);

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Create a standard symmetric real-number axis centred at zero.
    ///
    /// The axis spans [−rangeMax, +rangeMax] units, expressed as positive magnitudes.
    ///
    /// RealProportion  = Proportion(P0=ticksPerUnit, P1=maxTick)
    ///   GetUnit(Positive)    = P0 = ticksPerUnit (real denominator)
    ///   GetMeasure(Positive) = P1 = maxTick      (real numerator / right extent)
    ///
    /// ImagProportion  = Proportion(P0=maxTick, P1=0, None, Fixed)
    ///   GetUnit(Negative)    = P1 = 0 (Fixed)    (no imaginary unit — real axis)
    ///   GetMeasure(Negative) = P0 = maxTick       (imaginary extent / left extent)
    ///
    /// Derived:
    ///   Range = Proportion(maxTick, maxTick)      — equal extent in both directions
    ///   Unit  = Proportion(0, ticksPerUnit, Fixed, None)
    /// </summary>
    public static Axis Signed(double rangeMax, long ticksPerUnit,
        Chirality chirality = Chirality.Positive, string name = "")
    {
        long maxTick   = (long)Math.Round(rangeMax * ticksPerUnit);
        var  realProp  = new Proportion(ticksPerUnit, maxTick);
        var  imagProp  = new Proportion(maxTick, 0L, EndLock.None, EndLock.Fixed);
        return new Axis(realProp, imagProp, chirality, name);
    }

    /// <summary>
    /// Create a positive-only axis spanning [0, rangeMax] units.
    ///
    /// The axis has no left (imaginary) extent:
    ///   ImagProportion = Proportion(0, 0, Fixed, Fixed)
    ///   Both ImagProportion ends are Fixed at 0.
    ///
    /// Derived:
    ///   Range.P0 = 0 (Fixed) — no left extent
    ///   Unit.P0  = 0 (Fixed) — no imaginary unit
    ///
    /// Values with any non-zero P0 component are immediately "in tension."
    /// </summary>
    public static Axis Positive(double rangeMax, long ticksPerUnit,
        Chirality chirality = Chirality.Positive, string name = "")
    {
        long maxTick   = (long)Math.Round(rangeMax * ticksPerUnit);
        var  realProp  = new Proportion(ticksPerUnit, maxTick);
        var  imagProp  = new Proportion(0L, 0L, EndLock.Fixed, EndLock.Fixed);
        return new Axis(realProp, imagProp, chirality, name);
    }

    public override string ToString() =>
        $"Axis({(Name.Length > 0 ? Name + " " : "")}range={Range} unit={Unit} {Chirality})";
}
