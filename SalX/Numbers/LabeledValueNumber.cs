using System.Collections.Generic;
namespace SalX.Numbers;

public sealed class LabeledValueNumber : Number
{
    public string Label { get; private set; }
    public Number Value { get; private set; }
    public override NumberKind Kind => NumberKind.LabeledValue;
    public override List<Number> Children => new() { Value };
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => Value.IsConcrete;

    public LabeledValueNumber(string label, Number value)
    {
        Label = label;
        Value = value;
        Value.Parent = this;
        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public override string ToExpressionString() => $"{Label} = {Value.ToExpressionString()}";

    public override int CompareTo(Number? other)
    {
        if (other == null)
            return 1;

        if (other is LabeledValueNumber l)
            return Value.CompareTo(l.Value);

        return Value.CompareTo(other);
    }

    public override Number Substitute(Dictionary<string, Number> map)
        => new LabeledValueNumber(Label, Value.Substitute(map));

    public override Number CloneShallow()
        => new LabeledValueNumber(Label, Value.CloneForSubstitution());
}
