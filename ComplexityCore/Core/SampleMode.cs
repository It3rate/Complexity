namespace Complexity.Core;

/// <summary>
/// Describes the measurement protocol that generated a CCollection.
/// Determines which collection-level operations are semantically meaningful.
///
/// This is empirically discovered from the measurement context, not chosen arbitrarily:
/// the thing being measured tells you what mode applies.
///
/// Same entity, moving through time  → Trajectory  → Deltas, Displacement, PathLength
/// Different entities, one instant   → Ensemble    → Mean, Spread (population statistics)
/// Regular periodic sampling         → TimeSeries  → all Trajectory ops + periodicity
/// Identity gradually transforming   → Morphogenesis → Certainty tracks identity continuity
///
/// Key insight:
///   "Same entity" + "over time"  → ordering is inherent (time gives it to you)
///   "Different entities"         → ordering is not inherent (there is no natural sequence)
///   "Cyclic path"                → IsClosed = true on the collection (orthogonal to mode)
/// </summary>
public enum SampleMode
{
    /// <summary>
    /// No claim about the measurement protocol.
    /// All collection operations are available without mode validation.
    /// Use this when the mode is genuinely unknown or when validation is not wanted.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The same entity, measured at successive moments in time.
    /// Ordering is inherent: index 0 is the earliest sample, index N-1 is the latest.
    /// Identity is continuous across samples.
    /// Enables: Deltas, Displacement, PathLength.
    /// Example: tracking one dog's position as it walks across a field.
    /// </summary>
    Trajectory,

    /// <summary>
    /// Different entities, all measured at the same moment in time.
    /// No inherent ordering — the list order is arbitrary or by convenience.
    /// Identity varies across samples (each sample is a different thing).
    /// Enables: Mean, Spread (as population statistics).
    /// Example: measuring the eye positions of 10 dogs sitting in a yard.
    /// </summary>
    Ensemble,

    /// <summary>
    /// The same entity, sampled at uniform time intervals. A specialisation of Trajectory.
    /// Enables all Trajectory operations plus periodicity / frequency analysis.
    /// Example: GPS position logged every 1 second.
    /// </summary>
    TimeSeries,

    /// <summary>
    /// Different entities measured over time: distribution varies across both identity and time.
    /// The most general case — effectively a trajectory of ensembles.
    /// Enables all operations but requires care about what "distance" or "displacement" means.
    /// </summary>
    Field,

    /// <summary>
    /// The same entity undergoing categorical transformation (growth, decay, phase change).
    /// Identity continuity varies along the collection — reflected in Certainty on each CValue.
    /// A sharp drop in Certainty indicates a categorical boundary (seed → sprout → plant).
    /// Enables Trajectory operations; Certainty trajectory reveals the transformation structure.
    /// </summary>
    Morphogenesis,
}
