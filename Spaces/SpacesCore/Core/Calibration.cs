namespace Spaces.Core;

/// <summary>
/// Describes the mathematical relationship between a child <see cref="IValueSpace"/>
/// and its parent.  Enables sub-spaces that use different units, scales, origins,
/// or resolutions than their parent.
///
/// ── MOTIVATION ───────────────────────────────────────────────────────────────
///
/// When a <see cref="SpaceValue"/> acts as an <see cref="IValueSpace"/> for
/// sub-values, those sub-values often live in a DIFFERENT coordinate frame than
/// the parent <see cref="Space"/>.  Classic examples:
///
///   • World Space in kilometres; City sub-space in metres
///     (Scale = 1000 m/km, Origin = city-centre in parent ticks)
///
///   • Audio timeline in seconds; Measure sub-space in beats
///     (Scale = beats-per-second, Origin = bar-start tick)
///
///   • Screen canvas in pixels; Widget sub-space in relative % units
///     (Scale = widget_size / 100, Origin = widget top-left pixel)
///
/// Even when the UNITS are the same (e.g. both in metres), the origin may
/// differ (the sub-space's zero is at some non-zero parent position).
///
/// ── PLANNED STRUCTURE ────────────────────────────────────────────────────────
///
///   OriginInParent  — SpaceValue giving the parent-coordinate position of
///                     this sub-space's origin.  For a city sub-space at
///                     lat/lon (51.5, -0.1) km from the world origin, this
///                     would be a SpaceValue with those ticks.
///
///   Scale           — double[] (one per axis).  Ratio: 1 child unit = Scale
///                     parent units.  E.g. Scale = 1000.0 when child is metres
///                     and parent is kilometres.
///
///   ChildTicksPerUnit — long[] (one per axis).  The resolution of the child
///                     space.  May differ from the parent.  E.g. parent has
///                     tpu = 1 (one tick per km), child has tpu = 100 (one
///                     tick per centimetre of metre-space).
///
/// ── PLANNED TRANSFORM OPERATIONS ─────────────────────────────────────────────
///
///   ChildToParent(SpaceValue childValue) → SpaceValue
///     parentTicks = OriginInParentTicks
///                 + (childTicks / ChildTicksPerUnit) * Scale * ParentTicksPerUnit
///
///   ParentToChild(SpaceValue parentValue) → SpaceValue
///     childTicks = (parentTicks - OriginInParentTicks)
///                  / ParentTicksPerUnit / Scale * ChildTicksPerUnit
///
/// ── PLANNED RECALIBRATION OPERATIONS ─────────────────────────────────────────
///
///   WithOrigin(SpaceValue newOrigin) → Calibration
///     Returns a new Calibration with the origin shifted to newOrigin.
///     Use to "re-zero" the sub-space after a measurement reference changes.
///
///   WithScale(double[] newScale) → Calibration
///     Returns a new Calibration with updated unit ratios.
///     Use to zoom in/out or change physical unit system.
///
///   WithResolution(long[] newTicksPerUnit) → Calibration
///     Returns a new Calibration with finer or coarser resolution.
///     Use to increase precision without changing the scale.
///
///   WithExtendedBounds(double[] newRangeMax) → Calibration
///     Returns a new Calibration allowing a larger magnitude range.
///     Sub-values that were in tension may become in-bounds after this.
///
/// ── INTEGRATION WITH IValueSpace ─────────────────────────────────────────────
///
/// A future CalibratedValueSpace class will:
///   • Wrap a SpaceValue (its "anchor" in the parent Space)
///   • Hold a Calibration
///   • Implement IValueSpace, exposing its own Axes (built from ChildTicksPerUnit)
///   • Route CreateValue / AddValue through ChildToParent transforms when
///     populating the parent Space
///   • Expose ParentToChild access for reading parent values in child units
///
/// This will allow the following pattern:
///
///   var world     = Space.Segment(40_000.0, 1_000L, "world_km");
///   var cityAnchor = world.Value(5180.0, 5200.0); // London area in km
///   var citySpace  = CalibratedValueSpace.Create(
///       cityAnchor, scale: 1000.0, childTpu: 100_000L, "london_m");
///   var landmark   = citySpace.CreateValue(500.0, 200.0); // metres from city origin
///
/// The landmark's absolute position in world ticks is determined automatically
/// by the Calibration, without the caller needing to know the conversion details.
///
/// ── IMPLEMENTATION STATUS ────────────────────────────────────────────────────
///
/// This class is a placeholder.  The fields, constructor, and methods described
/// above are not yet implemented.  The architecture is locked as described here
/// and the IValueSpace interface is designed to accommodate it without changes.
/// </summary>
public class Calibration
{
    // ── Planned fields (to be implemented) ───────────────────────────────────
    //
    //   public SpaceValue OriginInParent { get; }
    //   public IReadOnlyList<double> Scale { get; }
    //   public IReadOnlyList<long> ChildTicksPerUnit { get; }
    //
    // ── Planned factories (to be implemented) ─────────────────────────────────
    //
    //   public static Calibration Identity(Space parentSpace)
    //   public Calibration WithOrigin(SpaceValue newOrigin)
    //   public Calibration WithScale(params double[] newScale)
    //   public Calibration WithResolution(params long[] newTicksPerUnit)
    //   public Calibration WithExtendedBounds(params double[] newRangeMax)
    //
    // ── Planned transforms (to be implemented) ────────────────────────────────
    //
    //   public SpaceValue ChildToParent(SpaceValue childValue)
    //   public SpaceValue ParentToChild(SpaceValue parentValue)
}
