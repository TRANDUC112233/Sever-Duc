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

// KHÔNG khai báo lại interface ở đây, chỉ sử dụng thôi!
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
            var nextSlot = now.Minute < 30
                ? new DateTime(now.Year, now.Month, now.Day, now.Hour, 30, 0)
                : new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);

            var delay = nextSlot - now;
            if (delay.TotalSeconds < 5) delay = delay.Add(TimeSpan.FromMinutes(30));

            _logger.LogInformation($"Waiting {delay.TotalSeconds} seconds until next slot at {nextSlot:u}");
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var users = await db.Users
                    .Select(u => new { u.Id, u.Username })
                    .ToListAsync(stoppingToken);

                var toAdd = new System.Collections.Generic.List<SensorData>();
                foreach (var user in users)
                {
                    var username = user.Username;
                    if (string.IsNullOrEmpty(username))
                    {
                        _logger.LogWarning($"User id {user.Id} not found username.");
                        continue;
                    }

                    var sensor = _mqttCache.GetLatestSensor(username);
                    if (sensor == null)
                    {
                        _logger.LogWarning($"No cached sensor data for user {username}.");
                        continue;
                    }

                    var slotTime = nextSlot;
                    var exists = await db.SensorDatas
                        .AnyAsync(sd => sd.UserId == username && sd.Time != null && sd.Time.Value == slotTime, stoppingToken);
                    if (!exists)
                    {
                        var gardenId = await db.Gardens
                            .Where(g => g.UserId == username && (g.EndDate == null || g.EndDate >= slotTime))
                            .OrderBy(g => g.StartDate)
                            .Select(g => g.Id)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (gardenId == 0)
                        {
                            _logger.LogWarning($"User {username} has no active garden at slotTime {slotTime:u}.");
                            continue;
                        }

                        var threeDaysAgo = slotTime.AddDays(-3);
                        var oldRecords = await db.SensorDatas
                            .Where(sd => sd.UserId == username && sd.Time != null && sd.Time < threeDaysAgo)
                            .ToListAsync(stoppingToken);
                        if (oldRecords.Any())
                        {
                            db.SensorDatas.RemoveRange(oldRecords);
                        }

                        var newData = new SensorData
                        {
                            UserId = username,
                            GardenId = gardenId,
                            Temperature = sensor.Temperature,
                            Humidity = sensor.Humidity,
                            WaterLevel = sensor.WaterLevel,
                            Time = slotTime
                        };
                        toAdd.Add(newData);

                        _logger.LogInformation($"[SensorData] User={username} Garden={gardenId} at {slotTime:u} Temp={sensor.Temperature} Hum={sensor.Humidity} Water={sensor.WaterLevel}");
                    }
                    else
                    {
                        _logger.LogInformation($"SensorData for user {username} at slot {slotTime:u} already exists.");
                    }
                }

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
        }
        _logger.LogInformation("SensorDataTimedLogger stopped.");
    }
}