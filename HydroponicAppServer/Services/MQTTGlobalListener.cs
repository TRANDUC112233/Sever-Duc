using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using HydroponicAppServer.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace HydroponicAppServer.Services
{
    public class MQTTGlobalListener : BackgroundService
    {
        private readonly AppDbContext _dbContext;
        private IMqttClient _mqttClient;
        private static ConcurrentDictionary<string, SensorData> _latestData = new();

        public MQTTGlobalListener(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected override async Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.emqx.io", 8883)
                .WithTls()
                .Build();

            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var parts = topic.Split('/');
                if (parts.Length != 2 || parts[1] != "Sensor") return Task.CompletedTask;
                var userId = parts[0];

                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                try
                {
                    var json = JsonDocument.Parse(payload);
                    double temp = json.RootElement.GetProperty("Temp").GetDouble();
                    double hum = json.RootElement.GetProperty("humidity").GetDouble();
                    double water = json.RootElement.TryGetProperty("waterlevel", out var waterProp) ? waterProp.GetDouble() : 0.0;

                    var data = new SensorData
                    {
                        UserId = userId,
                        Temperature = temp,
                        Humidity = hum,
                        WaterLevel = water,
                        Time = DateTime.UtcNow
                    };
                    _latestData[userId] = data; // Cache bản tin mới nhất
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MQTT] Parse error: {ex.Message}");
                }

                return Task.CompletedTask;
            });

            await _mqttClient.ConnectAsync(options, stoppingToken);
            await _mqttClient.SubscribeAsync("+/Sensor");
            Console.WriteLine("[MQTT] Listening for all UserId/Sensor topics...");

            // Task lưu DB vào các mốc giờ tròn 30 phút
            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;
                    int minutesToNextHalfHour = 30 - (now.Minute % 30);
                    if (minutesToNextHalfHour == 30) minutesToNextHalfHour = 0;
                    var nextRun = now.AddMinutes(minutesToNextHalfHour).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);

                    var delay = nextRun - now;
                    if (delay.TotalMilliseconds > 0)
                        await Task.Delay(delay, stoppingToken);

                    foreach (var kv in _latestData)
                    {
                        try
                        {
                            var copy = new SensorData
                            {
                                UserId = kv.Value.UserId,
                                Temperature = kv.Value.Temperature,
                                Humidity = kv.Value.Humidity,
                                WaterLevel = kv.Value.WaterLevel,
                                Time = DateTime.UtcNow
                            };
                            _dbContext.SensorDatas.Add(copy);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DB] Write error: {ex.Message}");
                        }
                    }
                    try
                    {
                        await _dbContext.SaveChangesAsync(stoppingToken);
                        Console.WriteLine($"[DB] Saved data at {DateTime.UtcNow:HH:mm}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DB] SaveChanges error: {ex.Message}");
                    }
                }
            }, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
