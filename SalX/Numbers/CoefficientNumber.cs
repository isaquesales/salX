using System.Collections.Generic;
namespace SalX.Numbers;

public sealed class CoefficientNumber : Number
{
    public Number Coef { get; private set; }
    public override NumberKind Kind => NumberKind.Coefficient;
    public override List<Number> Children => new() { Coef };
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => Coef.IsConcrete;
    public CoefficientNumber(Number coef)
    {
        Coef = coef;
        Coef.Parent = this;
        Steps.Clear();
        Steps.Add(ToExpressionString());
    }
    
    public override string ToExpressionString() => $"{Coef.ToExpressionString()}*";
    public override int CompareTo(Number? other) => ToExpressionString().CompareTo(other?.ToExpressionString());
    public override Number Substitute(Dictionary<string, Number> map) => new CoefficientNumber(Coef.Substitute(map));
}
