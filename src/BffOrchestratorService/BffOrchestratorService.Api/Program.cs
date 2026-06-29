using BffOrchestratorService.Dependency;
using BffOrchestratorService.Application.Mapping;
using BffOrchestratorService.Application.UseCases;
using BffOrchestratorService.Api.Endpoints;
using BffOrchestratorService.Domain.Observability;
using BffOrchestratorService.Persistence.Mapping;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability(metrics =>
{
    metrics.AddMeter(ProcessRuntimeOutboxMetrics.MeterName);
});
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(BffOrchestratorApplicationMappingProfile),
    typeof(BffOrchestratorPersistenceMappingProfile));
builder.Services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(GetBusinessProcessCatalogQuery).Assembly));
builder.Services.AddDigiTradeSwagger(
    builder.Configuration,
    title: "DigiTrade BffOrchestrator API",
    description: "Write-side orchestration BFF endpoints exposed behind Kong API Gateway.");

var app = builder.Build();
app.UseDigiTradeSwagger("DigiTrade BffOrchestrator API v1");
app.MapOrchestrationShellEndpoints();
app.MapBusinessProcessEndpoints();
app.MapBusinessCommandEndpoints();
app.MapDigiTradeHealthEndpoints();

app.Run();
