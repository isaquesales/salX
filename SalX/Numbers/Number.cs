using System;
using System.Collections.Generic;
using System.Linq;
using SalX.Language;
namespace SalX.Numbers;

public abstract class Number : IComparable<Number>
{
    protected readonly List<string> Steps = new List<string>();
    protected int currentStep = 0;

    public abstract NumberKind Kind { get; }
    public abstract List<Number> Children { get; }
    public abstract bool IsMainNumber { get; set; }
    public abstract bool IsConcrete { get; }
    public Number? Parent { get; set; }

    protected Number() { }

    public virtual Number EvaluateRoot()
    {
        // If this node can be reduced to a concrete Number, return that concrete number.
        // Check the common node types that implement TryEvaluate.
        if (this is FunctionCallNumber fc && fc.TryEvaluate(out var rf)) return rf;
        if (this is MethodCallNumber mc && mc.TryEvaluate(out var rm)) return rm;
        if (this is BinaryOperationNumber bn && bn.TryEvaluate(out var rb)) return rb;
        if (this is UnaryOperationNumber un && un.TryEvaluate(out var ru)) return ru;
        // default: nothing to evaluate at root
        return this;
    }

    public virtual string GetString()
    {
        if (Steps == null || Steps.Count == 0)
            return NormalizeDisplay(ToExpressionString());
        return NormalizeDisplay(Steps[Math.Clamp(currentStep, 0, Steps.Count - 1)]);
    }
    
    /// <summary>
    /// AdvanceOneStep: perform exactly one simplification step somewhere in the subtree
    /// Returns true if something changed (one step was performed)
    /// </summary>
    /// <returns></returns>
    public virtual bool AdvanceOneStep()
    {
        var kids = Children;
        for (int i = 0; i < kids.Count; i++)
        {
            var childBefore = kids[i];
            if (childBefore == null) continue;

            if (childBefore.AdvanceOneStep())
            {
                RecordStep();
                return true;
            }

            if (childBefore.SimplifyOnce())
            {
                RecordStep();
                return true;
            }

            var currentKids = Children;
            if (i < currentKids.Count && !ReferenceEquals(childBefore, currentKids[i]))
            {
                RecordStep();
                return true;
            }

        }

        if (SimplifyOnce())
        {
            RecordStep();
            return true;
        }

        return false;
    }

    /// <summary>
    /// SimplifyAllFull: fully simplify subtree until stable
    /// </summary>
    public virtual void SimplifyAllFull()
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var c in Children.ToList())
                c?.SimplifyAllFull();
            while (SimplifyOnce())
            {
                changed = true;
                RecordStep();
            }
        } while (changed);
    }

    /// <summary>
    /// Attempt a single simplification/collapse for this node. Return true if changed.
    /// </summary>
    /// <returns></returns>
    protected virtual bool SimplifyOnce()
    {
        var before = ToExpressionString();
        CollapseIfPossible();
        var after = ToExpressionString();
        return before != after;
    }

    /// <summary>
    /// Hook to collapse/evaluate node when children are concrete
    /// </summary>
    protected virtual void CollapseIfPossible() { }

    /// <summary>
    /// Utility to record a new step snapshot
    /// </summary>
    protected void RecordStep()
    {
        var s = ToExpressionString();
        if (Steps.Count == 0 || Steps[^1] != s)
        {
            Steps.Add(s);
            currentStep = Steps.Count - 1;
        }
    }

    public abstract string ToExpressionString();

    /// <summary>
    /// Parse wrapper
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static Number Parse(string expression)
    {
        var p = new ExpressionParser(expression);
        return p.ParseExpression();
    }

    public virtual Number Add(Number other) => new BinaryOperationNumber(BinaryOperator.Add, this, other);
    public virtual Number Sub(Number other) => new BinaryOperationNumber(BinaryOperator.Subtract, this, other);
    public virtual Number Mul(Number other) => new BinaryOperationNumber(BinaryOperator.Multiply, this, other);
    public virtual Number Div(Number other) => new BinaryOperationNumber(BinaryOperator.Divide, this, other);
    public virtual Number Mod(Number other) => new BinaryOperationNumber(BinaryOperator.Modulus, this, other);
    public virtual Number Pow(Number other) => new BinaryOperationNumber(BinaryOperator.Power, this, other);
    public virtual Number Negate() => new UnaryOperationNumber(UnaryOperator.Negate, this);

    public abstract int CompareTo(Number? other);

    // For substitution when evaluating user-defined functions
    public virtual Number Substitute(Dictionary<string, Number> map)
        => this;

    public virtual Number CloneShallow()
        => this;

    public override string ToString()
        => ToExpressionString();

    private static string NormalizeDisplay(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var s = text.Trim();
        while (HasSingleOuterParens(s))
            s = s[1..^1].Trim();
        return s;
    }

    private static bool HasSingleOuterParens(string s)
    {
        if (s.Length < 2 || s[0] != '(' || s[^1] != ')')
            return false;

        int depth = 0;
        for (int i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (ch == '(') depth++;
            else if (ch == ')') depth--;

            if (depth < 0)
                return false;

            // If we close the initial '(' before the end, then outer parens do not wrap all text.
            if (depth == 0 && i < s.Length - 1)
                return false;
        }

        return depth == 0;
    }
}
