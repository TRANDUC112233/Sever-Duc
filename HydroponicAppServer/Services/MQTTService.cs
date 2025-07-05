using System;
using System.Threading.Tasks;

namespace HydroponicAppServer.MQTT
{
    public class MQTTService
    {
        private readonly MQTTDeviceClient client;

        // Field waterlevel đổi thành waterPercent (kiểu int)
        public event Action<double, double, int>? OnSensorDataReceived;
        public event Action<string>? OnError;
        public bool IsConnected { get; private set; } = false;

        public MQTTService(string userId, string brokerAddress, int brokerPort, string clientId)
        {
            string topic = $"{userId}/Sensor";
            client = new MQTTDeviceClient(brokerAddress, brokerPort, topic, clientId);

            client.OnSensorDataReceived += (temp, hum, waterPercent) =>
            {
                OnSensorDataReceived?.Invoke(temp, hum, waterPercent);
            };
        }

        public async Task StartAsync(string? username = null, string? password = null)
        {
            try
            {
                await client.ConnectAsync(username, password);
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnError?.Invoke($"MQTT connect error: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (IsConnected)
                {
                    await client.DisconnectAsync();
                    IsConnected = false;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"MQTT disconnect error: {ex.Message}");
            }
        }
    }
}