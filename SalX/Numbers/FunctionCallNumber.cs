using System;
using System.Collections.Generic;
using System.Linq;
namespace SalX.Numbers;

public sealed class FunctionCallNumber : Number
{
    public string Name { get; private set; }
    public List<Number> Arguments { get; private set; } = new();
    public override NumberKind Kind => NumberKind.FunctionCall;
    public override List<Number> Children => Arguments;
    public override bool IsMainNumber { get; set; } = false;

    /// <summary>
    /// A function call is considered concrete when:
    ///  - all arguments are concrete AND
    ///  - there is a builtin evaluator (safe numeric leaf evaluation)
    /// For user-defined templates we keep IsConcrete=false so stepwise expansion can run.
    ///  </summary>
    public override bool IsConcrete
        => Arguments.All(a => a.IsConcrete) 
           && FunctionRegistry.TryGet(Name, out var d) 
           && d?.BuiltinEvaluator != null;
    
    public FunctionCallNumber(string name, IEnumerable<Number> args)
    {
        Name = name.ToLowerInvariant();
        Arguments.AddRange(args.Select(a => a));
        foreach (var a in Arguments)
            a.Parent = this;

        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString() => $"{Name}({string.Join(", ", Arguments.Select(a => a.ToExpressionString()))})";

    private static Number SimplifyAndEvaluateLocal(Number node)
    {
        bool changed;
        do
        {
            changed = false;
            while (node.AdvanceOneStep())
                changed = true;

            var evaluated = node.EvaluateRoot();
            if (!ReferenceEquals(evaluated, node))
            {
                node = evaluated;
                changed = true;
            }

        } while (changed);

        node.SimplifyAllFull();
        var finalEval = node.EvaluateRoot();
        if (!object.ReferenceEquals(finalEval, node)) node = finalEval;
        return node;
    }

    public bool TryEvaluate(out Number result)
    {
        result = null!;

        if (FunctionRegistry.TryGet(Name, out var def) && def?.BuiltinEvaluator != null)
        {
            if (!Arguments.All(a => IsNumericLeaf(a)))
                return false;

            var ds = Arguments.Select(a => BinaryOperationNumber.ToDouble(a)).ToArray();
            if (ds.Any(d => double.IsNaN(d) || double.IsInfinity(d)))
                return false;

            try
            {
                var rv = def.BuiltinEvaluator(ds);

                if (string.Equals(Name, "rad", StringComparison.InvariantCultureIgnoreCase) && ds.Length >= 1)
                {
                    if (Math.Abs(rv - Math.PI) < 1e-12)
                    {
                        result = new ConstantNumber("pi", new DoubleNumber(Math.PI));
                        return true;
                    }
                }

                if (double.IsNaN(rv) || double.IsInfinity(rv))
                    return false;

                result = new DoubleNumber(rv);
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (FunctionRegistry.TryGet(Name, out def) && def?.Template != null)
        {
            var paramNames = def.Parameters;
            var map = new Dictionary<string, Number>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < paramNames.Count; i++)
                map[paramNames[i]] = i < Arguments.Count ? Arguments[i] : new IntegerNumber(0);

            var bodyClone = def.Template.CloneForSubstitution();
            var substituted = bodyClone.Substitute(map);

            if (substituted.AdvanceOneStep())
            {
                result = substituted;
                return true;
            }

            var maybeEval = substituted.EvaluateRoot();
            if (!ReferenceEquals(maybeEval, substituted))
            {
                result = maybeEval;
                return true;
            }

            var final = SimplifyAndEvaluateLocal(substituted);

            if (final is DoubleNumber dfn && double.IsNaN(dfn.Value))
            {
                Number EvaluateRationalNodes(Number node)
                {
                    if (node is BinaryOperationNumber bNode)
                    {
                        var left = EvaluateRationalNodes(bNode.Left);
                        var right = EvaluateRationalNodes(bNode.Right);
                        var rebuilt = new BinaryOperationNumber(bNode.Op, left, right);

                        if (left.IsConcrete && right.IsConcrete && rebuilt.TryEvaluate(out var r) && !(r is DoubleNumber dr && double.IsNaN(dr.Value)))
                            return r;
                        return rebuilt;
                    }
                    else if (node is UnaryOperationNumber uNode)
                    {
                        var operand = EvaluateRationalNodes(uNode.Operand);
                        var rebuilt = new UnaryOperationNumber(uNode.Op, operand);
                        if (operand.IsConcrete && rebuilt.TryEvaluate(out var r) && !(r is DoubleNumber dr && double.IsNaN(dr.Value)))
                            return r;
                        return rebuilt;
                    }
                    else if (node is FunctionCallNumber fNode)
                    {
                        var args = fNode.Arguments.Select(a => EvaluateRationalNodes(a)).ToList();
                        var rebuiltF = new FunctionCallNumber(fNode.Name, args);

                        if (args.All(a => a.IsConcrete))
                        {
                            var dsLocal = args.Select(a => BinaryOperationNumber.ToDouble(a)).ToArray();
                            if (!dsLocal.Any(x => double.IsNaN(x) || double.IsInfinity(x))
                                && FunctionRegistry.TryGet(fNode.Name, out var defLocal)
                                && defLocal?.BuiltinEvaluator != null)
                            {
                                try
                                {
                                    var rvLocal = defLocal.BuiltinEvaluator(dsLocal);
                                    if (!double.IsNaN(rvLocal) && !double.IsInfinity(rvLocal))
                                        return new DoubleNumber(rvLocal);
                                }
                                catch { }
                            }
                        }

                        return rebuiltF;
                    }
                    else if (node is ConstantNumber c)
                        return new ConstantNumber(c.Name, EvaluateRationalNodes(c.Value));
                    else
                        return node.CloneForSubstitution();
                }

                var repaired = EvaluateRationalNodes(substituted);
                repaired.SimplifyAllFull();
                var repairedEval = repaired.EvaluateRoot();
                if (!(repairedEval is DoubleNumber dr2 && double.IsNaN(dr2.Value)))
                {
                    result = repairedEval;
                    return true;
                }

                result = substituted;
                return true;
            }

            result = final;
            return true;
        }

        return false;
    }

    private static bool IsNumericLeaf(Number n)
    {
        return n is IntegerNumber || n is FractionNumber || n is DecimalNumber || n is DoubleNumber
            || (n is ConstantNumber c && IsNumericLeaf(c.Value));
    }

    public override Number CloneShallow()
    {
        var args = Arguments.Select(a => a.CloneForSubstitution()).ToList();
        return new FunctionCallNumber(Name, args);
    }

    public override Number Substitute(Dictionary<string, Number> map)
    {
        var newArgs = Arguments.Select(a => a.Substitute(map)).ToList();
        return new FunctionCallNumber(Name, newArgs);
    }

    protected override void CollapseIfPossible()
    {
        if (Arguments.All(a => a.IsConcrete) && TryEvaluate(out var r))
        {
            if (Parent is BinaryOperationNumber pb)
            {
                if (pb.Left == this) { pb.Left = r; r.Parent = pb; return; }
                if (pb.Right == this) { pb.Right = r; r.Parent = pb; return; }
            }

            if (Parent is FunctionCallNumber pf)
            {
                for (int i = 0; i < pf.Arguments.Count; i++)
                {
                    if (pf.Arguments[i] == this)
                    {
                        pf.Arguments[i] = r;
                        r.Parent = pf;
                        return;
                    }
                }
            }

            if (Parent is UnaryOperationNumber un)
            {
                if (un.Operand == this)
                {
                    un.Operand = r;
                    r.Parent = un;
                    return;
                }
            }

            if (Parent != null)
            {
                var plist = Parent.Children;
                for (int i = 0; i < plist.Count; i++)
                {
                    if (ReferenceEquals(plist[i], this))
                    {
                        break;
                    }
                }
            }

            RecordStep();
        }
    }

    public override int CompareTo(Number? other) => ToExpressionString().CompareTo(other?.ToExpressionString());
}
