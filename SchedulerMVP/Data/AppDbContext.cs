using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Leaf> Leafs => Set<Leaf>();
    public DbSet<AreaLeaf> AreaLeafs => Set<AreaLeaf>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupType> GroupTypes => Set<GroupType>();
    public DbSet<ScheduleTemplate> ScheduleTemplates => Set<ScheduleTemplate>();
    public DbSet<BookingTemplate> BookingTemplates => Set<BookingTemplate>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<CalendarBooking> CalendarBookings => Set<CalendarBooking>();
    public DbSet<Modal> Modals => Set<Modal>();
    public DbSet<ModalReadBy> ModalReadBy => Set<ModalReadBy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CRITICAL FIX: Configure Guid properties to work with TEXT columns in PostgreSQL
        // This allows reading TEXT columns as Guid (for compatibility with SQLite migrations)
        // DateOnly and DateTime use native PostgreSQL types (DATE, TIMESTAMP) - no conversion needed
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Configure all Guid properties to use TEXT storage and string conversion
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                    {
                        property.SetColumnType("text");
                        // Use value converter to handle TEXT -> Guid conversion
                        if (property.ClrType == typeof(Guid))
                        {
                            property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Guid, string>(
                                v => v.ToString(),
                                v => Guid.Parse(v)));
                        }
                        else // Guid?
                        {
                            property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Guid?, string>(
                                v => v.HasValue ? v.Value.ToString() : null!,
                                v => string.IsNullOrEmpty(v) ? null : Guid.Parse(v)));
                        }
                    }
                }
            }
        }

        modelBuilder.Entity<AreaLeaf>()
            .HasKey(al => new { al.AreaId, al.LeafId });

        modelBuilder.Entity<Area>()
            .HasIndex(a => a.Path);

        modelBuilder.Entity<AreaLeaf>()
            .HasIndex(al => al.AreaId);
        modelBuilder.Entity<AreaLeaf>()
            .HasIndex(al => al.LeafId);

        modelBuilder.Entity<BookingTemplate>()
            .HasIndex(b => new { b.ScheduleTemplateId, b.DayOfWeek, b.StartMin });

        // Performance indexes for user filtering (UserId behålls för audit/spårning)
        modelBuilder.Entity<Group>()
            .HasIndex(g => g.UserId);
        
        modelBuilder.Entity<Place>()
            .HasIndex(p => p.UserId);
        
        modelBuilder.Entity<ScheduleTemplate>()
            .HasIndex(st => st.UserId);
        
        // Performance indexes for club filtering (ClubId används för access control/isolation)
        modelBuilder.Entity<Group>()
            .HasIndex(g => g.ClubId);
        
        modelBuilder.Entity<Place>()
            .HasIndex(p => p.ClubId);
        
        modelBuilder.Entity<ScheduleTemplate>()
            .HasIndex(st => st.ClubId);
        
        modelBuilder.Entity<GroupType>()
            .HasIndex(gt => gt.ClubId);

        // Performance indexes for CalendarBookings queries
        modelBuilder.Entity<CalendarBooking>()
            .HasIndex(cb => cb.Date);
        
        modelBuilder.Entity<CalendarBooking>()
            .HasIndex(cb => new { cb.Date, cb.AreaId });

        modelBuilder.Entity<Area>()
            .HasOne(a => a.ParentArea)
            .WithMany()
            .HasForeignKey(a => a.ParentAreaId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure Club relationships
        // Note: ApplicationUser is in ApplicationDbContext, so we can't configure relationship here
        // ClubId in ApplicationUser is just a Guid property without navigation/FK in EF Core
        
        modelBuilder.Entity<Place>()
            .HasOne(p => p.Club)
            .WithMany(c => c.Places)
            .HasForeignKey(p => p.ClubId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Club)
            .WithMany(c => c.Groups)
            .HasForeignKey(g => g.ClubId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<ScheduleTemplate>()
            .HasOne(st => st.Club)
            .WithMany(c => c.ScheduleTemplates)
            .HasForeignKey(st => st.ClubId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<GroupType>()
            .HasOne(gt => gt.Club)
            .WithMany(c => c.GroupTypes)
            .HasForeignKey(gt => gt.ClubId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure CalendarBooking.SourceTemplate to reference ScheduleTemplate
        // This matches the database constraint FK_CalendarBookings_ScheduleTemplates_SourceTemplateId
        modelBuilder.Entity<CalendarBooking>()
            .HasOne(cb => cb.SourceTemplate)
            .WithMany(st => st.CalendarBookings)
            .HasForeignKey(cb => cb.SourceTemplateId)
            .OnDelete(DeleteBehavior.SetNull); // Allow null when template is deleted

        // Configure ModalReadBy relationship
        modelBuilder.Entity<ModalReadBy>()
            .HasOne(mrb => mrb.Modal)
            .WithMany(m => m.ReadBy)
            .HasForeignKey(mrb => mrb.ModalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Explicitly configure table names for PostgreSQL (case-sensitive with quotes)
        // Must match exactly how tables were created in SQL script
        // DateOnly and DateTime properties use native PostgreSQL types (DATE, TIMESTAMP)
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            modelBuilder.Entity<Modal>()
                .ToTable("Modals", "public");
            
            modelBuilder.Entity<ModalReadBy>()
                .ToTable("ModalReadBy", "public");
        }

        // Performance indexes for Modals
        modelBuilder.Entity<Modal>()
            .HasIndex(m => new { m.StartDate, m.EndDate });

        // Performance index for ModalReadBy - critical for fast "has read" checks
        modelBuilder.Entity<ModalReadBy>()
            .HasIndex(mrb => new { mrb.ModalId, mrb.UserId });
    }
}


