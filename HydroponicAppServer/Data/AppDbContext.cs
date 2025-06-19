using Microsoft.EntityFrameworkCore;
using HydroponicAppServer.Models;

namespace HydroponicAppServer
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<SensorData> SensorDatas { get; set; }
        public DbSet<DeviceAction> DeviceActions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đặt tên bảng theo số nhiều nếu muốn rõ ràng
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<SensorData>().ToTable("SensorDatas");
            modelBuilder.Entity<DeviceAction>().ToTable("DeviceActions");

            // User
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            // SensorData: 1 user có nhiều sensor data
            modelBuilder.Entity<SensorData>()
                .HasKey(sd => sd.Id);
            modelBuilder.Entity<SensorData>()
                .HasOne(sd => sd.User)
                .WithMany(u => u.SensorDatas)
                .HasForeignKey(sd => sd.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // DeviceAction: 1 user có nhiều device action
            modelBuilder.Entity<DeviceAction>()
                .HasKey(da => da.Id);
            modelBuilder.Entity<DeviceAction>()
                .HasOne(da => da.User)
                .WithMany(u => u.DeviceActions)
                .HasForeignKey(da => da.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}