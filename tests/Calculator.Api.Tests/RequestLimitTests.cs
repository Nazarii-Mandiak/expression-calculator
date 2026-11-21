using System.Net;


namespace Calculator.Api.Tests;

public sealed class RequestLimitTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    // "11" + 499 x "+1" = 1,000 characters, a valid expression worth 510.
    private static readonly string ThousandCharacters = "11" + string.Concat(Enumerable.Repeat("+1", 499));

    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F7")]
    [InlineData("""{}""")]
    [InlineData("""{"expression":null}""")]
    [InlineData("""{"expression":42}""")]
    [InlineData("""{"expression":true}""")]
    [InlineData("""{"expression":["1+1"]}""")]
    [InlineData("""{"expression":"1+1" """)]
    [InlineData("""not json""")]
    [InlineData("")]
    public async Task Evaluate_MissingOrNonStringExpressionOrMalformedJson_Returns400ProblemDetails(string body)
    {
        var response = await _client.PostRawAsync("/v1/evaluations", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("application/problem+json", response.MediaType);
        Assert.Equal(400, response.Json.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("title").GetString()));
        Assert.Equal("POST /v1/evaluations", response.Json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("traceId").GetString()));
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F7")]
    [InlineData("""{}""")]
    [InlineData("""{"expression":null}""")]
    [InlineData("""{"expression":42}""")]
    [InlineData("""not json""")]
    public async Task Validate_MissingOrNonStringExpressionOrMalformedJson_Returns400ProblemDetails(string body)
    {
        var response = await _client.PostRawAsync("/v1/evaluations/validate", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("application/problem+json", response.MediaType);
        Assert.Equal(400, response.Json.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("title").GetString()));
        Assert.Equal("POST /v1/evaluations/validate", response.Json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("traceId").GetString()));
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F7")]
    [InlineData("/v1/evaluations", """{"expression":"1+1","explian":true}""")]
    [InlineData("/v1/evaluations/validate", """{"expression":"1+1","extra":1}""")]
    public async Task Post_UnknownJsonMember_Returns400ProblemDetails(string path, string body)
    {
        var response = await _client.PostRawAsync(path, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("application/problem+json", response.MediaType);
        Assert.Equal(400, response.Json.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("title").GetString()));
        Assert.Equal($"POST {path}", response.Json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("traceId").GetString()));
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    public async Task Evaluate_ExpressionOfExactly1000Characters_IsAccepted()
    {
        var response = await _client.PostExpressionAsync("/v1/evaluations", ThousandCharacters);

        Assert.Equal(1000, ThousandCharacters.Length);
        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("510", response.Json.GetProperty("result").GetString());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F7")]
    [InlineData("/v1/evaluations")]
    [InlineData("/v1/evaluations/validate")]
    public async Task Post_ExpressionOf1001Characters_Returns422InputTooLong(string path)
    {
        var response = await _client.PostExpressionAsync(path, "1" + ThousandCharacters);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.Status);
        Assert.Equal("application/problem+json", response.MediaType);
        Assert.Equal("InputTooLong", response.Json.GetProperty("errorCode").GetString());
        Assert.Equal(422, response.Json.GetProperty("status").GetInt32());
        Assert.Equal(1000, response.Json.GetProperty("position").GetInt32());
        Assert.Equal(1, response.Json.GetProperty("length").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("title").GetString()));
        Assert.Equal($"POST {path}", response.Json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("traceId").GetString()));
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F7")]
    [InlineData("/v1/evaluations")]
    [InlineData("/v1/evaluations/validate")]
    public async Task Post_ExpressionOf1005Characters_Returns422InputTooLongWithLengthOfExcess(string path)
    {
        var response = await _client.PostExpressionAsync(path, "11111" + ThousandCharacters);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.Status);
        Assert.Equal("application/problem+json", response.MediaType);
        Assert.Equal("InputTooLong", response.Json.GetProperty("errorCode").GetString());
        Assert.Equal(1000, response.Json.GetProperty("position").GetInt32());
        Assert.Equal(5, response.Json.GetProperty("length").GetInt32());
        Assert.Equal($"POST {path}", response.Json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("traceId").GetString()));
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    public async Task Evaluate_BodyJustUnder16KilobytesOfWhitespace_IsAccepted()
    {
        var response = await _client.PostRawAsync("/v1/evaluations", "{\"expression\":\"1+1\"" + new string(' ', 15_000) + "}");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("2", response.Json.GetProperty("result").GetString());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    [InlineData("/v1/evaluations")]
    [InlineData("/v1/evaluations/validate")]
    public async Task Post_BodyOver16Kilobytes_Returns413(string path)
    {
        var response = await _client.PostRawAsync(path, "{\"expression\":\"1+1\"" + new string(' ', 17_000) + "}");

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.Status);
        Assert.Equal("application/problem+json", response.MediaType);
        Assert.Equal(413, response.Json.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("title").GetString()));
        Assert.Equal($"POST {path}", response.Json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(response.Json.GetProperty("traceId").GetString()));
    }
}
