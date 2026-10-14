using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Contracts;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Database (EF Core Code-First, SQL Server) ----------------
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------- Session management ----------------
// The server-side session stores the logged-in user's Id and Role. The client only
// receives an opaque session cookie, which it sends back on every request.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});
builder.Services.AddHttpContextAccessor();

// ---------------- Application services ----------------
builder.Services.AddScoped<ICurrentUserService, SessionCurrentUserService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ---------------- MVC ----------------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Return validation failures in the same ApiError shape used by the services.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new ApiError
            {
                Message = "One or more validation errors occurred.",
                Errors = errors
            });
        };
    });

// ---------------- Swagger / OpenAPI ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "RESTful API for the RaceDay event management system (PROG6212 PoE Part 2).\n\n" +
            "**How to test protected endpoints:** call `POST /api/auth/login` first. The response sets a " +
            "`.RaceDay.Session` cookie which Swagger UI sends automatically on every following request. " +
            "Call `POST /api/auth/logout` to end the session.\n\n" +
            "**Roles:** *Organiser* manages events, categories, enrolments and results. " +
            "*Participant* enters events and views their own enrolments and results.\n\n" +
            "**Seeded accounts** (password `Password123!`): thabo.mokoena@raceday.co.za (Organiser), " +
            "sarah.vandermerwe@raceday.co.za (Organiser), lwando.dlamini@gmail.com (Participant), " +
            "priya.naidoo@gmail.com (Participant)."
    });

    options.EnableAnnotations();

    // Include the XML documentation comments written on controllers and DTOs.
    foreach (var xmlFile in new[] { $"{Assembly.GetExecutingAssembly().GetName().Name}.xml", "RaceDay.Contracts.xml" })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
});

var app = builder.Build();

// ---------------- Database migration + seed ----------------
// Only when running against SQL Server. The test project swaps in an in-memory
// provider and seeds its own data, so this block is skipped there.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    if (db.Database.IsSqlServer())
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordService>());
    }
}

// ---------------- HTTP pipeline ----------------
// Swagger is enabled in every environment so the marker can open it directly.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    options.DocumentTitle = "RaceDay API - Swagger UI";
    options.DisplayRequestDuration();
});

app.UseHttpsRedirection();

app.UseSession();

app.MapControllers();

app.Run();

/// <summary>Marker type so the test project can host the API with WebApplicationFactory.</summary>
public partial class Program
{
}
