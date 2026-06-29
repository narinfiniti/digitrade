using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Settlement.Api.Contracts;
using Settlement.Application.Events;
using Settlement.Domain.Settlements;
using Settlement.Persistence;
using Xunit;

namespace Settlement.Tests;

public sealed class SettlementEndpointsTests(SettlementApiFactory factory) : IClassFixture<SettlementApiFactory>
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
    public async Task InitiateFinalizeAndGetSettlementRoundTripsThroughApi()
    {
        using var client = factory.CreateClient();
        factory.ResetPublishedEnvelopes();

        var request = new InitiateSettlementInput(
            Guid.NewGuid(),
            $"acct-{Guid.NewGuid():N}",
            "USD",
            125.40m);

        using var initiateResponse = await client.PostAsJsonAsync("/api/v1/settlements", request);

        Assert.Equal(HttpStatusCode.Created, initiateResponse.StatusCode);

        var createdSettlement = await initiateResponse.Content.ReadFromJsonAsync<SettlementDto>();

        Assert.NotNull(createdSettlement);
        Assert.Equal(SettlementStatus.PendingFinalization, createdSettlement!.Status);

        using var finalizeResponse = await client.PostAsync($"/api/v1/settlements/{createdSettlement.SettlementId}/finalize", content: null);

        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);

        var finalizedSettlement = await finalizeResponse.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(finalizedSettlement);
        Assert.Equal(SettlementStatus.Finalized, finalizedSettlement!.Status);

        using var getResponse = await client.GetAsync($"/api/v1/settlements/{createdSettlement.SettlementId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedSettlement = await getResponse.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(fetchedSettlement);
        Assert.Equal(SettlementStatus.Finalized, fetchedSettlement!.Status);
        Assert.Equal(createdSettlement.SettlementId, fetchedSettlement.SettlementId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SettlementDbContext>();
        var outboxMessages = (await dbContext.OutboxMessages
                .Where(message => message.AggregateId == createdSettlement.SettlementId.ToString("D"))
                .ToListAsync())
            .OrderBy(message => message.OccurredAtUtc)
            .ToList();

        Assert.Collection(
            outboxMessages,
            message => Assert.Equal("settlement.initiated", message.EventName),
            message => Assert.Equal("settlement.finalized", message.EventName));

        Assert.All(outboxMessages, message => Assert.Equal(DigiTrade.Messaging.Persistence.Outbox.OutboxMessageStatus.Published, message.Status));
        Assert.Equal(2, factory.PublishedEnvelopes.Count);

        var finalizedEnvelope = Assert.Single(factory.PublishedEnvelopes.Where(envelope => envelope.IntegrationEvent.EventName == SettlementFinalizedIntegrationEvent.IntegrationEventName));
        var finalizedIntegrationEvent = Assert.IsType<SettlementFinalizedIntegrationEvent>(finalizedEnvelope.IntegrationEvent);
        Assert.Equal(createdSettlement.SettlementId.ToString("D"), finalizedIntegrationEvent.AggregateId);
        Assert.Equal(request.TradeId.ToString("D"), finalizedIntegrationEvent.TradeId);
        Assert.Equal(request.AccountId, finalizedIntegrationEvent.AccountId);
        Assert.Equal(request.CurrencyCode, finalizedIntegrationEvent.CurrencyCode);
        Assert.Equal(request.NetAmount, finalizedIntegrationEvent.NetAmount);
    }

    [Fact]
    public async Task InitiateFailAndGetSettlementRoundTripsThroughApi()
    {
        using var client = factory.CreateClient();
        factory.ResetPublishedEnvelopes();

        var request = new InitiateSettlementInput(
            Guid.NewGuid(),
            $"acct-{Guid.NewGuid():N}",
            "EUR",
            -42.75m);

        using var initiateResponse = await client.PostAsJsonAsync("/api/v1/settlements", request);

        Assert.Equal(HttpStatusCode.Created, initiateResponse.StatusCode);

        var createdSettlement = await initiateResponse.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(createdSettlement);

        using var failResponse = await client.PostAsJsonAsync(
            $"/api/v1/settlements/{createdSettlement!.SettlementId}/fail",
            new FailSettlementInput("provider rejected finalization"));

        Assert.Equal(HttpStatusCode.OK, failResponse.StatusCode);

        var failedSettlement = await failResponse.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(failedSettlement);
        Assert.Equal(SettlementStatus.Failed, failedSettlement!.Status);
        Assert.Equal("provider rejected finalization", failedSettlement.FailureReason);

        using var getResponse = await client.GetAsync($"/api/v1/settlements/{createdSettlement.SettlementId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedSettlement = await getResponse.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(fetchedSettlement);
        Assert.Equal(SettlementStatus.Failed, fetchedSettlement!.Status);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SettlementDbContext>();
        var outboxMessages = (await dbContext.OutboxMessages
                .Where(message => message.AggregateId == createdSettlement.SettlementId.ToString("D"))
                .ToListAsync())
            .OrderBy(message => message.OccurredAtUtc)
            .ToList();

        Assert.Collection(
            outboxMessages,
            message => Assert.Equal("settlement.initiated", message.EventName),
            message => Assert.Equal("settlement.failed", message.EventName));

        Assert.All(outboxMessages, message => Assert.Equal(DigiTrade.Messaging.Persistence.Outbox.OutboxMessageStatus.Published, message.Status));
        Assert.Equal(2, factory.PublishedEnvelopes.Count);
    }
}