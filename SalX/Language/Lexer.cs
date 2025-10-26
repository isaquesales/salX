namespace SalX.Language;

internal class Lexer
{
    private readonly string s;
    private int i = 0;
    public Lexer(string input)
    {
        s = input ?? "";
    }

    private char Current => (i >= s.Length) ? '\0' : s[i];
    public Token NextToken()
    {
        while (char.IsWhiteSpace(Current)) i++;
        if (Current == '\0') return new Token(TokenType.End, "");
        if (char.IsDigit(Current) || (Current == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
        {
            int start = i;
            while (char.IsDigit(Current)) i++;
            if (Current == '.')
            {
                i++;
                while (char.IsDigit(Current))
                    i++;
            }
            if (Current == 'e' || Current == 'E')
            {
                i++;
                if (Current == '+' || Current == '-')
                    i++;
                while (char.IsDigit(Current))
                    i++;
            }
            var txt = s.Substring(start, i - start);
            return new Token(TokenType.Number, txt);
        }
        if (char.IsLetter(Current) || Current == '_')
        {
            int start = i;
            while (char.IsLetterOrDigit(Current) || Current == '_')
                i++;

            var txt = s.Substring(start, i - start);
            return new Token(TokenType.Identifier, txt);
        }
        
        switch (Current)
        {
            case '+': i++; return new Token(TokenType.Plus, "+");
            case '-': i++; return new Token(TokenType.Minus, "-");
            case '*': i++; return new Token(TokenType.Star, "*");
            case '/': i++; return new Token(TokenType.Slash, "/");
            case '%': i++; return new Token(TokenType.Percent, "%");
            case '^': i++; return new Token(TokenType.Caret, "^");
            case '(' : i++; return new Token(TokenType.LParen, "(");
            case ')' : i++; return new Token(TokenType.RParen, ")");
            case ',' : i++; return new Token(TokenType.Comma, ",");
            default: i++; return new Token(TokenType.End, "");
        }
    }
}
