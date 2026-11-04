using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Data.Entities;

namespace ShareMaster.Server.Data;

public class ShareMasterDbContext(DbContextOptions<ShareMasterDbContext> options) : DbContext(options)
{
    public DbSet<Calendar> Calendars => Set<Calendar>();
    public DbSet<CalendarManager> CalendarManagers => Set<CalendarManager>();
    public DbSet<CalendarDay> CalendarDays => Set<CalendarDay>();
    public DbSet<DayType> DayTypes => Set<DayType>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<SystemAdmin> SystemAdmins => Set<SystemAdmin>();
    public DbSet<ChangeHistory> ChangeHistories => Set<ChangeHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite（Docker なしの簡易起動・テスト用）は DateTimeOffset の並べ替えに対応しないため数値に変換する
        if (Database.IsSqlite())
        {
            configurationBuilder.Properties<DateTimeOffset>()
                .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();
        }
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Calendar>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.WeeklyHolidays).HasMaxLength(20);
            e.Property(x => x.CreatedBy).HasMaxLength(256);
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<CalendarManager>(e =>
        {
            e.Property(x => x.UserKey).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.HasIndex(x => new { x.CalendarId, x.UserKey }).IsUnique();
            e.HasOne(x => x.Calendar).WithMany(x => x.Managers).HasForeignKey(x => x.CalendarId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<DayType>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Color).HasMaxLength(9).IsRequired();
        });
        b.Entity<CalendarDay>(e =>
        {
            e.HasIndex(x => new { x.CalendarId, x.Date }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.CreatedBy).HasMaxLength(256);
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasOne(x => x.Calendar).WithMany(x => x.Days).HasForeignKey(x => x.CalendarId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DayType).WithMany().HasForeignKey(x => x.DayTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PublicHoliday>(e =>
        {
            e.HasIndex(x => x.Date).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });
        b.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.UserKey);
            e.Property(x => x.UserKey).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(256);
        });
        b.Entity<SystemAdmin>(e =>
        {
            e.Property(x => x.UserKey).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.UserKey).IsUnique();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.CreatedBy).HasMaxLength(256);
        });
        b.Entity<ChangeHistory>(e =>
        {
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.EntityKey).HasMaxLength(100);
            e.Property(x => x.Action).HasMaxLength(20);
            e.Property(x => x.Summary).HasMaxLength(1000);
            e.Property(x => x.ChangedBy).HasMaxLength(256);
            e.Property(x => x.ChangedByName).HasMaxLength(200);
            e.HasIndex(x => new { x.CalendarId, x.ChangedAt });
            e.HasIndex(x => x.ChangedAt);
        });
        b.Entity<Notification>(e =>
        {
            e.Property(x => x.UserKey).HasMaxLength(256).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Message).HasMaxLength(2000);
            e.Property(x => x.LinkUrl).HasMaxLength(500);
            e.HasIndex(x => new { x.UserKey, x.IsRead, x.CreatedAt });
        });
    }
}
