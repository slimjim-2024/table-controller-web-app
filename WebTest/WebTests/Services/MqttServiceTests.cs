using asp_net_core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace WebTests.Services
{
    public class MqttServiceTests
    {
        private readonly Mock<ILogger<MQTT_Service>> _mockLogger;

        public MqttServiceTests()
        {
            _mockLogger = new Mock<ILogger<MQTT_Service>>();
        }

        [Fact]
        public void MqttService_InitializesCorrectly()
        {
            var service = new MQTT_Service(_mockLogger.Object);
            Assert.NotNull(service);
            Assert.False(service.IsConnected);
        }

        [Fact]
        public void MqttService_IsConnected_ReturnsFalse_WhenNotConnected()
        {
            var service = new MQTT_Service(_mockLogger.Object);
            var isConnected = service.IsConnected;
            Assert.False(isConnected);
        }
    }
}