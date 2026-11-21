namespace Calculator.Cli.Tests;

public sealed class FlagTests
{
    [Theory]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Run_HelpFlag_PrintsUsageOnStdoutAndExitsZero(string flag)
    {
        var result = CliRunner.OneShot(flag);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Contains("usage", result.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--help", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("--version", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("--explain", result.Stdout, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Feature", "F6")]
    public void Run_VersionFlag_PrintsExactVersionAndExitsZero()
    {
        var result = CliRunner.OneShot("--version");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("calc 1.0.0\n", result.NormalizedStdout);
        Assert.Equal("", result.Stderr);
    }

    [Theory]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    [InlineData(new[] { "-e", "10-2*6/4" }, "2 * 6 = 12\n12 / 4 = 3\n10 - 3 = 7\n7\n")]
    [InlineData(new[] { "--explain", "10-2*6/4" }, "2 * 6 = 12\n12 / 4 = 3\n10 - 3 = 7\n7\n")]
    [InlineData(new[] { "--explain", "2", "+", "3", "*", "4" }, "3 * 4 = 12\n2 + 12 = 14\n14\n")]
    [InlineData(new[] { "--explain", "7" }, "7\n")]
    public void Run_ExplainFlagWithExpression_PrintsStepsThenResult(string[] args, string expectedStdout)
    {
        var result = CliRunner.OneShot(args);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal(expectedStdout, result.NormalizedStdout);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Run_ExplainFlagInBatchMode_PrintsStepsPerLine()
    {
        var result = CliRunner.Run(["--explain"], stdin: "1+2*3\n7\n10-5-2\n", isInputRedirected: true);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("2 * 3 = 6\n1 + 6 = 7\n7\n7\n10 - 5 = 5\n5 - 2 = 3\n3\n", result.NormalizedStdout);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Run_ExplainFlagWithQuestion_PrintsStepsOfInterpretedExpression()
    {
        var result = CliRunner.OneShot("--explain", "What is 2 plus 3 times 4?");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("3 * 4 = 12\n2 + 12 = 14\n14\n", result.NormalizedStdout);
        Assert.Equal("interpreted as: 2 + 3 * 4\n", result.NormalizedStderr);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F6")]
    public void Run_ExplainFlagWithInvalidExpression_PrintsErrorBlockAndExitsOne()
    {
        var result = CliRunner.OneShot("-e", "10 + * 3");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(
            "error[MissingOperand]: expected a number after '+'\n  10 + * 3\n       ^\n",
            result.NormalizedStderr);
    }

    [Theory]
    [Trait("Requirement", "R1")]
    [Trait("Feature", "F6")]
    [InlineData("--bogus")]
    [InlineData("-x", "1+1")]
    [InlineData("--explain", "1", "--bogus")]
    public void Run_UnknownOptionWithInputNotRedirected_PrintsUsageAndExitsTwo(params string[] args)
    {
        var result = CliRunner.OneShot(args);

        Assert.Equal(CliApp.UsageError, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Contains("usage", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }
}
