#if DEBUG

using System;
using SalX.Numbers;
namespace SalX;

public static class NumberDemo
{
    public static void RunDemo()
    {
        Console.WriteLine("-- Demo: rad(180) -> should simplify to PI (~3.14159)");
        var n = Number.Parse("rad(180)");
        n = Engine.StepwiseSimplifyAndPrint(n);

        Console.WriteLine();
        Console.WriteLine("-- Demo: user-defined function f(a,b)=a^2 + b and f(3,4)");
        FunctionRegistry.DefineFunction("f", new[] { "a", "b" }, "a^2 + b");
        var fcall = Number.Parse("f(3,4)");
        fcall = Engine.StepwiseSimplifyAndPrint(fcall);

        Console.WriteLine();
        Console.WriteLine("-- Demo: more functions and multi-args: max(1,2,3*2)");
        var m = Number.Parse("max(1,2,3*2)");
        m = Engine.StepwiseSimplifyAndPrint(m);

        Console.WriteLine();
        Console.WriteLine("-- Demo: constants: pi, e");
        var expr = Number.Parse("pi + 1");
        expr = Engine.StepwiseSimplifyAndPrint(expr);

        Console.WriteLine();
        Console.WriteLine("-- You can define new functions: Example: g(x)=sin(rad(x)) and call g(90)");
        FunctionRegistry.DefineFunction("g", new[] { "x" }, "sin(rad(x))");
        var g = Number.Parse("g(90)");
        g = Engine.StepwiseSimplifyAndPrint(g);
    }
}
#endif
