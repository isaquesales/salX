using System;
using System.Collections.Generic;
namespace SalX.Numbers;

public sealed class VariableNumber : Number
{
    public string Name { get; private set; }
    public override NumberKind Kind => NumberKind.Variable;
    public override List<Number> Children => new();
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => false;
    public VariableNumber(string name) { Name = name; Steps.Clear(); Steps.Add(ToExpressionString()); }
    public override string ToExpressionString() => Name;
    public override int CompareTo(Number? other) => string.Compare(Name, other?.ToExpressionString(), StringComparison.InvariantCulture);
    public override Number Substitute(Dictionary<string, Number> map) { if (map != null && map.TryGetValue(Name, out var v)) return v; return this; }
}
