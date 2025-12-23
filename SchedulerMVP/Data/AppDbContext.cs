using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CRITICAL FIX: Configure Guid properties to work with TEXT columns in PostgreSQL
        // This allows reading TEXT columns as Guid (for compatibility with SQLite migrations)
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

        // Performance indexes for user filtering
        modelBuilder.Entity<Group>()
            .HasIndex(g => g.UserId);
        
        modelBuilder.Entity<Place>()
            .HasIndex(p => p.UserId);
        
        modelBuilder.Entity<ScheduleTemplate>()
            .HasIndex(st => st.UserId);

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

        // Configure CalendarBooking.SourceTemplate to reference ScheduleTemplate
        // This matches the database constraint FK_CalendarBookings_ScheduleTemplates_SourceTemplateId
        modelBuilder.Entity<CalendarBooking>()
            .HasOne(cb => cb.SourceTemplate)
            .WithMany(st => st.CalendarBookings)
            .HasForeignKey(cb => cb.SourceTemplateId)
            .OnDelete(DeleteBehavior.SetNull); // Allow null when template is deleted
    }
}


