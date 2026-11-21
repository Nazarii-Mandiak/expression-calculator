using System.Globalization;

namespace Calculator.Core.Tests;

/// <summary>
/// Property and differential tests: the engine is compared with <see cref="ReferenceEvaluator"/>
/// and with itself under whitespace changes and canonical round-trips on seeded random input.
/// Failures list the seed and every offending expression, so they are reproducible.
/// </summary>
[Trait("Feature", "F4")]
[Trait("Requirement", "R3")]
[Trait("Requirement", "R4")]
[Trait("Requirement", "R7")]
public sealed class DifferentialTests
{
    private const int Seed = 20261121;
    private const int Iterations = 2_000;

    [Fact]
    public void Evaluate_RandomExpressions_MatchesReferenceEvaluator()
    {
        var generator = new ExpressionGenerator(Seed);
        var failures = new List<string>();
        var divisionsByZero = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var text = generator.Render(generator.NextTokens(), maxSpaces: 2);
            var expected = ReferenceOutcome(text);
            var actual = EngineOutcome(text);
            if (expected == DivisionByZero) divisionsByZero++;
            if (actual != expected) failures.Add($"'{text}': engine {actual}, reference {expected}");
        }

        AssertNoFailures(failures);
        // Guards against a generator change silently dropping the error path from the comparison.
        Assert.True(divisionsByZero > 0, $"seed {Seed}: no division by zero was generated");
    }

    [Fact]
    public void Evaluate_SpacesRemovedOrAdded_SameOutcome()
    {
        var generator = new ExpressionGenerator(Seed);
        var failures = new List<string>();
        for (var i = 0; i < Iterations; i++)
        {
            var tokens = generator.NextTokens();
            var compact = string.Concat(tokens);
            var spaced = "  " + generator.Render(tokens, maxSpaces: 4).Replace(" ", "   ") + "  ";
            var compactOutcome = EngineOutcome(compact);
            var spacedOutcome = EngineOutcome(spaced);
            if (compactOutcome != spacedOutcome)
                failures.Add($"'{compact}' -> {compactOutcome} but '{spaced}' -> {spacedOutcome}");
        }

        AssertNoFailures(failures);
    }

    [Fact]
    public void Parse_CanonicalText_RoundTripsToSameTextAndOutcome()
    {
        var generator = new ExpressionGenerator(Seed);
        var failures = new List<string>();
        for (var i = 0; i < Iterations; i++)
        {
            var text = generator.Render(generator.NextTokens(), maxSpaces: 2);
            var canonical = Expression.Parse(text).ToString();
            var reparsed = Expression.Parse(canonical).ToString();
            if (reparsed != canonical)
                failures.Add($"'{text}': canonical '{canonical}' re-parses to '{reparsed}'");
            else if (EngineOutcome(canonical) != EngineOutcome(text))
                failures.Add($"'{text}' -> {EngineOutcome(text)} but canonical '{canonical}' -> {EngineOutcome(canonical)}");
        }

        AssertNoFailures(failures);
    }

    // decimal equality ignores scale (2.50 == 2.5), and record equality uses it.
    private sealed record Outcome(decimal? Value, ErrorCode? Error)
    {
        public override string ToString() => Error is { } code
            ? $"error {code}"
            : Value!.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static readonly Outcome DivisionByZero = new(null, ErrorCode.DivisionByZero);

    private static Outcome ReferenceOutcome(string text)
    {
        try
        {
            return new(ReferenceEvaluator.Evaluate(text), null);
        }
        catch (DivideByZeroException)
        {
            return DivisionByZero;
        }
    }

    private static Outcome EngineOutcome(string text)
    {
        try
        {
            return new(Expression.Parse(text).Evaluate().Value, null);
        }
        catch (CalculationException exception)
        {
            return new(null, exception.Code);
        }
    }

    private static void AssertNoFailures(List<string> failures) =>
        Assert.True(failures.Count == 0,
            $"seed {Seed}: {failures.Count} failure(s)\n{string.Join("\n", failures.Take(20))}");
}
