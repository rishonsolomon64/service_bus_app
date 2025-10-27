using System.Threading;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using servicebusapi2.Models;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace servicebusapi2.Schedule
{
    public class ScheduleTask
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName;
        private readonly ILogger<ScheduleTask> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private Timer _timer;
        private Details _scheduledDetails;
        private int _intervalMinutes;
        private int _durationHours;
        private DateTime _startTime;
        private volatile bool _stopped;

        private const string EventStarted = "ScheduleStarted";
        private const string EventTick = "ScheduledTickSent";
        private const string EventStopped = "ScheduleStopped";
        private const string EventError = "ScheduleError";

        // Toggle: set to false to remove per-tick console lines
        private const bool LogTickToConsole = false;

        public ScheduleTask(ServiceBusClient client,
                            string topicName,
                            ILogger<ScheduleTask> logger,
                            IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _topicName = topicName;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public void StartScheduling(Details details, int intervalMinutes, int durationHours)
        {
            _stopped = false;
            _scheduledDetails = details;
            _intervalMinutes = intervalMinutes > 0 ? intervalMinutes : 1;
            _durationHours = durationHours > 0 ? durationHours : 1;
            _startTime = DateTime.UtcNow;

            _timer?.Dispose();
            _timer = new Timer(async _ => await SendScheduledMessageAsync(),
                               null,
                               TimeSpan.Zero,
                               TimeSpan.FromMinutes(_intervalMinutes));

            _logger.LogInformation("Schedule started: every {interval}m for {duration}h",
                _intervalMinutes, _durationHours);
            Log(EventStarted, "Info", $"Schedule started: every {_intervalMinutes}m for {_durationHours}h");
        }

        // Return true if an active schedule was stopped, false if none active
        public bool StopScheduling(string reason = "Stopped manually.")
        {
            if (_stopped)
            {
                Console.WriteLine($"[ScheduleTask] STOP (duplicate request) {DateTime.UtcNow:O}");
                return false;
            }

            _stopped = true;
            _timer?.Change(Timeout.Infinite, 0);

            _logger.LogInformation("Schedule stopped. Reason: {reason}", reason);
            Log(EventStopped, "Info", reason);

            Console.WriteLine($"[Schedule] STOP t={DateTime.UtcNow:O} ran={(DateTime.UtcNow - _startTime):g} reason=\"{reason}\"");
            return true;
        }

        private async Task SendScheduledMessageAsync()
        {
            if (_stopped) return;

            try
            {
                if ((DateTime.UtcNow - _startTime).TotalHours >= _durationHours)
                {
                    _stopped = true;
                    _timer?.Change(Timeout.Infinite, 0);
                    Log(EventStopped, "Info", "Schedule duration elapsed. Stopped.");
                    Console.WriteLine($"[Schedule] STOP t={DateTime.UtcNow:O} (duration elapsed) total={(DateTime.UtcNow - _startTime):g}");
                    return;
                }


                string json = JsonSerializer.Serialize(_scheduledDetails);
                var sender = _client.CreateSender(_topicName);
                await sender.SendMessageAsync(new ServiceBusMessage(json));

                Log(EventTick, "Success", "Scheduled message sent.");

                if (LogTickToConsole)
                    Console.WriteLine($"[Schedule] TICK t={DateTime.UtcNow:O}");
            }
            catch (Exception ex)
            {
                Log(EventError, "Error", $"Scheduled send failed: {ex.Message}");
                Console.WriteLine($"[Schedule] ERROR t={DateTime.UtcNow:O} msg=\"{ex.Message}\"");
            }
        }

        private void Log(string eventType, string severity, string message)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ServiceBusLogContext>();
                ctx.ServiceBusLogs.Add(new ServiceBusLog
                {
                    EventType = eventType,
                    Severity = severity,
                    Message = message
                });
                ctx.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Schedule] LOG-FAIL t={DateTime.UtcNow:O} event={eventType} err=\"{ex.Message}\"");
            }
        }
    }
}
