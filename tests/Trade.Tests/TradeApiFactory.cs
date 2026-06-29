using DigiTrade.Messaging.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trade.Api;
using Trade.Persistence;

namespace Trade.Tests;

public sealed class TradeApiFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-trade-tests-{Guid.NewGuid():N}.db")}";

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
                ["TRADE_DB_CONNECTION"] = connectionString,
                ["ConnectionStrings:Trade"] = connectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<TradeDbContext>>();
            services.RemoveAll<DbContextOptions<TradeDbContext>>();
            services.RemoveAll<TradeDbContext>();
            services.RemoveAll<IIntegrationEventPublisher>();

            services.AddDbContext<TradeDbContext>(options => options.UseSqlite(connectionString));
            services.AddSingleton<TestIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<TestIntegrationEventPublisher>());

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}