using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Ledger.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);

var app = builder.Build();
await app.EnsureLedgerDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();

app.Run();
