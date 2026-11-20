using System.Globalization;

namespace Calculator.Core;

/// <summary>Formats results for display consistently across every host (CLI, REST, MCP).</summary>
public static class NumberFormatter
{
    /// <summary>Significant digits shown; the remaining digits of <see cref="decimal"/> act as guard digits.</summary>
    private const int DisplayDigits = 25;

    /// <summary>
    /// Formats <paramref name="value"/> with invariant culture, rounded half away from zero to
    /// max(0, 25 − integer digits) decimal places (integer digits is 0 when |value| &lt; 1), without
    /// trailing zeros, exponent notation or thousands separators. A non-zero value is never shown as 0.
    /// </summary>
    /// <param name="value">Value to format.</param>
    /// <returns>Display text, e.g. <c>2.5</c>.</returns>
    public static string Format(decimal value)
    {
        var integerPart = decimal.Truncate(Math.Abs(value));
        var integerDigits = integerPart == 0m ? 0 : integerPart.ToString(CultureInfo.InvariantCulture).Length;
        // DisplayDigits is below the 28 decimals that decimal.Round accepts, so no clamping is needed.
        var decimals = Math.Max(0, DisplayDigits - integerDigits);
        var rounded = decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
        // Rounding must not erase a tiny value; show it exactly instead.
        return rounded == 0m && value != 0m ? ToPlainText(value) : ToPlainText(rounded);
    }

    /// <summary>Prints without rounding, trailing zeros, exponent or separators; shared with <see cref="Expression.ToString"/>.</summary>
    internal static string ToPlainText(decimal value) =>
        // Optional digits drop trailing zeros; 28 is the most fractional digits a decimal can hold.
        value.ToString("0.############################", CultureInfo.InvariantCulture);
}
