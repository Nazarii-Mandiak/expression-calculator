namespace Calculator.Core;

internal enum TokenKind
{
    Number,
    Plus,
    Minus,
    Star,
    Slash,
    End,
}

/// <summary>A lexical unit with the span it covers in the original text; <paramref name="Value"/> is only set for numbers.</summary>
internal readonly record struct Token(TokenKind Kind, decimal Value, int Position, int Length);
