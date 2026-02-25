namespace Complexity.Core;

/// <summary>
/// A complexity space defined by axes + algebra table.
/// The algebra table IS the space — different tables give different math.
/// Addition is always component-wise (structural). Multiplication comes from the table.
/// Boolean operations work on ranges (structural).
///
/// Generalizes Domain: where Domain provides a single BasisFocal + MinMaxFocal,
/// CSpace provides N axes each with their own bounds and resolution.
/// Two CValues in the same CSpace instance share the same algebra.
///
/// All values in this space are stored as longs (ticks).
/// The TicksPerUnit on each axis determines resolution.
/// </summary>
public class CSpace
{
    public string Name { get; }
    public Axis[] Axes { get; }
    public AlgebraEntry[] Algebra { get; }

    public int Dims => Axes.Length;

    /// <summary>
    /// TicksPerUnit for the first axis — convenience for uniform-TPU spaces
    /// (all factory-created spaces use the same TPU on every axis).
    /// </summary>
    public long BasisTicks => Axes[0].TicksPerUnit;

    public CSpace(string name, Axis[] axes, AlgebraEntry[] algebra)
    {
        Name = name;
        Axes = (Axis[])axes.Clone();
        Algebra = (AlgebraEntry[])algebra.Clone();
    }

    /// <summary>
    /// Degrees of freedom with constraints applied.
    /// A line on a line = 2 DOF; lock the length = 1 DOF.
    /// </summary>
    public int DegreesOfFreedom(HashSet<int>? lockedAxes = null) =>
        Dims - (lockedAxes?.Count ?? 0);

    /// <summary>
    /// Create a zero value (all components 0 ticks) in this space.
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
    /// </summary>
    public CValue Value(params double[] components)
    {
        if (components.Length != Dims)
            throw new ArgumentException($"Expected {Dims} components, got {components.Length}");
        return new CValue(this, components);
    }

    /// <summary>
    /// Create a value with specific tick components (longs) directly.
    /// </summary>
    public CValue ValueFromTicks(params long[] ticks)
    {
        if (ticks.Length != Dims)
            throw new ArgumentException($"Expected {Dims} tick components, got {ticks.Length}");
        return new CValue(this, ticks);
    }

    // ================================================================
    //  FACTORY METHODS — Named space constructors
    // ================================================================

    /// <summary>
    /// 1 axis, bounded.
    /// Algebra: value × value = value. (a * b = ab)
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
    /// "30% of the way along a path" — the simplest complexity space
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
    /// 2 axes: unot (direction=-1, the "i" axis) and unit (direction=+1, the "1" axis).
    /// Algebra: complex multiplication.
    ///
    /// This IS the existing PRange system. The algebra table produces exactly the same
    /// multiplication formula: (ac-bd, ad+bc).
    ///
    /// PRange.operator* line 154:
    ///   new PRange(left.Start*right.End + left.End*right.Start,
    ///              left.End*right.End   - left.Start*right.Start)
    ///
    /// Which is this table applied by the multiply loop.
    /// </summary>
    public static CSpace Segment(double min = -1e15, double max = 1e15, long ticksPerUnit = 1)
    {
        return new CSpace("segment",
            new[]
            {
                new Axis("unot", -1, min, max, ticksPerUnit: ticksPerUnit),
                new Axis("unit", +1, min, max, ticksPerUnit: ticksPerUnit),
            },
            new[]
            {
                new AlgebraEntry(1, 1, 1, +1), // unit×unit → +unit  (ac)
                new AlgebraEntry(0, 0, 1, -1), // unot×unot → -unit  (-bd, i²=-1)
                new AlgebraEntry(1, 0, 0, +1), // unit×unot → +unot  (ad)
                new AlgebraEntry(0, 1, 0, +1), // unot×unit → +unot  (bc)
            }
        );
    }

    /// <summary>
    /// Same as segment, but the unot (i) axis is cyclic at 2π.
    /// For rotation/angle quantities where the imaginary axis wraps.
    /// </summary>
    public static CSpace RotationalSegment(long ticksPerUnit = 1000)
    {
        return new CSpace("rotational_segment",
            new[]
            {
                new Axis("unot", -1, double.NaN, double.NaN,
                    ticksPerUnit: ticksPerUnit, isCyclic: true, cyclePeriod: 2 * Math.PI),
                new Axis("unit", +1, double.NaN, double.NaN,
                    ticksPerUnit: ticksPerUnit),
            },
            new[]
            {
                new AlgebraEntry(1, 1, 1, +1),
                new AlgebraEntry(0, 0, 1, -1),
                new AlgebraEntry(1, 0, 0, +1),
                new AlgebraEntry(0, 1, 0, +1),
            }
        );
    }

    /// <summary>
    /// 2 axes, but j² = +1 instead of i² = -1.
    /// Different algebra → different math → different space.
    /// Appears in special relativity (Minkowski metric).
    /// </summary>
    public static CSpace SplitComplex(long ticksPerUnit = 1000)
    {
        return new CSpace("split-complex",
            new[]
            {
                new Axis("j", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("1", +1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
            },
            new[]
            {
                new AlgebraEntry(1, 1, 1, +1), // 1×1 → +1
                new AlgebraEntry(0, 0, 1, +1), // j×j → +1  (NOT -1 like complex!)
                new AlgebraEntry(1, 0, 0, +1), // 1×j → +j
                new AlgebraEntry(0, 1, 0, +1), // j×1 → +j
            }
        );
    }

    /// <summary>
    /// 2 axes, ε² = 0 (nilpotent).
    /// multiply(a+bε, c+dε) = ac + (ad+bc)ε. The ε² term vanishes.
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
                new AlgebraEntry(1, 1, 1, +1), // 1×1 → +1
                new AlgebraEntry(0, 0, 1,  0), // ε×ε → 0  (nilpotent!)
                new AlgebraEntry(1, 0, 0, +1), // 1×ε → +ε
                new AlgebraEntry(0, 1, 0, +1), // ε×1 → +ε
            }
        );
    }

    /// <summary>
    /// 4 axes: (1, i, j, k). i²=j²=k²=ijk=-1.
    /// 16 algebra entries. Same multiply() function, just more entries.
    /// </summary>
    public static CSpace Quaternion(long ticksPerUnit = 1000)
    {
        return new CSpace("quaternion",
            new[]
            {
                new Axis("1", +1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("i", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("j", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
                new Axis("k", -1, double.NaN, double.NaN, ticksPerUnit: ticksPerUnit),
            },
            new[]
            {
                // 1×?
                new AlgebraEntry(0, 0, 0, +1), new AlgebraEntry(0, 1, 1, +1),
                new AlgebraEntry(0, 2, 2, +1), new AlgebraEntry(0, 3, 3, +1),
                // i×?
                new AlgebraEntry(1, 0, 1, +1), new AlgebraEntry(1, 1, 0, -1),
                new AlgebraEntry(1, 2, 3, +1), new AlgebraEntry(1, 3, 2, -1),
                // j×?
                new AlgebraEntry(2, 0, 2, +1), new AlgebraEntry(2, 1, 3, -1),
                new AlgebraEntry(2, 2, 0, -1), new AlgebraEntry(2, 3, 1, +1),
                // k×?
                new AlgebraEntry(3, 0, 3, +1), new AlgebraEntry(3, 1, 2, +1),
                new AlgebraEntry(3, 2, 1, -1), new AlgebraEntry(3, 3, 0, -1),
            }
        );
    }

    // ================================================================
    //  CROSS-SPACE OPERATIONS
    // ================================================================

    /// <summary>
    /// Merge two spaces into a higher-order space.
    /// The cross algebra entries define how axes from the two original spaces
    /// interact during multiplication.
    ///
    /// Without cross terms: the spaces are independent (just grouped).
    /// With cross terms: they can multiply into each other.
    ///
    /// This is how you go from 1D lengths to 2D area, etc.
    /// Generalizes what PolyDomain does with multiple Domains.
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

        // Cross-term algebra (caller specifies how the spaces interact)
        if (crossAlgebra != null)
            mergedAlg.AddRange(crossAlgebra);

        return new CSpace(a.Name + "×" + b.Name, mergedAxes, mergedAlg.ToArray());
    }

    /// <summary>
    /// Project a value from this (higher) space into a lower-dimensional target space.
    /// axisMap[i] = which axis in this space maps to axis i in the target.
    /// </summary>
    public static CValue Project(CValue val, CSpace target, int[] axisMap)
    {
        var comps = new long[target.Dims];
        for (int i = 0; i < target.Dims; i++)
            comps[i] = val.Components[axisMap[i]];
        return new CValue(target, comps, val.Certainty);
    }

    /// <summary>
    /// Embed a value from a lower space into this (higher) space.
    /// axisMap[i] = which axis in the target this value's axis i maps to.
    /// defaults provides the tick values for unmapped axes.
    /// </summary>
    public static CValue Embed(CValue val, CSpace target, int[] axisMap, long[]? defaults = null)
    {
        var comps = defaults != null ? (long[])defaults.Clone() : new long[target.Dims];
        for (int i = 0; i < val.Space.Dims; i++)
            comps[axisMap[i]] = val.Components[i];
        return new CValue(target, comps, val.Certainty);
    }

    public override string ToString() => $"{Name}[{Dims}D, {Algebra.Length} rules, tpu={BasisTicks}]";
}
