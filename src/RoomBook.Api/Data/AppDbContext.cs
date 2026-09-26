using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Entities;

namespace RoomBook.Api.Data;

/// <summary>
/// Схема соответствует ER-диаграмме: таблицы user, room, equipment,
/// room_equipment, booking, notification; колонки в snake_case;
/// перечисления (роль, статус, тип уведомления) хранятся строками.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Нужно для ограничения-исключения, запрещающего пересечение броней (см. миграцию).
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("user");
            e.Property(u => u.Email).HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(200);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<Equipment>(e =>
        {
            e.ToTable("equipment");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Code).HasMaxLength(32);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasData(
                new { Id = 1, Code = "PROJECTOR", Name = "Проектор" },
                new { Id = 2, Code = "WHITEBOARD", Name = "Доска" },
                new { Id = 3, Code = "MICROPHONE", Name = "Микрофон" },
                new { Id = 4, Code = "VIDEOCONF", Name = "Видеосвязь" },
                new { Id = 5, Code = "COMPUTER", Name = "Компьютер" },
                new { Id = 6, Code = "SPEAKERS", Name = "Колонки" });
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.ToTable("room");
            e.Property(r => r.Name).HasMaxLength(200);
            e.Property(r => r.Building).HasMaxLength(100);
            e.Property(r => r.Description).HasMaxLength(1000);
            e.HasMany(r => r.EquipmentItems)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "room_equipment",
                    right => right.HasOne<Equipment>().WithMany().HasForeignKey("equipment_id"),
                    left => left.HasOne<Room>().WithMany().HasForeignKey("room_id"),
                    join => join.HasKey("room_id", "equipment_id"));
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.ToTable("booking");
            e.Property(b => b.Purpose).HasMaxLength(500);
            e.Property(b => b.RejectReason).HasMaxLength(500);
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(b => b.DecidedById).HasColumnName("decided_by");

            e.HasOne(b => b.Room).WithMany().HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.DecidedBy).WithMany().HasForeignKey(b => b.DecidedById).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(b => new { b.RoomId, b.StartAt, b.EndAt });
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notification");
            e.Property(n => n.Type).HasConversion<string>().HasMaxLength(16);
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.Booking).WithMany().HasForeignKey(n => n.BookingId).OnDelete(DeleteBehavior.SetNull);
        });

        // Колонки в snake_case, как на ER-диаграмме (FullName → full_name, StartAt → start_at).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.GetColumnName() == property.Name)
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }
        }
    }

    private static string ToSnakeCase(string name) =>
        Regex.Replace(name, "(?<=[a-z0-9])([A-Z])", "_$1").ToLowerInvariant();
}
