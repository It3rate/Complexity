namespace Spaces.Tests.CoreTests;

/// <summary>
/// Tests for IValueSpace implementation in both Space and SpaceValue.
///
/// HIERARCHY CONCEPTS UNDER TEST:
///
///   Space (root):
///     — Has no parent
///     — CreateValue() creates + registers in Space.Values
///     — Value() is pure factory: does NOT register
///     — AddValue() registers an existing value (must be from this Space)
///
///   SpaceValue (sub-frame):
///     — Has a parent Space (value.Space)
///     — Also implements IValueSpace for sub-value management
///     — CreateValue() creates a sub-value in the SAME parent Space + registers
///       in SpaceValue.Values (the sub-values list)
///     — AddValue() registers an existing SpaceValue in the sub-values list
///
/// CALIBRATION NOTE (future):
///   Currently sub-values share the same Space (same axes, same tpu).
///   Future: attach a Calibration to allow different units/origin for sub-values.
///   See Calibration.cs for the planned architecture.
/// </summary>
[TestClass]
public class ValueManagementTests
{
    // ── Space as IValueSpace ───────────────────────────────────────────────

    [TestMethod]
    public void Space_ImplementsIValueSpace_WithCorrectMetadata()
    {
        IValueSpace ivs = Space.Segment(10.0, 100L);
        Assert.IsNotNull(ivs);
        Assert.HasCount(1, ivs.Axes);
        Assert.AreEqual(100L, ivs.BasisTicks);
        Assert.AreEqual(2, ivs.ComponentCount);
    }

    [TestMethod]
    public void Space_InitiallyHasNoValues()
    {
        var s = Space.Segment(10.0, 100L);
        Assert.IsEmpty(s.Values);
    }

    [TestMethod]
    public void Space_PureValue_DoesNotRegister()
    {
        var s = Space.Segment(10.0, 100L);
        _ = s.Value(2.0, 4.0);
        Assert.IsEmpty(s.Values);
    }

    [TestMethod]
    public void Space_CreateValue_RegistersAndReturns()
    {
        var s = Space.Segment(10.0, 100L);
        var v = s.CreateValue(2.0, 4.0);
        Assert.HasCount(1, s.Values);
        Assert.AreSame(v, s.Values[0]);
        // Value has correct ticks (magnitude convention)
        Assert.AreEqual(200L, v.GetP0(0));
        Assert.AreEqual(400L, v.GetP1(0));
    }

    [TestMethod]
    public void Space_AddValue_RegistersExistingValue()
    {
        var s = Space.Segment(10.0, 100L);
        var v = s.Value(1.0, 2.0);   // pure factory
        Assert.IsEmpty(s.Values);

        s.AddValue(v);
        Assert.HasCount(1, s.Values);
        Assert.AreSame(v, s.Values[0]);
    }

    [TestMethod]
    public void Space_AddValue_WrongSpace_Throws()
    {
        var s1 = Space.Segment(10.0, 100L);
        var s2 = Space.Segment(10.0, 100L);
        var v  = s2.Value(1.0, 2.0);
        Assert.ThrowsExactly<InvalidOperationException>(() => s1.AddValue(v));
    }

    [TestMethod]
    public void Space_MultipleCreateValues_TrackedInOrder()
    {
        var s  = Space.Segment(10.0, 100L);
        var v1 = s.CreateValue(1.0, 2.0);
        var v2 = s.CreateValue(3.0, 4.0);
        var v3 = s.CreateValue(5.0, 6.0);
        Assert.HasCount(3, s.Values);
        Assert.AreSame(v1, s.Values[0]);
        Assert.AreSame(v2, s.Values[1]);
        Assert.AreSame(v3, s.Values[2]);
    }

    // ── SpaceValue as IValueSpace ──────────────────────────────────────────

    [TestMethod]
    public void SpaceValue_ImplementsIValueSpace()
    {
        var s   = Space.Segment(10.0, 100L);
        var v   = s.Value(3.0, 5.0);
        IValueSpace ivs = v;
        Assert.IsNotNull(ivs);
        // Delegates to parent space metadata
        Assert.HasCount(1, ivs.Axes);
        Assert.AreEqual(100L, ivs.BasisTicks);
        Assert.AreEqual(2, ivs.ComponentCount);
    }

    [TestMethod]
    public void SpaceValue_InitiallyHasNoSubValues()
    {
        var s = Space.Segment(10.0, 100L);
        var v = s.Value(3.0, 5.0);
        Assert.IsEmpty(v.Values);
    }

    [TestMethod]
    public void SpaceValue_CreateValue_AddsSubValueToLocalList()
    {
        var s      = Space.Segment(10.0, 100L);
        var parent = s.Value(3.0, 5.0);

        var sub = parent.CreateValue(1.0, 2.0);

        Assert.HasCount(1, parent.Values);
        Assert.AreSame(sub, parent.Values[0]);
    }

    [TestMethod]
    public void SpaceValue_CreateValue_SubValueBelongsToParentSpace()
    {
        // Sub-values are expressed in the same Space (until calibration is added)
        var s      = Space.Segment(10.0, 100L);
        var parent = s.Value(3.0, 5.0);
        var sub    = parent.CreateValue(1.0, 2.0);

        Assert.AreSame(s, sub.Space);
    }

    [TestMethod]
    public void SpaceValue_CreateValue_DoesNotAddToSpaceValues()
    {
        // Sub-values are in the SpaceValue's list, NOT the parent Space's list
        var s      = Space.Segment(10.0, 100L);
        var parent = s.Value(3.0, 5.0);
        _ = parent.CreateValue(1.0, 2.0);

        Assert.IsEmpty(s.Values);   // Space unchanged
        Assert.HasCount(1, parent.Values);
    }

    [TestMethod]
    public void SpaceValue_AddValue_RegistersInSubList()
    {
        var s      = Space.Segment(10.0, 100L);
        var parent = s.Value(3.0, 5.0);
        var extra  = s.Value(7.0, 8.0);

        parent.AddValue(extra);

        Assert.HasCount(1, parent.Values);
        Assert.AreSame(extra, parent.Values[0]);
    }

    [TestMethod]
    public void SpaceValue_AddValue_WrongSpace_Throws()
    {
        var s1     = Space.Segment(10.0, 100L);
        var s2     = Space.Segment(10.0, 100L);
        var parent = s1.Value(1.0, 2.0);
        var other  = s2.Value(3.0, 4.0);
        Assert.ThrowsExactly<InvalidOperationException>(() => parent.AddValue(other));
    }

    [TestMethod]
    public void SpaceValue_MultipleSubValues_TrackedInOrder()
    {
        var s      = Space.Segment(10.0, 100L);
        var parent = s.Value(5.0, 5.0);
        var sub1   = parent.CreateValue(1.0, 1.0);
        var sub2   = parent.CreateValue(2.0, 2.0);
        var sub3   = parent.CreateValue(3.0, 3.0);

        Assert.HasCount(3, parent.Values);
        Assert.AreSame(sub1, parent.Values[0]);
        Assert.AreSame(sub2, parent.Values[1]);
        Assert.AreSame(sub3, parent.Values[2]);
    }

    // ── IValueSpace polymorphism ────────────────────────────────────────────

    [TestMethod]
    public void IValueSpace_Space_And_SpaceValue_CanBeUsedPolymorphically()
    {
        // Both Space and SpaceValue implement IValueSpace,
        // enabling uniform handling in display/calibration layers.
        var s = Space.Segment(10.0, 100L);
        var v = s.Value(2.0, 3.0);

        var contexts = new IValueSpace[] { s, v };
        foreach (var ctx in contexts)
        {
            Assert.HasCount(1, ctx.Axes);
            Assert.AreEqual(100L, ctx.BasisTicks);
        }
    }
}

