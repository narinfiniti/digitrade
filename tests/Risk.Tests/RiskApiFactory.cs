using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DigiTrade.Messaging.Contracts;
using Risk.Api;
using Risk.Persistence;

namespace Risk.Tests;

public sealed class RiskApiFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-risk-tests-{Guid.NewGuid():N}.db")}";

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
                ["RISK_DB_CONNECTION"] = connectionString,
                ["ConnectionStrings:Risk"] = connectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<RiskDbContext>>();
            services.RemoveAll<DbContextOptions<RiskDbContext>>();
            services.RemoveAll<RiskDbContext>();
            services.RemoveAll<IIntegrationEventPublisher>();

            services.AddDbContext<RiskDbContext>(options => options.UseSqlite(connectionString));
            services.AddSingleton<TestIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<TestIntegrationEventPublisher>());

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}