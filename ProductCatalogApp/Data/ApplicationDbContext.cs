using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Models;

namespace ProductCatalogApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProductionSite> ProductionSites =>
     Set<ProductionSite>();

        public DbSet<Equipment> EquipmentItems =>
            Set<Equipment>();
        public DbSet<AppUser> Users { get; set; } = null!;

        public DbSet<Role> Roles { get; set; } = null!;

        public DbSet<Sensor> Sensors => Set<Sensor>();

        public DbSet<SensorReading> SensorReadings =>
            Set<SensorReading>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sensor>()
                .HasOne(sensor => sensor.Equipment)
                .WithMany()
                .HasForeignKey(sensor => sensor.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SensorReading>()
                .HasOne(reading => reading.Sensor)
                .WithMany(sensor => sensor.Readings)
                .HasForeignKey(reading => reading.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SensorReading>()
                .HasIndex(reading => new { reading.SensorId, reading.Timestamp });

            modelBuilder.Entity<SensorReading>()
                .HasIndex(reading => new { reading.IsAlarm, reading.IsAcknowledged });

            modelBuilder.Entity<AppUser>()
                .HasIndex(user => user.Username)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(role => role.Name)
                .IsUnique();

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    Name = "User"
                },
                new Role
                {
                    Id = 2,
                    Name = "Admin"
                }
            );
        }
    }
}