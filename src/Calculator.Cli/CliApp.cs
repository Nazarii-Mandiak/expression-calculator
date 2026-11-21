using Calculator.Core;

namespace Calculator.Cli;

/// <summary>
/// The console application, independent of the real console: streams and the "is stdin redirected"
/// flag are injected so every mode can be tested in memory. <c>Program.cs</c> wires it to <see cref="Console"/>.
/// </summary>
internal sealed class CliApp(TextReader input, TextWriter output, TextWriter error, bool isInputRedirected)
{
    /// <summary>Process exit code: every expression succeeded.</summary>
    public const int Success = 0;

    /// <summary>Process exit code: at least one expression failed.</summary>
    public const int EvaluationFailed = 1;

    /// <summary>Process exit code: invalid command-line usage.</summary>
    public const int UsageError = 2;

    private const int MaxLineLength = 10_000;

    private const string Usage = "usage: calc <expression>   (or pipe one expression per line to stdin)";

    /// <summary>Runs the application with the given command-line arguments and returns the exit code.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="Success"/>, <see cref="EvaluationFailed"/> or <see cref="UsageError"/>.</returns>
    public int Run(string[] args)
    {
        if (args.Any(IsOption))
        {
            return Fail(Usage);
        }

        if (args.Length > 0)
        {
            return Evaluate(string.Join(' ', args), linePrefix: "") ? Success : EvaluationFailed;
        }

        return isInputRedirected ? RunBatch() : Fail(Usage); // Known limit: REPL arrives with F6
    }

    // "-5", "--3" and a lone "-" are expression text; only "-x" and "--x" (x an ASCII letter) are options.
    private static bool IsOption(string arg)
    {
        var dashes = arg.StartsWith("--", StringComparison.Ordinal) ? 2 : 1;
        return arg.StartsWith('-') && arg.Length > dashes && char.IsAsciiLetter(arg[dashes]);
    }

    private int Fail(string message)
    {
        error.WriteLine(message);
        return UsageError;
    }

    private int RunBatch()
    {
        var allSucceeded = true;
        var lineNumber = 0;
        while (input.ReadLine() is { } line)
        {
            lineNumber++;
            if (lineNumber == 1)
            {
                line = line.TrimStart('\uFEFF');
            }

            // Known limit: a stream without newlines is still bounded only by memory; upgrade path = bounded reader.
            if (line.Length > MaxLineLength)
            {
                error.WriteLine($"line {lineNumber}: error[InputTooLong]: line is longer than {MaxLineLength} characters");
                allSucceeded = false;
                continue;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                continue;
            }

            allSucceeded &= Evaluate(trimmed, $"line {lineNumber}: ");
        }

        return allSucceeded ? Success : EvaluationFailed;
    }

    private bool Evaluate(string text, string linePrefix)
    {
        var isTranslation = false;
        if (text.Any(char.IsLetter) && NaturalLanguage.TryTranslate(text, out var translated))
        {
            text = translated;
            isTranslation = true;
        }

        Expression? expression = null;
        try
        {
            expression = Expression.Parse(text);
            if (isTranslation)
            {
                error.WriteLine($"interpreted as: {expression}");
            }

            output.WriteLine(NumberFormatter.Format(expression.Evaluate().Value));
            return true;
        }
        catch (CalculationException failure)
        {
            if (isTranslation && expression is null)
            {
                error.WriteLine($"interpreted as: {text}");
            }

            ErrorRenderer.Write(error, failure, text, linePrefix);
            return false;
        }
    }
}
