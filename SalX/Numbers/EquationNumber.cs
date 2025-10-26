using System.Collections.Generic;
namespace SalX.Numbers;

public sealed class EquationNumber : Number
{
    public Number Left { get; private set; }
    public Number Right { get; private set; }
    public override NumberKind Kind => NumberKind.Equation;
    public override List<Number> Children => new() { Left, Right };
    public override bool IsMainNumber { get; set; } = true;
    public override bool IsConcrete => Left.IsConcrete && Right.IsConcrete;
    public EquationNumber(Number left, Number right) { Left = left; Right = right; Left.Parent = this; Right.Parent = this; Steps.Clear(); Steps.Add(ToExpressionString()); }
    public override string ToExpressionString() => $"{Left.ToExpressionString()} = {Right.ToExpressionString()}";
    public override int CompareTo(Number? other) => ToExpressionString().CompareTo(other?.ToExpressionString());
    public override Number Substitute(Dictionary<string, Number> map) => new EquationNumber(Left.Substitute(map), Right.Substitute(map));
}
