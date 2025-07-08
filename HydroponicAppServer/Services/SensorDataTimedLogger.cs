using HydroponicAppServer;
using HydroponicAppServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class SensorDataTimedLogger : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IMqttSensorCache _mqttCache;
    private readonly ILogger<SensorDataTimedLogger> _logger;

    public SensorDataTimedLogger(IServiceProvider services, IMqttSensorCache mqttCache, ILogger<SensorDataTimedLogger> logger)
    {
        _services = services;
        _mqttCache = mqttCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SensorDataTimedLogger started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Tính thời điểm ghi log kế tiếp
            var now = DateTime.UtcNow;
            var nextSlot = now.Minute < 30
                ? new DateTime(now.Year, now.Month, now.Day, now.Hour, 30, 0, DateTimeKind.Utc)
                : new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);

            var delay = nextSlot - now;
            if (delay.TotalSeconds < 5)
            {
                delay = delay.Add(TimeSpan.FromMinutes(30));
            }

            _logger.LogInformation($"Waiting {delay.TotalSeconds:F1} seconds until next slot at {nextSlot:u}");

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var users = await db.Users
                .Select(u => new { u.Id, u.Username })
                .ToListAsync(stoppingToken);

            var toAdd = new System.Collections.Generic.List<SensorData>();

            foreach (var user in users)
            {
                var userId = user.Id;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Skipped user with empty ID.");
                    continue;
                }

                // Lấy dữ liệu cảm biến mới nhất từ cache
                var sensor = _mqttCache.GetLatestSensor(userId);
                if (sensor == null)
                {
                    _logger.LogWarning($"No cached sensor data for userId {userId}.");
                    continue;
                }

                // Kiểm tra dữ liệu đã tồn tại chưa
                bool exists = await db.SensorDatas
                    .AnyAsync(sd => sd.UserId == userId && sd.Time == nextSlot, stoppingToken);

                if (exists)
                {
                    _logger.LogInformation($"SensorData for userId {userId} at slot {nextSlot:u} already exists.");
                    continue;
                }

                // Kiểm tra vườn còn hoạt động
                var gardenId = await db.Gardens
                    .Where(g => g.UserId == userId && (g.EndDate == null || g.EndDate >= nextSlot))
                    .OrderBy(g => g.StartDate)
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync(stoppingToken);

                if (gardenId == 0)
                {
                    _logger.LogWarning($"User {userId} has no active garden at slotTime {nextSlot:u}.");
                    continue;
                }

                // Dọn dữ liệu cũ (trên 3 ngày)
                var threeDaysAgo = nextSlot.AddDays(-3);
                var oldRecords = await db.SensorDatas
                    .Where(sd => sd.UserId == userId && sd.Time < threeDaysAgo)
                    .ToListAsync(stoppingToken);

                if (oldRecords.Any())
                {
                    db.SensorDatas.RemoveRange(oldRecords);
                    _logger.LogInformation($"Removed {oldRecords.Count} old records for userId {userId}.");
                }

                // Tạo bản ghi mới
                var newData = new SensorData
                {
                    UserId = userId,
                    GardenId = gardenId,
                    Temperature = sensor.Temperature,
                    Humidity = sensor.Humidity,
                    WaterLevel = sensor.WaterLevel,
                    Time = nextSlot
                };

                toAdd.Add(newData);

                _logger.LogInformation(
                    $"[SensorData] User={userId}, Garden={gardenId}, Time={nextSlot:u}, Temp={sensor.Temperature}, Hum={sensor.Humidity}, Water={sensor.WaterLevel}"
                );
            }

            // Ghi dữ liệu mới
            if (toAdd.Count > 0)
            {
                db.SensorDatas.AddRange(toAdd);
                try
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Inserted {toAdd.Count} SensorData records.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving SensorData to database.");
                }
            }
            else
            {
                _logger.LogInformation("No new SensorData records to insert for this slot.");
            }
        }

        _logger.LogInformation("SensorDataTimedLogger stopped.");
    }
}
