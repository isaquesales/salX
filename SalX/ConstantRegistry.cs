using System;
using System.Collections.Generic;
using SalX.Numbers;
namespace SalX;

public static class ConstantRegistry
{
    private static readonly Dictionary<string, Number> consts = new(StringComparer.InvariantCultureIgnoreCase);
    
    public static void Register(string name, Number value)
        => consts[name] = value;

    public static bool TryGet(string name, out Number? value)
        => consts.TryGetValue(name, out value);
}
