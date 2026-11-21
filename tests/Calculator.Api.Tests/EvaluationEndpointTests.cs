using System.Net;
using System.Text.Json;


namespace Calculator.Api.Tests;

public sealed class EvaluationEndpointTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    private const string Path = "/v1/evaluations";
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    [InlineData("10 - 2 * 6 / 4", "10 - 2 * 6 / 4", "7")]
    [InlineData("10-2*6/4", "10 - 2 * 6 / 4", "7")]
    [InlineData("  2 × 3 ÷ 4 − 1 ", "2 * 3 / 4 - 1", "0.5")]
    [InlineData("1/3", "1 / 3", "0.3333333333333333333333333")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950335", "79228162514264337593543950335")]
    public async Task Evaluate_ValidExpression_Returns200WithCanonicalExpressionAndStringResult(
        string expression, string expectedCanonical, string expectedResult)
    {
        var response = await _client.PostExpressionAsync(Path, expression);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("application/json", response.MediaType);
        Assert.Equal(expectedCanonical, response.Json.GetProperty("expression").GetString());
        Assert.Equal(JsonValueKind.String, response.Json.GetProperty("result").ValueKind);
        Assert.Equal(expectedResult, response.Json.GetProperty("result").GetString());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    [InlineData("""{"expression":"10 - 2 * 6 / 4"}""")]
    [InlineData("""{"expression":"10 - 2 * 6 / 4","explain":false}""")]
    public async Task Evaluate_ExplainAbsentOrFalse_OmitsSteps(string body)
    {
        var response = await _client.PostRawAsync(Path, body);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("7", response.Json.GetProperty("result").GetString());
        Assert.False(response.Json.TryGetProperty("steps", out _));
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    public async Task Evaluate_ExplainTrue_ReturnsResultAndThreeSteps()
    {
        var response = await _client.PostRawAsync(Path, """{"expression":"10 - 2 * 6 / 4","explain":true}""");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("10 - 2 * 6 / 4", response.Json.GetProperty("expression").GetString());
        Assert.Equal("7", response.Json.GetProperty("result").GetString());
        Assert.Equal(3, response.Json.GetProperty("steps").GetArrayLength());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    [InlineData(0, "2", "*", "6", "12")]
    [InlineData(1, "12", "/", "4", "3")]
    [InlineData(2, "10", "-", "3", "7")]
    public async Task Evaluate_ExplainTrue_StepIsObjectOfStrings(int index, string left, string op, string right, string result)
    {
        var response = await _client.PostRawAsync(Path, """{"expression":"10-2*6/4","explain":true}""");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var step = response.Json.GetProperty("steps")[index];
        Assert.Equal(JsonValueKind.String, step.GetProperty("left").ValueKind);
        Assert.Equal(JsonValueKind.String, step.GetProperty("result").ValueKind);
        Assert.Equal(left, step.GetProperty("left").GetString());
        Assert.Equal(op, step.GetProperty("operator").GetString());
        Assert.Equal(right, step.GetProperty("right").GetString());
        Assert.Equal(result, step.GetProperty("result").GetString());
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    public async Task Evaluate_ExplainTrueWithoutOperators_ReturnsEmptySteps()
    {
        var response = await _client.PostRawAsync(Path, """{"expression":"7","explain":true}""");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("7", response.Json.GetProperty("result").GetString());
        Assert.Equal(0, response.Json.GetProperty("steps").GetArrayLength());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F7")]
    [InlineData("10 / 0", "DivisionByZero", 3, 1)]
    [InlineData("10 + * 3", "MissingOperand", 5, 1)]
    [InlineData("10 +", "MissingOperand", 4, 1)]
    [InlineData("79228162514264337593543950335 + 1", "Overflow", 30, 1)]
    [InlineData("1.2.3", "InvalidNumber", 0, 5)]
    [InlineData("", "EmptyExpression", 0, 1)]
    [InlineData("what is 2 plus 2", "UnexpectedCharacter", 0, 1)]
    public async Task Evaluate_InvalidExpression_Returns422ProblemDetailsWithLocation(
        string expression, string expectedCode, int expectedPosition, int expectedLength)
    {
        var response = await _client.PostExpressionAsync(Path, expression);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.Status);
        Assert.Equal("application/problem+json", response.MediaType);
        Assert.Equal(422, response.Json.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, response.Json.GetProperty("errorCode").GetString());
        Assert.Equal(expectedPosition, response.Json.GetProperty("position").GetInt32());
        Assert.Equal(expectedLength, response.Json.GetProperty("length").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("detail").GetString()));
        Assert.Equal("POST /v1/evaluations", response.Json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("traceId").GetString()));
    }
}
