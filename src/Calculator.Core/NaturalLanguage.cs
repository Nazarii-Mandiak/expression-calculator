using System.Diagnostics.CodeAnalysis;

namespace Calculator.Core;

/// <summary>
/// Deterministic, rule-based translation of simple English arithmetic questions into expression text.
/// It never computes anything: the output must still go through <see cref="Expression.Parse"/>.
/// </summary>
public static class NaturalLanguage
{
    /// <summary>
    /// Translates e.g. <c>What is 10 minus 2 multiplied by 6 divided by 4?</c> into <c>10 - 2 * 6 / 4</c>.
    /// </summary>
    /// <param name="text">The question.</param>
    /// <param name="expression">The translated expression text when successful.</param>
    /// <returns><see langword="true"/> if every word was understood; otherwise <see langword="false"/>.</returns>
    public static bool TryTranslate(string text, [NotNullWhen(true)] out string? expression) =>
        throw new NotImplementedException();
}
