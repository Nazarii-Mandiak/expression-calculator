using System.ComponentModel.DataAnnotations;

namespace Calculator.Api;

/// <summary>The optional <c>Llm</c> configuration section; the LLM fallback is on only when Endpoint and Model are set.</summary>
internal sealed class LlmOptions
{
    /// <summary>Base URL of an OpenAI-compatible API, e.g. <c>http://localhost:11434/v1</c> for Ollama.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Model or deployment name sent with every request.</summary>
    public string? Model { get; set; }

    /// <summary>Bearer key; local servers such as Ollama accept any value, so it is optional.</summary>
    public string? ApiKey { get; set; }

    /// <summary>How long one model call may take before the request fails with 503.</summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Whether <see cref="Endpoint"/> is an absolute http or https URI.</summary>
    public bool HasHttpEndpoint =>
        Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    /// <summary>Whether the key, if any, is only sent over https or to a loopback host.</summary>
    public bool KeyTravelsSafely =>
        string.IsNullOrWhiteSpace(ApiKey)
        || (Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.IsLoopback));

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Model);
}
