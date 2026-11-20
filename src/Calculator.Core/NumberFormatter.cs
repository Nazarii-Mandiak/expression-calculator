namespace Calculator.Core;

/// <summary>Formats results for display consistently across every host (CLI, REST, MCP).</summary>
public static class NumberFormatter
{
    /// <summary>Significant digits shown; the remaining digits of <see cref="decimal"/> act as guard digits.</summary>
    public const int DisplayDigits = 25;

    /// <summary>
    /// Formats <paramref name="value"/> with invariant culture, rounded half away from zero to
    /// <see cref="DisplayDigits"/> significant digits (the integer part is never rounded), without
    /// trailing zeros, exponent notation or thousands separators.
    /// </summary>
    /// <param name="value">Value to format.</param>
    /// <returns>Display text, e.g. <c>2.5</c>.</returns>
    public static string Format(decimal value) => throw new NotImplementedException();
}
