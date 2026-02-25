namespace Complexity.Core;

/// <summary>
/// One cell of the multiplication table for a complexity space.
/// Says: component[Left] × component[Right] contributes (Sign) to component[Target].
/// The full table has dims² entries (one per axis pair).
/// This is the DEFINING structure of the complexity space — different tables give different math.
///
/// For the existing segment/complex system (PRange), the table is:
///   unit×unit → +unit     (End*End, the ac in ac-bd)
///   unot×unot → -unit     (Start*Start, the -bd in ac-bd; i²=-1)
///   unit×unot → +unot     (End*Start, the ad in ad+bc)
///   unot×unit → +unot     (Start*End, the bc in ad+bc)
/// </summary>
public readonly struct AlgebraEntry
{
    public int Left { get; }
    public int Right { get; }
    public int Target { get; }
    public int Sign { get; } // +1, -1, or 0 (nilpotent)

    public AlgebraEntry(int left, int right, int target, int sign)
    {
        Left = left;
        Right = right;
        Target = target;
        Sign = sign;
    }

    public override string ToString() =>
        $"[{Left}]×[{Right}]→[{Target}]({(Sign >= 0 ? "+" : "")}{Sign})";
}
