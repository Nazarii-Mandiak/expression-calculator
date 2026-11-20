namespace Calculator.Core;

/// <summary>Machine-readable reason a calculation failed.</summary>
public enum ErrorCode
{
    /// <summary>The input is empty or whitespace only.</summary>
    EmptyExpression,

    /// <summary>The input contains a character that is not part of the grammar.</summary>
    UnexpectedCharacter,

    /// <summary>A numeric literal is malformed, e.g. <c>1.2.3</c>.</summary>
    InvalidNumber,

    /// <summary>A number was expected but an operator or the end of input was found.</summary>
    MissingOperand,

    /// <summary>An operator was expected between two numbers.</summary>
    MissingOperator,

    /// <summary>The right operand of a division is zero.</summary>
    DivisionByZero,

    /// <summary>A literal or intermediate result exceeds the <see cref="decimal"/> range.</summary>
    Overflow,
}

/// <summary>
/// Raised for invalid input (syntax) or arithmetic failures. <see cref="Position"/> and <see cref="Length"/>
/// identify the offending span in the original text so hosts can point at it.
/// </summary>
public sealed class CalculationException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable description.</param>
    /// <param name="position">Zero-based index of the offending span in the input text.</param>
    /// <param name="length">Length of the offending span (at least 1).</param>
    public CalculationException(ErrorCode code, string message, int position, int length)
        : base(message)
    {
        Code = code;
        Position = position;
        Length = length;
    }

    /// <summary>Machine-readable error code.</summary>
    public ErrorCode Code { get; }

    /// <summary>Zero-based index of the offending span in the input text.</summary>
    public int Position { get; }

    /// <summary>Length of the offending span.</summary>
    public int Length { get; }
}
