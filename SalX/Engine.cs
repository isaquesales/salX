using System;
using System.Collections.Generic;
using SalX.Numbers;
namespace SalX;

public static class Engine
{
    /// <summary>
    /// Gets the string of the final result of a number.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static string DoString(Number root)
    {
        root.SimplifyAllFull();
        var nFinal = root.EvaluateRoot();
        return nFinal.GetString();
    }

    /// <summary>
    /// Stepwise simplify and print each step. Returns the (possibly replaced) final root.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="label"></param>
    /// <returns></returns>
    public static Number StepwiseSimplifyAndPrint(Number root, string? label = null)
    {
        if (label != null) Console.WriteLine(label);
        Console.WriteLine("Initial: " + root.GetString());

        bool anyChange;
        do
        {
            anyChange = false;

            while (root.AdvanceOneStep())
            {
                Console.WriteLine("Step: " + root.GetString());
                anyChange = true;
            }

            var evaluated = root.EvaluateRoot();
            if (!ReferenceEquals(evaluated, root))
            {
                Console.WriteLine("Step: " + evaluated.GetString());
                root = evaluated;
                anyChange = true;
            }

        } while (anyChange);

        root.SimplifyAllFull();
        var finalEval = root.EvaluateRoot();
        if (!ReferenceEquals(finalEval, root))
        {
            Console.WriteLine("Step: " + finalEval.GetString());
            root = finalEval;
        }

        Console.WriteLine("Full Simplified: " + root.GetString());
        return root;
    }

    /// <summary>
    /// Collects steps into a list of strings (instead of printing)
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static List<string> CollectSteps(Number root)
    {
        var steps = new List<string>{
            root.GetString()
        };

        bool anyChange;
        do
        {
            anyChange = false;
            while (root.AdvanceOneStep())
            {
                steps.Add(root.GetString());
                anyChange = true;
            }

            var evaluated = root.EvaluateRoot();
            if (!object.ReferenceEquals(evaluated, root))
            {
                steps.Add(evaluated.GetString());
                root = evaluated;
                anyChange = true;
            }
        } while (anyChange);

        root.SimplifyAllFull();
        var finalEval = root.EvaluateRoot();
        if (!object.ReferenceEquals(finalEval, root))
        {
            steps.Add(finalEval.GetString());
            root = finalEval;
        }

        steps.Add("Full Simplified: " + root.GetString());
        return steps;
    }
}
