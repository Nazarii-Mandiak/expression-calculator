namespace Calculator.Cli.Tests;

public sealed class OneShotModeTests
{
    [Theory]
    [Trait("Requirement", "R1")]
    [Trait("Feature", "F5")]
    [InlineData(new[] { "10 - 2 * 6 / 4" }, "7\n")]
    [InlineData(new[] { "10", "-", "2", "/", "4" }, "9.5\n")]
    [InlineData(new[] { "-5+3" }, "-2\n")]
    [InlineData(new[] { "-.5+1" }, "0.5\n")]
    [InlineData(new[] { "-5", "+", "3" }, "-2\n")]
    public void Run_ValidExpressionArguments_PrintsOnlyResultAndExitsZero(string[] args, string expectedStdout)
    {
        var result = CliRunner.OneShot(args);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal(expectedStdout, result.NormalizedStdout);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    public void Run_NaturalLanguageQuestion_PrintsResultAndInterpretationOnStderr()
    {
        var result = CliRunner.OneShot("What is 10 minus 2 multiplied by 6 divided by 4?");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("7\n", result.NormalizedStdout);
        Assert.Equal("interpreted as: 10 - 2 * 6 / 4\n", result.NormalizedStderr);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Feature", "F5")]
    public void Run_ExpressionArgumentWithRedirectedInput_IgnoresStdin()
    {
        var result = CliRunner.Run(["1+1"], stdin: "5*5\n", isInputRedirected: true);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("2\n", result.NormalizedStdout);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_MissingOperand_PrintsExactErrorBlockAndExitsOne()
    {
        var result = CliRunner.OneShot("10 + * 3");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(
            "error[MissingOperand]: expected a number after '+'\n  10 + * 3\n       ^\n",
            result.NormalizedStderr);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_ErrorWithLengthTwo_RepeatsCaretUnderWholeSpan()
    {
        var result = CliRunner.OneShot("10 20");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("error[MissingOperator]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  10 20", result.StderrLines[1]);
        Assert.Equal("     ^^", result.StderrLines[2]);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_DivisionByZero_PrintsLocatedErrorAndExitsOne()
    {
        var result = CliRunner.OneShot("10 / 0");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("error[DivisionByZero]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  10 / 0", result.StderrLines[1]);
        Assert.Equal("     ^", result.StderrLines[2]);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_UntranslatableWords_ReportsErrorAndExitsOne()
    {
        var result = CliRunner.OneShot("what is the weather");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.StartsWith("error[", result.StderrLines[0], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Feature", "F5")]
    public void Run_UnknownOption_PrintsUsageOnStderrAndExitsTwo()
    {
        var result = CliRunner.OneShot("--bogus");

        Assert.Equal(CliApp.UsageError, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Contains("usage", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }
}
