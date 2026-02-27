using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

/// <summary>
/// Tests for the frame concept (CSpace as a number frame built from axes).
/// A frame is a container for types of numbers. Once you have a frame,
/// you can fill it with as many values as you want.
///
/// Tests cover:
/// - The 2.3 example (unit of 10 ticks, 23 ticks = 2.3)
/// - Multi-value frames
/// - Frame composition (complex, grid, combine)
/// - Resolution and tick-based storage
/// - Longs until evaluated, doubles when evaluated
/// </summary>
[TestClass]
public class FrameTests
{
    // ================================================================
    //  THE 2.3 EXAMPLE
    //  "if I wanted to make the number 2 and 3/10, I would have a
    //   unit of 10 ticks pointing to the right, a Max amount of something
    //   greater then three, and the value itself would be 23 ticks"
    // ================================================================

    [TestMethod]
    public void TwoAndThreeTenths_Example()
    {
        // Unit: 10 ticks pointing right
        // Range: max > 3 (we use 10 for comfort)
        var axis = Axis.Forward("value", 10, ticksPerUnit: 10);
        var frame = CSpace.FromAxis(axis);

        // Value: 23 ticks = 2.3
        var val = frame.CreateValueFromTicks(23);

        Assert.AreEqual(23L, val.Components[0]); // stored as long ticks
        Assert.AreEqual(2.3, val.Evaluate(), Utils.Tolerance); // evaluated as double
    }

    [TestMethod]
    public void TwoAndThreeTenths_ViaDoubleInput()
    {
        var axis = Axis.Forward("value", 10, ticksPerUnit: 10);
        var frame = CSpace.FromAxis(axis);

        // Create from double — converts to ticks internally
        var val = frame.CreateValue(2.3);

        Assert.AreEqual(23L, val.Components[0]); // stored as long ticks
        Assert.AreEqual(2.3, val.Evaluate(), Utils.Tolerance); // evaluated as double
    }

    // ================================================================
    //  FRAME HOLDS MANY VALUES
    //  "That frame and range needs to be able to accommodate many
    //   possible values inside it."
    // ================================================================

    [TestMethod]
    public void FrameHoldsMultipleValues()
    {
        var axis = Axis.Forward("value", 100, ticksPerUnit: 10);
        var frame = CSpace.FromAxis(axis);

        var v1 = frame.CreateValue(2.3);
        var v2 = frame.CreateValue(5.7);
        var v3 = frame.CreateValue(9.1);

        Assert.AreEqual(3, frame.Values.Count);
        Assert.AreEqual(2.3, v1.Evaluate(), Utils.Tolerance);
        Assert.AreEqual(5.7, v2.Evaluate(), Utils.Tolerance);
        Assert.AreEqual(9.1, v3.Evaluate(), Utils.Tolerance);
    }

    [TestMethod]
    public void FrameHoldsManyValuesFromTicks()
    {
        var axis = Axis.Forward("value", 100, ticksPerUnit: 10);
        var frame = CSpace.FromAxis(axis);

        // Add 100 values
        for (int i = 0; i < 100; i++)
            frame.CreateValueFromTicks(i * 5);

        Assert.AreEqual(100, frame.Values.Count);
        Assert.AreEqual(0L, frame.Values[0].Components[0]);
        Assert.AreEqual(495L, frame.Values[99].Components[0]);
        Assert.AreEqual(49.5, frame.Values[99].Evaluate(), Utils.Tolerance);
    }

    // ================================================================
    //  LONGS UNTIL EVALUATED
    //  "All values should be longs until evaluated,
    //   when evaluated they should be floats [doubles]."
    // ================================================================

    [TestMethod]
    public void ValuesAreTicksUntilEvaluated()
    {
        var axis = Axis.Forward("value", 100, ticksPerUnit: 10);
        var frame = CSpace.FromAxis(axis);

        // Create from ticks — stays as longs
        var v = frame.CreateValueFromTicks(23);
        Assert.AreEqual(23L, v.Components[0]);
        Assert.IsInstanceOfType(v.Components[0], typeof(long));

        // Only becomes a double when evaluated
        double evaluated = v.Evaluate();
        Assert.AreEqual(2.3, evaluated, Utils.Tolerance);
        Assert.IsInstanceOfType(evaluated, typeof(double));
    }

    [TestMethod]
    public void EvaluateAllReturnsDoubles()
    {
        var frame = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        var val = frame.CreateValue(3.5, 7.2);
        var evaluated = val.EvaluateAll();

        Assert.AreEqual(2, evaluated.Length);
        Assert.AreEqual(3.5, evaluated[0], Utils.Tolerance);
        Assert.AreEqual(7.2, evaluated[1], Utils.Tolerance);
    }

    // ================================================================
    //  FROMAXIS — build a frame from a single axis
    // ================================================================

    [TestMethod]
    public void FromAxis_CreatesOneAxisFrame()
    {
        var axis = Axis.Forward("x", 50, ticksPerUnit: 100);
        var frame = CSpace.FromAxis(axis);

        Assert.AreEqual(1, frame.Dims);
        Assert.AreEqual("x", frame.Axes[0].Name);
        Assert.AreEqual(+1, frame.Axes[0].Direction);
        Assert.AreEqual(100L, frame.BasisTicks);
        Assert.AreEqual(1, frame.Algebra.Length);
    }

    [TestMethod]
    public void FromAxis_SignedAxisSupportsNegatives()
    {
        // "To make a negative number we need a full positive and negative range
        //  and a single positive unit"
        var axis = Axis.Signed("value", 100, ticksPerUnit: 1);
        var frame = CSpace.FromAxis(axis);

        var positive = frame.CreateValue(42);
        var negative = frame.CreateValue(-17);

        Assert.AreEqual(42L, positive.Components[0]);
        Assert.AreEqual(-17L, negative.Components[0]);
        Assert.AreEqual(42.0, positive.Evaluate(), Utils.Tolerance);
        Assert.AreEqual(-17.0, negative.Evaluate(), Utils.Tolerance);
    }

    // ================================================================
    //  COMPLEX FRAME — combine two axes
    //  "To create a complex number we need two of these [axes]."
    //  "combine this frame with another left facing unit to make a complex number"
    // ================================================================

    [TestMethod]
    public void ComplexFrame_HasTwoAxes()
    {
        var realAxis = Axis.Signed("unit", 100, ticksPerUnit: 10);
        var imagAxis = Axis.Signed("unot", 100, ticksPerUnit: 10);
        var frame = CSpace.Complex(realAxis, imagAxis);

        Assert.AreEqual(2, frame.Dims);
        Assert.AreEqual("complex", frame.Name);
        Assert.AreEqual(-1, frame.Axes[0].Direction); // imaginary
        Assert.AreEqual(+1, frame.Axes[1].Direction); // real
        Assert.AreEqual(4, frame.Algebra.Length);
    }

    [TestMethod]
    public void ComplexFrame_MultiplicationMatchesSegment()
    {
        // CSpace.Complex and CSpace.Segment should produce identical math
        var segment = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        var complex = CSpace.Complex(
            Axis.Signed("unit", 100, ticksPerUnit: 10),
            Axis.Signed("unot", 100, ticksPerUnit: 10));

        // (3 + 4i) * (1 + 2i) = -5 + 10i
        var a_seg = new CValue(segment, new[] { 4.0, 3.0 }); // unot=4, unit=3
        var b_seg = new CValue(segment, new[] { 2.0, 1.0 }); // unot=2, unit=1
        var r_seg = a_seg * b_seg;

        var a_cmx = new CValue(complex, new[] { 4.0, 3.0 });
        var b_cmx = new CValue(complex, new[] { 2.0, 1.0 });
        var r_cmx = a_cmx * b_cmx;

        Assert.AreEqual(r_seg.GetValue(0), r_cmx.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(r_seg.GetValue(1), r_cmx.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void ComplexFrame_ISquaredIsNegativeOne()
    {
        var frame = CSpace.Complex(
            Axis.Signed("unit", 100, ticksPerUnit: 1000),
            Axis.Signed("unot", 100, ticksPerUnit: 1000));

        // i = (0 + 1i) → Components: [1000 ticks on imag, 0 on real]
        var i = new CValue(frame, new[] { 1.0, 0.0 });
        var iSq = i * i;

        Assert.AreEqual(0.0, iSq.GetValue(0), Utils.Tolerance); // imag = 0
        Assert.AreEqual(-1.0, iSq.GetValue(1), Utils.Tolerance); // real = -1
    }

    // ================================================================
    //  GRID FRAME — two orthogonal axes
    //  "To make a 2 dimensional value with XY, we need two of these axis."
    //  "combine with an orthogonal unit to make an XY grid"
    // ================================================================

    [TestMethod]
    public void GridFrame_HasTwoOrthogonalAxes()
    {
        var xAxis = Axis.Forward("x", 100, ticksPerUnit: 10);
        var yAxis = Axis.Forward("y", 100, ticksPerUnit: 10);
        var frame = CSpace.Grid(xAxis, yAxis);

        Assert.AreEqual(2, frame.Dims);
        Assert.AreEqual("grid", frame.Name);
        Assert.AreEqual(+1, frame.Axes[0].Direction); // x: forward
        Assert.AreEqual(+1, frame.Axes[1].Direction); // y: forward
        Assert.AreEqual(2, frame.Algebra.Length); // x*x and y*y only, no cross terms
    }

    [TestMethod]
    public void GridFrame_PointValues()
    {
        var frame = CSpace.Grid(
            Axis.Forward("x", 100, ticksPerUnit: 10),
            Axis.Forward("y", 100, ticksPerUnit: 10));

        var point = frame.CreateValue(3.5, 7.2);
        var values = point.EvaluateAll();

        Assert.AreEqual(3.5, values[0], Utils.Tolerance);
        Assert.AreEqual(7.2, values[1], Utils.Tolerance);
    }

    [TestMethod]
    public void GridFrame_AdditionIsComponentWise()
    {
        var frame = CSpace.Grid(
            Axis.Forward("x", 100, ticksPerUnit: 10),
            Axis.Forward("y", 100, ticksPerUnit: 10));

        var a = frame.CreateValue(3.0, 4.0);
        var b = frame.CreateValue(1.0, 2.0);
        var sum = a + b;

        Assert.AreEqual(4.0, sum.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(6.0, sum.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void GridFrame_MultiplicationIsIndependent()
    {
        // In a grid, x*x = x and y*y = y. No cross terms.
        var frame = CSpace.Grid(
            Axis.Forward("x", 100, ticksPerUnit: 10),
            Axis.Forward("y", 100, ticksPerUnit: 10));

        var a = frame.CreateValue(3.0, 4.0);
        var b = frame.CreateValue(2.0, 5.0);
        var product = a * b;

        Assert.AreEqual(6.0, product.GetValue(0), Utils.Tolerance);  // x: 3*2 = 6
        Assert.AreEqual(20.0, product.GetValue(1), Utils.Tolerance); // y: 4*5 = 20
    }

    // ================================================================
    //  GRID ARITHMETIC — addition, subtraction, negation, multiplication
    // ================================================================

    [TestMethod]
    public void GridFrame_SubtractionIsComponentWise()
    {
        var frame = CSpace.Grid(
            Axis.Signed("x", 100, ticksPerUnit: 10),
            Axis.Signed("y", 100, ticksPerUnit: 10));

        var a = frame.Value(7.0, 3.0);
        var b = frame.Value(2.0, 5.0);
        var diff = a - b;

        Assert.AreEqual(5.0, diff.GetValue(0), Utils.Tolerance);   // 7-2=5
        Assert.AreEqual(-2.0, diff.GetValue(1), Utils.Tolerance);   // 3-5=-2
    }

    [TestMethod]
    public void GridFrame_NegationNegatesBothAxes()
    {
        var frame = CSpace.Grid(
            Axis.Signed("x", 100, ticksPerUnit: 10),
            Axis.Signed("y", 100, ticksPerUnit: 10));

        var a = frame.Value(3.0, 5.0);
        var neg = -a;

        Assert.AreEqual(-3.0, neg.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(-5.0, neg.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void GridFrame_ScalarMultiplication()
    {
        var frame = CSpace.Grid(
            Axis.Forward("x", 100, ticksPerUnit: 10),
            Axis.Forward("y", 100, ticksPerUnit: 10));

        var a = frame.Value(3.0, 4.0);
        var scaled = a * 2.5;

        Assert.AreEqual(7.5, scaled.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(10.0, scaled.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void GridFrame_NoCrossTerms()
    {
        // Grid multiplication: x*x → x, y*y → y, no x*y cross terms.
        // This means (a,b) * (c,d) = (a*c, b*d) — independent per axis.
        var frame = CSpace.Grid(
            Axis.Forward("x", 100, ticksPerUnit: 10),
            Axis.Forward("y", 100, ticksPerUnit: 10));

        var a = frame.Value(3.0, 0.0);  // purely in x
        var b = frame.Value(0.0, 4.0);  // purely in y
        var product = a * b;

        // No cross terms: 3*0=0 and 0*4=0
        Assert.AreEqual(0.0, product.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(0.0, product.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void GridFrame_MultipleValuesInFrame()
    {
        var frame = CSpace.Grid(
            Axis.Signed("x", 100, ticksPerUnit: 100),
            Axis.Signed("y", 100, ticksPerUnit: 100));

        frame.CreateValue(1.0, 2.0);
        frame.CreateValue(3.0, 4.0);
        frame.CreateValue(-5.0, 7.5);

        Assert.AreEqual(3, frame.Values.Count);
        Assert.AreEqual(1.0, frame.Values[0].GetValue(0), Utils.Tolerance);
        Assert.AreEqual(7.5, frame.Values[2].GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void GridFrame_DistanceBetweenPoints()
    {
        var frame = CSpace.Grid(
            Axis.Signed("x", 100, ticksPerUnit: 100),
            Axis.Signed("y", 100, ticksPerUnit: 100));

        var a = frame.Value(0.0, 0.0);
        var b = frame.Value(3.0, 4.0);

        double dist = CValue.Distance(a, b);
        Assert.AreEqual(5.0, dist, Utils.Tolerance); // 3-4-5 triangle
    }

    // ================================================================
    //  SEGMENT ARITHMETIC — matches what the viewer renders
    // ================================================================

    [TestMethod]
    public void SegmentAddition_MatchesViewerDemo()
    {
        // A = (unot=1, unit=4), B = (unot=1, unit=2), A+B = (2, 6)
        var seg = CSpace.Segment(min: -6, max: 12, ticksPerUnit: 100);
        var A = seg.Value(1.0, 4.0);
        var B = seg.Value(1.0, 2.0);
        var sum = A + B;

        Assert.AreEqual(2.0, sum.UnotValue, Utils.Tolerance);
        Assert.AreEqual(6.0, sum.UnitValue, Utils.Tolerance);
    }

    [TestMethod]
    public void SegmentNegation_MatchesViewerDemo()
    {
        var seg = CSpace.Segment(min: -6, max: 12, ticksPerUnit: 100);
        var A = seg.Value(1.0, 4.0);
        var negA = -A;

        Assert.AreEqual(-1.0, negA.UnotValue, Utils.Tolerance);
        Assert.AreEqual(-4.0, negA.UnitValue, Utils.Tolerance);
    }

    // ================================================================
    //  FRAME COMPOSITION — CombineWith
    //  "I should be able to combine this frame with another"
    // ================================================================

    [TestMethod]
    public void CombineFrames_CreatesTwoAxisFrame()
    {
        var frame1 = CSpace.FromAxis(Axis.Forward("x", 100));
        var frame2 = CSpace.FromAxis(Axis.Forward("y", 100));
        var combined = frame1.CombineWith(frame2);

        Assert.AreEqual(2, combined.Dims);
        Assert.AreEqual("x", combined.Axes[0].Name);
        Assert.AreEqual("y", combined.Axes[1].Name);
        Assert.AreEqual(2, combined.Algebra.Length); // each axis's own multiply
    }

    [TestMethod]
    public void CombineWithCrossAlgebra()
    {
        var frame1 = CSpace.FromAxis(Axis.Forward("x", 100));
        var frame2 = CSpace.FromAxis(Axis.Forward("y", 100));
        var cross = new[] { new AlgebraEntry(0, 1, 0, +1) }; // x*y → x
        var combined = frame1.CombineWith(frame2, cross);

        Assert.AreEqual(3, combined.Algebra.Length); // 2 self + 1 cross
    }

    [TestMethod]
    public void MergedFramePreservesAxes()
    {
        // "If we merge the XY axis together we get a single dimensional area"
        var xFrame = CSpace.FromAxis(Axis.Forward("x", 10, ticksPerUnit: 1));
        var yFrame = CSpace.FromAxis(Axis.Forward("y", 10, ticksPerUnit: 1));
        var merged = CSpace.Merge(xFrame, yFrame);

        Assert.AreEqual(2, merged.Dims);
        Assert.AreEqual("x×y", merged.Name);

        var point = new CValue(merged, new[] { 3.0, 4.0 });
        Assert.AreEqual(3.0, point.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(4.0, point.GetValue(1), Utils.Tolerance);
    }

    // ================================================================
    //  RESOLUTION / TICK PRECISION
    //  "There is no floating point resolution at all in this system,
    //   the resolution is always based on the number of ticks in a unit."
    // ================================================================

    [TestMethod]
    public void IntegerResolution_OneTick()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 1000, ticksPerUnit: 1));
        var val = frame.CreateValue(42);
        Assert.AreEqual(42L, val.Components[0]);
        Assert.AreEqual(42.0, val.Evaluate(), Utils.Tolerance);
    }

    [TestMethod]
    public void FineResolution_ThousandTicks()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 10, ticksPerUnit: 1000));
        var val = frame.CreateValue(3.141);
        Assert.AreEqual(3141L, val.Components[0]);
        Assert.AreEqual(3.141, val.Evaluate(), 0.001);
    }

    [TestMethod]
    public void VeryFineResolution()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 10, ticksPerUnit: 1000000));
        var val = frame.CreateValue(3.141592);
        Assert.AreEqual(3141592L, val.Components[0]);
        Assert.AreEqual(3.141592, val.Evaluate(), 0.000001);
    }

    // ================================================================
    //  ARITHMETIC IN FRAMES
    // ================================================================

    [TestMethod]
    public void AdditionInFrame()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 100, ticksPerUnit: 10));
        var a = frame.CreateValueFromTicks(23); // 2.3
        var b = frame.CreateValueFromTicks(17); // 1.7
        var sum = a + b;

        Assert.AreEqual(40L, sum.Components[0]); // 23 + 17 = 40 ticks
        Assert.AreEqual(4.0, sum.Evaluate(), Utils.Tolerance); // 40 / 10 = 4.0
    }

    [TestMethod]
    public void MultiplicationInFrame()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 1000, ticksPerUnit: 10));
        var a = frame.CreateValueFromTicks(30); // 3.0
        var b = frame.CreateValueFromTicks(40); // 4.0
        var product = a * b;

        // 30 * 40 / 10 (basis ticks) = 120 ticks = 12.0
        Assert.AreEqual(120L, product.Components[0]);
        Assert.AreEqual(12.0, product.Evaluate(), Utils.Tolerance);
    }

    [TestMethod]
    public void DivisionInFrame()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 1000, ticksPerUnit: 10));
        var a = frame.CreateValueFromTicks(120); // 12.0
        var b = frame.CreateValueFromTicks(40);  // 4.0
        var quotient = a / b;

        Assert.AreEqual(30L, quotient.Components[0]); // 120 / 40 * 10 = 30 ticks = 3.0
        Assert.AreEqual(3.0, quotient.Evaluate(), Utils.Tolerance);
    }

    // ================================================================
    //  VALUE FACTORY — CSpace.Value and CSpace.ValueFromTicks
    //  (non-registering versions, for compatibility with existing tests)
    // ================================================================

    [TestMethod]
    public void ValueFactoryDoesNotRegister()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 100, ticksPerUnit: 10));
        var val = frame.Value(5.0); // does NOT add to Values collection
        Assert.AreEqual(0, frame.Values.Count);
        Assert.AreEqual(5.0, val.Evaluate(), Utils.Tolerance);
    }

    [TestMethod]
    public void CreateValueDoesRegister()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 100, ticksPerUnit: 10));
        var val = frame.CreateValue(5.0); // DOES add to Values collection
        Assert.AreEqual(1, frame.Values.Count);
        Assert.AreSame(frame.Values[0].Space, val.Space);
    }

    // ================================================================
    //  SEGMENT MATCHES COMPLEX FACTORY
    //  Both ways of creating complex/segment math should produce same results.
    // ================================================================

    [TestMethod]
    public void SegmentAndComplexProduceSameAlgebra()
    {
        var segment = CSpace.Segment(-100, 100, ticksPerUnit: 10);
        var complex = CSpace.Complex(
            Axis.Signed("unit", 100, ticksPerUnit: 10),
            Axis.Signed("unot", 100, ticksPerUnit: 10));

        Assert.AreEqual(segment.Dims, complex.Dims);
        Assert.AreEqual(segment.Algebra.Length, complex.Algebra.Length);
    }

    // ================================================================
    //  EDGE CASES
    // ================================================================

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CreateValue_WrongDimCount_Throws()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 100));
        frame.CreateValue(1.0, 2.0); // 1D frame but 2 components
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CreateValueFromTicks_WrongDimCount_Throws()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 100));
        frame.CreateValueFromTicks(1, 2); // 1D frame but 2 components
    }

    [TestMethod]
    public void ZeroAndOneInFrame()
    {
        var frame = CSpace.FromAxis(Axis.Forward("value", 100, ticksPerUnit: 10));
        var zero = frame.Zero();
        var one = frame.One();

        Assert.AreEqual(0L, zero.Components[0]);
        Assert.AreEqual(10L, one.Components[0]); // 1 unit = 10 ticks
        Assert.AreEqual(0.0, zero.Evaluate(), Utils.Tolerance);
        Assert.AreEqual(1.0, one.Evaluate(), Utils.Tolerance);
    }

    [TestMethod]
    public void FrameToStringShowsDimsAndTicks()
    {
        var frame = CSpace.FromAxis(Axis.Forward("test", 100, ticksPerUnit: 10));
        var str = frame.ToString();
        Assert.IsTrue(str.Contains("1D"), $"Expected '1D' in '{str}'");
        Assert.IsTrue(str.Contains("tpu=10"), $"Expected 'tpu=10' in '{str}'");
    }
}
