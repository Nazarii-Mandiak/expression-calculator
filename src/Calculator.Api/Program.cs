var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();

// Endpoints are implemented in features F7-F9 (see docs/SPEC.md).
app.Run();

/// <summary>Entry point; public so integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
