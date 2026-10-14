using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data;

/// <summary>
/// Seeds the database with the same realistic sample data as the Part 1 SQL
/// script (2 Organisers, 2 Participants, 3 Events, 6 Categories, 4 Enrolments,
/// 2 Results). Unlike the script, passwords are stored as proper hashes.
/// All seeded accounts use the password <c>Password123!</c>.
/// Seeding is idempotent: it does nothing when users already exist.
/// </summary>
public static class DbSeeder
{
    public const string SeedPassword = "Password123!";

    public static async Task SeedAsync(RaceDayDbContext db, IPasswordService passwords)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        // EventTypes are inserted by the migration (HasData). Make sure they exist when
        // the database was created with EnsureCreated (e.g. the in-memory test database).
        if (!await db.EventTypes.AnyAsync())
        {
            db.EventTypes.AddRange(
                new EventType { EventTypeId = 1, TypeName = "Run" },
                new EventType { EventTypeId = 2, TypeName = "Walk" },
                new EventType { EventTypeId = 3, TypeName = "Cycle" });
            await db.SaveChangesAsync();
        }

        var registered = new DateTime(2026, 1, 15, 9, 0, 0);

        // Hash per user so every account gets its own random salt (as real registrations do).
        var thabo = new User { Email = "thabo.mokoena@raceday.co.za", PasswordHash = passwords.Hash(SeedPassword), FullName = "Thabo Mokoena", Role = UserRole.Organiser, DateRegistered = registered };
        var sarah = new User { Email = "sarah.vandermerwe@raceday.co.za", PasswordHash = passwords.Hash(SeedPassword), FullName = "Sarah van der Merwe", Role = UserRole.Organiser, DateRegistered = registered };
        var lwando = new User { Email = "lwando.dlamini@gmail.com", PasswordHash = passwords.Hash(SeedPassword), FullName = "Lwando Dlamini", Role = UserRole.Participant, DateRegistered = registered };
        var priya = new User { Email = "priya.naidoo@gmail.com", PasswordHash = passwords.Hash(SeedPassword), FullName = "Priya Naidoo", Role = UserRole.Participant, DateRegistered = registered };

        db.Users.AddRange(thabo, sarah, lwando, priya);
        await db.SaveChangesAsync();

        var soweto = new Event
        {
            Organiser = thabo, EventTypeId = 1, Name = "Soweto Marathon",
            Description = "The ultimate road running challenge in Johannesburg.",
            EventDate = new DateTime(2026, 11, 1, 6, 0, 0), Location = "Soweto, Johannesburg",
            Distance = 42.20m, CreatedDate = registered
        };
        var cycleTour = new Event
        {
            Organiser = thabo, EventTypeId = 3, Name = "Cape Town Cycle Tour",
            Description = "The world's largest timed cycle race.",
            EventDate = new DateTime(2026, 3, 8, 6, 15, 0), Location = "Cape Town, Western Cape",
            Distance = 109.00m, CreatedDate = registered
        };
        var parkRun = new Event
        {
            Organiser = sarah, EventTypeId = 2, Name = "Durban Park Run",
            Description = "A community 5km walk and run.",
            EventDate = new DateTime(2026, 4, 18, 7, 0, 0), Location = "Durban, KwaZulu-Natal",
            Distance = 5.00m, CreatedDate = registered
        };

        db.Events.AddRange(soweto, cycleTour, parkRun);
        await db.SaveChangesAsync();

        var under20 = new Category { Event = soweto, CategoryName = "Under 20", Description = "Junior runners under the age of 20" };
        var senior = new Category { Event = soweto, CategoryName = "Senior", Description = "Open category for all ages over 20" };
        var elite = new Category { Event = cycleTour, CategoryName = "Elite", Description = "Professional and experienced cyclists" };
        var amateur = new Category { Event = cycleTour, CategoryName = "Amateur", Description = "Recreational cyclists" };
        var walkers = new Category { Event = parkRun, CategoryName = "Walkers", Description = "Participants walking the 5km route" };
        var runners = new Category { Event = parkRun, CategoryName = "Runners", Description = "Participants running the 5km route" };

        db.Categories.AddRange(under20, senior, elite, amateur, walkers, runners);
        await db.SaveChangesAsync();

        var enrolled = new DateTime(2026, 2, 1, 10, 0, 0);
        var e1 = new Enrolment { Participant = lwando, Event = soweto, Category = senior, Status = EnrolmentStatus.Confirmed, EnrolmentDate = enrolled };
        var e2 = new Enrolment { Participant = priya, Event = cycleTour, Category = amateur, Status = EnrolmentStatus.Pending, EnrolmentDate = enrolled };
        var e3 = new Enrolment { Participant = lwando, Event = parkRun, Category = walkers, Status = EnrolmentStatus.Confirmed, EnrolmentDate = enrolled };
        var e4 = new Enrolment { Participant = priya, Event = soweto, Category = under20, Status = EnrolmentStatus.Confirmed, EnrolmentDate = enrolled };

        db.Enrolments.AddRange(e1, e2, e3, e4);
        await db.SaveChangesAsync();

        db.Results.AddRange(
            new Result { Enrolment = e1, FinishTime = new TimeSpan(3, 45, 22), FinishingPosition = 120, Notes = "Personal Best" },
            new Result { Enrolment = e3, FinishTime = new TimeSpan(0, 28, 45), FinishingPosition = 45 });
        await db.SaveChangesAsync();
    }
}
