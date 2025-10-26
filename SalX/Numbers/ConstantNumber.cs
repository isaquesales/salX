using System;
using System.Collections.Generic;
namespace SalX.Numbers;

public sealed class ConstantNumber : Number
{
    public string Name { get; private set; }
    public Number Value { get; private set; }
    public override NumberKind Kind => NumberKind.Constant;
    public override List<Number> Children => new() { Value };
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => Value.IsConcrete;

    public ConstantNumber(string name, Number value)
    {
        Name = name;
        Value = value;
        Value.Parent = this;
        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString() => Name;

    public override Number Substitute(Dictionary<string, Number> map)
    {
        return this; // constants remain symbolic unless collapsed
    }

    protected override void CollapseIfPossible()
    {
    }

    public void ExpandToValue()
    {
        if (Parent == null) return;
        if (Parent is BinaryOperationNumber b)
        {
            if (b.Left == this) { b.Left = Value; Value.Parent = b; }
            else if (b.Right == this) { b.Right = Value; Value.Parent = b; }
        }
        else if (Parent is FunctionCallNumber f)
            for (int i = 0; i < f.Arguments.Count; i++)
                if (f.Arguments[i] == this)
                {
                    f.Arguments[i] = Value;
                    Value.Parent = f;
                }
    }

    public override int CompareTo(Number? other)
    {
        if (other == null) return 1;
        if (ReferenceEquals(this, other)) return 0;

        if (other is ConstantNumber c)
            return string.Compare(Name, c.Name, StringComparison.InvariantCultureIgnoreCase);

        if (Value != null && Value.IsConcrete)
            return Value.CompareTo(other);

        return string.Compare(ToExpressionString(), other.ToExpressionString(), StringComparison.InvariantCulture);
    }
}
