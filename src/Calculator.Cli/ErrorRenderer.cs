using System.Globalization;

using Calculator.Core;

namespace Calculator.Cli;

/// <summary>Writes a <see cref="CalculationException"/> as a three-line, caret-annotated message.</summary>
internal static class ErrorRenderer
{
    /// <param name="writer">Where the three lines go.</param>
    /// <param name="error">The failure to render.</param>
    /// <param name="text">The exact text that was evaluated; the carets point into it.</param>
    /// <param name="prefix">Text before <c>error[...]</c>, e.g. <c>line 4: </c>.</param>
    public static void Write(TextWriter writer, CalculationException error, string text, string prefix)
    {
        writer.WriteLine($"{prefix}error[{error.Code}]: {error.Message}");
        writer.WriteLine($"  {MakePrintable(text)}");
        // UTF-16 indexing is safe: the leftmost error wins, so everything before Position is a
        // single-column grammar character.
        writer.WriteLine($"  {new string(' ', error.Position)}{new string('^', error.Length)}");
    }

    // One output character per input character, so the caret stays aligned.
    private static string MakePrintable(string text) =>
        string.Create(text.Length, text, static (buffer, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                buffer[i] = c == '\t' ? ' '
                    : char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format ? '?'
                    : c;
            }
        });
}
