using System;
using System.Linq;
namespace SalX.Numbers;

internal static class Helpers
{
    /// <summary>
    /// Utility extension to clone templates for substitution
    /// </summary>
    public static Number CloneForSubstitution(this Number src)
    {
        return src switch
        {
            IntegerNumber ii => new IntegerNumber(ii.Value),
            FractionNumber f => new FractionNumber(f.Numerator, f.Denominator),
            DecimalNumber d => new DecimalNumber(d.Value),
            DoubleNumber dd => new DoubleNumber(dd.Value),
            VariableNumber v => new VariableNumber(v.Name),
            ConstantNumber c => new ConstantNumber(c.Name, c.Value.CloneForSubstitution()),
            LabeledValueNumber l => new LabeledValueNumber(l.Label, l.Value.CloneForSubstitution()),
            UnaryOperationNumber u => new UnaryOperationNumber(u.Op, u.Operand.CloneForSubstitution()),
            BinaryOperationNumber b => new BinaryOperationNumber(b.Op, b.Left.CloneForSubstitution(), b.Right.CloneForSubstitution()),
            FunctionCallNumber f => new FunctionCallNumber(f.Name, f.Arguments.Select((a, i) => new FunctionArgument(f.ArgumentNames[i], a.CloneForSubstitution()))),
            MethodCallNumber m => new MethodCallNumber(m.Target.CloneForSubstitution(), m.Name, m.Arguments.Select((a, i) => new FunctionArgument(m.ArgumentNames[i], a.CloneForSubstitution())), m.IsPropertyAccess),
            SequenceNumber s => new SequenceNumber(s.SequenceType, s.ConstructorName, s.FirstTerm, s.StepValue, s.KnownTerms.ToDictionary(k => k.Key, v => v.Value)),
            CoefficientNumber c => new CoefficientNumber(c.Coef.CloneForSubstitution()),
            EquationNumber e => new EquationNumber(e.Left.CloneForSubstitution(), e.Right.CloneForSubstitution()),
            _ => throw new NotSupportedException($"Clone not supported for {src.Kind}")
        };
    }
}
