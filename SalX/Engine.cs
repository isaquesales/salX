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
        bool changed;
        do
        {
            changed = false;
            root.SimplifyAllFull();
            var evaluated = root.EvaluateRoot();
            if (!ReferenceEquals(evaluated, root))
            {
                root = evaluated;
                changed = true;
            }
        } while (changed);

        return root.GetString();
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
        var lastPrinted = root.GetString();
        Console.WriteLine("Initial: " + lastPrinted);

        bool anyChange;
        do
        {
            anyChange = false;

            while (root.AdvanceOneStep())
            {
                var stepText = root.GetString();
                if (stepText != lastPrinted)
                {
                    Console.WriteLine("Step: " + stepText);
                    lastPrinted = stepText;
                }
                anyChange = true;
            }

            var evaluated = root.EvaluateRoot();
            if (!ReferenceEquals(evaluated, root))
            {
                var evalText = evaluated.GetString();
                if (evalText != lastPrinted)
                {
                    Console.WriteLine("Step: " + evalText);
                    lastPrinted = evalText;
                }
                root = evaluated;
                anyChange = true;
            }

        } while (anyChange);

        root.SimplifyAllFull();
        var finalEval = root.EvaluateRoot();
        if (!ReferenceEquals(finalEval, root))
        {
            var evalText = finalEval.GetString();
            if (evalText != lastPrinted)
            {
                Console.WriteLine("Step: " + evalText);
                lastPrinted = evalText;
            }
            root = finalEval;
        }

        var finalText = root.GetString();
        if (finalText != lastPrinted)
            Console.WriteLine("Full Simplified: " + finalText);
        return root;
    }

    /// <summary>
    /// Collects steps into a list of strings (instead of printing)
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static List<string> CollectSteps(Number root)
    {
        var steps = new List<string>();
        void AddStepIfChanged(string s)
        {
            if (steps.Count == 0 || steps[^1] != s)
                steps.Add(s);
        }

        AddStepIfChanged(root.GetString());

        bool anyChange;
        do
        {
            anyChange = false;
            while (root.AdvanceOneStep())
            {
                AddStepIfChanged(root.GetString());
                anyChange = true;
            }

            var evaluated = root.EvaluateRoot();
            if (!object.ReferenceEquals(evaluated, root))
            {
                AddStepIfChanged(evaluated.GetString());
                root = evaluated;
                anyChange = true;
            }
        } while (anyChange);

        root.SimplifyAllFull();
        var finalEval = root.EvaluateRoot();
        if (!object.ReferenceEquals(finalEval, root))
        {
            var evalText = finalEval.GetString();
            AddStepIfChanged(evalText);
            root = finalEval;
        }

        var finalText = root.GetString();
        AddStepIfChanged("Full Simplified: " + finalText);
        return steps;
    }
}
