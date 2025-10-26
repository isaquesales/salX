using System;
using SalX.Numbers;
namespace SalX.Usage;

class Program
{
    public static void Main(string[] args)
    {
        #if DEBUG
        NumberDemo.RunDemo();
        #endif
        
        if (args.Length < 1)
        {
            Console.WriteLine("0.0.1");
            return;
        }
        bool onlyFinal = args[0] == "f";

        while (true)
        {
            Console.Write("> ");

            string? line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                break;

            try
            {
                var n = Number.Parse(line);
                if (!onlyFinal)
                    Engine.StepwiseSimplifyAndPrint(n);
                else
                    Console.WriteLine(Engine.DoString(n));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        } 
    }
}
