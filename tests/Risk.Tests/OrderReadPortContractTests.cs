using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Risk.Application.Abstractions;
using Risk.Application.Models;
using Risk.Infrastructure.Clients;
using Xunit;

namespace Risk.Tests;

public sealed class OrderReadPortContractTests(OrderContractApiFactory orderFactory)
    : IClassFixture<OrderContractApiFactory>
{
    [Fact]
    public async Task RiskDependencyResolvesOrderReadPortAgainstOrderApi()
    {
        using var orderClient = orderFactory.CreateClient();

        using var placeResponse = await orderClient.PostAsJsonAsync(
            "/api/v1/orders",
            new
            {
                accountId = $"acct-{Guid.NewGuid():N}",
                instrumentId = "EURUSD",
                direction = 1,
                quantity = 1.25m,
                requestedPrice = 1.23456m,
            });

        placeResponse.EnsureSuccessStatusCode();

        var createdOrder = await placeResponse.Content.ReadFromJsonAsync<OrderCreatedResponse>();
        Assert.NotNull(createdOrder);

        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RISK_DB_CONNECTION"] = "Host=localhost;Database=digitrade;Username=digitrade;Password=digitrade",
                ["Services:Order:BaseUrl"] = orderClient.BaseAddress!.ToString(),
            })
            .Build();

        Risk.Dependency.DependencyExtensions.AddDependencyServices(services, configuration);

        using var provider = services.BuildServiceProvider();
        var resolvedPort = provider.GetRequiredService<IOrderReadPort>();
        Assert.NotNull(resolvedPort);

        var port = new OrderReadClient(orderClient);
        var snapshot = await port.GetByIdAsync(createdOrder!.OrderId);

        Assert.NotNull(snapshot);
        Assert.Equal(createdOrder.OrderId, snapshot!.OrderId);
        Assert.Equal(createdOrder.AccountId, snapshot.AccountId);
        Assert.Equal(createdOrder.InstrumentId, snapshot.InstrumentId);
        Assert.Equal(ExternalOrderDirection.Buy, snapshot.Direction);
        Assert.Equal(ExternalOrderStatus.PendingRiskApproval, snapshot.Status);
    }

    private sealed record OrderCreatedResponse(
        Guid OrderId,
        string AccountId,
        string InstrumentId);
}