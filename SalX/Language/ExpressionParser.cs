using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using SalX.Numbers;
namespace SalX.Language;

internal class ExpressionParser
{
    private readonly Lexer lex;
    private Token look;
    public ExpressionParser(string text)
    {
        lex = new Lexer(text);
        look = lex.NextToken();
    }
    
    private void Next() => look = lex.NextToken();

    public Number ParseExpression()
    {
        var expr = ParseAddSubtract();
        if (look.Type != TokenType.End)
            throw new Exception($"Unexpected token {look.Text}");
        return expr;
    }

    private Number ParseAddSubtract()
    {
        var left = ParseMultiplyDivide();
        while (look.Type == TokenType.Plus || look.Type == TokenType.Minus)
        {
            var op = look.Type;
            Next();
            var right = ParseMultiplyDivide();
            left = new BinaryOperationNumber(op == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract, left, right);
        }
        return left;
    }

    private Number ParseMultiplyDivide()
    {
        var left = ParsePower();
        while (look.Type == TokenType.Star || look.Type == TokenType.Slash || look.Type == TokenType.Percent)
        {
            var tok = look.Type;
            Next();
            var right = ParsePower();
            left = tok == TokenType.Star ? new BinaryOperationNumber(BinaryOperator.Multiply, left, right) : tok == TokenType.Slash ? new BinaryOperationNumber(BinaryOperator.Divide, left, right) : new BinaryOperationNumber(BinaryOperator.Modulus, left, right);
        }
        return left;
    }

    private Number ParsePower()
    {
        var left = ParseUnary();
        if (look.Type == TokenType.Caret)
        {
            Next();
            var right = ParseUnary();
            return new BinaryOperationNumber(BinaryOperator.Power, left, right);
        }
        return left;
    }

    private Number ParseUnary()
    {
        if (look.Type == TokenType.Minus)
        {
            Next();
            var operand = ParseUnary();
            return new UnaryOperationNumber(UnaryOperator.Negate, operand);
        }
        return ParsePrimary();
    }

    private Number ParsePrimary()
    {
        if (look.Type == TokenType.Number)
        {
            var txt = look.Text; Next();
            if (look.Type == TokenType.Slash)
            {
                Next();
                if (look.Type != TokenType.Number)
                    throw new Exception("Expected denominator");

                var den = look.Text;
                Next();

                if (txt.Contains('.') || den.Contains('.'))
                {
                    var d1 = decimal.Parse(txt, CultureInfo.InvariantCulture);
                    var d2 = decimal.Parse(den, CultureInfo.InvariantCulture);
                    return new DecimalNumber(d1 / d2);
                }
                return new FractionNumber(BigInteger.Parse(txt, CultureInfo.InvariantCulture), BigInteger.Parse(den, CultureInfo.InvariantCulture));
            }

            if (txt.Contains('.') || txt.Contains('e') || txt.Contains('E'))
            {
                if (decimal.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var dec))
                    return new DecimalNumber(dec);
                if (double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return new DoubleNumber(d);
            }
            return new IntegerNumber(BigInteger.Parse(txt, CultureInfo.InvariantCulture));
        }

        if (look.Type == TokenType.Identifier)
        {
            var id = look.Text; Next();
            if (look.Type == TokenType.LParen)
            {
                Next();
                var args = new List<Number>();
                if (look.Type != TokenType.RParen)
                    while (true)
                    {
                        args.Add(ParseAddSubtract());
                        if (look.Type == TokenType.Comma)
                        {
                            Next();
                            continue;
                        }
                        break;
                    }

                if (look.Type != TokenType.RParen)
                    throw new Exception("Expected )");
                
                Next();
                return new FunctionCallNumber(id, args);
            }
            if (ConstantRegistry.TryGet(id, out var constVal))
                return new ConstantNumber(id, constVal!.CloneForSubstitution());
            return new VariableNumber(id);
        }

        if (look.Type == TokenType.LParen)
        {
            Next();
            var inner = ParseAddSubtract();
            if (look.Type != TokenType.RParen)
                throw new Exception("Expected )");

            Next();
            return inner;
        }

        throw new Exception($"Unexpected token {look.Text}");
    }
}
