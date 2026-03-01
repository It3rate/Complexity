namespace Complexity.Core;

/// <summary>
/// Describes what physical or conceptual quantity an axis measures.
/// Used to infer valid collection operations and guide interpretation.
///
/// Does NOT affect arithmetic — this is semantic metadata that lives alongside
/// the mathematical structure (bounds, resolution, direction, IsCyclic).
///
/// The topology of the domain (cyclic vs linear) is already captured by IsCyclic.
/// AxisKind captures what *kind of thing* is being measured.
/// </summary>
public enum AxisKind
{
    /// <summary>
    /// A spatial dimension: position, length, distance, area.
    /// Supports distance, displacement, and geometric operations.
    /// This is the default for most axes.
    /// </summary>
    Spatial,

    /// <summary>
    /// A temporal dimension: elapsed time, sequence index, frame number.
    /// Signals that variation along this axis is time-ordered.
    /// Enables Trajectory interpretation in CCollection: if any axis is Temporal,
    /// samples varying along it are from the same entity over time.
    /// </summary>
    Temporal,

    /// <summary>
    /// An intensity or scalar quality: brightness, temperature, mass, pressure.
    /// Does not represent a spatial direction — adding two temperatures
    /// requires care about affine semantics (see AffineKind).
    /// </summary>
    Intensity,

    /// <summary>
    /// An ordinal or categorical dimension: rank, category, enumerated slot.
    /// Order matters but differences are not meaningful as magnitudes.
    /// Example: colour channel index (R=0, G=1, B=2).
    /// </summary>
    Ordinal,
}
