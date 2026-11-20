using System.Buffers;
using System.Globalization;
using System.Text;

namespace Calculator.Core;

/// <summary>
/// Hands out one token per <see cref="Next"/> call, rejecting anything outside the grammar. Reading on
/// demand lets the parser stop at the leftmost error instead of a later lexical one.
/// </summary>
internal sealed class Lexer(string text)
{
    private const int MaxEchoLength = 20;

    private int _position;

    /// <summary>Returns the next token; once the text is used up, every call returns the <see cref="TokenKind.End"/> token.</summary>
    public Token Next()
    {
        while (_position < text.Length && text[_position] is ' ' or '\t')
        {
            _position++;
        }

        // The end token is one character wide so "missing operand at end of input" has a span to report.
        if (_position >= text.Length)
        {
            return new Token(TokenKind.End, 0m, text.Length, 1);
        }

        var c = text[_position];
        if (char.IsAsciiDigit(c) || c == '.')
        {
            return ReadNumber();
        }

        TokenKind? kind = c switch
        {
            '+' => TokenKind.Plus,
            '-' or '−' => TokenKind.Minus,
            '*' or '×' => TokenKind.Star,
            '/' or '÷' => TokenKind.Slash,
            _ => null,
        };
        if (kind is null)
        {
            throw Unexpected(c);
        }

        return new Token(kind.Value, 0m, _position++, 1);
    }

    /// <summary>Shortens user text so error messages stay small however long the input is.</summary>
    public static string Echo(ReadOnlySpan<char> userText) =>
        userText.Length <= MaxEchoLength ? userText.ToString() : string.Concat(userText[..MaxEchoLength], "…");

    private Token ReadNumber()
    {
        var start = _position;
        while (_position < text.Length && (char.IsAsciiDigit(text[_position]) || text[_position] == '.'))
        {
            _position++;
        }

        // Digits and dots are consumed together so "1.2.3" is reported as one bad number.
        var literal = text.AsSpan(start, _position - start);
        var dots = literal.Count('.');
        if (dots > 1 || dots == literal.Length)
        {
            throw new CalculationException(
                ErrorCode.InvalidNumber, $"'{Echo(literal)}' is not a valid number", start, literal.Length);
        }

        if (!decimal.TryParse(literal, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            throw new CalculationException(
                ErrorCode.Overflow, $"'{Echo(literal)}' is too large to calculate with", start, literal.Length);
        }

        // decimal rounds surplus digits instead of failing, so compare what it kept with what was written.
        var dot = literal.IndexOf('.');
        var writtenFractionDigits = dot < 0 ? 0 : literal[(dot + 1)..].TrimEnd('0').Length;
        if (value.Scale < writtenFractionDigits)
        {
            throw new CalculationException(
                ErrorCode.InvalidNumber,
                $"'{Echo(literal)}' has more digits than can be represented exactly",
                start,
                literal.Length);
        }

        return new Token(TokenKind.Number, value, start, literal.Length);
    }

    private CalculationException Unexpected(char c)
    {
        var position = _position;
        if (c is '(' or ')')
        {
            return new CalculationException(ErrorCode.UnexpectedCharacter, "parentheses are not supported", position, 1);
        }

        if (c == ',')
        {
            return new CalculationException(
                ErrorCode.UnexpectedCharacter, "',' is not allowed; use '.' as the decimal point", position, 1);
        }

        // A surrogate pair is one character; a lone surrogate is invalid and counts as one char.
        var status = Rune.DecodeFromUtf16(text.AsSpan(position), out var rune, out var length);
        var shown = status == OperationStatus.Done ? Describe(rune) : CodePoint(c);
        return new CalculationException(
            ErrorCode.UnexpectedCharacter, $"unexpected character {shown}; use numbers and + - * /", position, length);
    }

    // Invisible characters would make the message unreadable or even spoof its layout, so show their code point.
    private static string Describe(Rune rune) => Rune.GetUnicodeCategory(rune) switch
    {
        UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate
            or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator
            or UnicodeCategory.SpaceSeparator => CodePoint(rune.Value),
        _ => $"'{rune}'",
    };

    private static string CodePoint(int value) => "U+" + value.ToString("X4", CultureInfo.InvariantCulture);
}
