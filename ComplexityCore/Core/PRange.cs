namespace Complexity.Core;

/// <summary>
/// A segment of two values, with the non aligned one using the inverted polarity.
/// </summary>
public struct PRange
{
    public static readonly PRange Empty = new PRange(0.0, 1.0, true);
    public static readonly PRange Zero = new PRange(0.0, 0.0);
    public static readonly PRange Unit = new PRange(0.0, 1.0);
    public static readonly PRange Unot = new PRange(1.0, 0.0);
    public static readonly PRange Umid = new PRange(-0.5, 0.5);
    public static readonly PRange MaxRange = new PRange(double.MaxValue, double.MaxValue);
    public static readonly PRange MinRange = new PRange(double.MinValue, double.MinValue);
    public static readonly double Tolerance = 0.000000001;

    public Polarity Polarity { get; private set; }
    public int PolarityDirection => IsAligned ? 1 : -1;
    public bool IsAligned => Polarity == Polarity.Aligned;
    public bool IsInverted => Polarity == Polarity.Inverted;

    public double Start { get; private set; }
    public double End
    {
        get;
        private set;
    }
    public float StartF => (float)Start;
    public float EndF => (float)End;

    private readonly bool _hasValue;
    public double UnitValue
    {
        get => IsAligned ? End : Start;
        set { if (IsAligned) { End = value; } else { Start = value; } }
    }
    public double UnotValue
    {
        get => IsAligned ? Start : End;
        set { if (IsAligned) { Start = value; } else { End = value; } }
    }
    public float RenderStart => (float)(IsAligned ? Start : -Start);
    public float RenderEnd => (float)(IsAligned ? End : -End);

    public PRange(int start, int end, bool isAligned = true)
    {
        _hasValue = true;
        Start = start;
        End = end;
        Polarity = isAligned ? Polarity.Aligned : Polarity.Inverted;
    }
    public PRange(double start, double end, bool isAligned = true)
    {
        _hasValue = true;
        Start = start;
        End = end;
        Polarity = isAligned ? Polarity.Aligned : Polarity.Inverted;
    }
    public PRange(PRange value, bool isAligned = true)
    {
        _hasValue = true;
        Start = value.Start;
        End = value.End;
        Polarity = isAligned ? Polarity.Aligned : Polarity.Inverted;
    }

    private PRange(double start, double end, bool isEmpty, bool isAligned = true) // empty ctor
    {
        _hasValue = !isEmpty;
        Start = start;
        End = end;
        Polarity = isAligned ? Polarity.Aligned : Polarity.Inverted;
    }

    public bool IsEmpty => _hasValue;
    public double Min => Start >= End ? End : Start;
    public double Max => End >= Start ? End : Start;
    public float MinF => (float)Min;
    public float MaxF => (float)Max;
    public double Length => PRange.AbsLength(this);
    public double DirectedLength() => PRange.DirectedLength(this);
    public double AbsLength() => PRange.AbsLength(this);
    public double TAtValue(double value)
    {
        var dist = value - (-Start);
        return dist / DirectedLength();
    }
    public PRange InvertStart() => new(-Start, End, IsAligned);
    public PRange InvertEnd() => new(Start, -End, IsAligned);
    public PRange InvertPolarity() => new(Start, End, !IsAligned);
    public PRange InvertRange() => new(-Start, -End, IsAligned);
    public PRange InvertPolarityAndRange() => new(-Start, -End, !IsAligned);

    public PRange Negation() => PRange.Negation(this);
    public PRange Conjugate() => PRange.Conjugate(this);
    public PRange Reciprocal() => PRange.Reciprocal(this);
    public PRange Square() => PRange.Square(this);
    public PRange Normalize() => PRange.Normalize(this);
    public PRange NormalizeTo(PRange value) => PRange.NormalizeTo(this, value);
    public PRange ClampInner() => PRange.ClampInner(this);
    public PRange ClampOuter() => PRange.ClampOuter(this);
    public PRange Round() => PRange.Round(this);
    public PRange PositiveDirection() => PositiveDirection(this);
    public PRange NegativeDirection() => NegativeDirection(this);
    public bool IsZero => End == 0 && Start == 0;
    public bool IsZeroLength => (End == Start);
    public bool IsForward => End - Start > Tolerance;
    public bool IsBackward => Start - End > Tolerance;
    public bool IsPoint => Length < Tolerance;
    public double Direction => End >= Start ? 1.0 : -1.0;

    // because everything is segments, can add 'prepositions' (before, after, between, entering, leaving, near etc)
    public bool IsWithin(PRange value) => Start >= value.Start && End <= value.End;
    public bool IsBetween(PRange value) => Start > value.Start && End < value.End;
    public bool IsBefore(PRange value) => Start < value.Start && End < value.Start;
    public bool IsAfter(PRange value) => Start > value.End && End > value.End;
    public bool IsBeginning(PRange value) => Start <= value.Start && End > value.Start;
    public bool IsEnding(PRange value) => Start >= value.End && End > value.End;
    public bool IsTouching(PRange value) => (Start >= value.Start && Start <= value.End) || (End >= value.Start && End <= value.End);
    public bool IsNotTouching(PRange value) => !IsTouching(value);


    public static PRange operator +(PRange a, double value) => a + new PRange(0, value);
    public static PRange operator -(PRange a, double value) => a - new PRange(0, value);
    public static PRange operator *(PRange a, double value) => a * new PRange(0, value);
    public static PRange operator /(PRange a, double value) => a / new PRange(0, value);

    public static PRange PositiveDirection(PRange value) => new PRange(value.Min, value.Max);
    public static PRange NegativeDirection(PRange value) => new PRange(value.Max, value.Min);
    public static PRange Negate(PRange value) => -value;
    public static PRange Add(PRange left, PRange right) => left + right;
    public static PRange Subtract(PRange left, PRange right) => left - right;
    public static PRange Multiply(PRange left, PRange right) => left * right;
    public static PRange Divide(PRange dividend, PRange divisor) => dividend / divisor;

    public static PRange operator -(PRange value) => new PRange(-value.Start, -value.End);
    public static PRange operator +(PRange left, PRange right)
    {
        var result = right.Clone();
        result.UnitValue += left.UnitValue;
        result.UnotValue += left.UnotValue;
        return result;
    }
    public static PRange operator -(PRange left, PRange right)
    {
        var result = left.Clone();
        result.UnitValue -= right.UnitValue;
        result.UnotValue -= right.UnotValue;
        return result;
    }
    public static PRange operator *(PRange left, PRange right)
    {
        var result = new PRange(left.Start * right.End + left.End * right.Start, left.End * right.End - left.Start * right.Start);
        result.Polarity = left.Polarity;
        result = result.SolvePolarityWith(right.Polarity); // probably can compute this properly with unit/unot values.
        return result;
    }
    public static PRange operator /(PRange left, PRange right)
    {
        PRange result;
        double real1 = left.End;
        double imaginary1 = left.Start;
        double real2 = right.End;
        double imaginary2 = right.Start;
        if (Math.Abs(imaginary2) < Math.Abs(real2))
        {
            double num = imaginary2 / real2;
            result = new PRange((imaginary1 - real1 * num) / (real2 + imaginary2 * num), (real1 + imaginary1 * num) / (real2 + imaginary2 * num));
        }
        else
        {
            double num1 = real2 / imaginary2;
            result = new PRange((-real1 + imaginary1 * num1) / (imaginary2 + real2 * num1), (imaginary1 + real1 * num1) / (imaginary2 + real2 * num1));
        }
        result.Polarity = left.Polarity;
        result = result.SolvePolarityWith(right.Polarity);
        return result;
    }
    public PRange SolvePolarityWith(Polarity right)
    {
        var result = this;
        if (Polarity == Polarity.Inverted && right == Polarity.Inverted)
        {
            result = InvertPolarityAndRange();
        }
        else if (Polarity == Polarity.Aligned && right == Polarity.Inverted)
        {
            result = InvertPolarity();
        }
        return result;
    }
    public static double DirectedLength(PRange a) => a.End + a.Start;
    public static double AbsLength(PRange a) => Math.Abs(a.End + a.Start);

    public static PRange Negation(PRange a) => new PRange(-a.Start, -a.End);
    public static PRange Conjugate(PRange a) => new PRange(a.Start, -a.End);
    public static PRange Reciprocal(PRange a) => a.End == 0.0 && a.Start == 0.0 ? PRange.Zero : PRange.Unit / a;
    public static PRange Square(PRange a) => a * a;
    public static PRange Normalize(PRange a) => a.IsZeroLength ? new PRange(0.5, 0.5) : a / a;
    public static PRange NormalizeTo(PRange from, PRange to) => from.Normalize() * to;

    public static PRange ClampInner(PRange a) => new PRange(Math.Floor(Math.Abs(a.Start)) * Math.Sign(a.Start), Math.Floor(Math.Abs(a.End)) * Math.Sign(a.End));
    public static PRange ClampOuter(PRange a) => new PRange(Math.Ceiling(Math.Abs(a.Start)) * Math.Sign(a.Start), Math.Ceiling(Math.Abs(a.End)) * Math.Sign(a.End));
    public static PRange Round(PRange a) => new PRange(Math.Round(a.Start), Math.Round(a.End));

    public static double Abs(PRange a)
    {
        if (double.IsInfinity(a.End) || double.IsInfinity(a.Start))
            return double.PositiveInfinity;
        double num1 = Math.Abs(a.End);
        double num2 = Math.Abs(a.Start);
        if (num1 > num2)
        {
            double num3 = num2 / num1;
            return num1 * Math.Sqrt(1.0 + num3 * num3);
        }
        if (num2 == 0.0)
            return num1;
        double num4 = num1 / num2;
        return num2 * Math.Sqrt(1.0 + num4 * num4);
    }

    public static PRange Pow(PRange value, PRange power)
    {
        if (power == PRange.Zero)
            return PRange.Unit;
        if (value == PRange.Zero)
            return PRange.Zero;
        double real1 = value.End;
        double imaginary1 = value.Start;
        double real2 = power.End;
        double imaginary2 = power.Start;
        double num1 = PRange.Abs(value);
        double num2 = Math.Atan2(imaginary1, real1);
        double num3 = real2 * num2 + imaginary2 * Math.Log(num1);
        double num4 = Math.Pow(num1, real2) * Math.Pow(Math.E, -imaginary2 * num2);
        return new PRange(num4 * Math.Sin(num3), num4 * Math.Cos(num3));
    }
    public static PRange Pow(PRange value, double power) => PRange.Pow(value, new PRange(0.0, power));
    private static PRange Scale(PRange value, double factor) => new PRange(factor * value.Start, factor * value.End);

    public bool IsSameDirection(PRange range) => Math.Abs(range.Direction + Direction) > Tolerance;
    public bool FullyContains(PRange toTest, bool includeEndpoints = true)
    {
        bool result = false;
        if (IsSameDirection(toTest))
        {
            var pd = PositiveDirection();
            var pdTest = toTest.PositiveDirection();
            result = includeEndpoints ? pdTest.IsWithin(pd) : pdTest.IsBetween(pd);
        }
        return result;
    }

    public float Midpoint() => (EndF - StartF) / 2f + StartF;
    public float SampleRandom(Random rnd) => (EndF - StartF) * (float)rnd.NextDouble() + StartF;
    public float SampleStdDev() => (EndF - StartF) / 2f + StartF; // todo: add standard deviation sampler

    public PRange Clone() => new PRange(Start, End, IsAligned);

    public static bool operator ==(PRange a, PRange b) // value type, so no nulls
    {
        return a.Equals(b);
    }
    public static bool operator !=(PRange a, PRange b) => !(a == b);
    public override bool Equals(object obj)
    {
        return obj is PRange other && Equals(other);
    }
    public bool Equals(PRange value)
    {
        return Start.Equals(value.Start) && End.Equals(value.End) && Polarity.Equals(value.Polarity);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Start.GetHashCode();
            hashCode = (hashCode * 397) ^ End.GetHashCode();
            hashCode = (hashCode * 17) ^ Polarity.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString()
    {
        var prefix = Polarity == Polarity.Inverted ? "~" : "";
        return $"{prefix}[{Start:0.00}->{End:0.00}]";
    }

}
