using Calculator.Core;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calculator.Api;

/// <summary>The <c>/v1/evaluations</c> endpoints: thin adapters over <see cref="Expression"/>.</summary>
internal static class EvaluationEndpoints
{
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
        if (request.Expression.Length > ResponseMapper.MaxExpressionLength)
        {
            return TooLong(request.Expression);
        }

        try
        {
            return TypedResults.Ok(ResponseMapper.Evaluate(request.Expression, request.Explain));
        }
        catch (CalculationException ex)
        {
            return Invalid(ex.Code.ToString(), ex.Message, ex.Position, ex.Length);
        }
    }

    private static Results<Ok<ValidationResponse>, ProblemHttpResult> Validate(ValidationRequest request)
    {
        return request.Expression.Length > ResponseMapper.MaxExpressionLength
            ? TooLong(request.Expression)
            : TypedResults.Ok(ResponseMapper.Validate(request.Expression));
    }

    private static ProblemHttpResult TooLong(string expression) => Invalid(
        "InputTooLong",
        ResponseMapper.TooLongMessage,
        ResponseMapper.MaxExpressionLength,
        expression.Length - ResponseMapper.MaxExpressionLength);

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
