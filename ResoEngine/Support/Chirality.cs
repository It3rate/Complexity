namespace ResoEngine.Support;

public enum Chirality
{
    Pro = +1,
    Con = -1,
}

public static class ChiralityExtensions
{
    public static bool IsPositive(this Chirality chirality) => (int)chirality > 0;
    public static bool IsNegative(this Chirality chirality) => (int)chirality < 0;
    public static Chirality Invert(this Chirality chirality) => (Chirality)(-(int)chirality);
}

