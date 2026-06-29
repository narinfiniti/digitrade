using BffNotificationService.Dependency;
using BffNotificationService.Api.Endpoints;
using BffNotificationService.Api.Mapping;
using BffNotificationService.Api.UseCases;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddOtelObservability();
builder.Services.AddDependencyServices(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(BffNotificationApiMappingProfile));
builder.Services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(ValidateTerminalCompletionQuery).Assembly));
builder.Services.AddDigiTradeSwagger(
	builder.Configuration,
	title: "DigiTrade BffNotification API",
	description: "Notification BFF endpoints exposed behind Kong API Gateway.");

var app = builder.Build();
app.UseDigiTradeSwagger("DigiTrade BffNotification API v1");
app.UseWebSockets();
app.MapNotificationEndpoints();
app.MapNotificationContractEndpoints();
app.MapDigiTradeHealthEndpoints();

app.Run();
