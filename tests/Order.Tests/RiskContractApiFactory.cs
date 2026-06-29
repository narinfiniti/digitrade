extern alias RiskApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Risk.Persistence;
using RiskProgram = RiskApi::Risk.Api.Program;

namespace Order.Tests;

public sealed class RiskContractApiFactory : WebApplicationFactory<RiskProgram>
{
    private readonly string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-risk-contract-{Guid.NewGuid():N}.db")}";

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

            services.AddDbContext<RiskDbContext>(options => options.UseSqlite(connectionString));

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}