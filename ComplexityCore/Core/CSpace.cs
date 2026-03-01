namespace Complexity.Core;

/// <summary>
/// A number frame: a configuration of one or more axes that defines a type of number.
/// The frame is the container for types of numbers — once you have a frame, you can
/// fill it with as many values as you want.
///
/// Different axis configurations create different number types:
///   - Single forward axis        → positive numbers (unsigned integers)
///   - Single signed axis         → signed integers (full positive and negative range)
///   - Two axes (forward+backward)→ segments/complex numbers ("football field" math)
///   - Two orthogonal axes        → XY grid / 2D coordinates
///   - Four axes                  → quaternions
///
/// The algebra table defines how multiplication works in this frame.
/// Addition is always component-wise (structural). Different tables give different math.
///
/// Three space-level semantic properties live here alongside the algebra:
///   AffineType — Position (absolute location) vs Vector (relative displacement)
///   Chirality  — +1 right-handed (standard math) or -1 left-handed (screen/game)
///   Metric     — Euclidean, Manhattan, or Chebyshev distance function
///
/// These do not change tick-level arithmetic. They constrain valid operations
/// (AffineType), document handedness convention (Chirality), and select the
/// distance function used by Distance() and collection operations (Metric).
///
/// All values in this frame are stored as longs (ticks).
/// Resolution comes from TicksPerUnit on each axis.
/// Values are longs until evaluated — evaluation converts ticks to doubles
/// by dividing by TicksPerUnit.
/// </summary>
public class CSpace
{
    public string Name { get; }
    public Axis[] Axes { get; }
    public AlgebraEntry[] Algebra { get; }

    /// <summary>
    /// Position = absolute location (zero means "the origin").
    /// Vector = relative displacement (zero means "no movement").
    /// Use ResultAffineKind() to check operation validity before adding/subtracting.
    /// Default: Vector (most frames work with displacements).
    /// </summary>
    public AffineKind AffineType { get; }

    /// <summary>
    /// +1 = right-handed (counterclockwise positive, standard math convention).
    /// -1 = left-handed (clockwise positive, some screen / game conventions).
    /// Chirality flips when a space is mirrored — use Mirror() to track this correctly.
    /// Default: +1.
    /// </summary>
    public int Chirality { get; }

    /// <summary>
    /// Distance function used by CValue.Distance() and CCollection operations.
    /// Euclidean (L²) is the default. Manhattan (L¹) for grid-aligned navigation.
    /// Chebyshev (L∞) when the worst single-axis error dominates.
    /// Cyclic axes always use shortest-arc difference regardless of metric.
    /// Default: Euclidean.
    /// </summary>
    public MetricKind Metric { get; }

    /// <summary>
    /// Values living in this frame. A frame can accommodate many values.
    /// "Once we have one of these frames we can fill it with as many numbers as we want."
    /// </summary>
    public List<CValue> Values { get; } = new();

    public int Dims => Axes.Length;

    /// <summary>
    /// TicksPerUnit for the first axis — convenience for uniform-TPU spaces
    /// (all factory-created spaces use the same TPU on every axis).
    /// </summary>
    public long BasisTicks => Axes[0].TicksPerUnit;

    public CSpace(string name, Axis[] axes, AlgebraEntry[] algebra,
        AffineKind affineType = AffineKind.Vector,
        int chirality = +1,
        MetricKind metric = MetricKind.Euclidean)
    {
        Name = name;
        Axes = (Axis[])axes.Clone();
        Algebra = (AlgebraEntry[])algebra.Clone();
        AffineType = affineType;
        Chirality = chirality;
        Metric = metric;
    }

    /// <summary>
    /// Degrees of freedom with constraints applied.
    /// </summary>
    public int DegreesOfFreedom(HashSet<int>? lockedAxes = null) =>
        Dims - (lockedAxes?.Count ?? 0);

    // ================================================================
    //  AFFINE OPERATION RULES
    // ================================================================

    /// <summary>
    /// Determine the affine kind of the result of adding or subtracting two values.
    /// Returns null when the operation is geometrically invalid.
    ///
    /// Addition rules:
    ///   Vector   + Vector   → Vector    (displacements add to a displacement)
    ///   Position + Vector   → Position  (location + offset = new location)
    ///   Vector   + Position → Position  (commutative)
    ///   Position + Position → null      (invalid: sum of two absolute locations)
    ///
    /// Subtraction rules:
    ///   Vector   - Vector   → Vector    (difference of two displacements)
    ///   Position - Vector   → Position  (location minus offset = new location)
    ///   Position - Position → Vector    (difference of two locations = displacement)
    ///   Vector   - Position → null      (invalid)
    /// </summary>
    public static AffineKind? ResultAffineKind(AffineKind left, AffineKind right, bool isSubtraction)
    {
        if (!isSubtraction)
        {
            return (left, right) switch
            {
                (AffineKind.Vector,   AffineKind.Vector)   => AffineKind.Vector,
                (AffineKind.Position, AffineKind.Vector)   => AffineKind.Position,
                (AffineKind.Vector,   AffineKind.Position) => AffineKind.Position,
                (AffineKind.Position, AffineKind.Position) => null,
                _ => null
            };
        }
        else
        {
            return (left, right) switch
            {
                (AffineKind.Vector,   AffineKind.Vector)   => AffineKind.Vector,
                (AffineKind.Position, AffineKind.Vector)   => AffineKind.Position,
                (AffineKind.Position, AffineKind.Position) => AffineKind.Vector,
                (AffineKind.Vector,   AffineKind.Position) => null,
                _ => null
            };
        }
    }

    // ================================================================
    //  CHIRALITY HELPERS
    // ================================================================

    /// <summary>
    /// Derive chirality from the algebra table.
    /// Looks at the first cross-product entry (Left != Right) and reads its sign.
    /// +1 = right-handed (standard math convention).
    /// -1 = left-handed.
    /// Returns +1 for 1D spaces (chirality is not meaningful for 1D).
    /// </summary>
    public static int DeriveChirality(AlgebraEntry[] algebra, int dims)
    {
        if (dims < 2) return +1;
        foreach (var entry in algebra)
        {
            if (entry.Left != entry.Right && entry.Sign != 0)
                return entry.Sign > 0 ? +1 : -1;
        }
        return +1;
    }

    /// <summary>
    /// Create a new CSpace that is this space mirrored across the given axis.
    /// Negates the specified axis direction and flips Chirality.
    /// Adjusts algebra entry signs to stay consistent with the mirrored axis.
    ///
    /// Use this when reflecting geometry (e.g., converting between screen Y-down
    /// and math Y-up coordinates) so chirality tracking remains correct.
    /// </summary>
    public CSpace Mirror(int axisIndex)
    {
        var newAxes = (Axis[])Axes.Clone();
        var ax = newAxes[axisIndex];
        newAxes[axisIndex] = new Axis(ax.Name, -ax.Direction,
            ax.MinBoundTicks, ax.MaxBoundTicks,
            ax.TicksPerUnit, ax.IsCyclic, ax.CyclePeriodTicks, ax.Kind);

        // When axis[i] is mirrored, any algebra entry where that axis appears as
        // Left, Right, or Target gets a sign contribution from the mirror.
        // Net sign change = (-1)^(number of times the mirrored axis appears).
        var newAlgebra = new AlgebraEntry[Algebra.Length];
        for (int i = 0; i < Algebra.Length; i++)
        {
            var e = Algebra[i];
            int flips = 0;
            if (e.Left   == axisIndex) flips++;
            if (e.Right  == axisIndex) flips++;
            if (e.Target == axisIndex) flips++;
            int signFlip = (flips % 2 == 0) ? +1 : -1;
            newAlgebra[i] = new AlgebraEntry(e.Left, e.Right, e.Target, e.Sign * signFlip);
        }

        return new CSpace(Name + "'", newAxes, newAlgebra,
            AffineType, -Chirality, Metric);
    }

    // ================================================================
    //  VALUE CREATION — create values that live in this frame
    //  Values are longs (ticks). They become doubles when evaluated.
    // ================================================================

    /// <summary>
    /// Create a zero value (all components 0 ticks) in this frame.
    /// </summary>
    public CValue Zero() => new CValue(this, new long[Dims]);

    /// <summary>
    /// Create the multiplicative identity (TicksPerUnit on first direction=+1 axis, 0 elsewhere).
    /// One unit = TicksPerUnit ticks.
    /// </summary>
    public CValue One()
    {
        var comps = new long[Dims];
        for (int i = 0; i < Dims; i++)
        {
            if (Axes[i].Direction == 1)
            {
                comps[i] = Axes[i].TicksPerUnit;
                break;
            }
        }
        return new CValue(this, comps);
    }

    /// <summary>
    /// Create a value with specific value-space components (doubles).
    /// Each component is converted to ticks: (long)Math.Round(value * TicksPerUnit).
    /// The value is NOT automatically added to the Values collection.
    /// Use CreateValue() to both create and register.
    /// </summary>
    public CValue Value(params double[] components)
    {
        if (components.Length != Dims)
            throw new ArgumentException($"Expected {Dims} components, got {components.Length}");
        return new CValue(this, components);
    }

    /// <summary>
    /// Create a value with specific tick components (longs) directly.
    /// The value is NOT automatically added to the Values collection.
    /// Use CreateValueFromTicks() to both create and register.
    /// </summary>
    public CValue ValueFromTicks(params long[] ticks)
    {
        if (ticks.Length != Dims)
            throw new ArgumentException($"Expected {Dims} tick components, got {ticks.Length}");
        return new CValue(this, ticks);
    }

    /// <summary>
    /// Create a value from value-space doubles AND register it in this frame's Values collection.
    /// "That frame and range needs to be able to accommodate many possible values inside it."
    /// </summary>
    public CValue CreateValue(params double[] components)
    {
        var val = Value(components);
        Values.Add(val);
        return val;
    }

    /// <summary>
    /// Create a value directly from ticks AND register it in this frame's Values collection.
    /// Values are longs until evaluated — this is the natural way to create tick-based values.
    /// </summary>
    public CValue CreateValueFromTicks(params long[] ticks)
    {
        var val = ValueFromTicks(ticks);
        Values.Add(val);
        return val;
    }

    // ================================================================
    //  FACTORY METHODS — axis-based frame construction
    //  Build frames from individual axes (the basic primitive).
    // ================================================================

    /// <summary>
    /// Create a 1D frame from a single axis.
    /// Algebra: value * value = value (scalar multiplication).
    /// This is the simplest frame: one axis, one direction.
    ///
    /// Example: FromAxis(Axis.Forward("value", 10, ticksPerUnit: 10))
    /// creates a frame for numbers 0..10 with tenths resolution.
    /// </summary>
    public static CSpace FromAxis(Axis axis)
    {
        return new CSpace(axis.Name,
            new[] { axis },
            new[] { new AlgebraEntry(0, 0, 0, +1) }
        );
    }

    /// <summary>
    /// Create a frame from multiple axes with explicit algebra.
    /// </summary>
    public static CSpace FromAxes(string name, Axis[] axes, AlgebraEntry[] algebra,
        AffineKind affineType = AffineKind.Vector,
        int chirality = +1,
        MetricKind metric = MetricKind.Euclidean)
    {
        return new CSpace(name, axes, algebra, affineType, chirality, metric);
    }

    /// <summary>
    /// Create a complex/segment frame from a real (unit) axis and an imaginary (unot) axis.
    /// "To create a complex number we need two of these [axes]."
    /// "To create football field style math we need 2 units one pointing left one pointing right."
    ///
    /// Produces complex multiplication: (a+bi)(c+di) = (ac-bd) + (ad+bc)i
    /// The imaginary axis is set to direction=-1 (backward/left-facing).
    /// The real axis is set to direction=+1 (forward/right-facing).
    /// Chirality is derived from the algebra (right-handed = +1 for standard complex).
    /// </summary>
    public static CSpace Complex(Axis realAxis, Axis imagAxis)
    {
        // Ensure correct directions for complex algebra; preserve Kind from input
        var imag = new Axis(imagAxis.Name, -1, imagAxis.MinBoundTicks, imagAxis.MaxBoundTicks,
            imagAxis.TicksPerUnit, imagAxis.IsCyclic, imagAxis.CyclePeriodTicks, imagAxis.Kind);
        var real = new Axis(realAxis.Name, +1, realAxis.MinBoundTicks, realAxis.MaxBoundTicks,
            realAxis.TicksPerUnit, realAxis.IsCyclic, realAxis.CyclePeriodTicks, realAxis.Kind);

        var algebra = new[]
        {
            new AlgebraEntry(1, 1, 1, +1), // real*real → +real   (ac)
            new AlgebraEntry(0, 0, 1, -1), // imag*imag → -real   (i²=-1)
            new AlgebraEntry(1, 0, 0, +1), // real*imag → +imag   (ad)
            new AlgebraEntry(0, 1, 0, +1), // imag*real → +imag   (bc)
        };

        return new CSpace("complex",
            new[] { imag, real }, // unot first, unit second (matches Segment convention)
            algebra,
            AffineKind.Vector,
            DeriveChirality(algebra, 2)
        );
    }

    /// <summary>
    /// Create an orthogonal 2D grid frame from two forward-pointing axes.
    /// "To make a 2 dimensional value with XY, we need two of these axes."
    ///
    /// No cross terms: axes are independent. Each axis multiplies only with itself.
    /// Both axes are set to direction=+1 (orthogonal, not opposing).
    /// Chirality defaults to +1 (standard math: X right, Y up).
    /// </summary>
    public static CSpace Grid(Axis xAxis, Axis yAxis)
    {
        var x = new Axis(xAxis.Name, +1, xAxis.MinBoundTicks, xAxis.MaxBoundTicks,
            xAxis.TicksPerUnit, xAxis.IsCyclic, xAxis.CyclePeriodTicks, xAxis.Kind);
        var y = new Axis(yAxis.Name, +1, yAxis.MinBoundTicks, yAxis.MaxBoundTicks,
            yAxis.TicksPerUnit, yAxis.IsCyclic, yAxis.CyclePeriodTicks, yAxis.Kind);

        return new CSpace("grid",
            new[] { x, y },
            new[]
            {
                new AlgebraEntry(0, 0, 0, +1), // x*x → x
                new AlgebraEntry(1, 1, 1, +1), // y*y → y
            },
            AffineKind.Vector,
            +1  // right-handed: X right, Y up
        );
    }

    // ================================================================
    //  NAMED FACTORY METHODS — common frame types
    // ================================================================

    /// <summary>
    /// 1 axis, bounded. Algebra: value * value = value.
    /// Bounds are in value space; converted to ticks internally.
    /// </summary>
    public static CSpace Integer(double min = -1e15, double max = 1e15, long ticksPerUnit = 1)
    {
        return new CSpace("integer",
            new[] { new Axis("value", +1, min, max, ticksPerUnit: ticksPerUnit) },
            new[] { new AlgebraEntry(0, 0, 0, +1) }
        );
    }

    /// <summary>
    /// 1 axis, cyclic. Clock arithmetic, remainders.
    /// Same algebra as integer, but wraps at modulus.
    /// </summary>
    public static CSpace Modular(int modulus, long ticksPerUnit = 1)
    {
        return new CSpace("mod_" + modulus,
            new[] { new Axis("value", +1, 0, modulus - 1, ticksPerUnit: ticksPerUnit,
                isCyclic: true, cyclePeriod: modulus) },
            new[] { new AlgebraEntry(0, 0, 0, +1) }
        );
    }

    /// <summary>
    /// 1 axis, bounded [0..1], variable resolution.
    /// "30% of the way along a path" — the simplest frame
    /// with actual information (two fixed points + one variable).
    /// </summary>
    public static CSpace Proportion(int divisions = 1000)
    {
        return new CSpace("proportion",
            new[] { new Axis("ratio", +1, 0.0, 1.0, ticksPerUnit: divisions) },
            new[] { new AlgebraEntry(0, 0, 0, +1) }
        );
    }

    /// <summary>
    /// 2 axes: unot (direction=-1) and unit (direction=+1).
    /// Algebra: complex multiplication.
    /// "Football field style math: 2 units one pointing left one pointing right
    ///  at the end points of a range."
    /// Chirality = +1 (right-handed, standard complex math convention).
    /// </summary>
    public static CSpace Segment(double min = -1e15, double max = 1e15, long ticksPerUnit = 1)
    {
        var algebra = new[]
        {
            new AlgebraEntry(1, 1, 1, +1), // unit*unit → +unit  (ac)
            new AlgebraEntry(0, 0, 1, -1), // unot*unot → -unit  (-bd, i²=-1)
            new AlgebraEntry(1, 0, 0, +1), // unit*unot → +unot  (ad)
            new AlgebraEntry(0, 1, 0, +1), // unot*unit → +unot  (bc)
        };
        return new CSpace("segment",
            new[]
            {
                new Axis("unot", -1, min, max, ticksPerUnit: ticksPerUnit),
                new Axis("unit", +1, min, max, ticksPerUnit: ticksPerUnit),
            },
            algebra,
            AffineKind.Vector,
            DeriveChirality(algebra, 2)
        );
    }

    /// <summary>
    /// Same as segment, but the unot (i) axis is cyclic at 2pi.
    /// For rotation/angle quantities where the imaginary axis wraps.
    /// </summary>
    public static CSpace RotationalSegment(long ticksPerUnit = 1000)
    {
        var algebra = new[]
        {
            new AlgebraEntry(1, 1, 1, +1),
            new AlgebraEntry(0, 0, 1, -1),
            new AlgebraEntry(1, 0, 0, +1),
            new AlgebraEntry(0, 1, 0, +1),
        };
        return new CSpace("rotational_segment",
            new[]
            {
                new Axis("unot", -1, double.NaN, double.NaN,
                    ticksPerUnit: ticksPerUnit, isCyclic: true, cyclePeriod: 2 * Math.PI),
                new Axis("unit", +1, double.NaN, double.NaN,
                    ticksPerUnit: ticksPerUnit),
            },
            algebra,
            AffineKind.Vector,
            DeriveChirality(algebra, 2)
        );
    }

    /// <summary>
    /// 2 axes, but j² = +1 instead of i² = -1.
    /// Different algebra → different math. Appears in special relativity (Minkowski).
    /// Chirality = +1 (cross terms are still positive).
    /// </summary>
    public static CSpace SplitComplex(long ticksPerUnit = 1000)
    {
        var algebra = new[]
        {
            new AlgebraEntry(1, 1, 1, +1), // 1*1 → +1
            new AlgebraEntry(0, 0, 1, +1), // j*j → +1  (NOT -1 like complex!)
            new AlgebraEntry(1, 0, 0, +1), // 1*j → +j
            new AlgebraEntry(0, 1, 0, +1), // j*1 → +j
        };
        return new CSpace("split-complex",
            new[]
            {
                new Axis("j", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("1", +1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
            },
            algebra,
            AffineKind.Vector,
            DeriveChirality(algebra, 2)
        );
    }

    /// <summary>
    /// 2 axes, epsilon² = 0 (nilpotent).
    /// multiply(a+b*eps, c+d*eps) = ac + (ad+bc)*eps. The eps² term vanishes.
    /// Useful for automatic differentiation.
    /// </summary>
    public static CSpace DualNumber(long ticksPerUnit = 1000)
    {
        return new CSpace("dual",
            new[]
            {
                new Axis("ε", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("1", +1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
            },
            new[]
            {
                new AlgebraEntry(1, 1, 1, +1), // 1*1 → +1
                new AlgebraEntry(0, 0, 1,  0), // ε*ε → 0  (nilpotent!)
                new AlgebraEntry(1, 0, 0, +1), // 1*ε → +ε
                new AlgebraEntry(0, 1, 0, +1), // ε*1 → +ε
            }
        );
    }

    /// <summary>
    /// 4 axes: (1, i, j, k). i²=j²=k²=ijk=-1.
    /// 16 algebra entries.
    /// Chirality = +1 (standard quaternion right-hand rule).
    /// </summary>
    public static CSpace Quaternion(long ticksPerUnit = 1000)
    {
        var algebra = new[]
        {
            // 1*?
            new AlgebraEntry(0, 0, 0, +1), new AlgebraEntry(0, 1, 1, +1),
            new AlgebraEntry(0, 2, 2, +1), new AlgebraEntry(0, 3, 3, +1),
            // i*?
            new AlgebraEntry(1, 0, 1, +1), new AlgebraEntry(1, 1, 0, -1),
            new AlgebraEntry(1, 2, 3, +1), new AlgebraEntry(1, 3, 2, -1),
            // j*?
            new AlgebraEntry(2, 0, 2, +1), new AlgebraEntry(2, 1, 3, -1),
            new AlgebraEntry(2, 2, 0, -1), new AlgebraEntry(2, 3, 1, +1),
            // k*?
            new AlgebraEntry(3, 0, 3, +1), new AlgebraEntry(3, 1, 2, +1),
            new AlgebraEntry(3, 2, 1, -1), new AlgebraEntry(3, 3, 0, -1),
        };
        return new CSpace("quaternion",
            new[]
            {
                new Axis("1", +1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("i", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("j", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("k", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
            },
            algebra,
            AffineKind.Vector,
            +1  // standard right-hand rule: i×j=k
        );
    }

    // ================================================================
    //  FRAME COMPOSITION — combine axes and frames
    //  "I should be able to combine this frame with another left facing unit
    //   to make a complex number, or an orthogonal unit to make an XY grid."
    // ================================================================

    /// <summary>
    /// Combine this frame with another to create a higher-dimensional frame.
    /// Optional cross algebra entries define how axes from the two original frames
    /// interact during multiplication.
    /// Without cross terms: the frames are independent (just grouped).
    /// With cross terms: they can multiply into each other (e.g., area from length*width).
    /// </summary>
    public CSpace CombineWith(CSpace other, AlgebraEntry[]? crossAlgebra = null)
    {
        return Merge(this, other, crossAlgebra);
    }

    /// <summary>
    /// Merge two frames into a higher-order frame.
    /// "If we merge the XY axis together we get a single dimensional area,
    ///  which is once again a single axis based on the size of the area."
    /// The merged space inherits AffineType from 'a', Metric from 'a', Chirality = a.Chirality * b.Chirality.
    /// </summary>
    public static CSpace Merge(CSpace a, CSpace b, AlgebraEntry[]? crossAlgebra = null)
    {
        var mergedAxes = new Axis[a.Dims + b.Dims];
        Array.Copy(a.Axes, 0, mergedAxes, 0, a.Dims);
        Array.Copy(b.Axes, 0, mergedAxes, a.Dims, b.Dims);

        var mergedAlg = new List<AlgebraEntry>();

        // a's internal algebra (indices unchanged)
        foreach (var entry in a.Algebra)
            mergedAlg.Add(entry);

        // b's internal algebra (indices shifted by a.Dims)
        foreach (var entry in b.Algebra)
            mergedAlg.Add(new AlgebraEntry(
                entry.Left + a.Dims,
                entry.Right + a.Dims,
                entry.Target + a.Dims,
                entry.Sign));

        // Cross-term algebra (caller specifies how the frames interact)
        if (crossAlgebra != null)
            mergedAlg.AddRange(crossAlgebra);

        // Combined chirality: product of the two chiralities
        int combinedChirality = a.Chirality * b.Chirality;

        return new CSpace(a.Name + "×" + b.Name, mergedAxes, mergedAlg.ToArray(),
            a.AffineType, combinedChirality, a.Metric);
    }

    /// <summary>
    /// Project a value from this (higher) frame into a lower-dimensional target frame.
    /// axisMap[i] = which axis in this frame maps to axis i in the target.
    /// </summary>
    public static CValue Project(CValue val, CSpace target, int[] axisMap)
    {
        var comps = new long[target.Dims];
        for (int i = 0; i < target.Dims; i++)
            comps[i] = val.Components[axisMap[i]];
        return new CValue(target, comps, val.Certainty);
    }

    /// <summary>
    /// Embed a value from a lower frame into this (higher) frame.
    /// axisMap[i] = which axis in the target this value's axis i maps to.
    /// </summary>
    public static CValue Embed(CValue val, CSpace target, int[] axisMap, long[]? defaults = null)
    {
        var comps = defaults != null ? (long[])defaults.Clone() : new long[target.Dims];
        for (int i = 0; i < val.Space.Dims; i++)
            comps[axisMap[i]] = val.Components[i];
        return new CValue(target, comps, val.Certainty);
    }

    public override string ToString() =>
        $"{Name}[{Dims}D, {Algebra.Length} rules, tpu={BasisTicks}, {AffineType}, chirality={Chirality:+#;-#}, {Metric}]";
}
