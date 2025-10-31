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

        modelBuilder.Entity<Area>()
            .HasOne(a => a.ParentArea)
            .WithMany()
            .HasForeignKey(a => a.ParentAreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


