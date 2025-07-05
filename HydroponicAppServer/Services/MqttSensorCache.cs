using System.Collections.Concurrent;
using HydroponicAppServer.Models;

public class MqttSensorCache : IMqttSensorCache
{
    private readonly ConcurrentDictionary<string, SensorData> _cache = new();

    public SensorData GetLatestSensor(string userId)
        => _cache.TryGetValue(userId, out var data) ? data : null;

    public void UpdateSensor(string userId, double? temp, double? hum, double? water)
    {
        _cache[userId] = new SensorData
        {
            UserId = userId,
            Temperature = temp,
            Humidity = hum,
            WaterLevel = water,
            Time = DateTime.UtcNow
        };
    }
}