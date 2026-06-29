extern alias OrderApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Order.Persistence;
using OrderProgram = OrderApi::Order.Api.Program;

namespace Risk.Tests;

public sealed class OrderContractApiFactory : WebApplicationFactory<OrderProgram>
{
    private readonly string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-order-contract-{Guid.NewGuid():N}.db")}";

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

            services.AddDbContext<OrderDbContext>(options => options.UseSqlite(connectionString));

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}