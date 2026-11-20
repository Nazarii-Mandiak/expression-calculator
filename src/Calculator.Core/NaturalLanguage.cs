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
    public static bool TryTranslate(string text, [NotNullWhen(true)] out string? expression)
    {
        ArgumentNullException.ThrowIfNull(text);

        expression = null;
        var trimmed = text.ToLowerInvariant().Trim();
        if (trimmed.EndsWith('?') || trimmed.EndsWith('.'))
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var output = new List<string>();
        var i = SkipFillers(words);
        while (i < words.Length)
        {
            var word = words[i++];
            string? symbol;
            if (word is "multiplied" or "divided")
            {
                if (i == words.Length || words[i] != "by")
                {
                    return false;
                }

                i++;
                symbol = word == "multiplied" ? "*" : "/";
            }
            else
            {
                symbol = IsSymbolRun(word) ? word : OperatorFor(word);
            }

            if (symbol is null)
            {
                return false;
            }

            output.Add(symbol);
        }

        if (output.Count == 0)
        {
            return false;
        }

        expression = string.Join(' ', output);
        return true;
    }

    // Fillers may be stacked ("please compute"), so keep skipping until none matches.
    private static int SkipFillers(string[] words)
    {
        var i = 0;
        int length;
        while ((length = FillerAt(words, i)) > 0)
        {
            i += length;
        }

        return i;
    }

    private static int FillerAt(string[] words, int start)
    {
        foreach (var filler in Fillers)
        {
            if (words.AsSpan(start).StartsWith(filler))
            {
                return filler.Length;
            }
        }

        return 0;
    }

    private static string? OperatorFor(string word) => word switch
    {
        "plus" => "+",
        "minus" => "-",
        "times" or "x" => "*",
        "over" => "/",
        _ => null,
    };

    // Numbers and symbolic operators pass through untouched; the parser has the final say on validity.
    private static bool IsSymbolRun(string word) => word.All(c => char.IsAsciiDigit(c) || "+-*/.\u00d7\u00f7\u2212".Contains(c));

    private static readonly string[][] Fillers =
    [
        ["how", "much", "is"],
        ["what", "is"],
        ["what's"],
        ["what\u2019s"],
        ["calculate"],
        ["compute"],
        ["please"],
    ];
}
