using MQTTnet;
using MQTTnet.Protocol;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Humanizer;

namespace asp_net_core.Services
{
    public class MQTT_Service : IDisposable
    {
        private static readonly string brokerAddress = "mosquitto";
        private MqttClientFactory mqttFactory = new();
        private IMqttClient? _mqttClient;
        private readonly ILogger<MQTT_Service> _logger;

        // Event fired when desk move message is received
        public event Action<string, int>? OnDeskMoveReceived;

        public event Action<string>? OnPicoConnected;

        // Event fired when connection status changes
        public event Action<bool>? OnConnectionStatusChanged;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public MQTT_Service(ILogger<MQTT_Service> logger)
        {
            _logger = logger;
        }

        public async Task ConnectAsync(string broker, int port, string? username = null, string? password = null)
        {
            try
            {
                _mqttClient = mqttFactory.CreateMqttClient();

                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(broker, port)
                    .WithClientId($"AspNetCore_{Guid.NewGuid()}")
                    .WithCleanSession()
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .WithTimeout(TimeSpan.FromSeconds(10));

                if (!string.IsNullOrEmpty(username))
                {
                    optionsBuilder.WithCredentials(username, password);
                }

                _mqttClient.ConnectedAsync += async e =>
                {
                    _logger.LogInformation("Connected to MQTT broker at {Broker}:{Port}", broker, port);
                    OnConnectionStatusChanged?.Invoke(true);
                    await SubscribeToTopicsAsync();
                };

                _mqttClient.DisconnectedAsync += e =>
                {
                    _logger.LogWarning("Disconnected from MQTT broker. Reason: {Reason}", e.Reason);
                    OnConnectionStatusChanged?.Invoke(false);
                    return Task.CompletedTask;
                };

                _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

                var options = optionsBuilder.Build();
                var result = await _mqttClient.ConnectAsync(options);

                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    _logger.LogInformation("MQTT connection successful");
                }
                else
                {
                    _logger.LogError("MQTT connection failed with result: {ResultCode}", result.ResultCode);
                    throw new Exception($"MQTT connection failed: {result.ResultCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to MQTT broker at {Broker}:{Port}", broker, port);
                throw;
            }
        }

        private async Task SubscribeToTopicsAsync()
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                _logger.LogWarning("Cannot subscribe: MQTT client not connected");
                return;
            }

            try
            {
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic("/online").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                    .WithTopicFilter(f => f.WithTopic("/tables/+/setHeight").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                    .Build();

                var result = await _mqttClient.SubscribeAsync(subscribeOptions);

                _logger.LogInformation("Subscribed to topics: online, tables/+/setHeight");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to topics");
            }
        }

        private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            _logger.LogInformation("Received message on topic '{Topic}': {Payload}", topic, payload);

            try
            {
                var topicParts = topic.Split('/');
                if (topicParts.Length == 4 && topicParts[1] == "tables" && topicParts[3] == "setHeight")
                {
                    if (int.TryParse(payload, out int height))
                    {
                        _logger.LogInformation("Desk height change received: {Height} for desk {1}", height, topicParts[2]);
                        OnDeskMoveReceived?.Invoke(topicParts[2], height);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid height value: {Payload}", payload);
                    }
                }
                else if (topic == "/online")
                {
                    _logger.LogInformation("Online message: {Payload}", payload);
                    OnPicoConnected?.Invoke(payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MQTT message from topic '{Topic}'", topic);
            }

            return Task.CompletedTask;
        }

        public async Task PublishAsync(string topic, string payload)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                _logger.LogWarning("Cannot publish: MQTT client not connected");
                throw new InvalidOperationException("MQTT client is not connected");
            }

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.PublishAsync(message);
                _logger.LogInformation("Published message to topic '{Topic}': {Payload}", topic, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing to topic '{Topic}'", topic);
                throw;
            }
        }

        public async Task Disconnect()
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
                        .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                        .Build();

                    await _mqttClient.DisconnectAsync(disconnectOptions);
                    _logger.LogInformation("Disconnected from MQTT broker");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from MQTT broker");
                }
            }
        }

        public void Dispose()
        {
            if (_mqttClient != null)
            {
                if (_mqttClient.IsConnected)
                {
                    _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
                }
                _mqttClient.Dispose();
            }
        }
    }
}
