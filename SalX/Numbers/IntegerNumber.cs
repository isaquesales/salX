using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
namespace SalX.Numbers;

public sealed class IntegerNumber : Number
{
    public BigInteger Value { get; private set; }
    public override NumberKind Kind => NumberKind.Integer;
    public override List<Number> Children => new();
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => true;

    public IntegerNumber(BigInteger v)
    {
        Value = v;
        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString() => Value.ToString(CultureInfo.InvariantCulture);

    public override int CompareTo(Number? other)
    {
        if (other == null) return 1;
        if (other is IntegerNumber ii) return Value.CompareTo(ii.Value);
        if (other is FractionNumber f) return (Value * f.Denominator).CompareTo(f.Numerator);
        if (other is DecimalNumber dec) return ((decimal)Value).CompareTo(dec.Value);
        if (other is DoubleNumber d) return ((double)Value).CompareTo(d.Value);
        return string.Compare(ToExpressionString(), other.ToExpressionString(), StringComparison.InvariantCulture);
    }

    public override Number Substitute(Dictionary<string, Number> map) => this; // leaf
}
