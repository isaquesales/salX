using System;
using System.Collections.Generic;
using System.Globalization;
namespace SalX.Numbers;

public sealed class DoubleNumber : Number
{
    public double Value { get; private set; }
    public override NumberKind Kind => NumberKind.Double;
    public override List<Number> Children => new();
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => true;

    public DoubleNumber(double v) { Value = v; Steps.Clear(); Steps.Add(ToExpressionString()); }
    public override string ToExpressionString() => Value.ToString("G17", CultureInfo.InvariantCulture);
    public override int CompareTo(Number? other)
    {
        if (other == null) return 1;
        if (other is DoubleNumber d) return Value.CompareTo(d.Value);
        if (other is IntegerNumber i) return Value.CompareTo((double)i.Value);
        if (other is FractionNumber f) return Value.CompareTo((double)f.Numerator / (double)f.Denominator);
        if (other is DecimalNumber dec) return Value.CompareTo((double)dec.Value);
        return string.Compare(ToExpressionString(), other.ToExpressionString(), StringComparison.InvariantCulture);
    }

    public override Number Substitute(Dictionary<string, Number> map) => this;
}
