using System.Diagnostics;

namespace Calculator.Core;

/// <summary>
/// Recursive descent over the grammar in docs/SPEC.md, one method per grammar rule. Repetitions are loops,
/// so the call depth is constant no matter how long the input is.
/// </summary>
internal sealed class Parser
{
    private readonly string _text;
    private readonly Lexer _lexer;
    private Token? _previous;
    private Token _current;

    private Parser(string text)
    {
        _text = text;
        _lexer = new Lexer(text);
        _current = _lexer.Next();
    }

    public static Node Parse(string text) => new Parser(text).ParseInput();

    private Node ParseInput()
    {
        if (_current.Kind == TokenKind.End)
        {
            throw new CalculationException(ErrorCode.EmptyExpression, "the expression is empty", 0, 1);
        }

        var root = ParseExpression();
        if (_current.Kind != TokenKind.End)
        {
            // Operators are consumed by the loops, so only a number can be left over.
            throw new CalculationException(
                ErrorCode.MissingOperator, $"expected an operator before {Describe(_current)}", _current.Position, _current.Length);
        }

        return root;
    }

    // expression = term , { ( "+" | "-" ) , term } ;
    private Node ParseExpression()
    {
        var first = ParseTerm();
        var rest = new List<ChainLink>();
        while (_current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var op = _current;
            Advance();
            rest.Add(new ChainLink(ToOperator(op.Kind), op.Position, ParseTerm()));
        }

        return rest.Count == 0 ? first : new ChainNode(first, rest);
    }

    // term = factor , { ( "*" | "/" ) , factor } ;
    private Node ParseTerm()
    {
        var first = ParseFactor();
        var rest = new List<ChainLink>();
        while (_current.Kind is TokenKind.Star or TokenKind.Slash)
        {
            var op = _current;
            Advance();
            rest.Add(new ChainLink(ToOperator(op.Kind), op.Position, ParseFactor()));
        }

        return rest.Count == 0 ? first : new ChainNode(first, rest);
    }

    // factor = { "+" | "-" } , number ;
    private NumberNode ParseFactor()
    {
        var negative = false;
        while (_current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            negative ^= _current.Kind == TokenKind.Minus;
            Advance();
        }

        if (_current.Kind != TokenKind.Number)
        {
            throw MissingOperand();
        }

        var value = _current.Value;
        Advance();
        return new NumberNode(negative ? -value : value);
    }

    private void Advance()
    {
        _previous = _current;
        _current = _lexer.Next();
    }

    private CalculationException MissingOperand()
    {
        var message = _previous is { } previous
            ? $"expected a number after {Describe(previous)}"
            : $"an expression must start with a number, found {Describe(_current)}";
        return new CalculationException(ErrorCode.MissingOperand, message, _current.Position, _current.Length);
    }

    // The end token's position is past the text, so it must never be used to index it.
    private string Describe(Token token) => token.Kind == TokenKind.End
        ? "end of input"
        : $"'{Lexer.Echo(_text.AsSpan(token.Position, token.Length))}'";

    private static BinaryOperator ToOperator(TokenKind kind) => kind switch
    {
        TokenKind.Plus => BinaryOperator.Add,
        TokenKind.Minus => BinaryOperator.Subtract,
        TokenKind.Star => BinaryOperator.Multiply,
        TokenKind.Slash => BinaryOperator.Divide,
        _ => throw new UnreachableException(),
    };
}
