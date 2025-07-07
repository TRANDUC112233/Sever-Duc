using System;
using System.Threading.Tasks;

namespace HydroponicAppServer.MQTT
{
    public class MqttControlService
    {
        private readonly MQTTDeviceClient mqttClient;
        private readonly string controlTopic;
        private readonly string userId; // Thêm dòng này

        public event Action<string>? OnCommandSent;
        public event Action<string>? OnError;
        public bool IsConnected { get; private set; } = false;

        public MqttControlService(string userId, string brokerAddress, int brokerPort, string clientId)
        {
            this.userId = userId; // Lưu lại userId
            controlTopic = $"{userId}/Device";
            mqttClient = new MQTTDeviceClient(brokerAddress, brokerPort, clientId); // chỉ 3 tham số
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

        // Sửa lại: Bản tin luôn là { "cmd": ..., "value": "on"/"off" }
        public async Task SendDeviceCommandAsync(string cmd, bool state)
        {
            if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");
            try
            {
                await mqttClient.SendCommandAsync(userId, cmd, state); // truyền userId vào đầu
                OnCommandSent?.Invoke($"{cmd}: {(state ? "ON" : "OFF")}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"MQTT send command error: {ex.Message}");
            }
        }

        // Thêm hàm gửi schedule nếu cần
        public async Task SendScheduleAsync(string cmd, string value, string time, string status)
        {
            if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");
            try
            {
                await mqttClient.SendScheduleAsync(userId, cmd, value, time, status); // truyền userId vào đầu
                OnCommandSent?.Invoke($"{cmd}: {value} @ {time} [{status}]");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"MQTT send schedule error: {ex.Message}");
            }
        }

        // Thêm hàm gửi special action nếu cần
        public async Task SendSpecialActionAsync(string cmd, string action, string value, string status)
        {
            if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");
            try
            {
                await mqttClient.SendSpecialActionAsync(userId, cmd, action, value, status); // truyền userId vào đầu
                OnCommandSent?.Invoke($"{cmd}: {action} {value} [{status}]");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"MQTT send special action error: {ex.Message}");
            }
        }
    }
}