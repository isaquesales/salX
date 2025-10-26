namespace SalX.Language;

internal struct Token
{
    public TokenType Type;
    public string Text;
    
    public Token(TokenType t, string txt)
    {
        Type = t;
        Text = txt;
    }
}
