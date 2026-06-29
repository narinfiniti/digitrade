using DigiTrade.Common.Extensions;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.Security.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.UseCases;
using Identity.Domain.Users;
using Identity.Infrastructure.Options;
using Identity.Infrastructure.Security;
using Identity.Persistence;
using Identity.Persistence.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<IdentityTokenOptions>()
            .Bind(configuration.GetSection("Identity:Token"));

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                GetIdentityConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.DefaultSchema)));
        services.AddSingleton<IIdentityAuthenticationSettings>(sp => sp.GetRequiredService<IOptions<IdentityTokenOptions>>().Value);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<ITokenIssuer>(sp => sp.GetRequiredService<JwtTokenService>());
        services.AddSingleton<ITokenIntrospectionService>(sp => sp.GetRequiredService<JwtTokenService>());
        typeof(IDomainService).ApplyForTypesInAssembly(type => services.AddTransient(type), typeof(UserDomainService).Assembly);
        services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly));
        services.AddTransient<IValidator<RegisterUserCommand.Model>, RegisterUserCommandValidator>();
        services.AddTransient<IValidator<IssueAccessTokenCommand.Model>, IssueAccessTokenCommandValidator>();

        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetIdentityConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Identity");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["IDENTITY_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("IDENTITY_DB_CONNECTION or ConnectionStrings:Identity must be configured for Identity persistence.");
    }

    public static async Task<WebApplication> EnsureIdentityDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}