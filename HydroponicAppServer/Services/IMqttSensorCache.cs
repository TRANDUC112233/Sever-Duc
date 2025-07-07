using System.Collections.Generic;
using HydroponicAppServer.Models;

public interface IMqttSensorCache
{
    SensorData GetLatestSensor(string userId);
    void UpdateSensor(string userId, double? temp, double? hum, double? water);
    IEnumerable<SensorData> GetAll();
}