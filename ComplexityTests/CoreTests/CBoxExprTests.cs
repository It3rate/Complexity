using Microsoft.VisualStudio.TestTools.UnitTesting;
using Complexity.Core;

namespace ComplexityTests.CoreTests;

[TestClass]
public class CBoxExprTests
{
    private CSpace _intSpace;
    private CSpace _segSpace;

    [TestInitialize]
    public void Init()
    {
        _intSpace = CSpace.Integer(-100, 100); // TPU=1
        _segSpace = CSpace.Segment(-100, 100); // TPU=1
    }

    // ================================================================
    //  CBox Tests — CBox still exists as a simulation container;
    //  it is just no longer the base of CExpr.
    // ================================================================

    [TestMethod]
    public void BoxDefaultsToZero()
    {
        var box = new CBox(_intSpace);
        Assert.AreEqual(0L, box.Value.Components[0]);
    }

    [TestMethod]
    public void BoxInitialValue()
    {
        var val = _intSpace.Value(42.0);
        var box = new CBox(_intSpace, val);
        Assert.AreEqual(42L, box.Value.Components[0]);
    }

    [TestMethod]
    public void BoxParamsConstructor()
    {
        var box = new CBox(_segSpace, 3.0, 7.0);
        Assert.AreEqual(3.0, box.Value.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(7.0, box.Value.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void BoxValidityInBounds()
    {
        var box = new CBox(_intSpace, _intSpace.Value(50.0));
        Assert.AreEqual(1.0, box.Validity, Utils.Tolerance);
        Assert.IsTrue(box.CurrentTension.IsValid);
    }

    [TestMethod]
    public void BoxValidityOutOfBounds()
    {
        var box = new CBox(_intSpace, _intSpace.Value(150.0));
        Assert.IsTrue(box.Validity < 1.0);
        Assert.IsFalse(box.CurrentTension.IsValid);
    }

    [TestMethod]
    public void BoxAddRemoveResolver()
    {
        var box = new CBox(_intSpace);
        var perspective = new Perspective(_intSpace, _intSpace.Value(50.0), 1.0);

        box.AddResolver("p1", perspective);
        Assert.AreEqual(1, box.Resolvers.Count);

        box.RemoveResolver("p1");
        Assert.AreEqual(0, box.Resolvers.Count);
    }

    [TestMethod]
    public void BoxStepWithResolverPushesTowardBounds()
    {
        var box = new CBox(_intSpace, _intSpace.Value(150.0));
        var perspective = new Perspective(_intSpace, _intSpace.Value(50.0), 1.0);
        box.AddResolver("constraint", perspective);

        long initialValue = box.Value.Components[0];

        for (int i = 0; i < 20; i++)
            box.Step(0.5);

        Assert.IsTrue(box.Value.Components[0] < initialValue,
            $"Value should decrease from {initialValue}, got {box.Value.Components[0]}");
    }

    [TestMethod]
    public void BoxStepRecordsHistory()
    {
        var box = new CBox(_intSpace, _intSpace.Value(50.0));
        Assert.AreEqual(0, box.History.Count);
        box.Step();
        Assert.AreEqual(1, box.History.Count);
        box.Step();
        Assert.AreEqual(2, box.History.Count);
    }

    [TestMethod]
    public void BoxClearHistory()
    {
        var box = new CBox(_intSpace, _intSpace.Value(50.0));
        box.Step();
        box.Step();
        Assert.AreEqual(2, box.History.Count);
        box.ClearHistory();
        Assert.AreEqual(0, box.History.Count);
    }

    [TestMethod]
    public void BoxToString()
    {
        var box = new CBox(_intSpace, _intSpace.Value(42.0));
        var str = box.ToString();
        Assert.IsTrue(str.Contains("CBox"));
        Assert.IsTrue(str.Contains("integer"));
    }

    // ================================================================
    //  CExpr Tests — Arithmetic Operations
    //  CExpr takes CValues directly. No CBox wrapper needed.
    // ================================================================

    [TestMethod]
    public void ExprAdd()
    {
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(20.0), OperationKind.Add);

        var result = expr.Evaluate();
        Assert.AreEqual(30L, result.Components[0]);
    }

    [TestMethod]
    public void ExprSubtract()
    {
        var expr = new CExpr(_intSpace.Value(30.0), _intSpace.Value(10.0), OperationKind.Subtract);

        var result = expr.Evaluate();
        Assert.AreEqual(20L, result.Components[0]);
    }

    [TestMethod]
    public void ExprMultiplyIntegers()
    {
        var expr = new CExpr(_intSpace.Value(6.0), _intSpace.Value(7.0), OperationKind.Multiply);

        var result = expr.Evaluate();
        Assert.AreEqual(42L, result.Components[0]);
    }

    [TestMethod]
    public void ExprMultiplySegments()
    {
        // (3 + 4i) * (1 + 2i) = -5 + 10i
        var left  = new CValue(_segSpace, new[] { 4.0, 3.0 });  // unot=4, unit=3
        var right = new CValue(_segSpace, new[] { 2.0, 1.0 });  // unot=2, unit=1
        var expr  = new CExpr(left, right, OperationKind.Multiply);

        var result = expr.Evaluate();
        Assert.AreEqual(10.0, result.GetValue(0), Utils.Tolerance); // imaginary
        Assert.AreEqual(-5.0, result.GetValue(1), Utils.Tolerance); // real
    }

    [TestMethod]
    public void ExprDivide()
    {
        var expr = new CExpr(_intSpace.Value(42.0), _intSpace.Value(6.0), OperationKind.Divide);

        var result = expr.Evaluate();
        Assert.AreEqual(7L, result.Components[0]);
    }

    [TestMethod]
    public void ExprPower()
    {
        var expr = new CExpr(_intSpace.Value(3.0), _intSpace.Value(4.0), OperationKind.Power);

        var result = expr.Evaluate();
        Assert.AreEqual(81L, result.Components[0]);
    }

    // ================================================================
    //  CExpr Tests — Unary Operations
    // ================================================================

    [TestMethod]
    public void ExprNegate()
    {
        var left = new CValue(_segSpace, new[] { 3.0, 5.0 });
        var expr = new CExpr(left, null, OperationKind.Negate);

        var result = expr.Evaluate();
        Assert.AreEqual(-3.0, result.GetValue(0), Utils.Tolerance);
        Assert.AreEqual(-5.0, result.GetValue(1), Utils.Tolerance);
    }

    [TestMethod]
    public void ExprReciprocal()
    {
        // Use higher-resolution space so 1/4 doesn't round to 0 in ticks.
        var hiRes = CSpace.Integer(-1000, 1000, ticksPerUnit: 100);
        var expr  = new CExpr(hiRes.Value(4.0), null, OperationKind.Reciprocal);

        var result = expr.Evaluate();
        Assert.AreEqual(0.25, result.GetValue(0), 0.01);
    }

    [TestMethod]
    public void ExprFlipPolarityIsConjugate()
    {
        var left = new CValue(_segSpace, new[] { 4.0, 3.0 });
        var expr = new CExpr(left, null, OperationKind.FlipPolarity);

        var result = expr.Evaluate();
        Assert.AreEqual(-4.0, result.GetValue(0), Utils.Tolerance); // imaginary negated
        Assert.AreEqual(3.0,  result.GetValue(1), Utils.Tolerance); // real unchanged
    }

    // ================================================================
    //  CExpr Tests — Filter and Mirror Operations
    // ================================================================

    [TestMethod]
    public void ExprFilterUnit()
    {
        var left = new CValue(_segSpace, new[] { 4.0, 3.0 });
        var expr = new CExpr(left, null, OperationKind.FilterUnit);

        var result = expr.Evaluate();
        Assert.AreEqual(0L, result.Components[0]); // unot zeroed
        Assert.AreEqual(3L, result.Components[1]); // unit kept
    }

    [TestMethod]
    public void ExprFilterUnot()
    {
        var left = new CValue(_segSpace, new[] { 4.0, 3.0 });
        var expr = new CExpr(left, null, OperationKind.FilterUnot);

        var result = expr.Evaluate();
        Assert.AreEqual(4L, result.Components[0]); // unot kept
        Assert.AreEqual(0L, result.Components[1]); // unit zeroed
    }

    [TestMethod]
    public void ExprMirrorOnUnit()
    {
        var left = new CValue(_segSpace, new[] { 4.0, 3.0 });
        var expr = new CExpr(left, null, OperationKind.MirrorOnUnit);

        var result = expr.Evaluate();
        Assert.AreEqual(4L,  result.Components[0]);  // unot unchanged
        Assert.AreEqual(-3L, result.Components[1]);  // unit negated
    }

    [TestMethod]
    public void ExprMirrorOnUnot()
    {
        var left = new CValue(_segSpace, new[] { 4.0, 3.0 });
        var expr = new CExpr(left, null, OperationKind.MirrorOnUnot);

        var result = expr.Evaluate();
        Assert.AreEqual(-4L, result.Components[0]); // unot negated
        Assert.AreEqual(3L,  result.Components[1]); // unit unchanged
    }

    // ================================================================
    //  CExpr Tests — Repeat Operations
    // ================================================================

    [TestMethod]
    public void ExprAppendAll()
    {
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(3.0), OperationKind.AppendAll)
        {
            Repeats = 3
        };

        var result = expr.Evaluate();
        Assert.AreEqual(19L, result.Components[0]); // 10 + 3 + 3 + 3
    }

    [TestMethod]
    public void ExprMultiplyAll()
    {
        var expr = new CExpr(_intSpace.Value(2.0), _intSpace.Value(3.0), OperationKind.MultiplyAll)
        {
            Repeats = 3
        };

        var result = expr.Evaluate();
        Assert.AreEqual(54L, result.Components[0]); // 2 * 3 * 3 * 3
    }

    [TestMethod]
    public void ExprIsUnary()
    {
        var unary  = new CExpr(_intSpace.Value(5.0), null, OperationKind.Negate);
        Assert.IsTrue(unary.IsUnary);

        var binary = new CExpr(_intSpace.Value(5.0), _intSpace.Value(3.0), OperationKind.Add);
        Assert.IsFalse(binary.IsUnary);
    }

    // ================================================================
    //  CExpr Tests — EvaluateWithTension (pure, no side effects)
    // ================================================================

    [TestMethod]
    public void EvaluateWithTensionInBounds()
    {
        var expr = new CExpr(_intSpace.Value(30.0), _intSpace.Value(20.0), OperationKind.Add);

        var (result, tension) = expr.EvaluateWithTension();
        Assert.AreEqual(50L, result.Components[0]);
        Assert.IsTrue(tension.IsValid);
    }

    [TestMethod]
    public void EvaluateWithTensionOutOfBounds()
    {
        var expr = new CExpr(_intSpace.Value(80.0), _intSpace.Value(40.0), OperationKind.Add);

        var (result, tension) = expr.EvaluateWithTension();
        Assert.AreEqual(120L, result.Components[0]);
        Assert.IsFalse(tension.IsValid);
        Assert.AreEqual(20L, tension.Total);
    }

    [TestMethod]
    public void ExprEvaluateWithTensionIsPure()
    {
        // Evaluating twice should yield identical results — no mutable state.
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(5.0), OperationKind.Multiply);

        var (result1, tension1) = expr.EvaluateWithTension();
        var (result2, tension2) = expr.EvaluateWithTension();

        Assert.AreEqual(50L, result1.Components[0]);
        Assert.AreEqual(result1.Components[0], result2.Components[0]);
        Assert.AreEqual(tension1.Total, tension2.Total);
    }

    [TestMethod]
    public void ExprToString()
    {
        var expr = new CExpr(_intSpace.Value(3.0), _intSpace.Value(4.0), OperationKind.Add);

        // ToString works without needing to evaluate first.
        var str = expr.ToString();
        Assert.IsTrue(str.Contains("+") || str.Contains("Add"),
            $"ToString should contain operator info, got: {str}");
    }

    // ================================================================
    //  DynamicExpr Tests
    //  State (history, resolvers) now lives on DynamicExpr, not CExpr.
    // ================================================================

    [TestMethod]
    public void DynamicExprStepAdvancesTick()
    {
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(5.0), OperationKind.Add);
        var dyn  = new DynamicExpr(expr);

        Assert.AreEqual(0, dyn.Tick);
        dyn.Step();
        Assert.AreEqual(1, dyn.Tick);
        dyn.Step();
        Assert.AreEqual(2, dyn.Tick);
    }

    [TestMethod]
    public void DynamicExprStepRecordsHistory()
    {
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(5.0), OperationKind.Add);
        var dyn  = new DynamicExpr(expr);

        dyn.Step();
        // History lives on dyn, not expr.
        Assert.AreEqual(1, dyn.History.Count);
    }

    [TestMethod]
    public void DynamicExprResolveConverges()
    {
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(5.0), OperationKind.Add);
        var dyn  = new DynamicExpr(expr);

        var result = dyn.Resolve(maxSteps: 10, threshold: 1);
        Assert.IsTrue(dyn.Converged);
        Assert.AreEqual(15L, result.Components[0]);
    }

    [TestMethod]
    public void DynamicExprReset()
    {
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(5.0), OperationKind.Add);
        var dyn  = new DynamicExpr(expr);

        dyn.Resolve(maxSteps: 5);
        Assert.IsTrue(dyn.Tick > 0);

        dyn.Reset();
        Assert.AreEqual(0, dyn.Tick);
        Assert.IsFalse(dyn.Converged);
        // History lives on dyn.
        Assert.AreEqual(0, dyn.History.Count);
    }

    [TestMethod]
    public void DynamicExprStepCompletedEvent()
    {
        var expr = new CExpr(_intSpace.Value(10.0), _intSpace.Value(5.0), OperationKind.Add);
        var dyn  = new DynamicExpr(expr);

        int eventCount = 0;
        CValue? lastValue = null;
        dyn.StepCompleted += (sender, val) =>
        {
            eventCount++;
            lastValue = val;
        };

        dyn.Step();
        Assert.AreEqual(1, eventCount);
        Assert.IsNotNull(lastValue);

        dyn.Step();
        Assert.AreEqual(2, eventCount);
    }

    [TestMethod]
    public void DynamicExprWithResolverPressure()
    {
        var space = CSpace.Integer(0, 10);
        var expr  = new CExpr(space.Value(8.0), space.Value(5.0), OperationKind.Add); // 8 + 5 = 13, out of bounds

        var constraint = new Perspective(space, space.Value(5.0), 1.0);
        // Resolvers live on dyn, not expr.
        var dyn = new DynamicExpr(expr);
        dyn.AddResolver("bound", constraint);

        var result = dyn.Resolve(maxSteps: 50, threshold: 1, damping: 0.3);

        Assert.IsTrue(result.Components[0] <= 13,
            $"Resolver should push value down from 13, got {result.Components[0]}");
    }

    [TestMethod]
    public void DynamicExprToString()
    {
        var expr = new CExpr(_intSpace.Value(10.0), null, OperationKind.Negate);
        var dyn  = new DynamicExpr(expr);

        var str = dyn.ToString();
        Assert.IsTrue(str.Contains("DynamicExpr"));
        Assert.IsTrue(str.Contains("tick=0"));
        Assert.IsTrue(str.Contains("converged=False"));
    }

    // ================================================================
    //  Default / passthrough operations
    // ================================================================

    [TestMethod]
    public void ExprUnknownOpPassesThrough()
    {
        var expr = new CExpr(_intSpace.Value(42.0), _intSpace.Value(5.0), OperationKind.None);

        var result = expr.Evaluate();
        Assert.AreEqual(42L, result.Components[0]);
    }

    [TestMethod]
    public void ExprUnaryUsesOneAsDefaultRight()
    {
        // When Right is null, the default right value is Space.One().
        // For integer space with TPU=1, One() = 1 tick = value 1.
        var expr = new CExpr(_intSpace.Value(42.0), null, OperationKind.Multiply);

        var result = expr.Evaluate();
        Assert.AreEqual(42L, result.Components[0]);
    }
}
