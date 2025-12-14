using MQTTnet;
using MQTTnet.Formatter;
using System.Text;

namespace asp_net_core.Services
{
    public class MQTT_Service : IDisposable
    {
        private static readonly string brokerAddress = "mosquitto";
        private MqttClientFactory mqttFactory = new ();
        private IMqttClient? _mqttClient;
        private readonly ILogger<MQTT_Service> _logger;

        // Event fired when desk move message is received
        public event Action<int>? OnDeskMoveReceived;

        // Event fired when connection status changes
        public event Action<bool>? OnConnectionStatusChanged;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;
        public async Task ConnectAsync(string broker, int port, string? username = null, string? password = null)
        {
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithClientId(Guid.NewGuid().ToString())
                .WithCleanSession();

            if (!string.IsNullOrEmpty(username))
            {
                options.WithCredentials(username, password);
            }

            _mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("Connected to MQTT broker");
                OnConnectionStatusChanged?.Invoke(true);
                await SubscribeToTopicsAsync();
            };

            _mqttClient.DisconnectedAsync += e =>
            {
                _logger.LogWarning("Disconnected from MQTT broker");
                OnConnectionStatusChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            await _mqttClient.ConnectAsync(options.Build());
        }
        private async Task SubscribeToTopicsAsync()
        {
            if (_mqttClient == null || !_mqttClient.IsConnected) return;

            // Subscribe to desk move topic
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic("online")
                .WithTopic("tables/+/setHeight")
                .Build());

            _logger.LogInformation("Subscribed to desk/move topic");
        }


        private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            _logger.LogInformation($"Received message on topic '{topic}': {payload}");
            Console.WriteLine(payload);

            try
            {
                var topicParts = topic.Split('/');
                if (topicParts[0] == "tables" && topicParts[2] == "setHeight")
                {
                    if (int.TryParse(payload, out int height))
                    {
                        // Fire event with lambda subscribers
                        OnDeskMoveReceived?.Invoke(height);
                    }
                }
                else if (topic == "online")
                {
                    _logger.LogInformation(payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling MQTT message");
            }

            return Task.CompletedTask;
        }


        public async Task PublishAsync(string topic, string payload)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                _logger.LogWarning("Cannot publish: MQTT client not connected");
                return;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
        }

        public MQTT_Service(ILogger<MQTT_Service> logger)
        {
            _logger = logger;
        }
        public async Task Disconnect()
        {

            _mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(brokerAddress).Build();
            await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());
        }
        //public async Task Connect()
        //{

        //    using var mqttClient = mqttFactory.CreateMqttClient();
        //    var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(brokerAddress).Build();

        //    var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        //    Console.WriteLine("The MQTT client is connected.");

        //    // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
        //    // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
        //    var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

        //    await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
        //}
        //public async Task Connect_Using_MQTTv5()
        //{
        //    /*
        //     * This sample creates a simple MQTT client and connects to a public broker using MQTTv5.
        //     *
        //     * This is a modified version of the sample _Connect_Client_! See other sample for more details.
        //     */

        //    var mqttFactory = new MqttClientFactory();

        //    using var mqttClient = mqttFactory.CreateMqttClient();
        //    var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(brokerAddress).WithProtocolVersion(MqttProtocolVersion.V500).Build();

        //    // In MQTTv5 the response contains much more information.
        //    var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        //    Console.WriteLine("The MQTT client is connected.");

        //}

        public void Dispose()
        {
            _mqttClient.Dispose();
        }
    }
}
