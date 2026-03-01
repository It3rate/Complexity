namespace Complexity.Core;

/// <summary>
/// A value living in a number frame (CSpace). All values are longs (ticks) until evaluated.
/// When evaluated, they become doubles by dividing by TicksPerUnit.
///
/// Components[i] is the tick value along axis i.
/// There is no floating point resolution in this system — resolution is always tick-based.
///
/// For a 2D segment space, Components[0]=unot (Start/imaginary) and
/// Components[1]=unit (End/real), matching PRange when evaluated to doubles.
///
/// All operators create new CValues — the Components array is never mutated
/// through operator results. Operators work entirely in long arithmetic.
/// Doubles are only derived on demand via Evaluate() / GetValue() / GetValues().
///
/// Storage: always longs (ticks).
/// Evaluation: always doubles (ticks / TicksPerUnit).
///
/// CValue is a class (reference type) so that mutable properties like Certainty
/// work naturally when values are stored in collections (List, array, etc.).
/// Equality is still value-based: two CValues with the same Space and Components
/// are equal regardless of whether they are the same object instance.
/// </summary>
public class CValue : IEquatable<CValue>
{
    /// <summary> Tolerance in ticks. 1 tick is the minimum distinguishable difference. </summary>
    public static readonly long Tolerance = 1;

    /// <summary> Tolerance for double comparisons (derived/evaluated values). </summary>
    public static readonly double DoubleTolerance = 0.000000001;

    public CSpace Space { get; }

    /// <summary> Tick values along each axis. This IS the value — longs are the source of truth. </summary>
    public long[] Components { get; }

    public double Certainty { get; set; }

    /// <summary>
    /// Create a CValue from tick components (primary constructor).
    /// Values are longs until evaluated.
    /// </summary>
    public CValue(CSpace space, long[] components, double certainty = 1.0)
    {
        Space = space;
        Components = (long[])components.Clone(); // defensive copy
        Certainty = certainty;
    }

    /// <summary>
    /// Create a CValue from value-space doubles (convenience constructor).
    /// Each component is converted to ticks: (long)Math.Round(value * TicksPerUnit).
    /// Once stored, the value is longs until evaluated again.
    /// </summary>
    public CValue(CSpace space, double[] values, double certainty = 1.0)
    {
        Space = space;
        Components = new long[space.Dims];
        for (int i = 0; i < space.Dims; i++)
            Components[i] = (long)Math.Round(values[i] * space.Axes[i].TicksPerUnit);
        Certainty = certainty;
    }

    public bool IsValid => Space != null && Components != null;
    public bool IsZero
    {
        get
        {
            if (!IsValid) return true;
            for (int i = 0; i < Space.Dims; i++)
                if (Components[i] != 0) return false;
            return true;
        }
    }

    // ================================================================
    //  EVALUATION — convert from ticks (longs) to value-space (doubles)
    //  "All values should be longs until evaluated,
    //   when evaluated they should be floats [doubles]."
    //  Values are evaluated based mostly on the unit (TicksPerUnit)
    //  but also on the range and other properties.
    // ================================================================

    /// <summary>
    /// Evaluate this value: convert from ticks to a double.
    /// For 1D values, returns the single component evaluated.
    /// For ND values, returns the unit (direction=+1) component evaluated.
    /// This is the main evaluation step: ticks / TicksPerUnit → double.
    /// </summary>
    public double Evaluate()
    {
        if (!IsValid) return 0.0;
        if (Space.Dims == 1)
            return (double)Components[0] / Space.Axes[0].TicksPerUnit;
        return UnitValue;
    }

    /// <summary>
    /// Evaluate all components: convert every axis from ticks to doubles.
    /// Returns an array of doubles, one per axis: ticks[i] / TicksPerUnit[i].
    /// Same as GetValues() — this is the explicit evaluation step.
    /// </summary>
    public double[] EvaluateAll() => GetValues();

    /// <summary>
    /// Get all components as value-space doubles: ticks / ticksPerUnit.
    /// </summary>
    public double[] GetValues()
    {
        var result = new double[Space.Dims];
        for (int i = 0; i < Space.Dims; i++)
            result[i] = (double)Components[i] / Space.Axes[i].TicksPerUnit;
        return result;
    }

    /// <summary>
    /// Get a single axis component as a value-space double.
    /// </summary>
    public double GetValue(int axis) => (double)Components[axis] / Space.Axes[axis].TicksPerUnit;

    // ================================================================
    //  TICK CONVERSION (for storage, matches Focal's long positions)
    // ================================================================

    /// <summary>
    /// Return tick components (clone of the internal array).
    /// Since storage IS ticks, this is just a clone.
    /// </summary>
    public long[] ToTicks() => (long[])Components.Clone();

    /// <summary>
    /// Create a CValue from tick components directly.
    /// </summary>
    public static CValue FromTicks(CSpace space, long[] ticks) =>
        new CValue(space, ticks);

    // ================================================================
    //  UNIT/UNOT ACCESSORS (compatibility with PRange/segment convention)
    //  These return doubles (evaluated from ticks).
    // ================================================================

    /// <summary>
    /// Value on the first direction=+1 axis (the "real" / "End" / "unit" component).
    /// Returns double: ticks / ticksPerUnit.
    /// </summary>
    public double UnitValue
    {
        get
        {
            if (!IsValid) return 0.0;
            for (int i = 0; i < Space.Dims; i++)
                if (Space.Axes[i].Direction == 1)
                    return (double)Components[i] / Space.Axes[i].TicksPerUnit;
            return 0.0;
        }
    }

    /// <summary>
    /// Value on the first direction=-1 axis (the "imaginary" / "Start" / "unot" component).
    /// Returns double: ticks / ticksPerUnit.
    /// </summary>
    public double UnotValue
    {
        get
        {
            if (!IsValid) return 0.0;
            for (int i = 0; i < Space.Dims; i++)
                if (Space.Axes[i].Direction == -1)
                    return (double)Components[i] / Space.Axes[i].TicksPerUnit;
            return 0.0;
        }
    }

    // ================================================================
    //  TENSION
    // ================================================================

    /// <summary>
    /// Compute how well this value fits its frame's bounds.
    /// </summary>
    public Tension ComputeTension() => Tension.Compute(this);

    // ================================================================
    //  SNAP TO BOUNDS — works directly on ticks
    //  No quantization step needed: values ARE ticks, already quantized.
    // ================================================================

    /// <summary>
    /// Clamp to tick bounds, wrap cyclic axes. Already quantized (it's ticks).
    /// </summary>
    public CValue Snap()
    {
        var comps = new long[Space.Dims];
        for (int i = 0; i < Space.Dims; i++)
        {
            var ax = Space.Axes[i];
            comps[i] = Components[i];
            // Clamp to tick bounds
            if (ax.HasMinBound)
                comps[i] = Math.Max(comps[i], ax.MinBoundTicks);
            if (ax.HasMaxBound)
                comps[i] = Math.Min(comps[i], ax.MaxBoundTicks);
            // Cyclic wrap in ticks
            if (ax.IsCyclic && ax.CyclePeriodTicks > 0)
            {
                comps[i] = comps[i] % ax.CyclePeriodTicks;
                if (comps[i] < 0) comps[i] += ax.CyclePeriodTicks;
            }
        }
        return new CValue(Space, comps, Certainty);
    }

    // ================================================================
    //  ADDITION — always component-wise, pure long arithmetic
    // ================================================================

    public static CValue operator +(CValue a, CValue b)
    {
        var comps = new long[a.Space.Dims];
        for (int i = 0; i < a.Space.Dims; i++)
        {
            comps[i] = a.Components[i] + b.Components[i];
            if (a.Space.Axes[i].IsCyclic && a.Space.Axes[i].CyclePeriodTicks > 0)
            {
                comps[i] = comps[i] % a.Space.Axes[i].CyclePeriodTicks;
                if (comps[i] < 0) comps[i] += a.Space.Axes[i].CyclePeriodTicks;
            }
        }
        return new CValue(a.Space, comps, Math.Min(a.Certainty, b.Certainty));
    }

    public static CValue operator -(CValue a)
    {
        var comps = new long[a.Space.Dims];
        for (int i = 0; i < a.Space.Dims; i++)
            comps[i] = -a.Components[i];
        return new CValue(a.Space, comps, a.Certainty);
    }

    public static CValue operator -(CValue a, CValue b) => a + (-b);

    // ================================================================
    //  MULTIPLICATION — the algebra table drives everything
    //
    //  Components are in ticks. Multiplying two tick values gives ticks².
    //  Divide by BasisTicks (TicksPerUnit) to get back to ticks.
    //
    //  For uniform TPU (all factory-created spaces):
    //    result_ticks[target] += a_ticks[left] * b_ticks[right] * sign / BasisTicks
    //
    //  For heterogeneous TPU:
    //    result_ticks[target] += a_ticks[left] * b_ticks[right] * sign
    //                           * Axes[target].TPU / (Axes[left].TPU * Axes[right].TPU)
    // ================================================================

    public static CValue operator *(CValue a, CValue b)
    {
        var comps = new long[a.Space.Dims];
        var space = a.Space;

        // Check if all axes have the same TPU (common case)
        bool uniformTPU = true;
        long basisTicks = space.BasisTicks;
        for (int i = 1; i < space.Dims; i++)
        {
            if (space.Axes[i].TicksPerUnit != basisTicks)
            {
                uniformTPU = false;
                break;
            }
        }

        if (uniformTPU)
        {
            // Fast path: single division by BasisTicks
            foreach (var entry in space.Algebra)
            {
                comps[entry.Target] += a.Components[entry.Left] * b.Components[entry.Right]
                    * entry.Sign / basisTicks;
            }
        }
        else
        {
            // General path: per-axis TPU scaling
            foreach (var entry in space.Algebra)
            {
                long tpuLeft = space.Axes[entry.Left].TicksPerUnit;
                long tpuRight = space.Axes[entry.Right].TicksPerUnit;
                long tpuTarget = space.Axes[entry.Target].TicksPerUnit;
                comps[entry.Target] += a.Components[entry.Left] * b.Components[entry.Right]
                    * entry.Sign * tpuTarget / (tpuLeft * tpuRight);
            }
        }

        return new CValue(a.Space, comps, Math.Min(a.Certainty, b.Certainty));
    }

    /// <summary>
    /// Division: a/b = a * conj(b) / |b|².
    /// Computed directly to avoid precision loss from rounding an intermediate Inverse.
    /// </summary>
    public static CValue operator /(CValue a, CValue b)
    {
        var conj = Conjugate(b);
        var magSq = MagnitudeSquared(b);
        if (Math.Abs(magSq) < DoubleTolerance)
            return new CValue(a.Space, new long[a.Space.Dims], Math.Min(a.Certainty, b.Certainty));

        var numerator = a * conj; // full tick precision
        var comps = new long[a.Space.Dims];
        for (int i = 0; i < a.Space.Dims; i++)
            comps[i] = (long)Math.Round(numerator.Components[i] / magSq);
        return new CValue(a.Space, comps, Math.Min(a.Certainty, b.Certainty));
    }

    /// <summary>
    /// Multiply by a scalar. Direct: scale each tick component.
    /// </summary>
    public static CValue operator *(CValue a, double scalar)
    {
        var comps = new long[a.Space.Dims];
        for (int i = 0; i < a.Space.Dims; i++)
            comps[i] = (long)Math.Round(a.Components[i] * scalar);
        return new CValue(a.Space, comps, a.Certainty);
    }

    public static CValue operator *(double scalar, CValue a) => a * scalar;

    public static CValue operator /(CValue a, double scalar) => a * (1.0 / scalar);

    // ================================================================
    //  CONJUGATE — negate all axes with direction == -1
    //  Pure long arithmetic.
    // ================================================================

    /// <summary>
    /// Generalized conjugate: negate all inverted-direction (direction=-1) axes.
    /// For complex/segment: conjugate(a + bi) = a - bi.
    /// For quaternions: conjugate(a + bi + cj + dk) = a - bi - cj - dk.
    /// </summary>
    public static CValue Conjugate(CValue a)
    {
        var comps = new long[a.Space.Dims];
        for (int i = 0; i < a.Space.Dims; i++)
        {
            comps[i] = a.Space.Axes[i].Direction == -1
                ? -a.Components[i]
                : a.Components[i];
        }
        return new CValue(a.Space, comps, a.Certainty);
    }

    public CValue Conjugate() => Conjugate(this);

    // ================================================================
    //  MAGNITUDE — returns doubles (evaluated from tick values)
    // ================================================================

    /// <summary>
    /// Magnitude squared: a * conjugate(a), taking the scalar (direction=+1) components.
    /// Returns a double in value-space (evaluated, not ticks).
    /// For complex: |a+bi|² = a² + b².
    /// </summary>
    public static double MagnitudeSquared(CValue a)
    {
        var product = a * Conjugate(a);
        double total = 0.0;
        for (int i = 0; i < a.Space.Dims; i++)
        {
            if (a.Space.Axes[i].Direction == 1)
                total += (double)product.Components[i] / a.Space.Axes[i].TicksPerUnit;
        }
        return total;
    }

    public double MagnitudeSquared() => MagnitudeSquared(this);
    public double Magnitude() => Math.Sqrt(Math.Abs(MagnitudeSquared(this)));

    /// <summary>
    /// Absolute value using the generalized formula (matches PRange.Abs).
    /// </summary>
    public static double Abs(CValue a) => a.Magnitude();

    // ================================================================
    //  INVERSE — uses evaluated doubles internally, returns tick-based CValue
    // ================================================================

    /// <summary>
    /// Multiplicative inverse: inv(a) = One / a.
    /// Uses the same direct numerator/|a|² approach as the division operator.
    /// </summary>
    public static CValue Inverse(CValue a)
    {
        var one = new CValue(a.Space, a.Space.One().Components);
        return one / a;
    }

    public CValue Inverse() => Inverse(this);
    public CValue Reciprocal() => Inverse(this);

    // ================================================================
    //  POWER — uses evaluated doubles (polar form), converts back to ticks
    // ================================================================

    /// <summary>
    /// Raise to a power. Converts to evaluated doubles, computes in polar form,
    /// converts back to ticks.
    /// </summary>
    public static CValue Pow(CValue baseVal, double exponent)
    {
        if (exponent == 0) return baseVal.Space.One();
        if (baseVal.IsZero) return baseVal.Space.Zero();

        var mag = baseVal.Magnitude();
        if (mag < DoubleTolerance) return baseVal.Space.Zero();

        // 1D: simple scalar power
        if (baseVal.Space.Dims == 1)
        {
            double val = baseVal.GetValue(0);
            double result = Math.Pow(val, exponent);
            return new CValue(baseVal.Space,
                new[] { (long)Math.Round(result * baseVal.Space.Axes[0].TicksPerUnit) },
                baseVal.Certainty);
        }

        // 2D: polar form — matches PRange.Pow
        if (baseVal.Space.Dims == 2)
        {
            int iAxis = -1, rAxis = -1;
            for (int i = 0; i < 2; i++)
            {
                if (baseVal.Space.Axes[i].Direction == -1) iAxis = i;
                else rAxis = i;
            }
            if (iAxis < 0 || rAxis < 0) { iAxis = 0; rAxis = 1; }

            double real = baseVal.GetValue(rAxis);
            double imag = baseVal.GetValue(iAxis);
            double angle = Math.Atan2(imag, real);
            double newMag = Math.Pow(mag, exponent);
            double newAngle = angle * exponent;

            var comps = new long[2];
            comps[iAxis] = (long)Math.Round(newMag * Math.Sin(newAngle) * baseVal.Space.Axes[iAxis].TicksPerUnit);
            comps[rAxis] = (long)Math.Round(newMag * Math.Cos(newAngle) * baseVal.Space.Axes[rAxis].TicksPerUnit);
            return new CValue(baseVal.Space, comps, baseVal.Certainty);
        }

        // Higher dimensions: repeated multiplication for integer exponents
        if (Math.Abs(exponent - Math.Round(exponent)) < DoubleTolerance && exponent > 0 && exponent < 100)
        {
            var result = baseVal.Space.One();
            int n = (int)Math.Round(exponent);
            for (int i = 0; i < n; i++)
                result = result * baseVal;
            return result;
        }

        throw new NotImplementedException(
            $"Non-integer power ({exponent}) not yet supported for {baseVal.Space.Dims}D spaces.");
    }

    /// <summary>
    /// Power with a CValue exponent (for complex exponentiation).
    /// </summary>
    public static CValue Pow(CValue baseVal, CValue power)
    {
        if (power.IsZero) return baseVal.Space.One();
        if (baseVal.IsZero) return baseVal.Space.Zero();

        // For 2D spaces, full complex exponentiation: b^p = e^(p * ln(b))
        if (baseVal.Space.Dims == 2)
        {
            int iAxis = -1, rAxis = -1;
            for (int i = 0; i < 2; i++)
            {
                if (baseVal.Space.Axes[i].Direction == -1) iAxis = i;
                else rAxis = i;
            }
            if (iAxis < 0 || rAxis < 0) { iAxis = 0; rAxis = 1; }

            double real1 = baseVal.GetValue(rAxis);
            double imag1 = baseVal.GetValue(iAxis);
            double real2 = power.GetValue(rAxis);
            double imag2 = power.GetValue(iAxis);

            double mag1 = Abs(baseVal);
            double angle = Math.Atan2(imag1, real1);
            double newAngle = real2 * angle + imag2 * Math.Log(mag1);
            double newMag = Math.Pow(mag1, real2) * Math.Pow(Math.E, -imag2 * angle);

            var comps = new long[2];
            comps[iAxis] = (long)Math.Round(newMag * Math.Sin(newAngle) * baseVal.Space.Axes[iAxis].TicksPerUnit);
            comps[rAxis] = (long)Math.Round(newMag * Math.Cos(newAngle) * baseVal.Space.Axes[rAxis].TicksPerUnit);
            return new CValue(baseVal.Space, comps, Math.Min(baseVal.Certainty, power.Certainty));
        }

        // For 1D: just use the unit component as scalar exponent
        return Pow(baseVal, power.UnitValue);
    }

    public CValue Pow(double exponent) => Pow(this, exponent);
    public CValue Pow(CValue power) => Pow(this, power);
    public CValue Square() => this * this;

    // ================================================================
    //  NORMALIZE — uses evaluated doubles, returns ticks
    // ================================================================

    /// <summary>
    /// Normalize: divide by magnitude, giving the unit-length version.
    /// </summary>
    public CValue Normalize()
    {
        var mag = Magnitude();
        if (mag < DoubleTolerance) return Space.One();
        return this / mag;
    }

    /// <summary>
    /// Normalize then scale to match target's magnitude.
    /// </summary>
    public CValue NormalizeTo(CValue target) => Normalize() * target;

    // ================================================================
    //  DISTANCE — metric-aware, cyclic-aware
    // ================================================================

    /// <summary>
    /// Distance between two values in the same frame.
    /// Uses the space's MetricKind (Euclidean, Manhattan, or Chebyshev).
    /// Cyclic axes always use the shortest-arc difference, regardless of metric:
    /// e.g. angle 350° is 20° away from 10°, not 340°.
    /// </summary>
    public static double Distance(CValue a, CValue b)
    {
        return a.Space.Metric switch
        {
            MetricKind.Manhattan => ManhattanDistance(a, b),
            MetricKind.Chebyshev => ChebyshevDistance(a, b),
            _                    => EuclideanDistance(a, b),
        };
    }

    /// <summary>
    /// Per-axis component difference in value-space, respecting cyclic axes.
    /// For a cyclic axis the result is the shortest-arc difference:
    /// always in the range [-period/2, +period/2].
    /// </summary>
    private static double AxisDiff(CValue a, CValue b, int i)
    {
        double d = a.GetValue(i) - b.GetValue(i);
        var ax = a.Space.Axes[i];
        if (ax.IsCyclic && ax.CyclePeriod > 0)
        {
            double p = ax.CyclePeriod;
            // Map d into (-p, p), then choose the shorter arc
            d = d % p;
            if (d >  p / 2) d -= p;
            if (d < -p / 2) d += p;
        }
        return d;
    }

    private static double EuclideanDistance(CValue a, CValue b)
    {
        double sum = 0;
        for (int i = 0; i < a.Space.Dims; i++)
        {
            double d = AxisDiff(a, b, i);
            sum += d * d;
        }
        return Math.Sqrt(sum);
    }

    private static double ManhattanDistance(CValue a, CValue b)
    {
        double sum = 0;
        for (int i = 0; i < a.Space.Dims; i++)
            sum += Math.Abs(AxisDiff(a, b, i));
        return sum;
    }

    private static double ChebyshevDistance(CValue a, CValue b)
    {
        double max = 0;
        for (int i = 0; i < a.Space.Dims; i++)
            max = Math.Max(max, Math.Abs(AxisDiff(a, b, i)));
        return max;
    }

    // ================================================================
    //  CLONE / EQUALITY — direct long comparison, no tolerance needed
    // ================================================================

    public CValue Clone() => new CValue(Space, Components, Certainty);

    public bool Equals(CValue? other)
    {
        if (other is null) return false;
        if (!IsValid || !other.IsValid) return !IsValid && !other.IsValid;
        if (Space != other.Space) return false;
        if (Components.Length != other.Components.Length) return false;
        for (int i = 0; i < Components.Length; i++)
        {
            if (Components[i] != other.Components[i]) return false;
        }
        return true;
    }

    public static bool operator ==(CValue? a, CValue? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));
    public static bool operator !=(CValue? a, CValue? b) => !(a == b);

    public override bool Equals(object? obj) => obj is CValue other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            if (Components != null)
            {
                for (int i = 0; i < Components.Length; i++)
                    hash = hash * 31 + Components[i].GetHashCode();
            }
            return hash;
        }
    }

    public override string ToString()
    {
        if (!IsValid) return "[invalid]";
        var parts = new string[Space.Dims];
        for (int i = 0; i < Space.Dims; i++)
            parts[i] = $"{GetValue(i):0.##}{Space.Axes[i].Name[0]}";
        return $"[{string.Join(", ", parts)}]";
    }
}
