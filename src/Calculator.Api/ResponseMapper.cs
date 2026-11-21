using Calculator.Core;

namespace Calculator.Api;

/// <summary>Maps engine results to the response contracts, shared by the REST endpoints and the MCP tools.</summary>
internal static class ResponseMapper
{
    internal const int MaxExpressionLength = 1_000;
    internal const int MaxBodyBytes = 16 * 1024;

    internal static string TooLongMessage { get; } = $"An expression may have at most {MaxExpressionLength} characters.";

    /// <exception cref="CalculationException">The expression is invalid or its evaluation fails.</exception>
    internal static EvaluationResponse Evaluate(string text, bool explain)
    {
        var expression = Expression.Parse(text);
        var evaluation = expression.Evaluate();
        var steps = explain ? evaluation.Steps.Select(ToDto).ToList() : null;
        return new EvaluationResponse(expression.ToString(), NumberFormatter.Format(evaluation.Value), steps);
    }

    internal static ValidationResponse Validate(string text)
    {
        try
        {
            return new ValidationResponse(true, Expression.Parse(text).ToString(), null);
        }
        catch (CalculationException ex)
        {
            return new ValidationResponse(false, null, new ValidationError(ex.Code.ToString(), ex.Position, ex.Length, ex.Message));
        }
    }

    private static StepDto ToDto(EvaluationStep step) => new(
        NumberFormatter.Format(step.Left),
        step.Operator.ToSymbol().ToString(),
        NumberFormatter.Format(step.Right),
        NumberFormatter.Format(step.Result));
}
