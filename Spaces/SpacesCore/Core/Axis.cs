namespace Spaces.Core;

/// <summary>
/// A single named dimension with defined resolution and extent.
///
/// ── STRUCTURE ───────────────────────────────────────────────────────────────
///
///   Range    — a Focal giving the minimum (Range.Lo) and maximum (Range.Hi)
///              allowed tick values.  Values outside this range are permitted
///              but enter an "overflow / tension" state.
///
///   Unit     — a Focal giving the resolution.  Unit.Hi is the number of ticks
///              that equal one positive unit.  Unit.Lo (normally locked to
///              −Unit.Hi via NegateOther) is the i-direction unit.
///              Example: Unit.Hi = 100  →  1 unit = 100 ticks  →  0.01 is the
///              smallest representable step.
///
///   Chirality — the observation perspective (Positive or Negative).
///
/// ── CONCEPTUAL 2×2 MATRIX ───────────────────────────────────────────────────
///
///   For a value V in this axis (V is a Focal), the layout is:
///
///     ┌──────────────────┬──────────────────┐
///     │ Unit.Lo  (i-unit)│ V.Hi   (r-value) │   ← top row: units + r-value
///     ├──────────────────┼──────────────────┤
///     │ V.Lo   (i-value) │ Unit.Hi (r-unit) │   ← bottom row: i-value + units
///     └──────────────────┴──────────────────┘
///
///   Reading the right column top-to-bottom gives a natural fraction
///   (numerator / denominator) for rational representations.
///   Rotating the matrix 90° is equivalent to a chirality change.
///
/// ── ENCODING EXAMPLE ────────────────────────────────────────────────────────
///
///   Axis.Signed(rangeMax: 10.0, ticksPerUnit: 100)
///     Range = Focal(−1000, 1000)        — axis spans [−10, +10] units
///     Unit  = Focal.SymmetricUnit(100)  — 100 ticks per unit; Lo locked to −Hi
///     Chirality = Positive
///
///   Value representing segment [−2, +4]:
///     Focal(lo=−200, hi=400) in ticks  — expressed as "2i + 4" in units
///
/// ── CHIRALITY ───────────────────────────────────────────────────────────────
///
///   Positive (default): r-direction is the "forward" direction.
///   Negative: the axis is viewed from the opposite end; all encoded values
///   have their signs flipped relative to the positive-chirality interpretation.
///   The stored Focal data does NOT automatically change — chirality is a label
///   that consumers use to interpret or convert the data.
/// </summary>
public sealed class Axis
{
    /// <summary>
    /// Extent of this axis in ticks.  Range.Lo = minimum tick (may be negative);
    /// Range.Hi = maximum tick.
    /// </summary>
    public Focal Range { get; }

    /// <summary>
    /// Resolution: ticks per unit.  Unit.Hi = r-direction unit magnitude;
    /// Unit.Lo = i-direction unit (typically NegateOther of Unit.Hi → always symmetric).
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

    /// <summary>Minimum allowed tick value (from Range.Lo).</summary>
    public long MinTick => Range.Lo;

    /// <summary>Maximum allowed tick value (from Range.Hi).</summary>
    public long MaxTick => Range.Hi;

    /// <summary>+1 for Positive chirality, −1 for Negative.</summary>
    public int ChiralitySign => (int)Chirality;

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>Convert a tick count to a double unit value.</summary>
    public double ToUnits(long ticks) =>
        TicksPerUnit == 0 ? 0.0 : (double)ticks / TicksPerUnit;

    /// <summary>Convert a double unit value to the nearest tick (round-half-to-even).</summary>
    public long ToTicks(double units) =>
        (long)Math.Round(units * TicksPerUnit, MidpointRounding.ToEven);

    /// <summary>True when <paramref name="ticks"/> is within [MinTick, MaxTick].</summary>
    public bool InRange(long ticks) => ticks >= MinTick && ticks <= MaxTick;

    // ── Chirality operations ──────────────────────────────────────────────────

    /// <summary>Return a copy of this axis with the given chirality.</summary>
    public Axis WithChirality(Chirality chirality) =>
        new Axis(Range, Unit, chirality, Name);

    /// <summary>Return a copy with the opposite chirality.</summary>
    public Axis FlipChirality() =>
        WithChirality(Chirality == Chirality.Positive ? Chirality.Negative : Chirality.Positive);

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Create a standard symmetric axis centred at zero.
    /// </summary>
    /// <param name="rangeMax">
    ///   The maximum extent in units.  The axis spans [−rangeMax, +rangeMax].
    ///   Stored as ±(rangeMax × ticksPerUnit) ticks.
    /// </param>
    /// <param name="ticksPerUnit">Resolution: ticks per unit.</param>
    /// <param name="chirality">Observation perspective (default Positive).</param>
    /// <param name="name">Optional label.</param>
    public static Axis Signed(double rangeMax, long ticksPerUnit,
        Chirality chirality = Chirality.Positive, string name = "")
    {
        long maxTick = (long)Math.Round(rangeMax * ticksPerUnit);
        var  range   = new Focal(-maxTick, maxTick);
        var  unit    = Focal.SymmetricUnit(ticksPerUnit);
        return new Axis(range, unit, chirality, name);
    }

    /// <summary>
    /// Create a positive-only axis spanning [0, rangeMax].
    /// The Range.Lo is Fixed at zero (no i-component in range).
    /// </summary>
    /// <param name="rangeMax">Maximum extent in units.</param>
    /// <param name="ticksPerUnit">Resolution.</param>
    /// <param name="chirality">Observation perspective.</param>
    /// <param name="name">Optional label.</param>
    public static Axis Positive(double rangeMax, long ticksPerUnit,
        Chirality chirality = Chirality.Positive, string name = "")
    {
        long maxTick = (long)Math.Round(rangeMax * ticksPerUnit);
        var  range   = Focal.ZeroImag(maxTick);   // lo=0 (Fixed), hi=maxTick (free)
        var  unit    = Focal.SymmetricUnit(ticksPerUnit);
        return new Axis(range, unit, chirality, name);
    }

    public override string ToString() =>
        $"Axis({(Name.Length > 0 ? Name + " " : "")}range={Range} unit={Unit} {Chirality})";
}
