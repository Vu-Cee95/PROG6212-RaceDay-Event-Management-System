using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Contracts;

var builder = WebApplication.CreateBuilder(args);

// SQL Server database (Code-First)
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session stores the logged-in user's Id and Role. The browser just keeps a cookie.
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

builder.Services.AddScoped<ICurrentUserService, SessionCurrentUserService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEnrolmentService, EnrolmentService>();
builder.Services.AddScoped<IResultService, ResultService>();

// /health - used by CI and to check the API is up
builder.Services.AddHealthChecks();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Validation errors use the same ApiError JSON as the rest of the API.
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

    // Swagger text comes from [SwaggerOperation] on each action.
    options.EnableAnnotations();
});

var app = builder.Build();

// Create/update the SQL Server database and add sample data on startup.
// Tests use an in-memory database so this is skipped there.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    if (db.Database.IsSqlServer())
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordService>());
    }
}

// If something unexpected goes wrong, return a simple JSON error (no stack trace).
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new ApiError
    {
        Message = "An unexpected error occurred. Please try again later."
    });
}));

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
app.MapHealthChecks("/health");

app.Run();

// Needed so the test project can host this API in memory.
public partial class Program
{
}
