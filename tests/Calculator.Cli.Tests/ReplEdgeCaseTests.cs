namespace Calculator.Cli.Tests;

public sealed class ReplEdgeCaseTests
{
    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_UppercaseExitCommand_EndsSession()
    {
        var result = CliRunner.Repl("EXIT\n1+10\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.DoesNotContain("11", result.Stdout, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_MixedCaseHelpCommand_PrintsHelp()
    {
        var result = CliRunner.Repl("Help\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Contains("explain", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Contains("quit", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_MixedCaseExplainCommand_PrintsStepThenResult()
    {
        var result = CliRunner.Repl("Explain 2*3\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> 2 * 3 = 6\n6\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Theory]
    [Trait("Requirement", "R5")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    [InlineData("explain\n1+1\n")]
    [InlineData("explain   \n1+1\n")]
    public void Repl_ExplainWithoutExpression_PrintsUsageAndContinues(string session)
    {
        var result = CliRunner.Repl(session);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Contains("usage: explain <expression>", result.Stderr, StringComparison.Ordinal);
        Assert.StartsWith("> > 2\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_LineOverTenThousandCharacters_ReportsInputTooLongAndContinues()
    {
        var result = CliRunner.Repl(new string('1', 10_001) + "\n1+1\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Contains("error[InputTooLong]", result.Stderr, StringComparison.Ordinal);
        Assert.StartsWith("> > 2\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Run_ExplainFlagWithoutExpressionAndInputNotRedirected_StartsReplThatExplains()
    {
        var result = CliRunner.Run(["--explain"], "2*3\n", isInputRedirected: false);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.StartsWith("> 2 * 3 = 6\n6\n> ", result.StdoutAfterBanner, StringComparison.Ordinal);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Repl_EndOfInput_StdoutEndsWithNewline()
    {
        var result = CliRunner.Repl("1+1\n");

        Assert.EndsWith("\n", result.NormalizedStdout, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F6")]
    public void Run_UnknownOption_MessageNamesTheOption()
    {
        var result = CliRunner.OneShot("--bogus");

        Assert.Equal(CliApp.UsageError, result.ExitCode);
        Assert.Contains("'--bogus'", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("usage", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F6")]
    public void Run_HelpFlag_MentionsExitStatusParenthesesAndComments()
    {
        var result = CliRunner.OneShot("--help");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Contains("exit status", result.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("parentheses", result.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#", result.Stdout, StringComparison.Ordinal);
    }
}
