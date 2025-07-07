using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HydroponicAppServer.Models;

// KHÔNG khai báo lại interface ở đây, chỉ sử dụng thôi!
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
        Console.WriteLine($"[MqttSensorCache] UpdateSensor for userId={userId}, Temp={temp}, Hum={hum}, Water={water} at {DateTime.UtcNow:u}");

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