using MQTTnet;
using MQTTnet.Formatter;

namespace asp_net_core.MQTT
{
    public static class MQTT_client
    {
        private static readonly string brokerAddress = "mosquitto";
        private static MqttClientFactory mqttFactory = new ();
        public static async Task Clean_Disconnect()
        {

            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(brokerAddress).Build();
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            await mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());
        }
        public static async Task Connect()
        {

            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(brokerAddress).Build();

            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");

            // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
            // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
            var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

            await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
        }
        public static async Task Connect_Using_MQTTv5()
        {
            /*
             * This sample creates a simple MQTT client and connects to a public broker using MQTTv5.
             *
             * This is a modified version of the sample _Connect_Client_! See other sample for more details.
             */

            var mqttFactory = new MqttClientFactory();

            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(brokerAddress).WithProtocolVersion(MqttProtocolVersion.V500).Build();

            // In MQTTv5 the response contains much more information.
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");

        }

    }
}
