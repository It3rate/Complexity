namespace Spaces.Core;

/// <summary>
/// One row of a <see cref="Space"/>'s multiplication table.
///
/// Describes how two "components" of a value combine during multiplication:
///
///   result_component[ResultIdx] += Sign × (A[LeftIdx] × B[RightIdx])
///
/// ── COMPONENT INDEXING ──────────────────────────────────────────────────────
///
/// Flat component indices map to Focal endpoints as follows:
///   For axis k:  lo-component = 2k,   hi-component = 2k+1
///
///   One-axis  space (segment): 0=axis0.lo, 1=axis0.hi
///   Two-axis  space (grid):    0=x.lo, 1=x.hi, 2=y.lo, 3=y.hi
///
/// ── SEGMENT / COMPLEX ALGEBRA ───────────────────────────────────────────────
///
///   A segment value is a complex number  v = lo·(−i) + hi·(+1)
///   (lo is the i-coefficient, hi is the real coefficient).
///
///   Complex multiplication (a_r + a_i·i)(b_r + b_i·i):
///     r-result = a_r·b_r − a_i·b_i   →  entries (+1,+1,+1,+1) and (0,0,1,−1)
///     i-result = a_r·b_i + a_i·b_r   →  entries (1,0,0,+1) and (0,1,0,+1)
///
///   (Here 0=lo/i-component, 1=hi/r-component for a one-axis space.)
///
/// ── NORMALISATION ───────────────────────────────────────────────────────────
///
/// The accumulated product in ticks is divided by BasisTicks before writing
/// back to the result Focal, keeping the units consistent.
/// </summary>
/// <param name="LeftIdx">Flat component index of the left operand.</param>
/// <param name="RightIdx">Flat component index of the right operand.</param>
/// <param name="ResultIdx">Flat component index of the accumulation target.</param>
/// <param name="Sign">+1 or −1 (the contribution sign for this entry).</param>
public readonly record struct AlgebraEntry(int LeftIdx, int RightIdx, int ResultIdx, int Sign)
{
    public override string ToString() =>
        $"c[{LeftIdx}] × c[{RightIdx}] → {(Sign >= 0 ? "+" : "−")}c[{ResultIdx}]";
}
