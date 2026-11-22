using System.Reflection;

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

    private const string ExplainCommand = "explain";

    private const string Usage = "usage: calc [-e|--explain] [expression]   (try --help)";

    private const string Help = """
        usage: calc [options] [expression]

        modes:
          calc "10 - 2 * 6 / 4"   evaluate one expression (several arguments are joined)
          ... | calc              evaluate one expression per line from stdin
          calc                    interactive session (when stdin is a terminal)

        operators: + - * /   (* and / bind tighter than + and -; unary signs allowed;
                             no parentheses or exponents)
        questions: "What is 10 minus 2 times 3?" is translated to an expression first

        options:
          -h, --help       show this help
          --version        show the version
          -e, --explain    print every step, e.g. "2 * 6 = 12", before the result

        piped input: blank lines and lines starting with # are skipped

        exit status: 0 ok, 1 an expression failed, 2 usage error

        interactive commands:
          help                  show this help
          explain <expression>  show the steps, then the result
          exit, quit, Ctrl+D    leave
        """;

    /// <summary>Runs the application with the given command-line arguments and returns the exit code.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="Success"/>, <see cref="EvaluationFailed"/> or <see cref="UsageError"/>.</returns>
    public int Run(string[] args)
    {
        var options = args.Where(IsOption).ToArray();
        var unknown = options.FirstOrDefault(option => option is not ("-h" or "--help" or "--version" or "-e" or "--explain"));
        if (unknown is not null)
        {
            return Fail($"calc: unknown option '{unknown}'");
        }

        if (options.Contains("-h") || options.Contains("--help"))
        {
            output.WriteLine(Help);
            return Success;
        }

        if (options.Contains("--version"))
        {
            output.WriteLine($"calc {Version}");
            return Success;
        }

        var explain = options.Contains("-e") || options.Contains("--explain");
        var expressionArgs = args.Where(arg => !IsOption(arg)).ToArray();
        if (expressionArgs.Length > 0)
        {
            var text = string.Join(' ', expressionArgs);
            if (text.Length > MaxLineLength)
            {
                ReportTooLong(prefix: "");
                return EvaluationFailed;
            }

            return Evaluate(text, linePrefix: "", explain) ? Success : EvaluationFailed;
        }

        return isInputRedirected ? RunBatch(explain) : RunRepl(explain);
    }

    private int RunBatch(bool explain)
    {
        var allSucceeded = true;
        var lineNumber = 0;
        while (input.ReadLine() is { } line)
        {
            lineNumber++;
            if (lineNumber == 1)
            {
                line = line.TrimStart('﻿');
            }

            // Known limit: a stream without newlines is still bounded only by memory; upgrade path = bounded reader.
            if (line.Length > MaxLineLength)
            {
                ReportTooLong($"line {lineNumber}: ");
                allSucceeded = false;
                continue;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                continue;
            }

            allSucceeded &= Evaluate(trimmed, $"line {lineNumber}: ", explain);
        }

        return allSucceeded ? Success : EvaluationFailed;
    }

    // Errors never end the session, so the exit code is always Success.
    private int RunRepl(bool explain)
    {
        output.WriteLine($"calc {Version} - type an expression, 'help' or 'exit'");
        while (true)
        {
            output.Write("> ");
            output.Flush();
            if (input.ReadLine() is not { } line)
            {
                output.WriteLine();
                return Success;
            }

            if (line.Length > MaxLineLength)
            {
                ReportTooLong(prefix: "");
                continue;
            }

            var command = line.Trim();
            if (command.Length == 0)
            {
                continue;
            }

            if (command.Equals("exit", StringComparison.OrdinalIgnoreCase)
                || command.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                return Success;
            }

            if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine(Help);
            }
            else if (IsExplainCommand(command))
            {
                var expression = command[ExplainCommand.Length..].Trim();
                if (expression.Length == 0)
                {
                    error.WriteLine("usage: explain <expression>");
                }
                else
                {
                    Evaluate(expression, linePrefix: "", explain: true);
                }
            }
            else
            {
                Evaluate(command, linePrefix: "", explain);
            }
        }
    }

    private bool Evaluate(string text, string linePrefix, bool explain)
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

            var evaluation = expression.Evaluate();
            if (explain)
            {
                foreach (var step in evaluation.Steps)
                {
                    output.WriteLine(
                        $"{NumberFormatter.Format(step.Left)} {step.Operator.ToSymbol()} {NumberFormatter.Format(step.Right)} = {NumberFormatter.Format(step.Result)}");
                }
            }

            output.WriteLine(NumberFormatter.Format(evaluation.Value));
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

    // The informational version looks like "1.0.0+<commit>"; the commit part is not part of the version.
    private static string Version =>
        typeof(CliApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "unknown";

    // "-5", "--3" and a lone "-" are expression text; only "-x" and "--x" (x an ASCII letter) are options.
    private static bool IsOption(string arg)
    {
        var dashes = arg.StartsWith("--", StringComparison.Ordinal) ? 2 : 1;
        return arg.StartsWith('-') && arg.Length > dashes && char.IsAsciiLetter(arg[dashes]);
    }

    // "explain" alone or followed by whitespace; "explained" is not the command.
    private static bool IsExplainCommand(string command) =>
        command.StartsWith(ExplainCommand, StringComparison.OrdinalIgnoreCase)
        && (command.Length == ExplainCommand.Length || char.IsWhiteSpace(command[ExplainCommand.Length]));

    private int Fail(string message)
    {
        error.WriteLine(message);
        error.WriteLine(Usage);
        return UsageError;
    }

    private void ReportTooLong(string prefix) =>
        error.WriteLine($"{prefix}error[InputTooLong]: line is longer than {MaxLineLength} characters");
}
