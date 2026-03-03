namespace Spaces.Core;

/// <summary>
/// A single named dimension with defined resolution, extent, and chirality.
///
/// ── STRUCTURE ───────────────────────────────────────────────────────────────
///
///   Range    — a Focal encoding the maximum extent in each direction.
///              Range.Lo = maximum LEFT (i / imaginary) magnitude, in ticks.
///              Range.Hi = maximum RIGHT (r / real)     magnitude, in ticks.
///              Both are non-negative: they are MAGNITUDES, not signed positions.
///
///              Example for Axis.Signed(10.0, 100):
///                Range = Focal(1000, 1000) — axis spans ±10 units from origin
///                MaxLoTick = 1000 (max 10 units to the LEFT)
///                MaxHiTick = 1000 (max 10 units to the RIGHT)
///
///   Unit     — a Focal giving the resolution.
///
///              For REAL (non-complex) axes — Axis.Signed and Axis.Positive:
///                Unit = ZeroImag(tpu) → Lo = 0 (Fixed), Hi = tpu
///                Lo = 0 means "there is no imaginary unit on this axis."
///                This prevents imaginary / left-pointing components from
///                accumulating during algebra on a purely real axis.
///                TicksPerUnit = Unit.Hi = tpu.
///
///              For COMPLEX axes (custom / future):
///                Unit = SymmetricUnit(tpu) → Lo = tpu (EqualOther), Hi = tpu
///                Both directions have equal positive unit magnitude.
///
///   Chirality — the observer perspective.  Does not change stored Focal data;
///               it is a label used at display / interpretation time.
///
/// ── VALUE MAGNITUDES vs NUMBER-LINE POSITIONS ────────────────────────────────
///
/// A SpaceValue Focal on this axis stores NON-NEGATIVE MAGNITUDES:
///   lo_ticks = distance in the LEFT direction from the axis origin
///   hi_ticks = distance in the RIGHT direction from the axis origin
///
/// To get number-line positions apply chirality:
///
///   Chirality.Positive:
///     Left endpoint  = −lo_ticks / TicksPerUnit  (negative on number line)
///     Right endpoint = +hi_ticks / TicksPerUnit  (positive on number line)
///     Display: "{lo/tpu}i + {hi/tpu}"
///     Example: Focal(500, 600) @ tpu=100 → [−5, +6], "5i + 6"
///
///   Chirality.Negative  (axis "viewed upside-down"):
///     Left endpoint  = +lo_ticks / TicksPerUnit  (positive on number line)
///     Right endpoint = −hi_ticks / TicksPerUnit  (negative on number line)
///     Display: "(−{lo/tpu}i − {hi/tpu})"
///     Example: Focal(500, 600) @ tpu=100 → [+5, −6], "(−5i − 6)"
///
/// ── CONCEPTUAL 2×2 MATRIX ───────────────────────────────────────────────────
///
///   For a value V in this axis (V is a Focal), the layout is:
///
///     ┌──────────────────┬──────────────────┐
///     │ Unit.Lo  (i-unit)│ V.Hi   (r-value) │   ← top row
///     ├──────────────────┼──────────────────┤
///     │ V.Lo   (i-value) │ Unit.Hi (r-unit) │   ← bottom row
///     └──────────────────┴──────────────────┘
///
///   For Axis.Signed:  Unit.Lo = 0 (Fixed)  — the i-unit slot is empty.
///   For complex axes: Unit.Lo = tpu         — both unit slots are filled.
///
/// ── OVERFLOW / TENSION ──────────────────────────────────────────────────────
///
/// A SpaceValue is "in bounds" when all its component magnitudes lie within
/// the axis range.  Out-of-range magnitudes represent tension (not an error).
///
///   LoInRange(ticks): valid when 0 ≤ ticks ≤ MaxLoTick
///   HiInRange(ticks): valid when 0 ≤ ticks ≤ MaxHiTick
///
/// Negative ticks (from arithmetic results) are always "in tension."
/// </summary>
public sealed class Axis
{
    /// <summary>
    /// Maximum extents of this axis, stored as positive magnitudes.
    /// Range.Lo = max ticks in the LEFT  (i) direction.
    /// Range.Hi = max ticks in the RIGHT (r) direction.
    /// </summary>
    public Focal Range { get; }

    /// <summary>
    /// Resolution: ticks per unit.
    /// For Axis.Signed and Axis.Positive:
    ///   Unit.Lo = 0 (Fixed)  — no imaginary unit; real axis only.
    ///   Unit.Hi = ticksPerUnit.
    /// For complex axes: Unit.Lo = Unit.Hi = ticksPerUnit (symmetric).
    /// </summary>
    public Focal Unit { get; }

    /// <summary>Which perspective this axis is observed from.</summary>
    public Chirality Chirality { get; }

    /// <summary>Optional human-readable label.</summary>
    public string Name { get; }

    public Axis(Focal range, Focal unit,
                Chirality chirality = Chirality.Positive,
                string name = "")
    {
        Range     = range;
        Unit      = unit;
        Chirality = chirality;
        Name      = name;
    }

    // ── Derived properties ────────────────────────────────────────────────────

    /// <summary>Ticks per unit (always positive; uses |Unit.Hi|).</summary>
    public long TicksPerUnit => Math.Abs(Unit.Hi);

    /// <summary>
    /// Maximum tick MAGNITUDE in the Lo (left / imaginary) direction.
    /// Equals Range.Lo.  For Axis.Positive this is 0 (no left extent).
    /// </summary>
    public long MaxLoTick => Range.Lo;

    /// <summary>
    /// Maximum tick MAGNITUDE in the Hi (right / real) direction.
    /// Equals Range.Hi.
    /// </summary>
    public long MaxHiTick => Range.Hi;

    /// <summary>
    /// Convenience alias for MaxHiTick.
    /// Use MaxLoTick / MaxHiTick for explicit direction-aware range checks.
    /// </summary>
    public long MaxTick => Range.Hi;

    /// <summary>+1 for Positive chirality, −1 for Negative.</summary>
    public int ChiralitySign => (int)Chirality;

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Convert a tick count to a double unit value.
    /// Works for both Lo and Hi components: divide by TicksPerUnit (= Unit.Hi).
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
    /// True when <paramref name="ticks"/> is a valid Lo (i / left) magnitude:
    /// must be in [0, MaxLoTick].
    /// Negative ticks are always out-of-range (they represent tension from arithmetic).
    /// </summary>
    public bool LoInRange(long ticks) => ticks >= 0 && ticks <= MaxLoTick;

    /// <summary>
    /// True when <paramref name="ticks"/> is a valid Hi (r / right) magnitude:
    /// must be in [0, MaxHiTick].
    /// Negative ticks are always out-of-range (they represent tension from arithmetic).
    /// </summary>
    public bool HiInRange(long ticks) => ticks >= 0 && ticks <= MaxHiTick;

    // ── Chirality operations ──────────────────────────────────────────────────

    /// <summary>Return a copy of this axis with the given chirality.</summary>
    public Axis WithChirality(Chirality chirality) =>
        new Axis(Range, Unit, chirality, Name);

    /// <summary>Return a copy with the opposite chirality.</summary>
    public Axis FlipChirality() =>
        WithChirality(Chirality == Chirality.Positive ? Chirality.Negative : Chirality.Positive);

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Create a standard symmetric real-number axis centred at zero.
    ///
    /// The axis spans [−rangeMax, +rangeMax] units, expressed as positive
    /// magnitudes in both directions.
    ///
    /// Unit = ZeroImag(ticksPerUnit):
    ///   Lo = 0 (Fixed) — no imaginary unit; prevents imaginary accumulation
    ///   Hi = ticksPerUnit — one real unit = ticksPerUnit ticks
    ///
    /// Range = Focal(maxTick, maxTick) — equal maximum magnitude in both directions.
    /// </summary>
    /// <param name="rangeMax">
    ///   Maximum extent in units.  Axis spans ±rangeMax (symmetric).
    ///   Values with Lo or Hi magnitudes exceeding rangeMax × ticksPerUnit
    ///   will be "in tension" (overflow).
    /// </param>
    /// <param name="ticksPerUnit">Resolution: ticks per unit.</param>
    /// <param name="chirality">Observation perspective (default Positive).</param>
    /// <param name="name">Optional label.</param>
    public static Axis Signed(double rangeMax, long ticksPerUnit,
        Chirality chirality = Chirality.Positive, string name = "")
    {
        long maxTick = (long)Math.Round(rangeMax * ticksPerUnit);
        var  range   = new Focal(maxTick, maxTick);   // both positive magnitudes
        var  unit    = Focal.ZeroImag(ticksPerUnit);   // lo=0 Fixed; real axis only
        return new Axis(range, unit, chirality, name);
    }

    /// <summary>
    /// Create a positive-only axis spanning [0, rangeMax] units.
    ///
    /// The axis has no left (imaginary) extent:
    ///   Range = ZeroImag(maxTick) → Lo = 0 (Fixed), Hi = maxTick
    ///   Unit  = ZeroImag(ticksPerUnit) → Lo = 0 (Fixed), Hi = ticksPerUnit
    ///
    /// Values with any non-zero Lo component are immediately "in tension."
    /// </summary>
    /// <param name="rangeMax">Maximum extent in units (all in positive / Hi direction).</param>
    /// <param name="ticksPerUnit">Resolution.</param>
    /// <param name="chirality">Observation perspective.</param>
    /// <param name="name">Optional label.</param>
    public static Axis Positive(double rangeMax, long ticksPerUnit,
        Chirality chirality = Chirality.Positive, string name = "")
    {
        long maxTick = (long)Math.Round(rangeMax * ticksPerUnit);
        var  range   = Focal.ZeroImag(maxTick);        // lo=0 (Fixed), hi=maxTick
        var  unit    = Focal.ZeroImag(ticksPerUnit);   // lo=0 (Fixed), hi=tpu
        return new Axis(range, unit, chirality, name);
    }

    public override string ToString() =>
        $"Axis({(Name.Length > 0 ? Name + " " : "")}range={Range} unit={Unit} {Chirality})";
}
