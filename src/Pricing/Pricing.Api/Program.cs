using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Pricing.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);

var app = builder.Build();
await app.EnsurePricingDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();

app.Run();
