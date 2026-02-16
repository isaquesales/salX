using System;
using System.Collections.Generic;
using System.Linq;
namespace SalX.Numbers;

public sealed class FunctionCallNumber : Number
{
    public string Name { get; private set; }
    public List<Number> Arguments { get; private set; } = new();
    public List<string?> ArgumentNames { get; private set; } = new();
    public override NumberKind Kind => NumberKind.FunctionCall;
    public override List<Number> Children => Arguments;
    public override bool IsMainNumber { get; set; } = false;

    /// <summary>
    /// A function call is considered concrete when:
    ///  - all arguments are concrete AND
    ///  - there is a builtin evaluator (safe numeric leaf evaluation)
    /// For user-defined templates and sequence constructors we keep IsConcrete=false
    /// so symbolic expansion / method dispatch can run.
    /// </summary>
    public override bool IsConcrete
        => !SequenceNumber.IsConstructorName(Name)
           && Arguments.All(a => a.IsConcrete)
           && FunctionRegistry.TryGet(Name, out var d)
           && d?.BuiltinEvaluator != null;

    public FunctionCallNumber(string name, IEnumerable<Number> args)
        : this(name, args.Select(a => new FunctionArgument(null, a)))
    {
    }

    public FunctionCallNumber(string name, IEnumerable<FunctionArgument> args)
    {
        Name = name.ToLowerInvariant();
        foreach (var arg in args)
        {
            Arguments.Add(arg.Value);
            ArgumentNames.Add(arg.Name);
            arg.Value.Parent = this;
        }

        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString()
    {
        var renderedArgs = Arguments.Select((arg, idx) =>
        {
            var argName = ArgumentNames[idx];
            return string.IsNullOrWhiteSpace(argName)
                ? arg.ToExpressionString()
                : $"{argName}: {arg.ToExpressionString()}";
        });

        return $"{Name}({string.Join(", ", renderedArgs)})";
    }

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
        if (!ReferenceEquals(finalEval, node))
            node = finalEval;
        return node;
    }

    public bool TryEvaluate(out Number result)
    {
        result = null!;

        if (SequenceNumber.IsConstructorName(Name))
        {
            if (!Arguments.All(a => IsNumericLeaf(a)))
                return false;

            result = SequenceNumber.CreateFromCall(Name, Arguments, ArgumentNames);
            return true;
        }

        if (FunctionRegistry.TryGet(Name, out var def) && def?.BuiltinEvaluator != null)
        {
            var orderedArgs = ReorderArgumentsForDefinition(def, allowMissing: false, allowExtraPositional: false);
            if (!orderedArgs.All(IsNumericLeaf))
                return false;

            var ds = orderedArgs.Select(BinaryOperationNumber.ToDouble).ToArray();
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
            var orderedArgs = ReorderArgumentsForDefinition(def, allowMissing: true, allowExtraPositional: true);

            var map = new Dictionary<string, Number>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < paramNames.Count; i++)
            {
                map[paramNames[i]] = i < orderedArgs.Count
                    ? orderedArgs[i]
                    : new IntegerNumber(0);
            }

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

                        if (left.IsConcrete
                            && right.IsConcrete
                            && rebuilt.TryEvaluate(out var r)
                            && !(r is DoubleNumber dr && double.IsNaN(dr.Value)))
                            return r;
                        return rebuilt;
                    }

                    if (node is UnaryOperationNumber uNode)
                    {
                        var operand = EvaluateRationalNodes(uNode.Operand);
                        var rebuilt = new UnaryOperationNumber(uNode.Op, operand);
                        if (operand.IsConcrete
                            && rebuilt.TryEvaluate(out var r)
                            && !(r is DoubleNumber dr && double.IsNaN(dr.Value)))
                            return r;
                        return rebuilt;
                    }

                    if (node is FunctionCallNumber fNode)
                    {
                        var args = fNode.Arguments
                            .Select((a, idx) => new FunctionArgument(fNode.ArgumentNames[idx], EvaluateRationalNodes(a)))
                            .ToList();
                        var rebuiltF = new FunctionCallNumber(fNode.Name, args);

                        if (args.All(a => a.Value.IsConcrete))
                        {
                            var dsLocal = args.Select(a => BinaryOperationNumber.ToDouble(a.Value)).ToArray();
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
                                catch
                                {
                                }
                            }
                        }

                        return rebuiltF;
                    }

                    if (node is MethodCallNumber mNode)
                    {
                        var target = EvaluateRationalNodes(mNode.Target);
                        var args = mNode.Arguments
                            .Select((a, idx) => new FunctionArgument(mNode.ArgumentNames[idx], EvaluateRationalNodes(a)))
                            .ToList();
                        return new MethodCallNumber(target, mNode.Name, args, mNode.IsPropertyAccess);
                    }

                    if (node is ConstantNumber c)
                        return new ConstantNumber(c.Name, EvaluateRationalNodes(c.Value));

                    if (node is LabeledValueNumber l)
                        return new LabeledValueNumber(l.Label, EvaluateRationalNodes(l.Value));

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

    private List<Number> ReorderArgumentsForDefinition(FunctionDefinition def, bool allowMissing, bool allowExtraPositional)
    {
        if (def.Parameters.Count == 0)
        {
            if (ArgumentNames.Any(n => !string.IsNullOrWhiteSpace(n)))
                throw new ArgumentException($"A função {Name} não aceita argumentos nomeados.");
            return Arguments.ToList();
        }

        if (!ArgumentNames.Any(n => !string.IsNullOrWhiteSpace(n)))
            return Arguments.ToList();

        var ordered = new Number?[def.Parameters.Count];
        int nextPositional = 0;

        for (int i = 0; i < Arguments.Count; i++)
        {
            var arg = Arguments[i];
            var argName = ArgumentNames[i];

            if (string.IsNullOrWhiteSpace(argName))
            {
                while (nextPositional < ordered.Length && ordered[nextPositional] != null)
                    nextPositional++;

                if (nextPositional >= ordered.Length)
                {
                    if (!allowExtraPositional)
                        throw new ArgumentException($"Muitos argumentos posicionais em {Name}.");
                    continue;
                }

                ordered[nextPositional++] = arg;
                continue;
            }

            var idx = def.Parameters.FindIndex(p => string.Equals(p, argName, StringComparison.InvariantCultureIgnoreCase));
            if (idx < 0)
                throw new ArgumentException($"Argumento nomeado desconhecido em {Name}: {argName}");
            if (ordered[idx] != null)
                throw new ArgumentException($"Argumento duplicado em {Name}: {argName}");
            ordered[idx] = arg;
        }

        var result = new List<Number>(ordered.Length);
        for (int i = 0; i < ordered.Length; i++)
        {
            if (ordered[i] != null)
            {
                result.Add(ordered[i]!);
                continue;
            }

            if (allowMissing)
                result.Add(new IntegerNumber(0));
            else
                throw new ArgumentException($"Argumento obrigatório ausente em {Name}: {def.Parameters[i]}");
        }

        return result;
    }

    private static bool IsNumericLeaf(Number n)
    {
        return n is IntegerNumber
            || n is FractionNumber
            || n is DecimalNumber
            || n is DoubleNumber
            || n is LabeledValueNumber l && IsNumericLeaf(l.Value)
            || n is ConstantNumber c && IsNumericLeaf(c.Value);
    }

    public override Number CloneShallow()
    {
        var args = Arguments.Select((a, i) => new FunctionArgument(ArgumentNames[i], a.CloneForSubstitution())).ToList();
        return new FunctionCallNumber(Name, args);
    }

    public override Number Substitute(Dictionary<string, Number> map)
    {
        var newArgs = Arguments.Select((a, i) => new FunctionArgument(ArgumentNames[i], a.Substitute(map))).ToList();
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

            if (Parent is MethodCallNumber pm)
            {
                if (pm.Target == this)
                {
                    pm.Target = r;
                    r.Parent = pm;
                    return;
                }

                for (int i = 0; i < pm.Arguments.Count; i++)
                {
                    if (pm.Arguments[i] == this)
                    {
                        pm.Arguments[i] = r;
                        r.Parent = pm;
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

            RecordStep();
        }
    }

    public override int CompareTo(Number? other)
        => string.Compare(ToExpressionString(), other?.ToExpressionString(), StringComparison.InvariantCulture);
}
