using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trade.Api.Contracts;
using Trade.Domain.Trades;
using Trade.Persistence;
using Xunit;

namespace Trade.Tests;

public sealed class TradeEndpointsTests(TradeApiFactory factory) : IClassFixture<TradeApiFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

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
    public async Task OpenTradePersistsTradeAndPublishesOutboxMessage()
    {
        using var client = factory.CreateClient();
        factory.ResetPublishedEnvelopes();

        var request = new OpenTradeInput(
            $"acct-{Guid.NewGuid():N}",
            "EURUSD",
            TradeDirection.Buy,
            1.25m,
            1.23456m);

        using var postResponse = await client.PostAsJsonAsync("/api/v1/trades", request);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        await using var postContent = await postResponse.Content.ReadAsStreamAsync();
        using var postDocument = await JsonDocument.ParseAsync(postContent);
        var createdTrade = postDocument.RootElement.GetProperty("data").Deserialize<TradeDto>(WebJsonOptions);

        Assert.NotNull(createdTrade);
        Assert.Equal(TradeStatus.Open, createdTrade!.Status);

        using var getResponse = await client.GetAsync($"/api/v1/trades/{createdTrade.TradeId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
        var outboxMessage = await dbContext.OutboxMessages.SingleAsync(message => message.AggregateId == createdTrade.TradeId.ToString("D"));

        Assert.Equal("trade.opened", outboxMessage.EventName);
        Assert.Equal(DigiTrade.Messaging.Persistence.Outbox.OutboxMessageStatus.Published, outboxMessage.Status);
        Assert.Contains(factory.PublishedEnvelopes, envelope => envelope.IntegrationEvent.EventId == outboxMessage.EventId);
    }
}