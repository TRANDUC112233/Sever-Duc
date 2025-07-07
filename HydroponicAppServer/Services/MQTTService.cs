using System;
using System.Threading.Tasks;

namespace HydroponicAppServer.MQTT
{
    public class MQTTService
    {
        private readonly MQTTDeviceClient client;

        // Event mới: nhận userId
        public event Action<string, double, double, int>? OnSensorDataReceived;
        public event Action<string>? OnError;
        public bool IsConnected { get; private set; } = false;

        // Không cần truyền userId vào đây nữa, vì sẽ lắng nghe tất cả userId/Sensor
        public MQTTService(string brokerAddress, int brokerPort, string clientId)
        {
            // MQTTDeviceClient chỉ dùng topic để publish control, sub sẽ là '+/Sensor' bên trong client
            client = new MQTTDeviceClient(brokerAddress, brokerPort, clientId);

            // Gắn event nhận dữ liệu sensor từ mọi userId
            client.OnSensorDataReceived += (userIdFromTopic, temp, hum, waterPercent) =>
            {
                OnSensorDataReceived?.Invoke(userIdFromTopic, temp, hum, waterPercent);
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
