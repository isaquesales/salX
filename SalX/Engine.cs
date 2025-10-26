namespace SalX;

public class Engine
{
    private static Number Run(string expr)
        => Number.PutExpression(expr);

    public static string DoString(string expr)
    => Run(expr).GetString;

    public static Number DoNumber(string expr)
    => Run(expr);
}
