namespace Calculator.Cli.Tests;

public sealed class BatchModeTests
{
    [Theory]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    [InlineData("1+2\n3*3\n", "3\n9\n")]
    [InlineData("1+2\r\n3*3\r\n", "3\n9\n")]
    [InlineData("1+2\n3*3", "3\n9\n")]
    [InlineData("1+2\n\n# comment\n3*3\n", "3\n9\n")]
    [InlineData("1+2\n   \n3*3\n", "3\n9\n")]
    public void Run_RedirectedLines_PrintsOneResultPerLineAndExitsZero(string stdin, string expectedStdout)
    {
        var result = CliRunner.Batch(stdin);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal(expectedStdout, result.NormalizedStdout);
        Assert.Equal("", result.Stderr);
    }

    [Theory]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    [InlineData("")]
    [InlineData("\n\n")]
    [InlineData("# only a comment\n")]
    public void Run_NothingToEvaluate_PrintsNothingAndExitsZero(string stdin)
    {
        var result = CliRunner.Batch(stdin);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_ErrorOnFourthLine_PrefixesLineNumberAndExitsOne()
    {
        var result = CliRunner.Batch("1+2\n\n# c\n10/0\n");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("3\n", result.NormalizedStdout);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("line 4: error[DivisionByZero]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  10/0", result.StderrLines[1]);
        Assert.Equal("    ^", result.StderrLines[2]);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_ErrorInMiddle_ContinuesWithLaterLines()
    {
        var result = CliRunner.Batch("1+2\n10/0\n3*3\n");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("3\n9\n", result.NormalizedStdout);
        Assert.StartsWith("line 2: error[DivisionByZero]: ", result.StderrLines[0], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_SyntaxErrorLine_UsesExactErrorFormatWithPrefix()
    {
        var result = CliRunner.Batch("10 + * 3\n");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(
            "line 1: error[MissingOperand]: expected a number after '+'\n  10 + * 3\n       ^\n",
            result.NormalizedStderr);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    public void Run_NaturalLanguageLine_PrintsResultAndInterpretation()
    {
        var result = CliRunner.Batch("what's 3 plus 4\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("7\n", result.NormalizedStdout);
        Assert.Equal("interpreted as: 3 + 4\n", result.NormalizedStderr);
    }
}
