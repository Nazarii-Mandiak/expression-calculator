using System.Globalization;
using System.Text;

namespace Calculator.Core.Tests;

/// <summary>
/// Seeded random generator of valid expressions as token lists: 1-12 operands, integers 0-999 or
/// one-decimal numbers 0.0-999.9, optional attached unary minus, all four operators.
/// </summary>
/// <remarks>
/// Overflow is impossible by construction. Every operand has |x| &lt;= 999.9 and every non-zero
/// operand has |x| &gt;= 0.1, so each '*' or '/' grows a product by at most 1000x. Runs of '*'/'/'
/// are capped at <see cref="MaxMultiplicativeRun"/>, so a term is at most 1000^7 = 1e21, and the
/// sum of at most 12 terms is about 1.2e22, far below decimal.MaxValue (~7.9e28). Without the cap,
/// twelve factors of 999 (~9.9e35) would overflow.
/// </remarks>
internal sealed class ExpressionGenerator(int seed)
{
    private const int MaxOperands = 12;
    private const int MaxMultiplicativeRun = 6;
    private static readonly string[] Operators = ["+", "-", "*", "/"];

    private readonly Random _random = new(seed);

    public string[] NextTokens()
    {
        var operandCount = _random.Next(1, MaxOperands + 1);
        var tokens = new List<string> { NextOperand() };
        var multiplicativeRun = 0;
        for (var i = 1; i < operandCount; i++)
        {
            var op = Operators[_random.Next(multiplicativeRun < MaxMultiplicativeRun ? 4 : 2)];
            multiplicativeRun = op is "*" or "/" ? multiplicativeRun + 1 : 0;
            tokens.Add(op);
            tokens.Add(NextOperand());
        }
        return [.. tokens];
    }

    /// <summary>Joins tokens with a random 0..maxSpaces spaces between each pair.</summary>
    public string Render(string[] tokens, int maxSpaces)
    {
        var text = new StringBuilder(tokens[0]);
        foreach (var token in tokens.Skip(1))
            text.Append(' ', _random.Next(maxSpaces + 1)).Append(token);
        return text.ToString();
    }

    private string NextOperand()
    {
        var sign = _random.Next(4) == 0 ? "-" : "";
        // Zero is over-represented so that division by zero is exercised regularly.
        if (_random.Next(40) == 0) return sign + (_random.Next(2) == 0 ? "0" : "0.0");
        var number = _random.Next(2) == 0
            ? _random.Next(1000).ToString(CultureInfo.InvariantCulture)
            : (_random.Next(10000) / 10m).ToString("0.0", CultureInfo.InvariantCulture);
        return sign + number;
    }
}
