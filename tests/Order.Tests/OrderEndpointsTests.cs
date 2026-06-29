using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.Api.Contracts;
using Order.Domain.Orders;
using Order.Persistence;
using Xunit;

namespace Order.Tests;

public sealed class OrderEndpointsTests(OrderApiFactory factory) : IClassFixture<OrderApiFactory>
{
    [Fact]
    public async Task HealthEndpointsReturnOk()
    {
        using var client = factory.CreateClient();

        using var liveResponse = await client.GetAsync("/health/live");
        using var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
    }

    [Fact]
    public async Task PlaceAcceptAndGetOrderRoundTripsThroughApi()
    {
        using var client = factory.CreateClient();
        factory.ResetPublishedEnvelopes();

        var request = new PlaceOrderInput(
            $"acct-{Guid.NewGuid():N}",
            "EURUSD",
            OrderDirection.Buy,
            1.5m,
            1.23456m);

        using var placeResponse = await client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.Created, placeResponse.StatusCode);

        var createdOrder = await placeResponse.Content.ReadFromJsonAsync<OrderDto>();

        Assert.NotNull(createdOrder);
        Assert.Equal(OrderStatus.PendingRiskApproval, createdOrder!.Status);

        using var acceptResponse = await client.PostAsync($"/api/v1/orders/{createdOrder.OrderId}/accept", content: null);

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var acceptedOrder = await acceptResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(acceptedOrder);
        Assert.Equal(OrderStatus.Accepted, acceptedOrder!.Status);

        using var getResponse = await client.GetAsync($"/api/v1/orders/{createdOrder.OrderId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedOrder = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        Assert.NotNull(fetchedOrder);
        Assert.Equal(OrderStatus.Accepted, fetchedOrder!.Status);
        Assert.Equal(createdOrder.OrderId, fetchedOrder.OrderId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var outboxMessages = (await dbContext.OutboxMessages
                .Where(message => message.AggregateId == createdOrder.OrderId.ToString("D"))
                .ToListAsync())
            .OrderBy(message => message.OccurredAtUtc)
            .ToList();

        Assert.Collection(
            outboxMessages,
            message => Assert.Equal("order.placed", message.EventName),
            message => Assert.Equal("order.accepted", message.EventName));

        Assert.All(outboxMessages, message => Assert.Equal(DigiTrade.Messaging.Persistence.Outbox.OutboxMessageStatus.Published, message.Status));
        Assert.Equal(2, factory.PublishedEnvelopes.Count);
    }

    [Fact]
    public async Task AcceptingOrderTwiceReturnsConflictAndDoesNotPublishDuplicateEvent()
    {
        using var client = factory.CreateClient();
        factory.ResetPublishedEnvelopes();

        using var placeResponse = await client.PostAsJsonAsync(
            "/api/v1/orders",
            new PlaceOrderInput(
                $"acct-{Guid.NewGuid():N}",
                "EURUSD",
                OrderDirection.Buy,
                1.25m,
                1.23456m));

        Assert.Equal(HttpStatusCode.Created, placeResponse.StatusCode);

        var createdOrder = await placeResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(createdOrder);

        using var firstAcceptResponse = await client.PostAsync($"/api/v1/orders/{createdOrder!.OrderId}/accept", content: null);
        Assert.Equal(HttpStatusCode.OK, firstAcceptResponse.StatusCode);

        using var duplicateAcceptResponse = await client.PostAsync($"/api/v1/orders/{createdOrder.OrderId}/accept", content: null);
        Assert.Equal(HttpStatusCode.Conflict, duplicateAcceptResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var outboxMessageCount = await dbContext.OutboxMessages
            .CountAsync(message => message.AggregateId == createdOrder.OrderId.ToString("D"));

        Assert.Equal(2, outboxMessageCount);
        Assert.Equal(2, factory.PublishedEnvelopes.Count);
    }
}