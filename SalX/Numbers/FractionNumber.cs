using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
namespace SalX.Numbers;

public sealed class FractionNumber : Number
{
    public BigInteger Numerator { get; private set; }
    public BigInteger Denominator { get; private set; }
    public override NumberKind Kind => NumberKind.Fraction;
    public override List<Number> Children => new();
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => true;

    public FractionNumber(BigInteger num, BigInteger den)
    {
        if (den.IsZero) throw new DivideByZeroException();
        Numerator = num; Denominator = den;
        Normalize();
        Steps.Clear(); Steps.Add(ToExpressionString());
    }

    void Normalize()
    {
        if (Denominator < 0) { Numerator = BigInteger.Negate(Numerator); Denominator = BigInteger.Negate(Denominator); }
        var g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(Numerator), BigInteger.Abs(Denominator));
        
        if (g > 1)
        {
            Numerator /= g;
            Denominator /= g;
        }
    }

    public override string ToExpressionString()
    {
        if (Denominator.IsOne)
            return Numerator.ToString(CultureInfo.InvariantCulture);
        return $"{Numerator.ToString(CultureInfo.InvariantCulture)}/{Denominator.ToString(CultureInfo.InvariantCulture)}";
    }

    public override int CompareTo(Number? other)
    {
        if (other == null) return 1;
        if (other is FractionNumber f) return (Numerator * f.Denominator).CompareTo(f.Numerator * Denominator);
        if (other is IntegerNumber i) return Numerator.CompareTo(i.Value * Denominator);
        if (other is DecimalNumber dec)
        {
            var vv = (decimal)Numerator / (decimal)Denominator;
            return vv.CompareTo(dec.Value);
        }
        if (other is DoubleNumber d)
        {
            var vv = (double)Numerator / (double)Denominator;
            return vv.CompareTo(d.Value);
        }
        return string.Compare(ToExpressionString(), other.ToExpressionString(), StringComparison.InvariantCulture);
    }

    public override Number Substitute(Dictionary<string, Number> map) => this;
}
