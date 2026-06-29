using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Risk.Api.Endpoints;
using Risk.Api.Mapping;
using Risk.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(RiskApiMappingProfile));

var app = builder.Build();
await app.EnsureRiskDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();
app.MapRiskEndpoints();

app.Run();

namespace Risk.Api
{
    public partial class Program;
}
