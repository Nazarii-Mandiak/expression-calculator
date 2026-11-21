using System.Text.Json;
using System.Text.Json.Serialization;

namespace Calculator.Api;

/// <summary>Body of <c>POST /v1/evaluations</c>.</summary>
/// <param name="Expression">The arithmetic expression text.</param>
/// <param name="Explain">When true, the response lists every operation performed.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EvaluationRequest(string Expression, bool Explain = false);

/// <summary>Successful evaluation; values are strings so JavaScript clients lose no precision.</summary>
/// <param name="Expression">Canonical form of the expression.</param>
/// <param name="Result">The formatted result.</param>
/// <param name="Steps">The operations performed in order; omitted unless requested.</param>
public sealed record EvaluationResponse(
    string Expression,
    string Result,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<StepDto>? Steps);

/// <summary>One binary operation, e.g. <c>2 * 6 = 12</c>.</summary>
/// <param name="Left">Left operand.</param>
/// <param name="Operator">One of <c>+ - * /</c>.</param>
/// <param name="Right">Right operand.</param>
/// <param name="Result">Result of the operation.</param>
public sealed record StepDto(string Left, string Operator, string Right, string Result);

/// <summary>Body of <c>POST /v1/evaluations/validate</c>.</summary>
/// <param name="Expression">The arithmetic expression text.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ValidationRequest(string Expression);

/// <summary>Outcome of validating an expression; exactly one of <see cref="Canonical"/> and <see cref="Error"/> is set.</summary>
/// <param name="Valid">Whether the expression parses.</param>
/// <param name="Canonical">Canonical form when valid.</param>
/// <param name="Error">Located error when invalid.</param>
public sealed record ValidationResponse(
    bool Valid,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Canonical,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ValidationError? Error);

/// <summary>Where and why an expression is invalid.</summary>
/// <param name="ErrorCode">Name of the Core <c>ErrorCode</c>.</param>
/// <param name="Position">Zero-based index of the offending span.</param>
/// <param name="Length">Length of the offending span.</param>
/// <param name="Message">Human-readable description.</param>
public sealed record ValidationError(string ErrorCode, int Position, int Length, string Message);

/// <summary>Body of <c>POST /v1/evaluations/natural-language</c>.</summary>
/// <param name="Question">The question in plain English, e.g. <c>What is 3 plus 4?</c>.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NaturalLanguageRequest(string Question);

/// <summary>Result of a natural-language question.</summary>
/// <param name="InterpretedExpression">The expression text the question was turned into; error positions refer to it.</param>
/// <param name="Result">The formatted result.</param>
/// <param name="Interpreter">Who produced the expression: <c>rules</c> or <c>llm</c>.</param>
public sealed record NaturalLanguageResponse(string InterpretedExpression, string Result, string Interpreter);

/// <summary>Build-time JSON metadata for the REST bodies and MCP tool results; MCP tool discovery still uses reflection.</summary>
[JsonSerializable(typeof(EvaluationRequest))]
[JsonSerializable(typeof(EvaluationResponse))]
[JsonSerializable(typeof(ValidationRequest))]
[JsonSerializable(typeof(ValidationResponse))]
[JsonSerializable(typeof(NaturalLanguageRequest))]
[JsonSerializable(typeof(NaturalLanguageResponse))]
internal sealed partial class ApiJsonContext : JsonSerializerContext;
