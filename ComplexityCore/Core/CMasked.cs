using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    //  ALL 16 BOOLEAN OPERATIONS (2-input truth table order)
    // ================================================================

    /// <summary>FALSE: always false regardless of inputs. (0000)</summary>
    public static CMasked False(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => false, a, b);

    /// <summary>AND: true only when both A and B are true. (0001)</summary>
    public static CMasked And(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x && y, a, b);

    /// <summary>AND_NOT: true only when A is true and B is false. (0010)</summary>
    public static CMasked AndNot(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x && !y, a, b);

    /// <summary>A: projection — result equals A, ignores B. (0011)</summary>
    public static CMasked ProjectA(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x, a, b);

    /// <summary>NOT_AND: true only when A is false and B is true. (0100)</summary>
    public static CMasked NotAnd(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !x && y, a, b);

    /// <summary>B: projection — result equals B, ignores A. (0101)</summary>
    public static CMasked ProjectB(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => y, a, b);

    /// <summary>XOR: true when exactly one of A or B is true. (0110)</summary>
    public static CMasked Xor(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x ^ y, a, b);

    /// <summary>OR: true when at least one of A or B is true. (0111)</summary>
    public static CMasked Or(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x || y, a, b);

    /// <summary>NOR: true only when both A and B are false. (1000)</summary>
    public static CMasked Nor(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !(x || y), a, b);

    /// <summary>XNOR: true when A and B have the same value. (1001)</summary>
    public static CMasked Xnor(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !(x ^ y), a, b);

    /// <summary>NOT_B: result is the inverse of B, ignores A. (1010)</summary>
    public static CMasked NotB(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !y, a, b);

    /// <summary>A_OR_NOT_B: true when A is true or B is false. (1011)</summary>
    public static CMasked AOrNotB(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => x || !y, a, b);

    /// <summary>NOT_A: result is the inverse of A, ignores B. (1100)</summary>
    public static CMasked NotA(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !x, a, b);

    /// <summary>NOT_A_OR_B: true when A is false or B is true. (1101)</summary>
    public static CMasked NotAOrB(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !x || y, a, b);

    /// <summary>NAND: true unless both A and B are true. (1110)</summary>
    public static CMasked Nand(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => !(x && y), a, b);

    /// <summary>TRUE: always true regardless of inputs. (1111)</summary>
    public static CMasked True(CSpace space, (CValue, CValue) a, (CValue, CValue) b) =>
        BoolOp(space, (x, y) => true, a, b);

    public override string ToString()
    {
        if (IsEmpty) return "∅";
        return $"{Count} region(s) in {Space.Name}";
    }
}





// Continuous Booleans

// Each of your 16 Boolean operations sits exactly at one of the corners
// of a tesseract or similar shape(16 points).
// The coordinates of each corner are defined by the 4 bits of its truth table.
// False sits at coordinate(0, 0, 0, 0).
// AND sits at coordinate(0, 0, 0, 1).
// OR sits at coordinate(0, 1, 1, 1).
// True sits at coordinate(1, 1, 1, 1).
// To transition smoothly from one state to another,
// you are simply drawing a line between two corners and walking across it.
// You are no longer flipping a switch;
// you are performing dimensional reduction on a conceptual space, 
// bringing a 4D problem down to a slider between 0.0 and 1.0.
// If you want to morph AND into OR, you interpolate their coordinates.
// Halfway between them is a coordinate of(0, 0.5, 0.5, 1). 
// This is a completely valid, continuous logic gate.
// It acts like an AND gate when both inputs are strong, but if only one input is present, 
// it offers a 50% "maybe" rather than a hard failure.

public class ContinuousGate
{
    // The 4 coordinates defining the operation's position in logic space
    public float W00 { get; }
    public float W10 { get; }
    public float W01 { get; }
    public float W11 { get; }

    public ContinuousGate(float w00, float w10, float w01, float w11)
    {
        W00 = w00;
        W10 = w10;
        W01 = w01;
        W11 = w11;
    }

    // The universal evaluation function
    public float Evaluate(float a, float b)
    {
        // Clamp inputs to ensure they remain within 0.0 to 1.0 bounds
        a = Math.Clamp(a, 0f, 1f);
        b = Math.Clamp(b, 0f, 1f);

        // Multilinear interpolation
        return W00 * (1 - a) * (1 - b) +
               W10 * a * (1 - b) +
               W01 * (1 - a) * b +
               W11 * a * b;
    }

    // ================================================================
    //  STATIC DEFINITIONS OF THE 16 CORNERS
    // ================================================================

    public static ContinuousGate False => new ContinuousGate(0, 0, 0, 0);
    public static ContinuousGate And => new ContinuousGate(0, 0, 0, 1);
    public static ContinuousGate Xor => new ContinuousGate(0, 1, 1, 0);
    public static ContinuousGate Or => new ContinuousGate(0, 1, 1, 1);
    public static ContinuousGate True => new ContinuousGate(1, 1, 1, 1);
    // ... the rest follow the same bit pattern mapping

    // ================================================================
    //  THE TRANSITION ENGINE (Morphing Gates)
    // ================================================================

    /// <summary>
    /// Interpolates between two logic gates by a factor of t (0.0 to 1.0).
    /// </summary>
    public static ContinuousGate Lerp(ContinuousGate gate1, ContinuousGate gate2, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new ContinuousGate(
            gate1.W00 + (gate2.W00 - gate1.W00) * t,
            gate1.W10 + (gate2.W10 - gate1.W10) * t,
            gate1.W01 + (gate2.W01 - gate1.W01) * t,
            gate1.W11 + (gate2.W11 - gate1.W11) * t
        );
    }
}







// this is probabilistic bools that combine to emulate multiplication, and ideally cover all ops in one swoop.
// Essentially multiplying complex numbers on different axis is an 'OR', and probabilities allow building a truthSpace that covers both, AB, BA, and neither possiblities as a result.

/* E.g.
 If an AI is evaluating two competing perspectives—say, 
 the probability of a door being a standard height (A = 80% confident) 
 and the proportion estimate from an image (B = 60% confident)—it multiplies them to synthesize the space:

var doorStandard = new ContinuousBool(0.80f);
var imageEstimate = new ContinuousBool(0.60f);

// Multiply segments to create the TruthSpace
var logicSpace = doorStandard * imageEstimate; 

// The space is automatically partitioned:
// Q1 (Both right): 0.48
// Q2 (Door standard right, image wrong): 0.32
// Q3 (Image right, door standard wrong): 0.12
// Q4 (Both wrong): 0.08

// The AI can now query the space for specific tensions:
float agreement = logicSpace.XNOR; // Q1 + Q4 = 0.56 (56% chance they align)
float tension = logicSpace.XOR;    // Q2 + Q3 = 0.44 (44% chance they conflict)


No Logic Branches: Notice there are no if/else statements anywhere in the Boolean operations.
Everything is solved algebraically.The CPU just does a few fast floating-point multiplications and additions.

Smooth Gradients: Because the outputs are continuous floats, you can feed these directly into your Resolvers or Validity properties in your Box interface.
If logicSpace.XOR(tension) is high, the Box can push back mathematically on its parent nodes to reduce that tension over time.

Information Preservation: logicSpace.A reconstructs the exact original value of A ($Q_1 + Q_2 = 0.48 + 0.32 = 0.80$).
Merging the dimensions didn't destroy the 1D inputs; they are fully preserved inside the 2D area.

*/




/// <summary>
/// Represents a 1D segment bounded between 0 and 1.
/// This acts as a continuous probability or "confidence" level.
/// </summary>
public readonly struct ContinuousBool
{
    public float Value { get; }
    public float NotValue => 1f - Value;

    public ContinuousBool(float value)
    {
        // Ensure the value stays within the 0 to 1 bounds
        Value = Math.Clamp(value, 0f, 1f);
    }

    // Synthesizing a higher-dimensional space:
    // Multiplying two 1D segments creates a 2D TruthSpace container.
    public static TruthSpace operator *(ContinuousBool a, ContinuousBool b)
    {
        return new TruthSpace(a, b);
    }

    public override string ToString() => $"{Value:P0}";
}

/// <summary>
/// The 2D container synthesized by multiplying two ContinuousBools.
/// It partitions the unit square into 4 continuous quadrants.
/// </summary>
public readonly struct TruthSpace
{
    // --- THE 4 FUNDAMENTAL QUADRANTS ---
    // These will always sum to exactly 1.0 (100% of the possibility space)

    public float Q1 { get; } // A and B (Both True)
    public float Q2 { get; } // A and Not B (A True, B False)
    public float Q3 { get; } // Not A and B (A False, B True)
    public float Q4 { get; } // Not A and Not B (Both False)

    public TruthSpace(ContinuousBool a, ContinuousBool b)
    {
        Q1 = a.Value * b.Value;
        Q2 = a.Value * b.NotValue;
        Q3 = a.NotValue * b.Value;
        Q4 = a.NotValue * b.NotValue;
    }

    // --- THE 16 BOOLEAN OPERATORS ---
    // Expressed as the sum of specific geometric areas.
    // Instead of returning True/False, they return the continuous probability (0f to 1f) of that state.

    // 0 Areas
    public float False => 0f;                                  // 0000

    // 1 Area (Intersections)
    public float AND => Q1;                                  // 1000
    public float A_And_Not_B => Q2;                                  // 0100
    public float Not_A_And_B => Q3;                                  // 0010
    public float NOR => Q4;                                  // 0001

    // 2 Areas (1D Projections and Equivalencies)
    public float A => Q1 + Q2;                             // 1100 (Recovers Segment A)
    public float B => Q1 + Q3;                             // 1010 (Recovers Segment B)
    public float Not_A => Q3 + Q4;                             // 0011
    public float Not_B => Q2 + Q4;                             // 0101
    public float XOR => Q2 + Q3;                             // 0110 (Tension / Disagreement)
    public float XNOR => Q1 + Q4;                             // 1001 (Equivalence / Agreement)

    // 3 Areas (Unions)
    public float OR => Q1 + Q2 + Q3;                        // 1110
    public float A_Or_Not_B => Q1 + Q2 + Q4;                        // 1101 (Implies B -> A)
    public float Not_A_Or_B => Q1 + Q3 + Q4;                        // 1011 (Implies A -> B)
    public float NAND => Q2 + Q3 + Q4;                        // 0111

    // 4 Areas
    public float True => Q1 + Q2 + Q3 + Q4;                   // 1111 (Always 1.0)
}