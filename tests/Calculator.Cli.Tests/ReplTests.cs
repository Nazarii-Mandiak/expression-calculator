namespace Calculator.Cli.Tests;

// REPL output rule: banner, prompts, results, explain steps and help go to stdout; errors and
// "interpreted as" go to stderr, as in the other modes. Input is not echoed, so on stdout a result
// directly follows the prompt it answers.
public sealed class ReplTests
{
    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_Start_PrintsBannerLineThenPrompt()
    {
        var result = CliRunner.Repl("");

        var lines = result.NormalizedStdout.Split('\n');
        Assert.NotEqual("", lines[0].Trim());
        Assert.False(lines[0].StartsWith("> ", StringComparison.Ordinal), $"first line was '{lines[0]}'");
        Assert.StartsWith("> ", result.StdoutAfterBanner, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R3")]
    [Trait("Feature", "F6")]
    [InlineData("10-2*6/4\n", "> 7\n> ")]
    [InlineData("10 - 2 / 4\n", "> 9.5\n> ")]
    [InlineData("0.1 + 0.2\n", "> 0.3\n> ")]
    [InlineData("   1+1   \n", "> 2\n> ")]
    [InlineData("1+1\n2*3\n", "> 2\n> 6\n> ")]
    public void Repl_ValidExpression_PrintsResultThenNextPrompt(string session, string expectedTranscriptStart)
    {
        var result = CliRunner.Repl(session);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith(expectedTranscriptStart, result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_ExplainCommand_PrintsStepsThenResult()
    {
        var result = CliRunner.Repl("explain 10-2*6/4\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> 2 * 6 = 12\n12 / 4 = 3\n10 - 3 = 7\n7\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_ExplainExpressionWithoutOperators_PrintsOnlyResult()
    {
        var result = CliRunner.Repl("explain 7\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> 7\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_InvalidExpression_PrintsErrorBlockOnStderrThenNextPrompt()
    {
        var result = CliRunner.Repl("2 +\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> > ", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("error[MissingOperand]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  2 +", result.StderrLines[1]);
        Assert.Equal("     ^", result.StderrLines[2]);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_ExpressionAfterErrors_StillEvaluates()
    {
        var result = CliRunner.Repl("2 +\n10 / 0\n1+1\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> > > 2\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Contains("error[MissingOperand]", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error[DivisionByZero]", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_BlankLines_OnlyRePrompt()
    {
        var result = CliRunner.Repl("\n   \n1+1\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> > > 2\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_HelpCommand_ListsCommandsAndOperators()
    {
        var result = CliRunner.Repl("help\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        var transcript = result.StdoutAfterBanner;
        Assert.Contains("help", transcript, StringComparison.Ordinal);
        Assert.Contains("explain", transcript, StringComparison.Ordinal);
        Assert.Contains("exit", transcript, StringComparison.Ordinal);
        Assert.Contains("quit", transcript, StringComparison.Ordinal);
        Assert.Contains("+", transcript, StringComparison.Ordinal);
        Assert.Contains("-", transcript, StringComparison.Ordinal);
        Assert.Contains("*", transcript, StringComparison.Ordinal);
        Assert.Contains("/", transcript, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Theory]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    [InlineData("exit\n1+10\n")]
    [InlineData("quit\n1+10\n")]
    [InlineData("  exit  \n1+10\n")]
    public void Repl_ExitCommand_EndsSessionWithExitZero(string session)
    {
        var result = CliRunner.Repl(session);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.DoesNotContain("11", result.Stdout, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_EndOfInputAfterError_EndsSessionWithExitZero()
    {
        var result = CliRunner.Repl("10 + * 3");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Contains("error[MissingOperand]", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_NaturalLanguageQuestion_PrintsResultAndInterpretationOnStderr()
    {
        var result = CliRunner.Repl("What is 10 minus 2 multiplied by 6 divided by 4?\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> 7\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal("interpreted as: 10 - 2 * 6 / 4\n", result.NormalizedStderr);
    }
}
