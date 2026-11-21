using Calculator.Api;

using Microsoft.AspNetCore.Http.Features;

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
        limit.MaxRequestBodySize = EvaluationEndpoints.MaxBodyBytes;
    }

    // Known limit: TestServer has no server-level limit, so this Content-Length check is the test-host
    // ceiling; real servers enforce the feature above, also for bodies without a Content-Length.
    if (context.Request.ContentLength > EvaluationEndpoints.MaxBodyBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return Task.CompletedTask;
    }

    return next(context);
});
app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapEvaluationEndpoints();
app.Run();

/// <summary>Entry point; public so integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
