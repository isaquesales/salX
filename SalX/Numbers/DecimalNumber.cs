using System;
using System.Collections.Generic;
using System.Globalization;
namespace SalX.Numbers;

public sealed class DecimalNumber : Number
{
    public decimal Value { get; private set; }
    public override NumberKind Kind => NumberKind.Decimal;
    public override List<Number> Children => new();
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => true;

    public DecimalNumber(decimal v)
    {
        Value = v; Steps.Clear(); Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString() => Value.ToString(CultureInfo.InvariantCulture);

    public override int CompareTo(Number? other)
    {
        if (other == null) return 1;
        if (other is DecimalNumber d) return Value.CompareTo(d.Value);
        if (other is IntegerNumber i) return Value.CompareTo((decimal)i.Value);
        if (other is FractionNumber f) { var vv = (decimal)f.Numerator / (decimal)f.Denominator; return Value.CompareTo(vv); }
        if (other is DoubleNumber dd) return ((double)Value).CompareTo(dd.Value);
        return string.Compare(ToExpressionString(), other.ToExpressionString(), StringComparison.InvariantCulture);
    }

    public override Number Substitute(Dictionary<string, Number> map) => this;
}
