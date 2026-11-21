using Calculator.Core;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calculator.Api;

/// <summary>The <c>/v1/evaluations</c> endpoints: thin adapters over <see cref="Expression"/>.</summary>
internal static class EvaluationEndpoints
{
    internal const int MaxExpressionLength = 1_000;
    internal const int MaxBodyBytes = 16 * 1024;

    public static IEndpointRouteBuilder MapEvaluationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/v1/evaluations")
            .WithTags("Evaluations")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/", Evaluate)
            .WithName("Evaluate")
            .WithSummary("Evaluates an expression, optionally listing each step.");
        group.MapPost("/validate", Validate)
            .WithName("Validate")
            .WithSummary("Checks that an expression parses, without evaluating it.");

        return routes;
    }

    private static Results<Ok<EvaluationResponse>, ProblemHttpResult> Evaluate(EvaluationRequest request)
    {
        if (request.Expression.Length > MaxExpressionLength)
        {
            return TooLong(request.Expression);
        }

        try
        {
            var expression = Expression.Parse(request.Expression);
            var evaluation = expression.Evaluate();
            var steps = request.Explain ? evaluation.Steps.Select(ToDto).ToList() : null;
            return TypedResults.Ok(new EvaluationResponse(expression.ToString(), NumberFormatter.Format(evaluation.Value), steps));
        }
        catch (CalculationException ex)
        {
            return Invalid(ex.Code.ToString(), ex.Message, ex.Position, ex.Length);
        }
    }

    private static Results<Ok<ValidationResponse>, ProblemHttpResult> Validate(ValidationRequest request)
    {
        if (request.Expression.Length > MaxExpressionLength)
        {
            return TooLong(request.Expression);
        }

        try
        {
            return TypedResults.Ok(new ValidationResponse(true, Expression.Parse(request.Expression).ToString(), null));
        }
        catch (CalculationException ex)
        {
            var error = new ValidationError(ex.Code.ToString(), ex.Position, ex.Length, ex.Message);
            return TypedResults.Ok(new ValidationResponse(false, null, error));
        }
    }

    private static ProblemHttpResult TooLong(string expression) => Invalid(
        "InputTooLong",
        $"An expression may have at most {MaxExpressionLength} characters.",
        MaxExpressionLength,
        expression.Length - MaxExpressionLength);

    private static StepDto ToDto(EvaluationStep step) => new(
        NumberFormatter.Format(step.Left),
        step.Operator.ToSymbol().ToString(),
        NumberFormatter.Format(step.Right),
        NumberFormatter.Format(step.Result));

    // The one place that builds a 422, so every invalid-expression problem has the same shape.
    private static ProblemHttpResult Invalid(string errorCode, string detail, int position, int length) => TypedResults.Problem(
        title: "The expression is invalid.",
        detail: detail,
        statusCode: StatusCodes.Status422UnprocessableEntity,
        extensions: new Dictionary<string, object?>
        {
            ["errorCode"] = errorCode,
            ["position"] = position,
            ["length"] = length,
        });
}
