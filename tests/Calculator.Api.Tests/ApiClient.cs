using System.Net;
using System.Text;
using System.Text.Json;

namespace Calculator.Api.Tests;

/// <summary>Sends raw JSON so tests control the exact bytes on the wire (including malformed bodies).</summary>
internal static class ApiClient
{
    public static Task<ApiResponse> PostExpressionAsync(this HttpClient client, string path, string expression) =>
        client.PostRawAsync(path, JsonSerializer.Serialize(new { expression }));

    public static async Task<ApiResponse> PostRawAsync(this HttpClient client, string path, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(path, content);
        return await ApiResponse.ReadAsync(response);
    }

    public static async Task<ApiResponse> GetResponseAsync(this HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        return await ApiResponse.ReadAsync(response);
    }
}

internal sealed record ApiResponse(HttpStatusCode Status, string? MediaType, string Body)
{
    public static async Task<ApiResponse> ReadAsync(HttpResponseMessage response) =>
        new(response.StatusCode, response.Content.Headers.ContentType?.MediaType, await response.Content.ReadAsStringAsync());

    /// <summary>The parsed body; property lookups are case-sensitive, so camelCase names are asserted exactly.</summary>
    public JsonElement Json => JsonDocument.Parse(Body).RootElement;
}
