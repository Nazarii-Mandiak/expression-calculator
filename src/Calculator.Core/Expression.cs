using System.Diagnostics;

namespace Calculator.Core;

/// <summary>
/// A syntactically valid arithmetic expression. Instances can only be obtained through
/// <see cref="Parse"/>, so holding an <see cref="Expression"/> proves the input was validated
/// ("parse, don't validate"). Instances are immutable and thread-safe.
/// </summary>
public sealed class Expression
{
    private readonly Node _root;

    private Expression(Node root) => _root = root;

    /// <summary>Parses <paramref name="text"/> into a validated expression without evaluating it.</summary>
    /// <param name="text">Expression text such as <c>10 - 2 * 6 / 4</c>.</param>
    /// <returns>The validated expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="CalculationException">The text is not a valid expression (syntax error).</exception>
    public static Expression Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new Expression(Parser.Parse(text));
    }

    /// <summary>
    /// Evaluates the expression with standard precedence (<c>*</c> and <c>/</c> before <c>+</c> and <c>-</c>),
    /// left to right within the same precedence level.
    /// </summary>
    /// <remarks>
    /// Literals are exact. Each operation's result keeps <see cref="decimal"/>'s 28-29 significant digits,
    /// so results below 1e-28 become 0. Only overflow is an error.
    /// </remarks>
    /// <returns>The value plus every binary operation performed, in evaluation order.</returns>
    /// <exception cref="CalculationException">Division by zero or arithmetic overflow.</exception>
    public Evaluation Evaluate() => Evaluator.Evaluate(_root);

    /// <summary>Returns the canonical form, e.g. <c>10 - 2 * 6 / 4</c> (single spaces, ASCII operators).</summary>
    /// <returns>The canonical expression text.</returns>
    public override string ToString() => Format(_root);

    private static string Format(Node node) => node switch
    {
        NumberNode number => NumberFormatter.ToPlainText(number.Value),
        ChainNode chain => string.Concat(
            Format(chain.First),
            string.Concat(chain.Rest.Select(link => $" {link.Operator.ToSymbol()} {Format(link.Operand)}"))),
        _ => throw new UnreachableException(),
    };
}
