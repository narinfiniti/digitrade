using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;
using Identity.Api.Endpoints;
using Identity.Api.Mapping;
using Identity.Dependency;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddProblemDetails();
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(IdentityApiMappingProfile));
builder.Services.AddDigiTradeSwagger(
    builder.Configuration,
    title: "DigiTrade Identity API",
    description: "Identity endpoints for user registration and token issuance.");

var app = builder.Build();
await app.EnsureIdentityDatabaseMigratedAsync();
app.UseDigiTradeSwagger("DigiTrade Identity API v1");
app.MapIdentityEndpoints();
app.MapDigiTradeHealthEndpoints();

app.Run();

namespace Identity.Api
{
    public partial class Program
    {
    }
}
