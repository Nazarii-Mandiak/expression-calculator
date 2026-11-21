using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Calculator.Api.Tests;

/// <summary>Hosts the API in the Production environment so error responses look exactly as clients see them.</summary>
public sealed class ProductionApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");
}
