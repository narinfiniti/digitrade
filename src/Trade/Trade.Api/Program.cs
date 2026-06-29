using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Trade.Api.Endpoints;
using Trade.Api.Mapping;
using Trade.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(TradeApiMappingProfile));

var app = builder.Build();
await app.EnsureTradeDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();
app.MapTradeEndpoints();

app.Run();

namespace Trade.Api
{
    public partial class Program;
}
