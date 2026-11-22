namespace Calculator.Cli.Tests;

public sealed class OneShotLengthLimitTests
{
    // Both inputs are valid expressions (worth 5001) if the limit were not enforced.
    private static readonly string LongSingleArgument = "1" + string.Concat(Enumerable.Repeat("+1", 5000));
    private static readonly string FirstHalf = "1" + string.Concat(Enumerable.Repeat("+1", 2500));
    private static readonly string SecondHalf = string.Concat(Enumerable.Repeat("+1", 2500));

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "Hardening")]
    public void Run_SingleArgumentOver10000Characters_ReportsInputTooLongWithoutEchoAndExitsOne()
    {
        var result = CliRunner.OneShot(LongSingleArgument);

        Assert.Equal(10_001, LongSingleArgument.Length);
        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Contains("error[InputTooLong]", result.Stderr, StringComparison.Ordinal);
        Assert.True(result.Stderr.Length < 1_000, $"stderr has {result.Stderr.Length} characters");
    }

    [Fact]
    [Trait("Requirement", "R1")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "Hardening")]
    public void Run_ArgumentsTotallingOver10000Characters_ReportsInputTooLongWithoutEchoAndExitsOne()
    {
        var result = CliRunner.OneShot(FirstHalf, SecondHalf);

        Assert.Equal(CliApp.EvaluationFailed, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Contains("error[InputTooLong]", result.Stderr, StringComparison.Ordinal);
        Assert.True(result.Stderr.Length < 1_000, $"stderr has {result.Stderr.Length} characters");
    }
}
