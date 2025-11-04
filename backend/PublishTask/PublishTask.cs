using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using servicebusapi2.Models;
using System.Text.Json;

namespace servicebusapi2.Publish
{
    public class PublishTask
    {
        private readonly string _connectionString;
        private readonly string _topicName;
        private readonly ILogger<PublishTask> _logger;
        private readonly ServiceBusLogContext _logContext;

        private const string EventMessageSent = "MessageSent";
        private const string EventPublishError = "PublishError";

        public PublishTask(
            string connectionString,
            string topicName,
            ILogger<PublishTask> logger,
            ServiceBusLogContext logContext)
        {
            _connectionString = connectionString;
            _topicName = topicName;
            _logger = logger;
            _logContext = logContext;
        }

        public async Task PublishSingleMessageAsync(Details details)
        {
            var correlationId = details?.sequenceNumber;

            try
            {
                string payload = JsonSerializer.Serialize(details);
                await using var client = new ServiceBusClient(_connectionString);
                var sender = client.CreateSender(_topicName);
                await sender.SendMessageAsync(new ServiceBusMessage(payload));

                _logger.LogInformation("Message published to Service Bus topic {Topic}.", _topicName);

                _logContext.ServiceBusLogs.Add(new ServiceBusLog
                {
                    EventType = EventMessageSent,
                    Severity = "Success",
                    Message = "Message published successfully"
                });
                await _logContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to topic {Topic}.", _topicName);

                _logContext.ServiceBusLogs.Add(new ServiceBusLog
                {
                    EventType = EventPublishError,
                    Severity = "Error",
                    Message = $"Publish failed: {ex.Message}"
                });
                await _logContext.SaveChangesAsync();
                throw;
            }
        }
    }
}
