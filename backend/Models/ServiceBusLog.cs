namespace servicebusapi2.Models
{
    public class ServiceBusLog
    {
        public int Id { get; set; }


        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string EventType { get; set; }

        public string Severity { get; set; }

        public string Message { get; set; }
    }
}
