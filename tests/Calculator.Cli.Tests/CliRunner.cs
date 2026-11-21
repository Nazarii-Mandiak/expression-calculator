namespace Calculator.Cli.Tests;

internal sealed record CliResult(int ExitCode, string Stdout, string Stderr)
{
    public string[] StderrLines => Normalize(Stderr).TrimEnd('\n').Split('\n');

    public string NormalizedStdout => Normalize(Stdout);

    public string NormalizedStderr => Normalize(Stderr);

    /// <summary>Interactive transcript on stdout without the banner (its first line).</summary>
    public string StdoutAfterBanner => NormalizedStdout[(NormalizedStdout.IndexOf('\n', StringComparison.Ordinal) + 1)..];

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}

internal static class CliRunner
{
    public static CliResult OneShot(params string[] args) => Run(args, stdin: "", isInputRedirected: false);

    public static CliResult Batch(string stdin) => Run([], stdin, isInputRedirected: true);

    public static CliResult Repl(string stdin) => Run([], stdin, isInputRedirected: false);

    public static CliResult Run(string[] args, string stdin, bool isInputRedirected)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = new CliApp(new StringReader(stdin), output, error, isInputRedirected).Run(args);
        return new CliResult(exitCode, output.ToString(), error.ToString());
    }
}
