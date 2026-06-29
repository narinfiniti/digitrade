using BffAggregatorService.Dependency;
using BffAggregatorService.Api.Endpoints;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddDigiTradeSwagger(
	builder.Configuration,
	title: "DigiTrade BffAggregator API",
	description: "Read-side BFF endpoints exposed behind Kong API Gateway.");

var app = builder.Build();
app.UseDigiTradeSwagger("DigiTrade BffAggregator API v1");
app.MapServiceAggregationEndpoints();
app.MapTradingQueryEndpoints();
app.MapDigiTradeHealthEndpoints();

app.Run();
