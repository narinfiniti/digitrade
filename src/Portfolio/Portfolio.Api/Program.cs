using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Portfolio.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);

var app = builder.Build();
await app.EnsurePortfolioDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();

app.Run();
