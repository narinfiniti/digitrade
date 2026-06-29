using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Risk.Api.Contracts;
using Risk.Persistence;
using Xunit;

namespace Risk.Tests;

public sealed class RiskEndpointsTests(RiskApiFactory factory) : IClassFixture<RiskApiFactory>
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
    public async Task OpenReserveReleaseAndGetMarginAccountRoundTripsThroughApi()
    {
        using var client = factory.CreateClient();
        factory.ResetPublishedEnvelopes();

        var request = new OpenMarginAccountInput(
            $"acct-{Guid.NewGuid():N}",
            "USD",
            1000m);

        using var openResponse = await client.PostAsJsonAsync("/api/v1/margin-accounts", request);

        Assert.Equal(HttpStatusCode.Created, openResponse.StatusCode);

        var openedAccount = await openResponse.Content.ReadFromJsonAsync<MarginAccountDto>();

        Assert.NotNull(openedAccount);
        Assert.Equal(0m, openedAccount!.ReservedMargin);

        using var reserveResponse = await client.PostAsJsonAsync($"/api/v1/margin-accounts/{openedAccount.MarginAccountId}/reserve", new AdjustMarginInput(250m));

        Assert.Equal(HttpStatusCode.OK, reserveResponse.StatusCode);

        var reservedAccount = await reserveResponse.Content.ReadFromJsonAsync<MarginAccountDto>();
        Assert.NotNull(reservedAccount);
        Assert.Equal(250m, reservedAccount!.ReservedMargin);

        using var releaseResponse = await client.PostAsJsonAsync($"/api/v1/margin-accounts/{openedAccount.MarginAccountId}/release", new AdjustMarginInput(100m));

        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        var releasedAccount = await releaseResponse.Content.ReadFromJsonAsync<MarginAccountDto>();
        Assert.NotNull(releasedAccount);
        Assert.Equal(150m, releasedAccount!.ReservedMargin);

        using var getResponse = await client.GetAsync($"/api/v1/margin-accounts/{openedAccount.MarginAccountId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedAccount = await getResponse.Content.ReadFromJsonAsync<MarginAccountDto>();
        Assert.NotNull(fetchedAccount);
        Assert.Equal(150m, fetchedAccount!.ReservedMargin);
        Assert.Equal(openedAccount.MarginAccountId, fetchedAccount.MarginAccountId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
        var outboxMessages = (await dbContext.OutboxMessages
                .Where(message => message.AggregateId == openedAccount.MarginAccountId.ToString("D"))
                .ToListAsync())
            .OrderBy(message => message.OccurredAtUtc)
            .ToList();

        Assert.Collection(
            outboxMessages,
            message => Assert.Equal("risk.margin-account.opened", message.EventName),
            message => Assert.Equal("risk.margin.reserved", message.EventName),
            message => Assert.Equal("risk.margin.released", message.EventName));

        Assert.All(outboxMessages, message => Assert.Equal(DigiTrade.Messaging.Persistence.Outbox.OutboxMessageStatus.Published, message.Status));
        Assert.Equal(3, factory.PublishedEnvelopes.Count);
    }

    [Fact]
    public async Task ReservingBeyondAvailableMarginReturnsConflictAndDoesNotPublishDuplicateEvent()
    {
        using var client = factory.CreateClient();
        factory.ResetPublishedEnvelopes();

        using var openResponse = await client.PostAsJsonAsync(
            "/api/v1/margin-accounts",
            new OpenMarginAccountInput(
                $"acct-{Guid.NewGuid():N}",
                "USD",
                1000m));

        Assert.Equal(HttpStatusCode.Created, openResponse.StatusCode);

        var openedAccount = await openResponse.Content.ReadFromJsonAsync<MarginAccountDto>();
        Assert.NotNull(openedAccount);

        using var reserveResponse = await client.PostAsJsonAsync(
            $"/api/v1/margin-accounts/{openedAccount!.MarginAccountId}/reserve",
            new AdjustMarginInput(250m));

        Assert.Equal(HttpStatusCode.OK, reserveResponse.StatusCode);

        using var invalidReserveResponse = await client.PostAsJsonAsync(
            $"/api/v1/margin-accounts/{openedAccount.MarginAccountId}/reserve",
            new AdjustMarginInput(800m));

        Assert.Equal(HttpStatusCode.Conflict, invalidReserveResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
        var outboxMessageCount = await dbContext.OutboxMessages
            .CountAsync(message => message.AggregateId == openedAccount.MarginAccountId.ToString("D"));

        Assert.Equal(2, outboxMessageCount);
        Assert.Equal(2, factory.PublishedEnvelopes.Count);
    }
}