using System.Net;


namespace Calculator.Api.Tests;

public sealed class ServiceEndpointTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    public async Task Health_Get_Returns200()
    {
        var response = await _client.GetResponseAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.Status);
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    [InlineData("/v1/evaluations")]
    [InlineData("/v1/evaluations/validate")]
    public async Task OpenApi_Get_Returns200DocumentListingPath(string path)
    {
        var response = await _client.GetResponseAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.True(response.Json.GetProperty("paths").TryGetProperty(path, out _));
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F7")]
    [InlineData("/v1/evaluations", "200")]
    [InlineData("/v1/evaluations", "400")]
    [InlineData("/v1/evaluations", "413")]
    [InlineData("/v1/evaluations", "422")]
    [InlineData("/v1/evaluations/validate", "200")]
    [InlineData("/v1/evaluations/validate", "400")]
    [InlineData("/v1/evaluations/validate", "413")]
    [InlineData("/v1/evaluations/validate", "422")]
    public async Task OpenApi_PostOperation_DeclaresResponseStatus(string path, string status)
    {
        var response = await _client.GetResponseAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var responses = response.Json.GetProperty("paths").GetProperty(path).GetProperty("post").GetProperty("responses");
        Assert.True(responses.TryGetProperty(status, out _));
    }
}
