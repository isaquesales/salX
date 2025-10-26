using System;
namespace SalX.Usage;

class Program
{
    public static void Main()
    {
        while (true)
        {
            Console.Write("> ");

            string? line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                break;

            string result = Engine.DoString(line);
            Console.WriteLine(result);
        }
    }
}
