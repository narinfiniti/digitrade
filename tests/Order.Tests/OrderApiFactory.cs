using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DigiTrade.Messaging.Contracts;
using Order.Api;
using Order.Persistence;

namespace Order.Tests;

public sealed class OrderApiFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-order-tests-{Guid.NewGuid():N}.db")}";

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
                ["ORDER_DB_CONNECTION"] = connectionString,
                ["ConnectionStrings:Order"] = connectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<OrderDbContext>>();
            services.RemoveAll<DbContextOptions<OrderDbContext>>();
            services.RemoveAll<OrderDbContext>();
            services.RemoveAll<IIntegrationEventPublisher>();

            services.AddDbContext<OrderDbContext>(options => options.UseSqlite(connectionString));
            services.AddSingleton<TestIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<TestIntegrationEventPublisher>());

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}