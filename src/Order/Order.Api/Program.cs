using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Order.Api.Endpoints;
using Order.Api.Mapping;
using Order.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(OrderApiMappingProfile));

var app = builder.Build();
await app.EnsureOrderDatabaseMigratedAsync();
app.MapDigiTradeHealthEndpoints();
app.MapOrderEndpoints();

app.Run();

namespace Order.Api
{
    public partial class Program;
}
