using System.Net;
using System.Net.Http.Json;

namespace Calculator.Api.Tests;

/// <summary>DNS-rebinding protection: only loopback host names are served by default.</summary>
public sealed class HostFilteringTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public async Task Evaluate_ForeignHostHeader_Returns400()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/evaluations")
        {
            Content = JsonContent.Create(new { expression = "1 + 1" }),
        };
        request.Headers.Host = "evil.example";

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public async Task Evaluate_DefaultLocalhostHost_Returns200()
    {
        using var client = factory.CreateClient();

        var response = await client.PostExpressionAsync("/v1/evaluations", "1 + 1");

        Assert.Equal(HttpStatusCode.OK, response.Status);
    }
}
