using System.ClientModel;
using System.ClientModel.Primitives;
using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;

using Calculator.Api;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using ModelContextProtocol;

using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}");
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
    // A missing or null "expression" fails binding (400) instead of reaching the endpoint as null.
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});

var llm = builder.Configuration.GetSection("Llm").Get<LlmOptions>() ?? new LlmOptions();
builder.Services.AddOptions<LlmOptions>()
    .BindConfiguration("Llm")
    .ValidateDataAnnotations()
    .Validate(o => !o.IsConfigured || o.HasHttpEndpoint, "Llm:Endpoint must be an absolute http or https URI")
    .Validate(o => !o.IsConfigured || o.KeyTravelsSafely, "Llm:Endpoint must use https when Llm:ApiKey is set and the host is not loopback")
    .ValidateOnStart();
if (llm.IsConfigured)
{
    // Any OpenAI-compatible server works (Ollama, vLLM, Azure OpenAI); no client is registered otherwise,
    // which is what switches the LLM fallback off. Built from validated options, so a bad endpoint never gets here.
    builder.Services.AddSingleton<IChatClient>(services =>
    {
        var o = services.GetRequiredService<IOptions<LlmOptions>>().Value;
        // Local servers such as Ollama need no key, but the SDK rejects an empty one.
        var key = string.IsNullOrWhiteSpace(o.ApiKey) ? "not-needed" : o.ApiKey;
        // One attempt only: a retry would multiply the cost and the wait of a request that has its own timeout.
        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(o.Endpoint!), RetryPolicy = new ClientRetryPolicy(0) };
        return new OpenAIClient(new ApiKeyCredential(key), clientOptions).GetChatClient(o.Model).AsIChatClient();
    });
}

builder.Services.AddSingleton<ExpressionInterpreter>();
// Known limit: shared by all clients behind one proxy address; use forwarded headers when deploying behind one.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        // The concurrency cap has no window to report, so it falls back to one second: a busy slot frees up quickly.
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var window) ? window : TimeSpan.FromSeconds(1);
        context.HttpContext.Response.Headers.RetryAfter =
            ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        return ValueTask.CompletedTask;
    };
    // Chained with the per-client policy below. Caps the whole natural-language endpoint, not just the LLM calls:
    // rules-path requests finish in microseconds, so in practice only requests waiting on the LLM ever hold a permit.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
            == EvaluationEndpoints.NaturalLanguageRateLimit
            ? RateLimitPartition.GetConcurrencyLimiter(
                EvaluationEndpoints.NaturalLanguageRateLimit,
                _ => new ConcurrencyLimiterOptions { PermitLimit = 4, QueueLimit = 0 })
            : RateLimitPartition.GetNoLimiter(""));
    options.AddPolicy(
        EvaluationEndpoints.NaturalLanguageRateLimit,
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

// Proof of concept: REST and MCP share one host. The tools are pure, so stateless HTTP needs no sessions.
var mcpJson = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions);
mcpJson.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
builder.Services.AddMcpServer().WithHttpTransport(options => options.Stateless = true).WithTools<CalculatorTools>(mcpJson);

var app = builder.Build();
// First, so an unexpected exception anywhere below becomes a 500 Problem Details response.
app.UseExceptionHandler();
// Turns bodiless error statuses (400 from bad JSON, 413, 404) into Problem Details.
app.UseStatusCodePages();
app.Use((context, next) =>
{
    // Caps chunked bodies on any server; read-only once the request body has started.
    var limit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (limit is { IsReadOnly: false })
    {
        limit.MaxRequestBodySize = ResponseMapper.MaxBodyBytes;
    }

    // Known limit: TestServer has no server-level limit, so this Content-Length check is the test-host
    // ceiling; real servers enforce the feature above, also for bodies without a Content-Length.
    if (context.Request.ContentLength > ResponseMapper.MaxBodyBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return Task.CompletedTask;
    }

    return next(context);
});
// The limiter rejects with a bodiless 429, which the status code pages above turn into Problem Details.
app.UseRateLimiter();
app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapEvaluationEndpoints();
app.MapMcp("/mcp");
app.Run();

/// <summary>Entry point; public so integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
