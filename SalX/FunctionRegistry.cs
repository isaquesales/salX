using System;
using System.Collections.Generic;
using System.Linq;
using SalX.Numbers;
namespace SalX;

public static class FunctionRegistry
{
    private static readonly Dictionary<string, FunctionDefinition> defs = new(StringComparer.InvariantCultureIgnoreCase);

    static FunctionRegistry()
    {
        Register("sin", new[] { "x" }, ds => Math.Sin(ds[0]));
        Register("cos", new[] { "x" }, ds => Math.Cos(ds[0]));
        Register("tan", new[] { "x" }, ds => Math.Tan(ds[0]));
        Register("sqrt", new[] { "x" }, ds => Math.Sqrt(ds[0]));
        Register("exp", new[] { "x" }, ds => Math.Exp(ds[0]));
        Register("ln", new[] { "x" }, ds => Math.Log(ds[0]));
        Register("log", new[] { "x" }, ds => Math.Log10(ds[0]));
        Register("abs", new[] { "x" }, ds => Math.Abs(ds[0]));
        Register("floor", new[] { "x" }, ds => Math.Floor(ds[0]));
        Register("ceil", new[] { "x" }, ds => Math.Ceiling(ds[0]));
        Register("rad", new[] { "x" }, ds => ds[0] * Math.PI / 180.0); // degrees -> radians

        // multi-arg example: max/min
        Register("max", null, ds => ds.Length == 0 ? double.NaN : ds.Max());
        Register("min", null, ds => ds.Length == 0 ? double.NaN : ds.Min());

        // user may define functions via DefineFunction

        // constants
        ConstantRegistry.Register("pi", new DoubleNumber(Math.PI));
        ConstantRegistry.Register("e", new DoubleNumber(Math.E));
    }

    public static void Register(string name, string[]? parameters, Func<double[], double> evaluator)
    {
        var def = new FunctionDefinition {
            Name = name,
            Parameters = parameters?.ToList() ?? new List<string>(),
            BuiltinEvaluator = evaluator
        };
        defs[name] = def;
    }

    public static void DefineFunction(string name, string[] parameterNames, string expression)
    {
        var template = Number.Parse(expression);
        var def = new FunctionDefinition { Name = name, Parameters = parameterNames?.ToList() ?? new List<string>(), Template = template };
        defs[name] = def;
    }

    public static bool TryGet(string name, out FunctionDefinition? def)
        => defs.TryGetValue(name, out def);
}
