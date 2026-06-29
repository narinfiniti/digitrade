using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Position.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);

var app = builder.Build();
await app.EnsurePositionDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();

app.Run();
