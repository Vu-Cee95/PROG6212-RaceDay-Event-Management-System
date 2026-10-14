using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// EF Core (Code-First) DbContext for RaceDay.
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- 1. Users ----------------
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", t =>
                t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));

            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).UseIdentityColumn();

            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(u => u.DateRegistered)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");
        });

        // ---------------- 2. EventTypes ----------------
        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventTypes");
            entity.HasKey(t => t.EventTypeId);
            entity.Property(t => t.EventTypeId).UseIdentityColumn();
            entity.Property(t => t.TypeName).HasMaxLength(50).IsRequired();
            entity.HasIndex(t => t.TypeName).IsUnique();

            // Static lookup data, identical to the Part 1 script seed.
            entity.HasData(
                new EventType { EventTypeId = 1, TypeName = "Run" },
                new EventType { EventTypeId = 2, TypeName = "Walk" },
                new EventType { EventTypeId = 3, TypeName = "Cycle" });
        });

        // ---------------- 3. Events ----------------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).UseIdentityColumn();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.EventDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Distance).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(e => e.BannerImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedDate)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.EventType)
                  .WithMany(t => t.Events)
                  .HasForeignKey(e => e.EventTypeId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ---------------- 4. Categories ----------------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.CategoryId);
            entity.Property(c => c.CategoryId).UseIdentityColumn();
            entity.Property(c => c.CategoryName).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(100);

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ---------------- 5. Enrolments ----------------
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(en => en.EnrolmentId);
            entity.Property(en => en.EnrolmentId).UseIdentityColumn();
            entity.Property(en => en.EnrolmentDate)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");
            entity.Property(en => en.Status)
                  .HasMaxLength(20)
                  .IsRequired()
                  .HasDefaultValue(EnrolmentStatus.Pending);

            entity.HasOne(en => en.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(en => en.ParticipantId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(en => en.Event)
                  .WithMany(e => e.Enrolments)
                  .HasForeignKey(en => en.EventId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ---------------- 6. Results ----------------
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(r => r.ResultId);
            entity.Property(r => r.ResultId).UseIdentityColumn();
            entity.Property(r => r.FinishTime).HasColumnType("time(0)");
            entity.Property(r => r.Notes).HasMaxLength(255);

            // UNIQUE EnrolmentId => one Result per Enrolment (1:1).
            entity.HasIndex(r => r.EnrolmentId).IsUnique();
            entity.HasOne(r => r.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
