using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Instrument.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);

var app = builder.Build();
await app.EnsureInstrumentDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();

app.Run();
