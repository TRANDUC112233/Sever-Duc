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
            var now = DateTime.UtcNow;
            // Tính thời gian đến slot tiếp theo (00 hoặc 30 phút)
            var nextSlot = now.Minute < 30
                ? new DateTime(now.Year, now.Month, now.Day, now.Hour, 30, 0)
                : new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);

            var delay = nextSlot - now;
            if (delay.TotalSeconds < 5) delay = delay.Add(TimeSpan.FromMinutes(30)); // Tránh chạy 2 lần sát nhau

            _logger.LogInformation($"Waiting {delay.TotalSeconds} seconds until next slot at {nextSlot:u}");
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break; // Stop gracefully
            }

            // Khi tới đúng slot
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var users = await db.Users.Select(u => u.Id).ToListAsync(stoppingToken);

                var toAdd = new System.Collections.Generic.List<SensorData>();
                foreach (var userId in users)
                {
                    // Lấy sensor mới nhất từ cache MQTT
                    var sensor = _mqttCache.GetLatestSensor(userId);
                    if (sensor == null) continue;

                    // SlotTime phải là nextSlot, chứ không lấy lại từ now
                    var slotTime = nextSlot;

                    // Nếu đã có bản ghi slot này rồi thì bỏ qua
                    var exists = await db.SensorDatas
                        .AnyAsync(sd => sd.UserId == userId && sd.Time != null && sd.Time.Value == slotTime, stoppingToken);

                    if (!exists)
                    {
                        // Lấy GardenId đang active của user (EndDate == null hoặc EndDate > slotTime)
                        var gardenId = await db.Gardens
                            .Where(g => g.UserId == userId && (g.EndDate == null || g.EndDate >= slotTime))
                            .OrderBy(g => g.StartDate)
                            .Select(g => g.Id)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (gardenId == 0) continue; // Không có garden active thì bỏ qua

                        // Xoá bản ghi cũ hơn 3 ngày
                        var threeDaysAgo = slotTime.AddDays(-3);
                        var oldRecords = await db.SensorDatas
                            .Where(sd => sd.UserId == userId && sd.Time != null && sd.Time < threeDaysAgo)
                            .ToListAsync(stoppingToken);
                        if (oldRecords.Any())
                        {
                            db.SensorDatas.RemoveRange(oldRecords);
                        }

                        // Thêm bản ghi mới
                        var newData = new SensorData
                        {
                            UserId = userId,
                            GardenId = gardenId,
                            Temperature = sensor.Temperature,
                            Humidity = sensor.Humidity,
                            WaterLevel = sensor.WaterLevel,
                            Time = slotTime
                        };
                        toAdd.Add(newData);

                        _logger.LogInformation($"[SensorData] User={userId} Garden={gardenId} at {slotTime:u} Temp={sensor.Temperature} Hum={sensor.Humidity} Water={sensor.WaterLevel}");
                    }
                }

                if (toAdd.Count > 0)
                {
                    db.SensorDatas.AddRange(toAdd);
                    try
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving SensorData to database.");
                    }
                }
            }
        }
        _logger.LogInformation("SensorDataTimedLogger stopped.");
    }
}

public interface IMqttSensorCache
{
    SensorData GetLatestSensor(string userId);
    void UpdateSensor(string userId, double? temp, double? hum, double? water);
}