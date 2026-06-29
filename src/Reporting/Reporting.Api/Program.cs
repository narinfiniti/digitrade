using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Reporting.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);

var app = builder.Build();
await app.EnsureReportingDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();

app.Run();
