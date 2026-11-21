using System.Net;
using System.Text.Json;


namespace Calculator.Api.Tests;

public sealed class ValidateEndpointTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    private const string Path = "/v1/evaluations/validate";
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    [InlineData("10-2", "10 - 2")]
    [InlineData("10 - 2", "10 - 2")]
    [InlineData("10 / 0", "10 / 0")]
    [InlineData("79228162514264337593543950335 * 2", "79228162514264337593543950335 * 2")]
    public async Task Validate_ParsableExpression_Returns200ValidWithCanonicalAndNeverEvaluates(string expression, string expectedCanonical)
    {
        var response = await _client.PostExpressionAsync(Path, expression);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.True(response.Json.GetProperty("valid").GetBoolean());
        Assert.Equal(expectedCanonical, response.Json.GetProperty("canonical").GetString());
        Assert.False(response.Json.TryGetProperty("error", out _));
        Assert.False(response.Json.TryGetProperty("result", out _));
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F7")]
    [InlineData("10 + * 3", "MissingOperand", 5, 1)]
    [InlineData("2 + a", "UnexpectedCharacter", 4, 1)]
    [InlineData("10 20", "MissingOperator", 3, 2)]
    [InlineData("", "EmptyExpression", 0, 1)]
    public async Task Validate_InvalidExpression_Returns200InvalidWithLocatedError(
        string expression, string expectedCode, int expectedPosition, int expectedLength)
    {
        var response = await _client.PostExpressionAsync(Path, expression);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.False(response.Json.GetProperty("valid").GetBoolean());
        Assert.False(response.Json.TryGetProperty("canonical", out _));
        var error = response.Json.GetProperty("error");
        Assert.Equal(expectedCode, error.GetProperty("errorCode").GetString());
        Assert.Equal(expectedPosition, error.GetProperty("position").GetInt32());
        Assert.Equal(expectedLength, error.GetProperty("length").GetInt32());
        Assert.Equal(JsonValueKind.String, error.GetProperty("message").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
    }
}
