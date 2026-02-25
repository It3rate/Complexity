namespace Complexity.Core;

/// <summary>
/// An expression: left op right → result, evaluated within a complexity space.
/// Extends CBox with operation semantics.
///
/// All values are tick-based (longs). Operations work on CValue (tick components).
///
/// Corresponds to the Expr interface from the outline, which extends Box:
///   Left      → Left CBox
///   Right     → Right CBox (optional for unary)
///   Op        → OperationKind
///   Repeats   → Repeats (for power-like repetition)
///
/// Generalizes Transform: where Transform has Left + Right + OperationKind + Result,
/// CExpr has the same plus resolvers, history, and validity tracking.
/// Transform's lifecycle (Start/Tick/End) maps to DynamicExpr's step/resolve.
/// </summary>
public class CExpr : CBox
{
    public CBox Left { get; }
    public CBox? Right { get; }
    public OperationKind Op { get; set; }
    public int Repeats { get; set; } = 1;

    public bool IsUnary => Right == null;

    public CExpr(CBox left, CBox? right, OperationKind op)
        : base(left.Space)
    {
        Left = left;
        Right = right;
        Op = op;
    }

    /// <summary>
    /// Evaluate the expression and return the result as a CValue (tick-based).
    /// The result space is the same as the left operand's space.
    /// </summary>
    public CValue Evaluate()
    {
        var l = Left.Value;
        var r = Right?.Value ?? Left.Space.One();

        return Op switch
        {
            OperationKind.Add => l + r,
            OperationKind.Subtract => l - r,
            OperationKind.Multiply => l * r,
            OperationKind.Divide => l / r,
            OperationKind.Power => CValue.Pow(l, r),
            OperationKind.Negate => -l,
            OperationKind.Reciprocal => CValue.Inverse(l),
            OperationKind.FlipPolarity => CValue.Conjugate(l),

            // Filters: extract single-axis components
            OperationKind.FilterUnit => ExtractUnit(l),
            OperationKind.FilterUnot => ExtractUnot(l),

            // Mirrors: reflect across an axis
            OperationKind.MirrorOnUnit => MirrorUnit(l),
            OperationKind.MirrorOnUnot => MirrorUnot(l),

            // Repeats: apply operation N times
            OperationKind.AppendAll => RepeatAdd(l, r, Repeats),
            OperationKind.MultiplyAll => RepeatMultiply(l, r, Repeats),

            _ => l // default passthrough for unimplemented ops
        };
    }

    /// <summary>
    /// Evaluate and compute tension of the result.
    /// </summary>
    public (CValue Result, Tension Tension) EvaluateWithTension()
    {
        var result = Evaluate();
        Value = result;
        UpdateValidity();
        return (result, CurrentTension);
    }

    // ================================================================
    //  HELPER OPERATIONS — work directly on long[] components
    // ================================================================

    private static CValue ExtractUnit(CValue val)
    {
        var comps = new long[val.Space.Dims];
        for (int i = 0; i < val.Space.Dims; i++)
        {
            if (val.Space.Axes[i].Direction == +1)
                comps[i] = val.Components[i];
        }
        return new CValue(val.Space, comps, val.Certainty);
    }

    private static CValue ExtractUnot(CValue val)
    {
        var comps = new long[val.Space.Dims];
        for (int i = 0; i < val.Space.Dims; i++)
        {
            if (val.Space.Axes[i].Direction == -1)
                comps[i] = val.Components[i];
        }
        return new CValue(val.Space, comps, val.Certainty);
    }

    private static CValue MirrorUnit(CValue val)
    {
        var comps = (long[])val.Components.Clone();
        for (int i = 0; i < val.Space.Dims; i++)
        {
            if (val.Space.Axes[i].Direction == +1)
                comps[i] = -comps[i];
        }
        return new CValue(val.Space, comps, val.Certainty);
    }

    private static CValue MirrorUnot(CValue val)
    {
        var comps = (long[])val.Components.Clone();
        for (int i = 0; i < val.Space.Dims; i++)
        {
            if (val.Space.Axes[i].Direction == -1)
                comps[i] = -comps[i];
        }
        return new CValue(val.Space, comps, val.Certainty);
    }

    private static CValue RepeatAdd(CValue value, CValue amount, int repeats)
    {
        var result = value;
        for (int i = 0; i < repeats; i++)
            result = result + amount;
        return result;
    }

    private static CValue RepeatMultiply(CValue value, CValue factor, int repeats)
    {
        var result = value;
        for (int i = 0; i < repeats; i++)
            result = result * factor;
        return result;
    }

    public override string ToString()
    {
        var symbol = Op.GetSymbol();
        return $"{Left.Value} {symbol} {Right?.Value.ToString() ?? ""} = {Value}";
    }
}
