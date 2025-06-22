using System;
using System.Threading.Tasks;

namespace HydroponicAppServer.MQTT
{
    public class MqttControlService
    {
        private readonly MQTTDeviceClient mqttClient;
        private readonly string controlTopic;

        public event Action<string>? OnCommandSent;
        public event Action<string>? OnError;
        public bool IsConnected { get; private set; } = false;

        public MqttControlService(string userId, string brokerAddress, int brokerPort, string clientId)
        {
            controlTopic = $"{userId}/Device";
            mqttClient = new MQTTDeviceClient(brokerAddress, brokerPort, controlTopic, clientId);
        }

        public async Task StartAsync(string? username = null, string? password = null)
        {
            try
            {
                await mqttClient.ConnectAsync(username, password);
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
                    await mqttClient.DisconnectAsync();
                    IsConnected = false;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"MQTT disconnect error: {ex.Message}");
            }
        }

        public async Task SendDeviceCommandAsync(string cmd, bool state)
        {
            if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");
            try
            {
                await mqttClient.SendCommandAsync(cmd, state ? 1 : 0);
                OnCommandSent?.Invoke($"{cmd}: {(state ? "ON" : "OFF")}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"MQTT send command error: {ex.Message}");
            }
        }
    }
}