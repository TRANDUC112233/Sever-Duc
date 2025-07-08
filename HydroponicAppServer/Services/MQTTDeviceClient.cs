using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HydroponicAppServer.MQTT
{
    public class MQTTDeviceClient
    {
        private TcpClient client;
        private SslStream sslStream;
        private string clientId;
        private string topic;
        private string broker;
        private int port;

        public event Action<string, double, double, int>? OnSensorDataReceived;

        public MQTTDeviceClient(string brokerAddress, int brokerPort, string clientId)
        {
            this.broker = brokerAddress;
            this.port = brokerPort;
            this.clientId = clientId;
            this.topic = null;
        }

        public async Task ConnectAsync(string username = null, string password = null)
        {
            client = new TcpClient();
            await client.ConnectAsync(broker, port);

            var networkStream = client.GetStream();
            sslStream = new SslStream(networkStream, false, (sender, cert, chain, error) => true);
            await sslStream.AuthenticateAsClientAsync(broker);

            var connectPacket = Packet.Connect(clientId, username, password, 60);
            await SendPacketAsync(connectPacket);
            await ReadResponseAsync(); // CONNACK

            var subscribePacket = Packet.Subscribe("+/Sensor", 0);
            await SendPacketAsync(subscribePacket);
            await ReadResponseAsync(); // SUBACK

            _ = Task.Run(() => ListenWithReconnectLoop());
        }

        public async Task DisconnectAsync()
        {
            var disconnectPacket = Packet.Disconnect();
            await SendPacketAsync(disconnectPacket);

            sslStream?.Close();
            client?.Close();
        }

        private async Task SendPacketAsync(Packet packet)
        {
            var data = packet.ToBytes();
            await sslStream.WriteAsync(data, 0, data.Length);
            await sslStream.FlushAsync();
        }

        private async Task ReadResponseAsync()
        {
            byte[] header = new byte[2];
            await ReadExactAsync(sslStream, header, 0, 2);
            int length = header[1];
            byte[] payload = new byte[length];
            await ReadExactAsync(sslStream, payload, 0, length);
        }

        private async Task ReadExactAsync(SslStream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    throw new Exception("Stream closed unexpectedly.");
                totalRead += bytesRead;
            }
        }

        private async Task ListenWithReconnectLoop()
        {
            while (true)
            {
                try
                {
                    await ListenForMessagesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MQTT] Listener crashed: {ex.Message}");
                }

                // Khi lỗi → đợi vài giây rồi reconnect lại
                await Task.Delay(5000);
                try
                {
                    Console.WriteLine("[MQTT] Attempting to reconnect...");
                    await ConnectAsync();
                    Console.WriteLine("[MQTT] Reconnected.");
                    return;
                }
                catch (Exception reconEx)
                {
                    Console.WriteLine($"[MQTT] Reconnect failed: {reconEx.Message}");
                    // Tiếp tục vòng lặp để thử lại
                }
            }
        }

        private async Task ListenForMessagesAsync()
        {
            while (true)
            {
                byte[] fixedHeader = new byte[2];
                await ReadExactAsync(sslStream, fixedHeader, 0, 2);

                byte packetType = (byte)(fixedHeader[0] >> 4);
                int remainingLength = fixedHeader[1];

                byte[] payload = new byte[remainingLength];
                await ReadExactAsync(sslStream, payload, 0, remainingLength);

                if (packetType == 3) // PUBLISH
                {
                    int topicLength = (payload[0] << 8) + payload[1];
                    string topicReceived = Encoding.UTF8.GetString(payload, 2, topicLength);
                    int messageStartIndex = 2 + topicLength;
                    int messageLength = remainingLength - messageStartIndex;

                    string messagePayload = Encoding.UTF8.GetString(payload, messageStartIndex, messageLength);

                    if (!string.IsNullOrWhiteSpace(messagePayload) && messagePayload.Contains("{"))
                    {
                        try
                        {
                            var json = JsonDocument.Parse(messagePayload);

                            double temp = json.RootElement.TryGetProperty("Temp", out var tempProp)
                                ? tempProp.GetDouble() : 0.0;
                            double hum = json.RootElement.TryGetProperty("humidity", out var humProp)
                                ? humProp.GetDouble() : 0.0;
                            int water = json.RootElement.TryGetProperty("water_percent", out var waterProp)
                                ? waterProp.GetInt32() : 0;

                            string userId = topicReceived.Split('/')[0];

                            OnSensorDataReceived?.Invoke(userId, temp, hum, water);
                        }
                        catch
                        {
                            // Ignore malformed payload
                        }
                    }
                }
            }
        }

        public async Task SendCommandAsync(string userId, string cmd, bool state)
        {
            var command = new { cmd = cmd, value = state ? "on" : "off" };
            string json = JsonSerializer.Serialize(command);
            string controlTopic = $"{userId}/Device";
            var publishPacket = Packet.Publish(controlTopic, Encoding.UTF8.GetBytes(json), 0, false);
            await SendPacketAsync(publishPacket);
        }

        public async Task SendScheduleAsync(string userId, string cmd, string value, string time, string status)
        {
            var schedule = new { cmd, value, time, action = "schedule", status };
            string json = JsonSerializer.Serialize(schedule);
            string controlTopic = $"{userId}/Device";
            var publishPacket = Packet.Publish(controlTopic, Encoding.UTF8.GetBytes(json), 0, false);
            await SendPacketAsync(publishPacket);
        }

        public async Task SendSpecialActionAsync(string userId, string cmd, string action, string value, string status)
        {
            var msg = new { cmd, action, value, status };
            string json = JsonSerializer.Serialize(msg);
            string controlTopic = $"{userId}/Device";
            var publishPacket = Packet.Publish(controlTopic, Encoding.UTF8.GetBytes(json), 0, false);
            await SendPacketAsync(publishPacket);
        }
    }
}
