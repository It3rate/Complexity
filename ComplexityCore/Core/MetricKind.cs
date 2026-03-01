namespace Complexity.Core;

/// <summary>
/// The distance function (metric) used to measure separation between values in a frame.
///
/// Affects:
///   - CValue.Distance()              — how far apart two values are
///   - CCollection.PathLength()       — total length of a trajectory
///   - CCollection.Spread()           — distance of each sample from the mean
///
/// All three metrics agree when all component differences are zero (distance = 0).
/// They differ in how they combine the per-axis component differences.
///
/// Cyclic axes use the shortest-arc distance regardless of the metric chosen:
/// the per-axis difference is always the minimum of going "the short way" or "the long way"
/// around the cycle. The metric then combines those per-axis differences.
/// </summary>
public enum MetricKind
{
    /// <summary>
    /// L² norm: √(Σ dᵢ²). Standard "as-the-crow-flies" distance.
    /// A circle is geometrically round. Default for most frames.
    /// Natural for continuous spatial quantities.
    /// </summary>
    Euclidean,

    /// <summary>
    /// L¹ norm: Σ |dᵢ|. Taxicab / city-block distance.
    /// A "circle" (equidistant set) looks like a diamond.
    /// Natural for grid-aligned navigation where diagonal movement is not allowed.
    /// </summary>
    Manhattan,

    /// <summary>
    /// L∞ norm: max(|dᵢ|). Chessboard / king-move distance.
    /// A "circle" (equidistant set) looks like a square.
    /// Natural when the worst single-axis error dominates (matches Tension's per-axis logic).
    /// </summary>
    Chebyshev,
}
