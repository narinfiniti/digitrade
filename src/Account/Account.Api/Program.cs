using Account.Dependency;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);

var app = builder.Build();
await app.EnsureAccountDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();

app.Run();
