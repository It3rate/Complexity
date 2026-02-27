namespace Complexity.Core;

/// <summary>
/// A pure expression: Left op Right → result, within a number frame (CSpace).
///
/// CExpr is a stateless transform — it takes values in and produces a value out.
/// It carries no history, no resolvers, no validity tracking, and no mutable state.
/// Those are simulation concerns and belong in DynamicExpr.
///
/// Both operands live in the same space (Left.Space).
/// The space's algebra table defines what multiplication means.
/// For cross-space operations, project or embed into a shared frame first.
///
/// Corresponds to the Expr concept from the outline:
///   Left  → Left CValue
///   Right → Right CValue (null for unary)
///   Op    → OperationKind
///   Repeats → repetition count (for AppendAll, MultiplyAll)
/// </summary>
public class CExpr
{
    public CValue Left { get; }
    public CValue? Right { get; }
    public OperationKind Op { get; set; }
    public int Repeats { get; set; } = 1;

    /// <summary>The number frame this expression lives in (from the Left operand).</summary>
    public CSpace Space => Left.Space;

    public bool IsUnary => Right == null;

    public CExpr(CValue left, CValue? right, OperationKind op)
    {
        Left = left;
        Right = right;
        Op = op;
    }

    /// <summary>
    /// Evaluate the expression and return the result as a CValue (tick-based).
    /// Pure: no state mutation. Calling this multiple times with the same operands
    /// always yields the same result.
    /// </summary>
    public CValue Evaluate()
    {
        var l = Left;
        var r = Right ?? Left.Space.One();

        return Op switch
        {
            OperationKind.Add         => l + r,
            OperationKind.Subtract    => l - r,
            OperationKind.Multiply    => l * r,
            OperationKind.Divide      => l / r,
            OperationKind.Power       => CValue.Pow(l, r),
            OperationKind.Negate      => -l,
            OperationKind.Reciprocal  => CValue.Inverse(l),
            OperationKind.FlipPolarity => CValue.Conjugate(l),

            // Filters: extract single-axis components, zero all others
            OperationKind.FilterUnit  => ExtractUnit(l),
            OperationKind.FilterUnot  => ExtractUnot(l),

            // Mirrors: reflect across an axis
            OperationKind.MirrorOnUnit  => MirrorUnit(l),
            OperationKind.MirrorOnUnot  => MirrorUnot(l),

            // Repeats: apply operation N times
            OperationKind.AppendAll   => RepeatAdd(l, r, Repeats),
            OperationKind.MultiplyAll => RepeatMultiply(l, r, Repeats),

            _ => l // default passthrough for unimplemented ops
        };
    }

    /// <summary>
    /// Evaluate the expression and also compute the tension of the result
    /// against the space bounds.
    /// Pure: both the result and the tension are returned without mutating any state.
    /// </summary>
    public (CValue Result, Tension Tension) EvaluateWithTension()
    {
        var result = Evaluate();
        return (result, Tension.Compute(result));
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
        return IsUnary
            ? $"{symbol} {Left}"
            : $"{Left} {symbol} {Right}";
    }
}
