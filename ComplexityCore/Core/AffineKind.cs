namespace Complexity.Core;

/// <summary>
/// Distinguishes absolute positions (points in space) from relative displacements (vectors).
///
/// This is the affine/vector distinction from geometry:
///   - A Position represents "where something is" — an absolute location.
///     Zero means "the origin", a specific place.
///   - A Vector represents "how far and in which direction" — a relative displacement.
///     Zero means "no movement", the additive identity.
///
/// Operation validity rules:
///   Vector   + Vector   → Vector    ✓  (two displacements sum to a displacement)
///   Position + Vector   → Position  ✓  (moving from a location by an offset → new location)
///   Position - Position → Vector    ✓  (the difference of two locations is a displacement)
///   Position + Position → invalid   ✗  (adding two locations has no geometric meaning)
///
/// Use CSpace.ResultAffineKind() to check the validity of an operation before performing it.
/// The arithmetic in CValue is not affected — this is a semantic constraint layer.
///
/// The default is Vector: most frames work with displacements rather than absolute positions.
/// Set AffineType = Position when a frame represents absolute coordinates.
/// </summary>
public enum AffineKind
{
    /// <summary>
    /// Relative displacement: zero means "no movement", not "the origin".
    /// Vector + Vector = Vector. Scalar multiplication is always well-defined.
    /// This is the default for most number frames.
    /// </summary>
    Vector,

    /// <summary>
    /// Absolute position: zero means "the origin", a specific location.
    /// Position + Position is geometrically invalid.
    /// Use a CCollection centroid (Mean) when averaging positions.
    /// Position - Position = Vector (the connecting displacement).
    /// </summary>
    Position,
}
