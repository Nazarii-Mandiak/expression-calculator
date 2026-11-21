using Calculator.Core;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Calculator.Api;

/// <summary>The <c>/v1/evaluations</c> endpoints: thin adapters over <see cref="Expression"/>.</summary>
internal static class EvaluationEndpoints
{
    internal const string NaturalLanguageRateLimit = "natural-language";

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
        group.MapPost("/natural-language", EvaluateNaturalLanguage)
            .WithName("EvaluateNaturalLanguage")
            .WithSummary("Evaluates a plain-English question, using rules first and an optional LLM second.")
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireRateLimiting(NaturalLanguageRateLimit);

        return routes;
    }

    private static Results<Ok<EvaluationResponse>, ProblemHttpResult> Evaluate(EvaluationRequest request)
    {
        if (request.Expression.Length > ResponseMapper.MaxExpressionLength)
        {
            return TooLong(request.Expression, ResponseMapper.TooLongMessage);
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
            ? TooLong(request.Expression, ResponseMapper.TooLongMessage)
            : TypedResults.Ok(ResponseMapper.Validate(request.Expression));
    }

    private static async Task<Results<Ok<NaturalLanguageResponse>, ProblemHttpResult>> EvaluateNaturalLanguage(
        NaturalLanguageRequest request, ExpressionInterpreter interpreter, CancellationToken cancellationToken)
    {
        // Checked before interpreting so an oversized question never reaches the LLM.
        if (request.Question.Length > ResponseMapper.MaxExpressionLength)
        {
            return TooLong(request.Question, "A question may have at most 1000 characters.");
        }

        var interpretation = await interpreter.InterpretAsync(request.Question, cancellationToken);
        switch (interpretation.Status)
        {
            case InterpretationStatus.Uninterpretable:
                return TypedResults.Problem(
                    title: "The question could not be interpreted.",
                    detail: "Rephrase the question using numbers and plus, minus, times or divided by.",
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    extensions: new Dictionary<string, object?> { ["errorCode"] = "Uninterpretable" });
            case InterpretationStatus.LlmUnavailable:
                return TypedResults.Problem(
                    title: "The language model is unavailable.",
                    detail: "The language model did not answer in time or failed. Try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            var evaluation = ResponseMapper.Evaluate(interpretation.Expression, explain: false);
            return TypedResults.Ok(
                new NaturalLanguageResponse(interpretation.Expression, evaluation.Result, interpretation.Interpreter));
        }
        catch (CalculationException ex)
        {
            return Invalid(
                ex.Code.ToString(), ex.Message, ex.Position, ex.Length,
                new Dictionary<string, object?>
                {
                    ["interpretedExpression"] = interpretation.Expression,
                    ["interpreter"] = interpretation.Interpreter,
                });
        }
    }

    private static ProblemHttpResult TooLong(string text, string message) => Invalid(
        "InputTooLong",
        message,
        ResponseMapper.MaxExpressionLength,
        text.Length - ResponseMapper.MaxExpressionLength);

    // The one place that builds a 422, so every invalid-expression problem has the same shape.
    private static ProblemHttpResult Invalid(
        string errorCode, string detail, int position, int length, Dictionary<string, object?>? extra = null)
    {
        var extensions = new Dictionary<string, object?>
        {
            ["errorCode"] = errorCode,
            ["position"] = position,
            ["length"] = length,
        };
        foreach (var (key, value) in extra ?? [])
        {
            extensions[key] = value;
        }

        return TypedResults.Problem(
            title: "The expression is invalid.",
            detail: detail,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            extensions: extensions);
    }
}
