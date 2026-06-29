using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Abstractions;
using Order.Application.Models;
using Order.Infrastructure.Clients;
using Xunit;

namespace Order.Tests;

public sealed class RiskMarginAccountReadPortContractTests(RiskContractApiFactory riskFactory)
    : IClassFixture<RiskContractApiFactory>
{
    [Fact]
    public async Task OrderDependencyResolvesMarginAccountReadPortAgainstRiskApi()
    {
        using var riskClient = riskFactory.CreateClient();

        using var openResponse = await riskClient.PostAsJsonAsync(
            "/api/v1/margin-accounts",
            new
            {
                accountId = $"acct-{Guid.NewGuid():N}",
                currencyCode = "USD",
                totalMargin = 1000m,
            });

        openResponse.EnsureSuccessStatusCode();

        var createdMarginAccount = await openResponse.Content.ReadFromJsonAsync<MarginAccountCreatedResponse>();
        Assert.NotNull(createdMarginAccount);

        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ORDER_DB_CONNECTION"] = "Host=localhost;Database=digitrade;Username=digitrade;Password=digitrade",
                ["Services:Risk:BaseUrl"] = riskClient.BaseAddress!.ToString(),
            })
            .Build();

        Order.Dependency.DependencyExtensions.AddDependencyServices(services, configuration);

        using var provider = services.BuildServiceProvider();
        var resolvedPort = provider.GetRequiredService<IMarginAccountReadPort>();
        Assert.NotNull(resolvedPort);

        var port = new RiskMarginAccountReadClient(riskClient);
        var snapshot = await port.GetByIdAsync(createdMarginAccount!.MarginAccountId);

        Assert.NotNull(snapshot);
        Assert.Equivalent(
            new MarginAccountSnapshotModel(
                createdMarginAccount.MarginAccountId,
                createdMarginAccount.AccountId,
                createdMarginAccount.CurrencyCode,
                createdMarginAccount.TotalMargin,
                createdMarginAccount.ReservedMargin,
                snapshot!.Version,
                snapshot.CreatedAt,
                snapshot.UpdatedAt),
            snapshot,
            strict: false);
    }

    private sealed record MarginAccountCreatedResponse(
        Guid MarginAccountId,
        string AccountId,
        string CurrencyCode,
        decimal TotalMargin,
        decimal ReservedMargin);
}