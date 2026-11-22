using System.Text.Json;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Calculator.Api.Tests;

/// <summary>Talks to <c>/mcp</c> over the real MCP Streamable HTTP protocol, as an LLM host would.</summary>
public sealed class McpEndpointTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    private async Task<McpClient> ConnectAsync()
    {
        var httpClient = factory.CreateClient();
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            httpClient,
            loggerFactory: null,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public async Task ListTools_Mcp_ReturnsExactlyTheTwoCalculatorTools()
    {
        await using var client = await ConnectAsync();

        var tools = await client.ListToolsAsync();

        Assert.Equal(["evaluate_expression", "validate_expression"], tools.Select(tool => tool.Name).Order());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    [InlineData("evaluate_expression")]
    [InlineData("validate_expression")]
    public async Task ListTools_EachTool_HasDescriptionAndReadOnlyAnnotations(string toolName)
    {
        await using var client = await ConnectAsync();

        var tools = await client.ListToolsAsync();

        var tool = Assert.Single(tools, candidate => candidate.Name == toolName).ProtocolTool;
        Assert.False(string.IsNullOrWhiteSpace(tool.Description));
        Assert.NotNull(tool.Annotations);
        Assert.True(tool.Annotations.ReadOnlyHint);
        Assert.False(tool.Annotations.DestructiveHint);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public async Task ListTools_EvaluateExpression_DescriptionForbidsComputingTheAnswer()
    {
        await using var client = await ConnectAsync();

        var tools = await client.ListToolsAsync();

        var description = Assert.Single(tools, tool => tool.Name == "evaluate_expression").Description;
        Assert.Contains("+ - * /", description, StringComparison.Ordinal);
        Assert.Contains("Never compute the answer yourself", description, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public async Task CallTool_EvaluateChallengeExpression_ReturnsResult7AsTextAndStructuredContent()
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            "evaluate_expression",
            new Dictionary<string, object?> { ["expression"] = "10 - 2 * 6 / 4" });

        Assert.NotEqual(true, result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("7", text, StringComparison.Ordinal);
        var structured = JsonSerializer.SerializeToElement(result.StructuredContent);
        Assert.Equal("10 - 2 * 6 / 4", structured.GetProperty("expression").GetString());
        Assert.Equal("7", structured.GetProperty("result").GetString());
        Assert.Equal(3, structured.GetProperty("steps").GetArrayLength());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    [InlineData("10 + * 3", "MissingOperand", "index 5")]
    [InlineData("10 / 0", "DivisionByZero", "index 3")]
    public async Task CallTool_EvaluateInvalidExpression_ReturnsToolErrorWithErrorCode(string expression, string expectedCode, string expectedIndex)
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            "evaluate_expression",
            new Dictionary<string, object?> { ["expression"] = expression });

        Assert.True(result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains(expectedCode, text, StringComparison.Ordinal);
        Assert.Contains(expectedIndex, text, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    [InlineData("evaluate_expression")]
    [InlineData("validate_expression")]
    public async Task CallTool_1001Characters_ReturnsToolErrorWithInputTooLongAtIndex1000(string toolName)
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            toolName,
            new Dictionary<string, object?> { ["expression"] = new string('1', 1001) });

        Assert.True(result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("InputTooLong", text, StringComparison.Ordinal);
        Assert.Contains("index 1000", text, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    [InlineData("evaluate_expression")]
    [InlineData("validate_expression")]
    public async Task CallTool_NullExpression_ReturnsToolErrorWithMissingExpression(string toolName)
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            toolName,
            new Dictionary<string, object?> { ["expression"] = null });

        Assert.True(result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("MissingExpression", text, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public async Task CallTool_ValidateUnformattedExpression_ReturnsValidWithCanonical()
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            "validate_expression",
            new Dictionary<string, object?> { ["expression"] = "10-2" });

        Assert.NotEqual(true, result.IsError);
        var structured = JsonSerializer.SerializeToElement(result.StructuredContent);
        Assert.True(structured.GetProperty("valid").GetBoolean());
        Assert.Equal("10 - 2", structured.GetProperty("canonical").GetString());
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public async Task CallTool_ValidateInvalidExpression_ReturnsInvalidWithLocatedError()
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            "validate_expression",
            new Dictionary<string, object?> { ["expression"] = "10 + * 3" });

        Assert.NotEqual(true, result.IsError);
        var structured = JsonSerializer.SerializeToElement(result.StructuredContent);
        Assert.False(structured.GetProperty("valid").GetBoolean());
        Assert.Equal("MissingOperand", structured.GetProperty("error").GetProperty("errorCode").GetString());
        Assert.Equal(5, structured.GetProperty("error").GetProperty("position").GetInt32());
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "Hardening")]
    [InlineData("evaluate_expression")]
    [InlineData("validate_expression")]
    public async Task CallTool_1005Characters_ReturnsToolErrorWithInputTooLongAtIndex1000Length5(string toolName)
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            toolName,
            new Dictionary<string, object?> { ["expression"] = new string('1', 1005) });

        Assert.True(result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("InputTooLong", text, StringComparison.Ordinal);
        Assert.Contains("index 1000", text, StringComparison.Ordinal);
        Assert.Contains("length 5", text, StringComparison.Ordinal);
    }
}
