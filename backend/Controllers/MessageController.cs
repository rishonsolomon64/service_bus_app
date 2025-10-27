using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using servicebusapi2.Models;
using servicebusapi2.Publish;
using servicebusapi2.Schedule;
using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace servicebusapi2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly PublishTask _publishTask;
        private readonly ScheduleTask _scheduleTask;
        private readonly ILogger<MessageController> _logger;
        private readonly ServiceBusLogContext _logContext;
        private readonly IWebHostEnvironment _env;

        public MessageController(
            PublishTask publishTask,
            ScheduleTask scheduleTask,
            ILogger<MessageController> logger,
            ServiceBusLogContext logContext,
            IWebHostEnvironment env)
        {
            _publishTask = publishTask;
            _scheduleTask = scheduleTask;
            _logger = logger;
            _logContext = logContext;
            _env = env;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishSingleMessage([FromBody] Details details)
        {
            if (details == null)
                return BadRequest("Invalid details provided.");

            try
            {
                await _publishTask.PublishSingleMessageAsync(details);
                return Ok(new { message = "Message published successfully!" });
            }
            catch (Exception ex)
            {
                _logContext.ServiceBusLogs.Add(new ServiceBusLog
                {
                    EventType = "PublishError",
                    Severity = "Error",
                    Message = $"Publish endpoint failed: {ex.Message}"
                });
                await _logContext.SaveChangesAsync();
                return StatusCode(500, new { message = $"Error publishing message: {ex.Message}" });
            }
        }


        public class ScheduleRequest
        {
            public Details Details { get; set; }
            public int intervalMinutes { get; set; }
            public int durationHours { get; set; }
        }

        [HttpPost("schedule")]
        public IActionResult Schedule([FromBody] ScheduleRequest request)
        {
            if (request?.Details == null || request.intervalMinutes <= 0 || request.durationHours <= 0)
                return BadRequest("Valid details, positive interval and duration are required.");

            _scheduleTask.StartScheduling(request.Details, request.intervalMinutes, request.durationHours);

            return Ok(new
            {
                message = $"Scheduling started. Every {request.intervalMinutes} minute(s) for {request.durationHours} hour(s)."
            });
        }

        [HttpPost("stop")]
        public IActionResult StopSchedule()
        {
            var stopped = _scheduleTask.StopScheduling();
            if (!stopped)
                return BadRequest(new { message = "No active schedule to stop." });

            return Ok(new { message = "Schedule stopped." });
        }

        [HttpGet("print")]
        public async Task<IActionResult> PrintAllLogs()
        {
            var logs = _logContext.ServiceBusLogs
                                  .OrderBy(l => l.Id)
                                  .ToList();

            // Simple CSV builder with basic escaping for quotes
            static string EscapeForCsv(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return $"\"{s.Replace("\"", "\"\"")}\"";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Id,Timestamp,EventType,Severity,Message");

            foreach (var l in logs)
            {
                var id = l.Id;
                var timestamp = l.Timestamp.ToString("o"); // ISO 8601
                var eventType = EscapeForCsv(l.EventType);
                var severity = EscapeForCsv(l.Severity);
                var message = EscapeForCsv(l.Message);

                sb.AppendLine($"{id},{timestamp},{eventType},{severity},{message}");
            }

            // Ensure exports directory exists in app content root
            var exportsDir = Path.Combine(_env.ContentRootPath, "exports");
            Directory.CreateDirectory(exportsDir);

            var fileName = $"servicebus_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(exportsDir, fileName);

            await System.IO.File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);

            // Console and structured log on success
            Console.WriteLine($"[Export] Wrote {logs.Count} logs to {filePath}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            // Return file for download with appropriate content type
            return File(bytes, "text/csv", fileName);
        }
    }
}

