using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using HydroponicAppServer.MQTT;

public class MqttListenerService : BackgroundService
{
    private readonly IMqttSensorCache _cache;
    private readonly ILogger<MqttListenerService> _logger;
    private MQTTDeviceClient _mqttClient;

    public MqttListenerService(IMqttSensorCache cache, ILogger<MqttListenerService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqttClient = new MQTTDeviceClient("broker.emqx.io", 8883, "ServerClient");

        _mqttClient.OnSensorDataReceived += (userId, temp, hum, water) =>
        {
            _cache.UpdateSensor(userId, temp, hum, water);
#if DEBUG
        // Chỉ log khi đang chạy debug để tránh làm nặng console khi chạy production
        _logger.LogInformation($"[MQTT] Cached sensor data for userId={userId}: Temp={temp}, Hum={hum}, Water={water}");
#endif
        };

        try
        {
            await _mqttClient.ConnectAsync();
            _logger.LogInformation("Connected to MQTT broker.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MQTT broker.");
        }
    }
}

