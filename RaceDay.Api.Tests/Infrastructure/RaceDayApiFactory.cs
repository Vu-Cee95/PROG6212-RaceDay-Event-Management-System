using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Infrastructure;

// Hosts the real RaceDay API in memory for tests.
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

    // Runs an action against the test database (for asserting persisted state).
    public async Task<T> QueryDbAsync<T>(Func<RaceDayDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        return await query(db);
    }

    // Creates a client that keeps cookies, so the session survives between requests.
    public HttpClient CreateSessionClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
}
