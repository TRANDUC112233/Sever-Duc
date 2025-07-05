using Microsoft.EntityFrameworkCore;
using HydroponicAppServer.Models;

namespace HydroponicAppServer
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Garden> Gardens { get; set; }
        public DbSet<SensorData> SensorDatas { get; set; }
        public DbSet<DeviceAction> DeviceActions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đặt tên bảng cho rõ ràng
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Garden>().ToTable("Gardens");
            modelBuilder.Entity<SensorData>().ToTable("SensorDatas");
            modelBuilder.Entity<DeviceAction>().ToTable("DeviceActions");

            // User
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            // Garden: 1 user có nhiều garden
            modelBuilder.Entity<Garden>()
                .HasKey(g => g.Id);
            modelBuilder.Entity<Garden>()
                .HasOne(g => g.User)
                .WithMany(u => u.Gardens)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Chỉ Cascade ở đây

            // SensorData: 1 user có nhiều sensor data, 1 garden có nhiều sensor data
            modelBuilder.Entity<SensorData>()
                .HasKey(sd => sd.Id);
            modelBuilder.Entity<SensorData>()
                .HasOne(sd => sd.User)
                .WithMany(u => u.SensorDatas)
                .HasForeignKey(sd => sd.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SensorData>()
                .HasOne(sd => sd.Garden)
                .WithMany(g => g.SensorDatas)
                .HasForeignKey(sd => sd.GardenId)
                .OnDelete(DeleteBehavior.Restrict); // Sửa thành Restrict cho Garden

            // DeviceAction: 1 user có nhiều device action, 1 garden có nhiều device action
            modelBuilder.Entity<DeviceAction>()
                .HasKey(da => da.Id);
            modelBuilder.Entity<DeviceAction>()
                .HasOne(da => da.User)
                .WithMany(u => u.DeviceActions)
                .HasForeignKey(da => da.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DeviceAction>()
                .HasOne(da => da.Garden)
                .WithMany(g => g.DeviceActions)
                .HasForeignKey(da => da.GardenId)
                .OnDelete(DeleteBehavior.Restrict); // Sửa thành Restrict cho Garden
        }
    }
}