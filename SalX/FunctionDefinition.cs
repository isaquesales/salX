using System;
using System.Collections.Generic;
using SalX.Numbers;
namespace SalX;

public sealed class FunctionDefinition
{
    public string? Name { get; init; }
    public List<string> Parameters { get; init; } = new();

    /// <summary>
    /// BuiltinEvaluator: double[] -> double for numeric builtins
    /// </summary>
    public Func<double[], double>? BuiltinEvaluator { get; init; }
    /// <summary>
    /// Template: parsed Number template with parameter VariableNodes
    /// </summary>
    public Number? Template { get; init; }
}
