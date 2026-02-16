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

    public void SetValue(Number value)
    {
        Value = value;
        Value.Parent = this;
    }

    public override bool AdvanceOneStep()
    {
        if (Value.AdvanceOneStep())
        {
            RecordStep();
            return true;
        }

        var evaluated = Value.EvaluateRoot();
        if (!ReferenceEquals(evaluated, Value))
        {
            SetValue(evaluated);
            RecordStep();
            return true;
        }

        return false;
    }

    public override void SimplifyAllFull()
    {
        bool changed;
        do
        {
            changed = false;
            Value.SimplifyAllFull();
            var evaluated = Value.EvaluateRoot();
            if (!ReferenceEquals(evaluated, Value))
            {
                SetValue(evaluated);
                changed = true;
            }
        } while (changed);

        RecordStep();
    }
}
