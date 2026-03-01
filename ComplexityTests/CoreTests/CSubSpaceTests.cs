using Complexity.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Complexity.Tests.CoreTests;

/// <summary>
/// Tests for the recursive nesting / "values as frames" feature.
///
/// Any CValue can promote itself to a CSpace via ToSubSpace().
/// The sub-space shares the same coordinate system (same ticks, same units)
/// as the parent but has tighter axis bounds derived from the value's components.
///
/// This creates a layered tension system:
///   A child value can overflow its sub-space bounds (expected range)
///   while remaining within the parent's bounds (capability range).
///
/// Analogy: range of light the eyes can see (parent) vs range currently
/// expected (sub-space). Surprise ≠ incapacity.
/// </summary>
[TestClass]
public class CSubSpaceTests
{
    // ================================================================
    //  ToSubSpace — structure preservation and bound derivation
    // ================================================================

    [TestMethod]
    public void ToSubSpace_CreatesSpaceWithValueBoundsAsLimits()
    {
        // Segment space from -100 to +100; value = [-3, +6]
        var parent = CSpace.Segment(-100, 100, ticksPerUnit: 1);
        var val = parent.Value(-3, 6);    // unot=-3, unit=+6

        var sub = val.ToSubSpace();

        // All axes should be bounded to [min(-3,6), max(-3,6)] = [-3, +6]
        Assert.AreEqual(-3L, sub.Axes[0].MinBoundTicks);
        Assert.AreEqual(6L, sub.Axes[0].MaxBoundTicks);
        Assert.AreEqual(-3L, sub.Axes[1].MinBoundTicks);
        Assert.AreEqual(6L, sub.Axes[1].MaxBoundTicks);
    }

    [TestMethod]
    public void ToSubSpace_PreservesAlgebra()
    {
        var parent = CSpace.Segment(-100, 100);
        var val = parent.Value(-3, 6);
        var sub = val.ToSubSpace();

        Assert.AreEqual(parent.Algebra.Length, sub.Algebra.Length);
        for (int i = 0; i < parent.Algebra.Length; i++)
        {
            var pe = parent.Algebra[i];
            var se = sub.Algebra[i];
            Assert.AreEqual(pe.Left,   se.Left);
            Assert.AreEqual(pe.Right,  se.Right);
            Assert.AreEqual(pe.Target, se.Target);
            Assert.AreEqual(pe.Sign,   se.Sign);
        }
    }

    [TestMethod]
    public void ToSubSpace_PreservesTickResolution()
    {
        var parent = CSpace.Segment(-100, 100, ticksPerUnit: 100);
        var val = parent.Value(-3, 6);
        var sub = val.ToSubSpace();

        Assert.AreEqual(parent.BasisTicks, sub.BasisTicks);
        Assert.AreEqual(100L, sub.BasisTicks);
    }

    [TestMethod]
    public void ToSubSpace_PreservesMetricAndChirality()
    {
        var parent = CSpace.Segment(-100, 100);
        var val = parent.Value(-3, 6);
        var sub = val.ToSubSpace();

        Assert.AreEqual(parent.Metric,     sub.Metric);
        Assert.AreEqual(parent.Chirality,  sub.Chirality);
        Assert.AreEqual(parent.AffineType, sub.AffineType);
    }

    [TestMethod]
    public void ToSubSpace_SetsParentAndOriginCorrectly()
    {
        var parent = CSpace.Segment(-100, 100);
        var val = parent.Value(-3, 6);
        var sub = val.ToSubSpace();

        Assert.AreSame(parent, sub.Parent);
        Assert.AreSame(val,    sub.Origin);
    }

    [TestMethod]
    public void ToSubSpace_IsSubSpaceIsTrue()
    {
        var parent = CSpace.Segment(-100, 100);
        var val = parent.Value(-3, 6);
        var sub = val.ToSubSpace();

        Assert.IsTrue(sub.IsSubSpace);
        Assert.IsFalse(parent.IsSubSpace);
    }

    [TestMethod]
    public void ToSubSpace_DepthIsOne()
    {
        var parent = CSpace.Segment(-100, 100);
        var val = parent.Value(-3, 6);
        var sub = val.ToSubSpace();

        Assert.AreEqual(0, parent.Depth);
        Assert.AreEqual(1, sub.Depth);
    }

    [TestMethod]
    public void ToSubSpace_RootIsGrandparent_WhenTwoLevelsDeep()
    {
        // Root → sub1 → sub2
        var root = CSpace.Segment(-100, 100);
        var sub1 = root.Value(-10, 20).ToSubSpace();
        var sub2 = sub1.Value(-5, 10).ToSubSpace();

        Assert.AreEqual(0, root.Depth);
        Assert.AreEqual(1, sub1.Depth);
        Assert.AreEqual(2, sub2.Depth);
        Assert.AreSame(root, sub2.Root);
        Assert.AreSame(root, sub1.Root);
        Assert.AreSame(root, root.Root);
    }

    [TestMethod]
    public void ToSubSpace_CustomName_UsedInSpaceName()
    {
        var parent = CSpace.Segment(-100, 100);
        var val = parent.Value(-3, 6);
        var sub = val.ToSubSpace("vision_range");

        Assert.AreEqual("vision_range", sub.Name);
    }

    [TestMethod]
    public void ToSubSpace_DegeneratePoint_AllowedAndStillValid()
    {
        // Value at exactly one point: both components equal
        var parent = CSpace.Segment(-100, 100);
        var val = parent.Value(5, 5);   // start == end: degenerate segment
        var sub = val.ToSubSpace();     // bounds = [5, 5] — valid but tight

        Assert.AreEqual(5L, sub.Axes[0].MinBoundTicks);
        Assert.AreEqual(5L, sub.Axes[0].MaxBoundTicks);
    }

    [TestMethod]
    public void ToSubSpace_1D_UsesComponentAsBound()
    {
        var parent = CSpace.Integer(0, 100);
        var val = parent.Value(7);
        var sub = val.ToSubSpace();

        // 1D: min==max==7 for a point-value — bounds are [7,7]
        Assert.AreEqual(7L, sub.Axes[0].MinBoundTicks);
        Assert.AreEqual(7L, sub.Axes[0].MaxBoundTicks);
    }

    // ================================================================
    //  TransformToParent
    // ================================================================

    [TestMethod]
    public void TransformToParent_ReturnsValueInParentSpace()
    {
        var parent = CSpace.Segment(-100, 100);
        var framingVal = parent.Value(-3, 6);
        var sub = framingVal.ToSubSpace();

        // Create a child value and transform it up
        var child = sub.Value(-1, 4);
        var inParent = sub.TransformToParent(child);

        Assert.AreSame(parent, inParent.Space);
        Assert.AreEqual(-1L, inParent.Components[0]);   // tick values are identical
        Assert.AreEqual(4L,  inParent.Components[1]);
    }

    [TestMethod]
    public void TransformToParent_ThrowsForRootSpace()
    {
        var root = CSpace.Segment(-100, 100);
        var val = root.Value(3, 5);

        Assert.ThrowsException<InvalidOperationException>(() => root.TransformToParent(val));
    }

    [TestMethod]
    public void TransformToAncestors_WalksChainCorrectly()
    {
        var root = CSpace.Segment(-100, 100);
        var sub1 = root.Value(-10, 20).ToSubSpace("sub1");
        var sub2 = sub1.Value(-5, 10).ToSubSpace("sub2");

        var child = sub2.Value(-3, 8);
        var ancestors = sub2.TransformToAncestors(child).ToList();

        Assert.AreEqual(2, ancestors.Count);
        Assert.AreSame(sub1, ancestors[0].Frame);   // innermost first
        Assert.AreSame(root, ancestors[1].Frame);   // root last
        // Tick values identical throughout the chain
        Assert.AreEqual(-3L, ancestors[0].Value.Components[0]);
        Assert.AreEqual(-3L, ancestors[1].Value.Components[0]);
    }

    // ================================================================
    //  Layered Tension — the two-level error state
    // ================================================================

    [TestMethod]
    public void ComputeAgainstSpace_ValueInBothSpaces_BothValid()
    {
        var parent = CSpace.Segment(-100, 100);
        var sub = parent.Value(-3, 6).ToSubSpace();
        var child = sub.Value(0, 4);    // clearly within [-3, +6]

        var subTension    = Tension.ComputeAgainstSpace(child, sub);
        var parentTension = Tension.ComputeAgainstSpace(child, parent);

        Assert.IsTrue(subTension.IsValid,    "in sub-space bounds");
        Assert.IsTrue(parentTension.IsValid, "in parent bounds");
    }

    [TestMethod]
    public void ComputeAgainstSpace_ValueOutsideSubSpaceButInParent_OnlySubSpaceTension()
    {
        // parent = [-100, +100]; sub = [-3, +6]; child value at +8 → sub overflow, parent ok
        var parent = CSpace.Segment(-100, 100, ticksPerUnit: 1);
        var sub    = parent.Value(-3, 6).ToSubSpace();

        // Force tick components directly: unot=4, unit=8 (unit overflows sub's max of 6)
        var child = sub.ValueFromTicks(-1L, 8L);

        var subTension    = Tension.ComputeAgainstSpace(child, sub);
        var parentTension = Tension.ComputeAgainstSpace(child, parent);

        Assert.IsFalse(subTension.IsValid,   "should overflow sub-space");
        Assert.IsTrue(parentTension.IsValid, "should be within parent");
        Assert.AreEqual(2L, subTension.Total); // 8 - 6 = 2 ticks out
    }

    [TestMethod]
    public void ComputeAgainstSpace_ValueOutsideBothSpaces_BothHaveTension()
    {
        var parent = CSpace.Segment(-100, 100, ticksPerUnit: 1);
        var sub    = parent.Value(-3, 6).ToSubSpace();

        // value at 150 — outside both sub [-3,6] and parent [-100,100]
        var child = sub.ValueFromTicks(0L, 150L);

        var subTension    = Tension.ComputeAgainstSpace(child, sub);
        var parentTension = Tension.ComputeAgainstSpace(child, parent);

        Assert.IsFalse(subTension.IsValid,    "outside sub-space");
        Assert.IsFalse(parentTension.IsValid, "outside parent");
    }

    [TestMethod]
    public void ComputeLayered_RootSpace_OneEntry()
    {
        var root  = CSpace.Segment(-100, 100);
        var val   = root.Value(3, 5);
        var chain = Tension.ComputeLayered(val);

        Assert.AreEqual(1, chain.Count);
        Assert.AreSame(root, chain[0].Space);
        Assert.IsTrue(chain[0].T.IsValid);
    }

    [TestMethod]
    public void ComputeLayered_SubSpace_TwoEntries_InnerFirst()
    {
        var parent = CSpace.Segment(-100, 100, ticksPerUnit: 1);
        var sub    = parent.Value(-3, 6).ToSubSpace();

        // Child overflows sub but not parent
        var child = sub.ValueFromTicks(-1L, 8L);
        var chain = Tension.ComputeLayered(child);

        Assert.AreEqual(2, chain.Count);
        Assert.AreSame(sub,    chain[0].Space); // innermost first
        Assert.AreSame(parent, chain[1].Space);
        Assert.IsFalse(chain[0].T.IsValid, "sub-space tension");
        Assert.IsTrue(chain[1].T.IsValid,  "parent tension ok");
    }

    [TestMethod]
    public void ComputeLayered_TwoLevels_ThreeEntries()
    {
        var root = CSpace.Segment(-100, 100, ticksPerUnit: 1);
        var sub1 = root.Value(-20, 20).ToSubSpace();
        var sub2 = sub1.Value(-5, 10).ToSubSpace();

        // child at 12 — overflows sub2 (max=10) but within sub1 (max=20) and root (max=100)
        var child = sub2.ValueFromTicks(0L, 12L);
        var chain = Tension.ComputeLayered(child);

        Assert.AreEqual(3, chain.Count);
        Assert.AreSame(sub2, chain[0].Space);
        Assert.AreSame(sub1, chain[1].Space);
        Assert.AreSame(root, chain[2].Space);

        Assert.IsFalse(chain[0].T.IsValid, "outside sub2");
        Assert.IsTrue(chain[1].T.IsValid,  "within sub1");
        Assert.IsTrue(chain[2].T.IsValid,  "within root");
    }
}
