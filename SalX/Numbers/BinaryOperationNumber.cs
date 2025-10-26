using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
namespace SalX.Numbers;

public sealed class BinaryOperationNumber : Number
{
    public BinaryOperator Op { get; private set; }
    public Number Left { get; set; }
    public Number Right { get; set; }
    public override NumberKind Kind => NumberKind.BinaryOperation;
    public override List<Number> Children => new() { Left, Right };
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => Left.IsConcrete && Right.IsConcrete && TryEvaluate(out _);

    public BinaryOperationNumber(BinaryOperator op, Number left, Number right)
    {
        Op = op;
        Left = left;
        Right = right;
        Left.Parent = this;
        Right.Parent = this;
        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString()
    {
        string opSym = Op switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Modulus => "%",
            BinaryOperator.Power => "^",
            _ => "?"
        };

        var leftStr = Left != null ? Left.ToExpressionString() : "?";
        var rightStr = Right != null ? Right.ToExpressionString() : "?";
        return $"({leftStr} {opSym} {rightStr})";
    }

    /// <summary>
    /// Try evaluate into a concrete Number
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException"></exception>
    public bool TryEvaluate(out Number result)
    {
        result = null!;
        if (!Left.IsConcrete || !Right.IsConcrete) return false;

        // Prefer rational arithmetic for integer/fraction operands
        if ((Left is IntegerNumber || Left is FractionNumber) && (Right is IntegerNumber || Right is FractionNumber))
        {
            var a = ToFraction(Left);
            var b = ToFraction(Right);
            switch (Op)
            {
                case BinaryOperator.Add: return TryReturnFraction(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator, out result);
                case BinaryOperator.Subtract: return TryReturnFraction(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator, out result);
                case BinaryOperator.Multiply: return TryReturnFraction(a.Numerator * b.Numerator, a.Denominator * b.Denominator, out result);
                case BinaryOperator.Divide:
                    if (b.Numerator.IsZero) throw new DivideByZeroException();
                    return TryReturnFraction(a.Numerator * b.Denominator, a.Denominator * b.Numerator, out result);
                case BinaryOperator.Modulus:
                    if (a.Denominator.IsOne && b.Denominator.IsOne)
                    {
                        var mod = a.Numerator % b.Numerator;
                        result = new IntegerNumber(mod);
                        return true;
                    }
                    break;
                case BinaryOperator.Power:
                    if (b.Denominator.IsOne && b.Numerator >= 0 && b.Numerator <= 20) // small exponent
                    {
                        int exp = (int)b.Numerator;
                        var numPow = BigInteger.Pow(a.Numerator, exp);
                        var denPow = BigInteger.Pow(a.Denominator, exp);
                        return TryReturnFraction(numPow, denPow, out result);
                    }
                    break;
            }
        }

        // Fallback to double arithmetic
        double ld = ToDouble(Left);
        double rd = ToDouble(Right);
        double rv = Op switch
        {
            BinaryOperator.Add => ld + rd,
            BinaryOperator.Subtract => ld - rd,
            BinaryOperator.Multiply => ld * rd,
            BinaryOperator.Divide => Math.Abs(rd) < double.Epsilon ? throw new DivideByZeroException() : ld / rd,
            BinaryOperator.Modulus => ld % rd,
            BinaryOperator.Power => Math.Pow(ld, rd),
            _ => double.NaN
        };

        // SAFETY: do not accept NaN/Infinity as a valid evaluation result.
        // Return false so the evaluation is considered not possible (keeps symbolic form).
        if (double.IsNaN(rv) || double.IsInfinity(rv))
        {
            result = null!;
            return false;
        }

        result = new DoubleNumber(rv);
        return true;
    }

    private static bool TryReturnFraction(BigInteger num, BigInteger den, out Number r)
    {
        r = null!;
        if (den.IsZero)
            throw new DivideByZeroException();
        
        var frac = new FractionNumber(num, den);
        r = frac;
        return true;
    }

    private static FractionNumber ToFraction(Number n)
    {
        if (n is FractionNumber f) return f;
        if (n is IntegerNumber i) return new FractionNumber(i.Value, BigInteger.One);
        if (n is DecimalNumber dec)
        {
            var s = dec.Value.ToString(CultureInfo.InvariantCulture);
            if (s.Contains('.'))
            {
                var parts = s.Split('.');
                var whole = BigInteger.Parse(parts[0]);
                var frac = parts[1];
                var denom = BigInteger.Pow(10, frac.Length);
                var numer = whole * denom + BigInteger.Parse(frac);
                return new FractionNumber(numer, denom);
            }
            else return new FractionNumber(new BigInteger(dec.Value), BigInteger.One);
        }
        if (n is DoubleNumber d)
            return new FractionNumber(new BigInteger(d.Value), BigInteger.One);
        throw new NotSupportedException();
    }

    public static double ToDouble(Number n)
    {
        return n switch
        {
            DoubleNumber dd => dd.Value,
            DecimalNumber dec => (double)dec.Value,
            IntegerNumber ii => (double)ii.Value,
            FractionNumber f => (double)f.Numerator / (double)f.Denominator,
            ConstantNumber c => ToDouble(c.Value),
            _ => double.NaN
        };
    }

    protected override void CollapseIfPossible()
    {
        if (Left.IsConcrete && Right.IsConcrete && TryEvaluate(out var r))
        {
            // Replace in parent if present
            if (Parent is BinaryOperationNumber pb)
            {
                if (pb.Left == this)
                {
                    pb.Left = r;
                    r.Parent = pb;
                }
                else if (pb.Right == this)
                {
                    pb.Right = r;
                    r.Parent = pb;
                }
            }
            else if (Parent is FunctionCallNumber pf)
                for (int i = 0; i < pf.Arguments.Count; i++)
                    if (pf.Arguments[i] == this)
                    {
                        pf.Arguments[i] = r;
                        r.Parent = pf;
                        break;
                    }
            else
                RecordStep();
        }
    }

    public override int CompareTo(Number? other) => ToExpressionString().CompareTo(other?.ToExpressionString());

    public override Number Substitute(Dictionary<string, Number> map)
        => new BinaryOperationNumber(Op, Left.Substitute(map), Right.Substitute(map));

    public override Number CloneShallow() => new BinaryOperationNumber(Op, Left, Right);
}
