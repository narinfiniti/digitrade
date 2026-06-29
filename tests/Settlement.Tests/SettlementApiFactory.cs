using DigiTrade.Messaging.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Settlement.Api;
using Settlement.Persistence;

namespace Settlement.Tests;

public sealed class SettlementApiFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-settlement-tests-{Guid.NewGuid():N}.db")}";

    public IReadOnlyCollection<IntegrationEnvelope> PublishedEnvelopes
        => Services.GetRequiredService<TestIntegrationEventPublisher>().PublishedEnvelopes;

    public void ResetPublishedEnvelopes()
    {
        Services.GetRequiredService<TestIntegrationEventPublisher>().Clear();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SETTLEMENT_DB_CONNECTION"] = connectionString,
                ["ConnectionStrings:Settlement"] = connectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<SettlementDbContext>>();
            services.RemoveAll<DbContextOptions<SettlementDbContext>>();
            services.RemoveAll<SettlementDbContext>();
            services.RemoveAll<IIntegrationEventPublisher>();

            services.AddDbContext<SettlementDbContext>(options => options.UseSqlite(connectionString));
            services.AddSingleton<TestIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<TestIntegrationEventPublisher>());

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<SettlementDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}