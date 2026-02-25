namespace Complexity.Core;

/// <summary>
/// Result of a boolean operation on CValues: zero or more disjoint regions in the space.
/// Generalizes MaskedFocal/MaskedNumber from 1D segments to N-dimensional hyperrectangles.
///
/// Each region is a pair (Low, High) defining the corners of a hyperrectangle.
/// For 1D, this is just a list of intervals. For 2D, a list of rectangles. Etc.
///
/// All boundaries are in ticks (longs), matching the tick-based CValue storage.
/// </summary>
public class CMasked
{
    public CSpace Space { get; }
    public (CValue Low, CValue High)[] Regions { get; }

    public int Count => Regions.Length;
    public bool IsEmpty => Count == 0;
    public bool IsSingle => Count == 1;

    public CMasked(CSpace space, (CValue Low, CValue High)[] regions)
    {
        Space = space;
        Regions = regions;
    }

    public static CMasked Empty(CSpace space) =>
        new CMasked(space, Array.Empty<(CValue, CValue)>());

    // ================================================================
    //  PER-AXIS BOOLEAN LOGIC
    //
    //  Same algorithm as Focal.BuildTruthTable + ApplyOpToTruthTable,
    //  applied independently per axis in ticks, then combined.
    // ================================================================

    /// <summary>
    /// Apply a boolean operation to two regions (hyperrectangles).
    /// Each region is defined by a (Low, High) CValue pair with tick components.
    /// </summary>
    public static CMasked BoolOp(CSpace space, Func<bool, bool, bool> op,
        (CValue Low, CValue High) a, (CValue Low, CValue High) b)
    {
        // For each axis, compute 1D truth table and get resulting segments (in ticks)
        var perAxisSegments = new List<(long, long)>[space.Dims];

        for (int axis = 0; axis < space.Dims; axis++)
        {
            var aLo = Math.Min(a.Low.Components[axis], a.High.Components[axis]);
            var aHi = Math.Max(a.Low.Components[axis], a.High.Components[axis]);
            var bLo = Math.Min(b.Low.Components[axis], b.High.Components[axis]);
            var bHi = Math.Max(b.Low.Components[axis], b.High.Components[axis]);

            perAxisSegments[axis] = BoolOp1D(op, aLo, aHi, bLo, bHi);
        }

        // Cartesian product of per-axis segments gives N-D regions
        var regions = CartesianProduct(space, perAxisSegments);
        return new CMasked(space, regions);
    }

    /// <summary>
    /// 1D boolean operation on two intervals [aLo..aHi] and [bLo..bHi] in ticks.
    /// Returns a list of result intervals in ticks.
    /// </summary>
    private static List<(long, long)> BoolOp1D(
        Func<bool, bool, bool> op, long aLo, long aHi, long bLo, long bHi)
    {
        // Collect all boundary points and sort
        var boundaries = new SortedSet<long> { aLo, aHi, bLo, bHi };
        var sorted = boundaries.ToArray();

        var result = new List<(long, long)>();
        long? segStart = null;

        for (int i = 0; i < sorted.Length; i++)
        {
            // Test the midpoint of each segment between consecutive boundaries
            long testPoint;
            if (i == 0)
                testPoint = sorted[0]; // at the first boundary
            else
                testPoint = sorted[i - 1] + (sorted[i] - sorted[i - 1]) / 2; // midpoint (integer division)

            bool aState = testPoint >= aLo && testPoint <= aHi;
            bool bState = testPoint >= bLo && testPoint <= bHi;
            bool opResult = op(aState, bState);

            if (opResult && segStart == null)
            {
                segStart = i == 0 ? sorted[0] : sorted[i - 1];
            }
            else if (!opResult && segStart != null)
            {
                result.Add((segStart.Value, sorted[i - 1]));
                segStart = null;
            }
        }

        // Check the region at/after the last boundary
        {
            bool aState = sorted[^1] >= aLo && sorted[^1] <= aHi;
            bool bState = sorted[^1] >= bLo && sorted[^1] <= bHi;
            bool opResult = op(aState, bState);
            if (opResult && segStart != null)
            {
                result.Add((segStart.Value, sorted[^1]));
            }
            else if (opResult && segStart == null)
            {
                result.Add((sorted[^1], sorted[^1]));
            }
        }

        // If no segments found, return the full range for ops like TRUE/NOR that may need it
        if (result.Count == 0 && op(false, false))
        {
            result.Add((Math.Min(aLo, bLo), Math.Max(aHi, bHi)));
        }

        return result;
    }

    /// <summary>
    /// Cartesian product of per-axis tick segment lists → N-D regions.
    /// </summary>
    private static (CValue Low, CValue High)[] CartesianProduct(
        CSpace space, List<(long, long)>[] perAxisSegments)
    {
        var regions = new List<(long[] low, long[] high)>();
        regions.Add((new long[space.Dims], new long[space.Dims]));

        for (int axis = 0; axis < space.Dims; axis++)
        {
            var segs = perAxisSegments[axis];
            if (segs.Count == 0)
                return Array.Empty<(CValue, CValue)>(); // no valid segments → empty result

            var newRegions = new List<(long[] low, long[] high)>();
            foreach (var region in regions)
            {
                foreach (var seg in segs)
                {
                    var low = (long[])region.low.Clone();
                    var high = (long[])region.high.Clone();
                    low[axis] = seg.Item1;
                    high[axis] = seg.Item2;
                    newRegions.Add((low, high));
                }
            }
            regions = newRegions;
        }

        return regions
            .Select(r => (new CValue(space, r.low), new CValue(space, r.high)))
            .ToArray();
    }

    // ================================================================
    //  COMMON BOOLEAN OPERATIONS
    // ================================================================

    public static CMasked And(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x && y, a, b);

    public static CMasked Or(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x || y, a, b);

    public static CMasked Xor(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x ^ y, a, b);

    public static CMasked Nand(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !(x && y), a, b);

    public static CMasked Nor(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !(x || y), a, b);

    public override string ToString()
    {
        if (IsEmpty) return "∅";
        return $"{Count} region(s) in {Space.Name}";
    }
}
