namespace Complexity.Core;

// ════════════════════════════════════════════════════════════════════════════
//  TwinSquareGeometry
//  Geometric helpers for the Twin-Square space (CSpace.TwinSquare).
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Geometric helper class for the Twin-Square space (<see cref="CSpace.TwinSquare"/>).
///
/// ── WHAT IS THE TWIN-SQUARE SPACE? ──────────────────────────────────────────
///
/// Two ordinary 2D "square" planes (90° between their own axes) are joined at
/// the origin. One is rotated 90° relative to the other. The resulting 3D space
/// has 4 oblique axes, and every cross-plane axis pair is at exactly 120°.
///
/// The four basis vectors embedded in R³:
///
///   Plane A (XY plane):
///     A1 = ( 1,    0,     0    )   →  the standard +X direction
///     A2 = ( 0,    1,     0    )   →  the standard +Y direction
///
///   Plane B (tilted 90° to Plane A, sharing only the origin):
///     B1 = (−0.5, −0.5,  1/√2 )   →  into the −X−Y+Z octant
///     B2 = (−0.5, −0.5, −1/√2 )   →  into the −X−Y−Z octant
///
/// Verified angle properties (all exact, not approximate):
///   A1·A2 = 0        →  90°  (square within Plane A)
///   B1·B2 = 0        →  90°  (square within Plane B)
///   A1·B1 = −0.5     →  120° (cross-plane)
///   A1·B2 = −0.5     →  120°
///   A2·B1 = −0.5     →  120°
///   A2·B2 = −0.5     →  120°
///
///   A1+A2+B1+B2 = (0,0,0)  →  vector equilibrium (perfectly balanced)
///
/// ── WHAT A VALUE REPRESENTS ─────────────────────────────────────────────────
///
/// A CValue in this space has 8 components (4 lo/hi pairs):
///   Index 0 = lo_a1,  1 = hi_a1   ← extent along the A1 direction
///   Index 2 = lo_a2,  3 = hi_a2   ← extent along the A2 direction
///   Index 4 = lo_b1,  5 = hi_b1   ← extent along the B1 direction
///   Index 6 = lo_b2,  7 = hi_b2   ← extent along the B2 direction
///
/// The 8 "extreme points" in R³ are:
///   hi_a1·A1,  lo_a1·A1   ← the ±a₁ spoke tips
///   hi_a2·A2,  lo_a2·A2   ← the ±a₂ spoke tips
///   hi_b1·B1,  lo_b1·B1   ← the ±b₁ spoke tips  (lo is neg, so flips direction)
///   hi_b2·B2,  lo_b2·B2   ← the ±b₂ spoke tips
///
/// Their convex hull forms a 16-edge polytope: 4 edges within each plane
/// (a diamond shape), plus 8 cross-plane "bridge" edges.
///
/// ── INTERSECTION AND TENSION ────────────────────────────────────────────────
///
/// Intersection is component-wise on each axis pair (same as the 1D segment
/// model). If any pair has an empty intersection (lo > hi), the values do not
/// overlap on that axis — a "non-intersection tension state."
///
/// ── AI / FUTURE READER NOTE ─────────────────────────────────────────────────
///
/// To reproduce the basis vectors algebraically:
///   "Given a₁=(1,0,0) and a₂=(0,1,0), find b₁ and b₂ such that:
///    (1) b₁·b₂=0  (2) |b₁|=|b₂|=1  (3) a_i·b_j=−½ for all i,j.
///    Solution: b₁=(−½,−½,1/√2), b₂=(−½,−½,−1/√2).
///    Note: a₁+a₂+b₁+b₂=(0,0,0) — this is a property of the solution,
///    not an input constraint."
/// </summary>
public static class TwinSquareGeometry
{
    // ── Component index constants ────────────────────────────────────────────
    // Use these names everywhere instead of raw integers to keep code readable.

    /// <summary>Index of the lo (unot) component along axis a₁.</summary>
    public const int LoA1 = 0;
    /// <summary>Index of the hi (unit) component along axis a₁.</summary>
    public const int HiA1 = 1;
    /// <summary>Index of the lo (unot) component along axis a₂.</summary>
    public const int LoA2 = 2;
    /// <summary>Index of the hi (unit) component along axis a₂.</summary>
    public const int HiA2 = 3;
    /// <summary>Index of the lo (unot) component along axis b₁.</summary>
    public const int LoB1 = 4;
    /// <summary>Index of the hi (unit) component along axis b₁.</summary>
    public const int HiB1 = 5;
    /// <summary>Index of the lo (unot) component along axis b₂.</summary>
    public const int LoB2 = 6;
    /// <summary>Index of the hi (unit) component along axis b₂.</summary>
    public const int HiB2 = 7;

    // ── The four basis vectors embedded in R³ ───────────────────────────────
    //
    // These are the actual geometric directions of the 4 axes in 3D Euclidean
    // space. All arithmetic on "values" in this space is done in oblique
    // coordinates (components 0-7 of a CValue), but any geometric computation
    // (distance, intersection shape, rendering) requires converting to Cartesian
    // R³ using these vectors.
    //
    // Mathematical derivation:
    //   A1 and A2 are the standard XY basis — chosen freely.
    //   B1 and B2 are then uniquely determined (up to ±Z sign choice) by the
    //   constraints: |B1|=1, |B2|=1, B1·B2=0, and A_i·B_j=cos(120°)=−½.

    /// <summary>
    /// Basis vector for the a₁ axis: the +X direction in R³.
    /// a₁ is one of the two orthogonal axes of Plane A (the XY plane).
    /// </summary>
    public static readonly (double x, double y, double z) A1 = (1.0, 0.0, 0.0);

    /// <summary>
    /// Basis vector for the a₂ axis: the +Y direction in R³.
    /// a₂ is the other orthogonal axis of Plane A.
    /// a₁·a₂ = 0  →  90° within Plane A (it really is a square).
    /// </summary>
    public static readonly (double x, double y, double z) A2 = (0.0, 1.0, 0.0);

    /// <summary>
    /// Basis vector for the b₁ axis: (−½, −½, 1/√2) in R³.
    /// b₁ is one of the two orthogonal axes of Plane B.
    ///
    /// The −½ components push into the −X−Y quadrant (the "back-left" in XY).
    /// The +1/√2 Z component lifts it above the XY plane.
    ///
    /// Key relationships:
    ///   b₁·b₂ = 0         →  90° within Plane B
    ///   a₁·b₁ = −½        →  120° to a₁
    ///   a₂·b₁ = −½        →  120° to a₂
    ///   |b₁|  = 1         →  unit length (verified: ¼+¼+½ = 1)
    /// </summary>
    public static readonly (double x, double y, double z) B1 = (-0.5, -0.5,  Inv_Sqrt2);

    /// <summary>
    /// Basis vector for the b₂ axis: (−½, −½, −1/√2) in R³.
    /// b₂ is the other orthogonal axis of Plane B.
    ///
    /// Identical XY components to b₁, but Z flipped — this is the "mirror"
    /// of b₁ across the XY plane. Together b₁ and b₂ span Plane B, which
    /// is perpendicular to Plane A and shares only the origin with it.
    ///
    /// Key relationships:
    ///   b₁·b₂ = ¼+¼−½ = 0  →  90° (orthogonal within Plane B)
    ///   a₁·b₂ = −½          →  120° to a₁
    ///   a₂·b₂ = −½          →  120° to a₂
    /// </summary>
    public static readonly (double x, double y, double z) B2 = (-0.5, -0.5, -Inv_Sqrt2);

    // 1/√2 ≈ 0.70710678 — used in the B-plane vectors and distance calculations
    private const double Inv_Sqrt2 = 0.7071067811865476;

    // ════════════════════════════════════════════════════════════════════════
    //  VERTEX / EXTREME-POINT GEOMETRY
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Compute the 8 "extreme point" positions in R³ for a given value.
    ///
    /// Each of the 4 axis directions contributes two extreme points: one in
    /// the positive direction (hi component × basis vector) and one in the
    /// negative direction (lo component × basis vector, where lo is typically
    /// a negative number so lo·vector points the other way).
    ///
    /// Return order:
    ///   [0] = hi_a1 · A1    (+a₁ tip)
    ///   [1] = lo_a1 · A1    (−a₁ tip, lo is negative)
    ///   [2] = hi_a2 · A2    (+a₂ tip)
    ///   [3] = lo_a2 · A2    (−a₂ tip)
    ///   [4] = hi_b1 · B1    (+b₁ tip)
    ///   [5] = lo_b1 · B1    (−b₁ tip, since lo is negative this points in +b₁ direction's opposite)
    ///   [6] = hi_b2 · B2    (+b₂ tip)
    ///   [7] = lo_b2 · B2    (−b₂ tip)
    ///
    /// For the "regular" value (all hi = r, all lo = −r), the 8 points are
    /// all at distance r from the origin (equidistant), forming the most
    /// balanced configuration of the space.
    /// </summary>
    public static (double x, double y, double z)[] GetExtreme8(CValue v)
    {
        double ha1 = v.GetValue(HiA1), la1 = v.GetValue(LoA1);
        double ha2 = v.GetValue(HiA2), la2 = v.GetValue(LoA2);
        double hb1 = v.GetValue(HiB1), lb1 = v.GetValue(LoB1);
        double hb2 = v.GetValue(HiB2), lb2 = v.GetValue(LoB2);

        return new[]
        {
            Scale(A1, ha1),   // [0] +a₁ extreme  (hi_a1, 0, 0)
            Scale(A1, la1),   // [1] −a₁ extreme  (lo_a1, 0, 0)  lo is negative → left
            Scale(A2, ha2),   // [2] +a₂ extreme  (0, hi_a2, 0)
            Scale(A2, la2),   // [3] −a₂ extreme  (0, lo_a2, 0)  lo is negative → down
            Scale(B1, hb1),   // [4] +b₁ extreme  hi_b1·(−½,−½,1/√2) → back-left-up
            Scale(B1, lb1),   // [5] −b₁ extreme  lo_b1·(−½,−½,1/√2) → front-right-down
            Scale(B2, hb2),   // [6] +b₂ extreme  hi_b2·(−½,−½,−1/√2) → back-left-down
            Scale(B2, lb2),   // [7] −b₂ extreme  lo_b2·(−½,−½,−1/√2) → front-right-up
        };
    }

    /// <summary>
    /// The 16 edges of the Twin-Square polytope, given as index pairs into
    /// the GetExtreme8 array.
    ///
    /// Structure:
    ///   4 edges form the diamond within Plane A: 0→2→1→3→0
    ///   4 edges form the diamond within Plane B: 4→6→5→7→4
    ///   8 "bridge" edges connect each plane's spokes to the near spokes of
    ///     the other plane (near = 120° apart, not 60°):
    ///       0(+a₁)↔5(−b₁), 0(+a₁)↔7(−b₂)  — +a goes to −b (they point "toward" each other)
    ///       2(+a₂)↔5(−b₁), 2(+a₂)↔7(−b₂)
    ///       1(−a₁)↔4(+b₁), 1(−a₁)↔6(+b₂)  — −a goes to +b
    ///       3(−a₂)↔4(+b₁), 3(−a₂)↔6(+b₂)
    ///
    /// Total: 4 + 4 + 8 = 16 edges, 8 vertices → 10 faces (Euler: V−E+F=2).
    /// This is the "decahedron" shape of the regular twin-square.
    /// </summary>
    public static readonly (int a, int b)[] Edges =
    {
        // Plane A diamond (4 edges): connect +a₁ → +a₂ → −a₁ → −a₂ → back
        (0, 2), (2, 1), (1, 3), (3, 0),

        // Plane B diamond (4 edges): connect +b₁ → +b₂ → −b₁ → −b₂ → back
        (4, 6), (6, 5), (5, 7), (7, 4),

        // Bridge edges (8): each +a spoke → both − b spokes (nearest neighbours)
        (0, 5), (0, 7),   // +a₁ bridges to −b₁, −b₂
        (2, 5), (2, 7),   // +a₂ bridges to −b₁, −b₂
        // Each −a spoke → both +b spokes
        (1, 4), (1, 6),   // −a₁ bridges to +b₁, +b₂
        (3, 4), (3, 6),   // −a₂ bridges to +b₁, +b₂
    };

    /// <summary>
    /// Vertices that face the viewer in a standard isometric projection
    /// (viewing from the +X+Y+Z direction). Front-facing spokes are:
    ///   0 (+a₁) — extends to the right in isometric
    ///   2 (+a₂) — extends upward in isometric
    ///   5 (−b₁) — lo_b1 is negative, so this points in the +X+Y−Z direction (front-right)
    ///   7 (−b₂) — lo_b2 is negative, so this points in the +X+Y+Z direction (front-right-up)
    /// Back-facing (at 35-40% opacity for depth cuing): 1, 3, 4, 6.
    /// </summary>
    public static readonly HashSet<int> FrontVertices = new() { 0, 2, 5, 7 };

    // ════════════════════════════════════════════════════════════════════════
    //  REGULARITY
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// True when the value is "regular" — the most balanced state of the space.
    ///
    /// A regular value has all four axis pairs at the same symmetric extent:
    ///   hi_a1 = hi_a2 = hi_b1 = hi_b2 = r   (same positive radius)
    ///   lo_a1 = lo_a2 = lo_b1 = lo_b2 = −r  (symmetric around origin)
    ///
    /// In this state all 8 extreme points are at the same distance r from
    /// the origin, and the vector equilibrium property is perfectly expressed.
    ///
    /// Analogous to a regular octahedron in the old space, or a regular
    /// segment [−r, r] in the 1D segment space.
    /// </summary>
    public static bool IsRegular(CValue v, double tolerance = 1e-9)
    {
        double r = v.GetValue(HiA1);
        return r > 0
            // All four hi values must equal r
            && Math.Abs(v.GetValue(HiA2) - r) < tolerance
            && Math.Abs(v.GetValue(HiB1) - r) < tolerance
            && Math.Abs(v.GetValue(HiB2) - r) < tolerance
            // All four lo values must equal −r (symmetric)
            && Math.Abs(v.GetValue(LoA1) + r) < tolerance
            && Math.Abs(v.GetValue(LoA2) + r) < tolerance
            && Math.Abs(v.GetValue(LoB1) + r) < tolerance
            && Math.Abs(v.GetValue(LoB2) + r) < tolerance;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PLANE EXTENT EXTRACTION
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Extract the Plane A extents (lo/hi along a₁ and a₂).
    /// Plane A is the XY plane. The extents define the reach of the value
    /// along each of Plane A's two axes.
    /// Returns (lo_a1, hi_a1, lo_a2, hi_a2).
    /// </summary>
    public static (double loA1, double hiA1, double loA2, double hiA2) PlaneAExtents(CValue v) =>
        (v.GetValue(LoA1), v.GetValue(HiA1), v.GetValue(LoA2), v.GetValue(HiA2));

    /// <summary>
    /// Extract the Plane B extents (lo/hi along b₁ and b₂).
    /// Plane B is the tilted plane spanned by b₁ and b₂.
    /// Returns (lo_b1, hi_b1, lo_b2, hi_b2).
    /// </summary>
    public static (double loB1, double hiB1, double loB2, double hiB2) PlaneBExtents(CValue v) =>
        (v.GetValue(LoB1), v.GetValue(HiB1), v.GetValue(LoB2), v.GetValue(HiB2));

    // ════════════════════════════════════════════════════════════════════════
    //  INTERSECTION
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// True if the two values overlap on all four axis pairs.
    ///
    /// "Overlap" is defined component-wise in oblique coordinates: two values
    /// intersect when their extents along every axis direction share at least
    /// one point. A non-intersection on any pair puts the values in a
    /// "cross-tension" state (analogous to a segment value exceeding its bounds).
    /// </summary>
    public static bool Intersects(CValue a, CValue b) =>
        IntervalsOverlap(a.GetValue(LoA1), a.GetValue(HiA1), b.GetValue(LoA1), b.GetValue(HiA1)) &&
        IntervalsOverlap(a.GetValue(LoA2), a.GetValue(HiA2), b.GetValue(LoA2), b.GetValue(HiA2)) &&
        IntervalsOverlap(a.GetValue(LoB1), a.GetValue(HiB1), b.GetValue(LoB1), b.GetValue(HiB1)) &&
        IntervalsOverlap(a.GetValue(LoB2), a.GetValue(HiB2), b.GetValue(LoB2), b.GetValue(HiB2));

    /// <summary>
    /// Compute the axis-wise intersection of two values.
    ///
    /// For each axis pair: intersection = [max(lo_a, lo_b), min(hi_a, hi_b)].
    /// Returns null if any pair is empty (lo > hi after intersection) — meaning
    /// the two values are disjoint in that direction (a cross-tension state).
    ///
    /// When both values are regular and identical, the result is also regular.
    /// When one value is stretched relative to the other, the result is the
    /// "trimmed" overlap — neither regular nor guaranteed to be symmetric.
    /// </summary>
    public static CValue? Intersect(CValue a, CValue b)
    {
        double loA1 = Math.Max(a.GetValue(LoA1), b.GetValue(LoA1));
        double hiA1 = Math.Min(a.GetValue(HiA1), b.GetValue(HiA1));
        double loA2 = Math.Max(a.GetValue(LoA2), b.GetValue(LoA2));
        double hiA2 = Math.Min(a.GetValue(HiA2), b.GetValue(HiA2));
        double loB1 = Math.Max(a.GetValue(LoB1), b.GetValue(LoB1));
        double hiB1 = Math.Min(a.GetValue(HiB1), b.GetValue(HiB1));
        double loB2 = Math.Max(a.GetValue(LoB2), b.GetValue(LoB2));
        double hiB2 = Math.Min(a.GetValue(HiB2), b.GetValue(HiB2));

        // Any empty interval → no intersection (cross-tension state)
        if (loA1 > hiA1 || loA2 > hiA2 || loB1 > hiB1 || loB2 > hiB2) return null;

        return a.Space.Value(loA1, hiA1, loA2, hiA2, loB1, hiB1, loB2, hiB2);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CROSS-VALUE TENSION (gap between two separate values)
    // ════════════════════════════════════════════════════════════════════════
    //
    // "Cross-tension" measures how far apart two values are — zero when they
    // overlap, positive when they are disjoint on at least one axis pair.
    // This is the inter-value equivalent of the per-value Tension that
    // measures how far a value exceeds its space's bounds.

    /// <summary>Gap on the a₁ axis pair. 0 when the intervals overlap.</summary>
    public static double GapA1(CValue a, CValue b) =>
        Math.Max(0.0,
            Math.Max(a.GetValue(LoA1), b.GetValue(LoA1))
            - Math.Min(a.GetValue(HiA1), b.GetValue(HiA1)));

    /// <summary>Gap on the a₂ axis pair.</summary>
    public static double GapA2(CValue a, CValue b) =>
        Math.Max(0.0,
            Math.Max(a.GetValue(LoA2), b.GetValue(LoA2))
            - Math.Min(a.GetValue(HiA2), b.GetValue(HiA2)));

    /// <summary>Gap on the b₁ axis pair.</summary>
    public static double GapB1(CValue a, CValue b) =>
        Math.Max(0.0,
            Math.Max(a.GetValue(LoB1), b.GetValue(LoB1))
            - Math.Min(a.GetValue(HiB1), b.GetValue(HiB1)));

    /// <summary>Gap on the b₂ axis pair.</summary>
    public static double GapB2(CValue a, CValue b) =>
        Math.Max(0.0,
            Math.Max(a.GetValue(LoB2), b.GetValue(LoB2))
            - Math.Min(a.GetValue(HiB2), b.GetValue(HiB2)));

    /// <summary>
    /// Total cross-tension: sum of all four axis-pair gaps.
    /// 0.0  = the two values have a perfect intersection on every axis pair.
    /// &gt; 0  = partial or total non-intersection; larger = further apart.
    /// </summary>
    public static double CrossTension(CValue a, CValue b) =>
        GapA1(a, b) + GapA2(a, b) + GapB1(a, b) + GapB2(a, b);

    // ════════════════════════════════════════════════════════════════════════
    //  ANGLE / VERIFICATION HELPERS  (useful for tests and documentation)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Compute the angle in degrees between two 3D vectors.
    /// Uses the dot-product formula: θ = arccos(a·b / |a||b|).
    /// Clamped to [−1,1] to handle floating-point noise.
    /// </summary>
    public static double AngleDegrees(
        (double x, double y, double z) u,
        (double x, double y, double z) w)
    {
        double dot  = u.x*w.x + u.y*w.y + u.z*w.z;
        double lenU = Math.Sqrt(u.x*u.x + u.y*u.y + u.z*u.z);
        double lenW = Math.Sqrt(w.x*w.x + w.y*w.y + w.z*w.z);
        if (lenU < 1e-15 || lenW < 1e-15) return double.NaN;
        return Math.Acos(Math.Clamp(dot / (lenU * lenW), -1.0, 1.0)) * (180.0 / Math.PI);
    }

    /// <summary>
    /// Dot product of two 3D vectors.
    /// The cross-plane dot products all equal −0.5 (= cos 120°).
    /// </summary>
    public static double Dot(
        (double x, double y, double z) u,
        (double x, double y, double z) w) =>
        u.x*w.x + u.y*w.y + u.z*w.z;

    // ── Internal helpers ─────────────────────────────────────────────────────

    // Scale a basis vector by a scalar to get a 3D Cartesian position.
    // Used to compute the spoke tip positions from axis extents.
    private static (double x, double y, double z) Scale(
        (double x, double y, double z) v, double s) =>
        (v.x * s, v.y * s, v.z * s);

    // Two 1D intervals [lo1,hi1] and [lo2,hi2] overlap when neither is
    // entirely to the left of the other. Touching at a boundary counts as overlap.
    private static bool IntervalsOverlap(double lo1, double hi1, double lo2, double hi2) =>
        hi1 >= lo2 && hi2 >= lo1;
}
