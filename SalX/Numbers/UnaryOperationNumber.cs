using System.Collections.Generic;
using System.Numerics;
namespace SalX.Numbers;

public sealed class UnaryOperationNumber : Number
{
    public UnaryOperator Op { get; private set; }
    public Number Operand { get; set; }
    public override NumberKind Kind => NumberKind.UnaryOperation;
    public override List<Number> Children => new() { Operand };
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => Operand.IsConcrete && TryEvaluate(out _);

    public UnaryOperationNumber(UnaryOperator op, Number operand)
    {
        Op = op;
        Operand = operand;
        Operand.Parent = this;
        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString() => Op == UnaryOperator.Negate ? $"(-{Operand.ToExpressionString()})" : $"({Op} {Operand.ToExpressionString()})";

    public bool TryEvaluate(out Number result)
    {
        result = null!; if (!Operand.IsConcrete) return false;
        var baseOperand = Operand is LabeledValueNumber l ? l.Value : Operand;
        if (baseOperand is IntegerNumber ii && Op == UnaryOperator.Negate) { result = new IntegerNumber(BigInteger.Negate(ii.Value)); return true; }
        if (baseOperand is FractionNumber f && Op == UnaryOperator.Negate) { result = new FractionNumber(BigInteger.Negate(f.Numerator), f.Denominator); return true; }
        double v = BinaryOperationNumber.ToDouble(baseOperand);
        if (Op == UnaryOperator.Negate) { result = new DoubleNumber(-v); return true; }
        return false;
    }

    protected override void CollapseIfPossible()
    {
        if (Operand.IsConcrete && TryEvaluate(out var r))
        {
            if (Parent is BinaryOperationNumber pb)
            {
                if (pb.Left == this) { pb.Left = r; r.Parent = pb; }
                else if (pb.Right == this) { pb.Right = r; r.Parent = pb; }
            }
            else if (Parent is FunctionCallNumber pf)
            {
                for (int i = 0; i < pf.Arguments.Count; i++)
                    if (pf.Arguments[i] == this)
                    {
                        pf.Arguments[i] = r;
                        r.Parent = pf;
                        break;
                    }
            }
            else if (Parent is MethodCallNumber pm)
            {
                if (pm.Target == this)
                {
                    pm.Target = r;
                    r.Parent = pm;
                }
                else
                {
                    for (int i = 0; i < pm.Arguments.Count; i++)
                        if (pm.Arguments[i] == this)
                        {
                            pm.Arguments[i] = r;
                            r.Parent = pm;
                            break;
                        }
                }
            }
            else if (Parent is LabeledValueNumber lv && lv.Value == this)
            {
                lv.SetValue(r);
                r.Parent = lv;
            }
            else RecordStep();
        }
    }

    public override int CompareTo(Number? other) => ToExpressionString().CompareTo(other?.ToExpressionString());
    public override Number Substitute(Dictionary<string, Number> map) => new UnaryOperationNumber(Op, Operand.Substitute(map));
    public override Number CloneShallow() => new UnaryOperationNumber(Op, Operand);
}
