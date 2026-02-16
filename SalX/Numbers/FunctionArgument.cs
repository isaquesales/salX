namespace SalX.Numbers;

public sealed class FunctionArgument
{
    public string? Name { get; }
    public Number Value { get; }

    public FunctionArgument(string? name, Number value)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name;
        Value = value;
    }
}
