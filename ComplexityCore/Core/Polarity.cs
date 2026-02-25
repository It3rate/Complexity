namespace Complexity.Core;

public enum Polarity { None, Unknown, Aligned, Inverted };//, Zero, Max }

public static class PolarityExtension
{
    public static bool HasPolarity(this Polarity polarity) => polarity == Polarity.Aligned || polarity == Polarity.Inverted;
    public static bool IsTrue(this Polarity polarity) => polarity == Polarity.Aligned;
    public static bool IsFalse(this Polarity polarity) => polarity == Polarity.Inverted;
    public static int Direction(this Polarity polarity) => polarity == Polarity.Aligned ? 1 : polarity == Polarity.Inverted ? -1 : 0;
    public static int ForceValue(this Polarity polarity) => polarity == Polarity.Inverted ? -1 : 1;

    public static Polarity Invert(this Polarity polarity)
    {
        return polarity switch
        {
            Polarity.Aligned => Polarity.Inverted,
            Polarity.Inverted => Polarity.Aligned,
            _ => polarity
        };
    }
}