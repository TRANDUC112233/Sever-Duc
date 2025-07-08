using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HydroponicAppServer.Models;

public class MqttSensorCache : IMqttSensorCache
{
    private readonly ConcurrentDictionary<string, SensorData> _cache = new();

    public SensorData GetLatestSensor(string userId)
    {
        if (_cache.TryGetValue(userId, out var data))
        {
            return data;
        }
        return null;
    }

    public void UpdateSensor(string userId, double? temp, double? hum, double? water)
    {
        // ✅ Gỡ bỏ log ra Console để tránh làm nặng server, giữ nguyên logic cập nhật cache
        _cache[userId] = new SensorData
        {
            UserId = userId,
            Temperature = temp,
            Humidity = hum,
            WaterLevel = water,
            Time = DateTime.UtcNow
        };
    }

    public IEnumerable<SensorData> GetAll()
    {
        return _cache.Values;
    }
}
