using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Settlement.Api.Endpoints;
using Settlement.Api.Mapping;
using Settlement.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(SettlementApiMappingProfile));

var app = builder.Build();
await app.EnsureSettlementDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();
app.MapSettlementEndpoints();

app.Run();

namespace Settlement.Api
{
    public partial class Program;
}
