namespace SalX;

public class Number
{
    public string OriginalExpression { get; }
    private bool IsMainNumber { get; }

    private Number(string expression)
    {
        this.OriginalExpression = expression;
        this.IsMainNumber = true;
    }

    public static Number PutExpression(string expression)
        => new Number(expression);

    public string GetString => OriginalExpression;
}
