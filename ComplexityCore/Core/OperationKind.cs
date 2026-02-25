namespace Complexity.Core;

using System;
using System.Collections.Generic;

public enum OperationKind
{
    None,

    // bool
    FALSE,
    AND,
    AND_NOT,
    A,
    NOT_AND,
    B,
    XOR,
    OR,
    NOR,
    XNOR,
    NOT_B,
    A_OR_NOT_B,
    NOT_A,
    NOT_A_OR_B,
    NAND,
    TRUE,

    // unary
    Negate, // multiplicative inverse of negative
    NegateInPlace, // Flip arrow in place, additive inverse of negation
    FlipPolarity, // multiplicative inverse of polarity (*i)
    FlipPolarityInPlace,  // Flip polarity in place, additive inverse of polarity 
    Reciprocal, // multiplicative inverse of multiplication (additive reciprocal would be length becomes 1/len, in place)
    ReciprocalInPlace, // additive inverse of multiplication (length becomes 1/len, in place)

    MirrorOnUnit,
    MirrorOnUnot,
    MirrorOnStart,
    MirrorOnEnd,
    FilterUnit, // always 0 to r value
    FilterUnot, // always 0 to i value
    FilterStart, // depends on polarity
    FilterEnd, // depends on polarity

    // Binary
    Add,
    Subtract, // really just add negated
    Multiply,
    Divide, // just multiply reciprocal

    Power,

    Repeat, // powers
    RepeatInPlace, // additive powers
    Root, // inverted powers
    RootInPlace, // inverted additive powers
    Wedge,
    DotProduct,
    CrossProduct,
    GeometricProduct,
    Blend, // multiply as in area, blend from unot to unit

    // bool comparisons return original number if true, otherwise nil
    GreaterThan, // A all to right of B
    GreaterThanOrEqual, // no part of A to left of B
    GreaterThanAndEqual, // no part of A to left of B, and part to the right of B (overlap right)
    ContainedBy, // A fits inside B
    Equals, // B matches A
    Contains, // B fits inside A
    LessThanAndEqual, // no part of A to right of B, and part to the left of B  (overlap left)
    LessThanOrEqual, // no part of A to right of B
    LessThan, // A all to left of B

    // (these are just the above ops, with a comparison to the original, so 'fully greater than' etc.)
    EvaluateGreaterThan, // T/F division: A all to right of B
    EvaluateGreaterThanOrEqual, // T/F division: no part of A to left of B
    EvaluateGreaterThanAndEqual, // T/F division: no part of A to left of B, and part to the right of B (overlap right)
    EvaluateContainedBy, // T/F division: A fits inside B
    EvaluateEquals, // T/F division: B matches A
    EvaluateContains, // T/F division: B fits inside A
    EvaluateLessThanAndEqual, // T/F division: no part of A to right of B, and part to the left of B  (overlap left)
    EvaluateLessThanOrEqual, // T/F division: no part of A to right of B
    EvaluateLessThan, // T/F division: A all to left of B

    AppendAll, // repeat are added together ('regular' multiplication)
    MultiplyAll, // repeat are multiplied together (exponents)
    Average,
}
public static class OperationKindExtension
{
    public static bool IsBoolOp(this OperationKind kind)
    {
        return kind >= OperationKind.FALSE && kind <= OperationKind.TRUE;
    }
    public static bool IsBoolCompare(this OperationKind kind)
    {
        return kind >= OperationKind.GreaterThan && kind <= OperationKind.LessThan;
    }
    public static bool IsUnary(this OperationKind kind)
    {
        return kind >= OperationKind.Negate && kind <= OperationKind.FilterEnd;
    }
    public static bool IsBinary(this OperationKind kind)
    {
        return kind >= OperationKind.Add && kind <= OperationKind.Blend;
    }
    public static bool IsMultipleOp(this OperationKind kind)
    {
        return kind >= OperationKind.AppendAll && kind <= OperationKind.Average;
    }
    public static IEnumerable<OperationKind> BoolOpKinds()
    {
        var result = OperationKind.FALSE;
        while (result <= OperationKind.TRUE)
        {
            yield return result;
            result += 1;
        }
    }
    public static IEnumerable<OperationKind> BoolCompareKinds()
    {
        var result = OperationKind.GreaterThan;
        while (result <= OperationKind.LessThan)
        {
            yield return result;
            result += 1;
        }
	}
	public static OperationKind InverseOp(this OperationKind kind)
	{
		OperationKind result = OperationKind.None;
		switch (kind)
		{
			case OperationKind.FALSE:
				result = OperationKind.TRUE;
				break;
			case OperationKind.AND:
				result = OperationKind.NAND;
				break;
			case OperationKind.AND_NOT:
				result = OperationKind.NOT_A_OR_B;
				break;
			case OperationKind.A:
				result = OperationKind.NOT_A;
				break;
			case OperationKind.NOT_AND:
				result = OperationKind.A_OR_NOT_B;
				break;
			case OperationKind.B:
				result = OperationKind.NOT_B;
				break;
			case OperationKind.XOR:
				result = OperationKind.XNOR;
				break;
			case OperationKind.OR:
				result = OperationKind.NOR;
				break;
			case OperationKind.NOR:
				result = OperationKind.OR;
				break;
			case OperationKind.XNOR:
				result = OperationKind.XOR;
				break;
			case OperationKind.NOT_B:
				result = OperationKind.B;
				break;
			case OperationKind.A_OR_NOT_B:
				result = OperationKind.NOT_AND;
				break;
			case OperationKind.NOT_A:
				result = OperationKind.A;
				break;
			case OperationKind.NOT_A_OR_B:
				result = OperationKind.AND_NOT;
				break;
			case OperationKind.NAND:
				result = OperationKind.AND;
				break;
			case OperationKind.TRUE:
				result = OperationKind.FALSE;
				break;
				// todo: add all other ops when implemented
		}
		return result;
	}

	public static string GetSymbol(this OperationKind kind)
    {
        //  0000	Never			0			FALSE
        //  0001	Both            A ^ B       AND
        //  0010	Only A          A ^ !B      A AND NOT B
        //  0011	A Maybe B       A           A
        //  0100	Only B			!A ^ B      NOT A AND B
        //  0101	B Maybe A       B           B
        //  0110	One of          A xor B     XOR
        //  0111	At least one    A v B       OR
        //  1000	No one          A nor B     NOR
        //  1001	Both or no one  A XNOR B    XNOR
        //  1010	A or no one		!B          NOT B
        //  1011	Not B alone     A v !B      A OR NOT B
        //  1100	B or no one		!A          NOT A
        //  1101	Not A alone		!A v B      NOT A OR B
        //  1110	Not both        A nand B    NAND
        //  1111	Always			1			TRUE
        var result = kind.GetName();
        switch (kind)
        {
            case OperationKind.FALSE:
                result = "0";
                break;
            case OperationKind.AND:
                result = "&";
                break;
            case OperationKind.AND_NOT:
                result = "&!";
                break;
            case OperationKind.A:
                result = "A";
                break;
            case OperationKind.NOT_AND:
                result = "!A&B";
                break;
            case OperationKind.B:
                result = "B";
                break;
            case OperationKind.XOR:
                result = "~|";
                break;
            case OperationKind.OR:
                result = "|";
                break;
            case OperationKind.NOR:
                result = "!|";
                break;
            case OperationKind.XNOR:
                result = "~!|";
                break;
            case OperationKind.NOT_B:
                result = "!B";
                break;
            case OperationKind.A_OR_NOT_B:
                result = "A|!B";
                break;
            case OperationKind.NOT_A:
                result = "!A";
                break;
            case OperationKind.NOT_A_OR_B:
                result = "!A|B";
                break;
            case OperationKind.NAND:
                result = "!&";
                break;
            case OperationKind.TRUE:
                result = "1";
                break;
            case OperationKind.Negate:
                result = "-";
                break;
            case OperationKind.Reciprocal:
                result = "1/";
                break;
            case OperationKind.FlipPolarityInPlace:
                result = "ObverseInPlace";
                break;
            case OperationKind.FlipPolarity:
                result = "Obverse";
                break;
            case OperationKind.MirrorOnUnit:
                result = "MirrorOnUnit";
                break;
            case OperationKind.MirrorOnUnot:
                result = "MirrorOnUnot";
                break;
            case OperationKind.MirrorOnStart:
                result = "MirrorOnStart";
                break;
            case OperationKind.MirrorOnEnd:
                result = "MirrorOnEnd";
                break;
            case OperationKind.FilterUnit:
                result = "FilterUnit";
                break;
            case OperationKind.FilterUnot:
                result = "FilterUnot";
                break;
            case OperationKind.FilterStart:
                result = "FilterStart";
                break;
            case OperationKind.FilterEnd:
                result = "FilterEnd";
                break;
            case OperationKind.NegateInPlace:
                result = "!";
                break;
            case OperationKind.Add:
                result = "+";
                break;
            case OperationKind.Subtract:
                result = "-";
                break;
            case OperationKind.Multiply:
                result = "*";
                break;
            case OperationKind.Divide:
                result = "/";
                break;
            case OperationKind.Root:
                result = "√";
                break;
            case OperationKind.Wedge:
                result = "^";
                break;
            case OperationKind.DotProduct:
                result = "⋅";
                break;
            case OperationKind.CrossProduct:
                result = "x";
                break;
            case OperationKind.GeometricProduct:
                result = "GeometricProduct";
                break;
            case OperationKind.Blend:
                result = "Blend";
                break;
            case OperationKind.Power:
                result = "^";
                break;
            case OperationKind.AppendAll:
                result = "AppendAll";
                break;
            case OperationKind.MultiplyAll:
                result = "MultiplyAll";
                break;
            case OperationKind.Average:
                result = "Average";
                break;
        }
        return result;
    }
    public static string GetName(this OperationKind kind)
    {
        var result = "None";
        switch (kind)
        {
            case OperationKind.None:
                result = "None";
                break;
            case OperationKind.FALSE:
                result = "FALSE";
                break;
            case OperationKind.AND:
                result = "AND";
                break;
            case OperationKind.AND_NOT:
                result = "AND_NOT";
                break;
            case OperationKind.A:
                result = "A";
                break;
            case OperationKind.NOT_AND:
                result = "NOT_AND";
                break;
            case OperationKind.B:
                result = "B";
                break;
            case OperationKind.XOR:
                result = "XOR";
                break;
            case OperationKind.OR:
                result = "OR";
                break;
            case OperationKind.NOR:
                result = "NOR";
                break;
            case OperationKind.XNOR:
                result = "XNOR";
                break;
            case OperationKind.NOT_B:
                result = "NOT_B";
                break;
            case OperationKind.A_OR_NOT_B:
                result = "A_OR_NOT_B";
                break;
            case OperationKind.NOT_A:
                result = "NOT_A";
                break;
            case OperationKind.NOT_A_OR_B:
                result = "NOT_A_OR_B";
                break;
            case OperationKind.NAND:
                result = "NAND";
                break;
            case OperationKind.TRUE:
                result = "TRUE";
                break;
            case OperationKind.Negate:
                result = "Negate";
                break;
            case OperationKind.Reciprocal:
                result = "Reciprocal";
                break;
            case OperationKind.FlipPolarityInPlace:
                result = "ObverseInPlace";
                break;
            case OperationKind.FlipPolarity:
                result = "Obverse";
                break;
            case OperationKind.MirrorOnUnit:
                result = "MirrorOnUnit";
                break;
            case OperationKind.MirrorOnUnot:
                result = "MirrorOnUnot";
                break;
            case OperationKind.MirrorOnStart:
                result = "MirrorOnStart";
                break;
            case OperationKind.MirrorOnEnd:
                result = "MirrorOnEnd";
                break;
            case OperationKind.FilterUnit:
                result = "FilterUnit";
                break;
            case OperationKind.FilterUnot:
                result = "FilterUnot";
                break;
            case OperationKind.FilterStart:
                result = "FilterStart";
                break;
            case OperationKind.FilterEnd:
                result = "FilterEnd";
                break;
            case OperationKind.NegateInPlace:
                result = "NegateInPlace";
                break;
            case OperationKind.Add:
                result = "Add";
                break;
            case OperationKind.Subtract:
                result = "Subtract";
                break;
            case OperationKind.Multiply:
                result = "Multiply";
                break;
            case OperationKind.Divide:
                result = "Divide";
                break;
            case OperationKind.Root:
                result = "Root";
                break;
            case OperationKind.Wedge:
                result = "Wedge";
                break;
            case OperationKind.DotProduct:
                result = "DotProduct";
                break;
            case OperationKind.CrossProduct:
                result = "CrossProduct";
                break;
            case OperationKind.GeometricProduct:
                result = "GeometricProduct";
                break;
            case OperationKind.Blend:
                result = "Blend";
                break;

            case OperationKind.GreaterThan:
                result = "GreaterThan";
                break;
            case OperationKind.GreaterThanOrEqual:
                result = "GreaterThanOrEqual";
                break;
            case OperationKind.GreaterThanAndEqual:
                result = "GreaterThanAndEqual";
                break;
            case OperationKind.ContainedBy:
                result = "ContainedBy";
                break;
            case OperationKind.Equals:
                result = "Equals";
                break;
            case OperationKind.Contains:
                result = "Contains";
                break;
            case OperationKind.LessThanAndEqual:
                result = "LessThanAndEqual";
                break;
            case OperationKind.LessThanOrEqual:
                result = "LessThanOrEqual";
                break;
            case OperationKind.LessThan:
                result = "LessThan";
                break;

            case OperationKind.Power:
                result = "Power";
                break;
            case OperationKind.AppendAll:
                result = "AppendAll";
                break;
            case OperationKind.MultiplyAll:
                result = "MultiplyAll";
                break;
            case OperationKind.Average:
                result = "Average";
                break;
        }
        return result;
    }
}


// todo: Evaluation of two segments are very much like the 16 bool operations, probably the same.
public enum EvaluationKind
{
    // can use overlap, equal, contain, not contain, minmax ranges, unit range, basis direction, number direction...

    None, // Never continue
    InTarget, // continue unless result escapes containment of EvaluationTarget
    NotInTarget,  // continue unless result enters containment of EvaluationTarget
    TargetIn,  // continue unless EvaluationTarget escapes containment of result
    NotTargetIn,  // continue unless EvaluationTarget enters containment of result
    Always, // always continue
}
