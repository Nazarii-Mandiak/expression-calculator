namespace Calculator.Cli.Tests;

public sealed class CliEdgeCaseTests
{
    [Theory]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    [InlineData(new[] { "- 5" }, "-5\n")]
    [InlineData(new[] { "--5" }, "5\n")]
    [InlineData(new[] { "5", "--3" }, "8\n")]
    public void Run_DashArgumentNotFollowedByLetter_IsPartOfExpression(string[] args, string expectedStdout)
    {
        var result = CliRunner.OneShot(args);

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal(expectedStdout, result.NormalizedStdout);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_LoneDashArgument_ReportsMissingOperandAndExitsOne()
    {
        var result = CliRunner.OneShot("-");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("error[MissingOperand]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  -", result.StderrLines[1]);
        Assert.Equal("   ^", result.StderrLines[2]);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Feature", "F5")]
    public void Run_UnknownOptionAfterExpression_PrintsUsageAndExitsTwo()
    {
        var result = CliRunner.OneShot("1", "--x");

        Assert.Equal(CliApp.UsageError, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Contains("usage", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Feature", "F5")]
    public void Run_NoArgumentsAndInputNotRedirected_ExitsTwoWithEmptyStdout()
    {
        var result = CliRunner.Run([], stdin: "", isInputRedirected: false);

        Assert.Equal(CliApp.UsageError, result.ExitCode);
        Assert.Equal("", result.Stdout);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_BatchLineWithTabs_EchoesSpacesAndAlignsCaret()
    {
        var result = CliRunner.Batch("1\t+\t2 +\n");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("line 1: error[MissingOperand]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  1 + 2 +", result.StderrLines[1]);
        Assert.Equal("         ^", result.StderrLines[2]);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_ControlCharactersAfterError_EchoesQuestionMarksAndKeepsCaretAligned()
    {
        var result = CliRunner.OneShot("2 + * \u001b\u0007");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.DoesNotContain("\u001b", result.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("\u0007", result.Stderr, StringComparison.Ordinal);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("error[MissingOperand]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  2 + * ??", result.StderrLines[1]);
        Assert.Equal("      ^", result.StderrLines[2]);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_EscapeSequenceInExpression_WritesNoRawControlCharactersAndExitsOne()
    {
        var result = CliRunner.OneShot("2 + 3 \u001b[31m x");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.DoesNotContain("\u001b", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    public void Run_BatchInputStartingWithByteOrderMark_IgnoresMark()
    {
        var result = CliRunner.Batch("﻿1+2\n");

        Assert.Equal(CliApp.Success, result.ExitCode);
        Assert.Equal("3\n", result.NormalizedStdout);
        Assert.Equal("", result.Stderr);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    public void Run_BatchLineOverTenThousandCharacters_ReportsInputTooLongAndContinues()
    {
        var result = CliRunner.Batch(new string('1', 10_001) + "\n2+2\n");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("4\n", result.NormalizedStdout);
        Assert.StartsWith("line 1: error[InputTooLong]", result.Stderr, StringComparison.Ordinal);
        Assert.True(result.Stderr.Length < 1_000, $"stderr length was {result.Stderr.Length}");
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Requirement", "R6")]
    [Trait("Feature", "F5")]
    public void Run_QuestionTranslatesButDoesNotParse_ShowsInterpretationThenErrorBlock()
    {
        var result = CliRunner.OneShot("What is 10 minus");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(4, result.StderrLines.Length);
        Assert.Equal("interpreted as: 10 -", result.StderrLines[0]);
        Assert.StartsWith("error[MissingOperand]: ", result.StderrLines[1], StringComparison.Ordinal);
        Assert.Equal("  10 -", result.StderrLines[2]);
        Assert.Equal("      ^", result.StderrLines[3]);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F5")]
    public void Run_BatchLineWithLeadingWhitespace_EchoesTrimmedTextWithAlignedCaret()
    {
        var result = CliRunner.Batch("   10 +\n");

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal(3, result.StderrLines.Length);
        Assert.StartsWith("line 1: error[MissingOperand]: ", result.StderrLines[0], StringComparison.Ordinal);
        Assert.Equal("  10 +", result.StderrLines[1]);
        Assert.Equal("      ^", result.StderrLines[2]);
    }
}
