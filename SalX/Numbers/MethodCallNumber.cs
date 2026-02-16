using System;
using System.Collections.Generic;
using System.Linq;
namespace SalX.Numbers;

public sealed class MethodCallNumber : Number
{
    public Number Target { get; set; }
    public string Name { get; private set; }
    public List<Number> Arguments { get; private set; } = new();
    public List<string?> ArgumentNames { get; private set; } = new();
    public bool IsPropertyAccess { get; private set; }

    public override NumberKind Kind => NumberKind.MethodCall;
    public override List<Number> Children
    {
        get
        {
            var list = new List<Number>(1 + Arguments.Count) { Target };
            list.AddRange(Arguments);
            return list;
        }
    }
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => Target.IsConcrete && Arguments.All(a => a.IsConcrete) && TryEvaluate(out _);

    public MethodCallNumber(Number target, string name, IEnumerable<FunctionArgument> args, bool isPropertyAccess)
    {
        Target = target;
        Name = name.ToLowerInvariant();
        IsPropertyAccess = isPropertyAccess;

        foreach (var arg in args)
        {
            Arguments.Add(arg.Value);
            ArgumentNames.Add(arg.Name);
            arg.Value.Parent = this;
        }

        Target.Parent = this;
        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public MethodCallNumber(Number target, string name, IEnumerable<Number> args, bool isPropertyAccess = false)
        : this(target, name, args.Select(a => new FunctionArgument(null, a)), isPropertyAccess)
    {
    }

    public override string ToExpressionString()
    {
        var target = Target.ToExpressionString();
        if (IsPropertyAccess && Arguments.Count == 0)
            return $"{target}.{Name}";

        var renderedArgs = Arguments.Select((arg, idx) =>
        {
            var argName = ArgumentNames[idx];
            return string.IsNullOrWhiteSpace(argName)
                ? arg.ToExpressionString()
                : $"{argName}: {arg.ToExpressionString()}";
        });

        return $"{target}.{Name}({string.Join(", ", renderedArgs)})";
    }

    public bool TryEvaluate(out Number result)
    {
        result = null!;
        if (!Target.IsConcrete || Arguments.Any(a => !a.IsConcrete))
            return false;

        if (Target is SequenceNumber seq && seq.TryInvoke(Name, Arguments, ArgumentNames, out var seqResult))
        {
            result = seqResult;
            return true;
        }

        return false;
    }

    protected override void CollapseIfPossible()
    {
        if (TryEvaluate(out var r))
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

            if (Parent is UnaryOperationNumber un && un.Operand == this)
            {
                un.Operand = r;
                r.Parent = un;
                return;
            }

            if (Parent is LabeledValueNumber lv && lv.Value == this)
            {
                lv.SetValue(r);
                r.Parent = lv;
                return;
            }

            RecordStep();
        }
    }

    public override int CompareTo(Number? other)
        => string.Compare(ToExpressionString(), other?.ToExpressionString(), StringComparison.InvariantCulture);

    public override Number Substitute(Dictionary<string, Number> map)
    {
        var target = Target.Substitute(map);
        var args = Arguments.Select((arg, idx) => new FunctionArgument(ArgumentNames[idx], arg.Substitute(map)));
        return new MethodCallNumber(target, Name, args, IsPropertyAccess);
    }

    public override Number CloneShallow()
    {
        var target = Target.CloneForSubstitution();
        var args = Arguments.Select((arg, idx) => new FunctionArgument(ArgumentNames[idx], arg.CloneForSubstitution()));
        return new MethodCallNumber(target, Name, args, IsPropertyAccess);
    }
}
