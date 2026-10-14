using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the real RaceDay API in memory for tests. The SQL Server DbContext is
/// replaced with the EF Core InMemory provider (a fresh database per factory) and
/// seeded with the same sample data as the SQL script, so tests exercise the
/// genuine controllers, filters, session handling and services end to end.
/// </summary>
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration added in Program.cs ...
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<RaceDayDbContext>))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // ... and replace it with an isolated in-memory database.
            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Database.EnsureCreated();
            DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordService>())
                    .GetAwaiter().GetResult();
        });
    }

    /// <summary>Runs an action against the test database (for asserting persisted state).</summary>
    public async Task<T> QueryDbAsync<T>(Func<RaceDayDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        return await query(db);
    }

    /// <summary>Creates a client that keeps cookies, so the session survives between requests.</summary>
    public HttpClient CreateSessionClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
}
