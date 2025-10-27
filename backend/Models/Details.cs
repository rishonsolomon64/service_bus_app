using servicebusapi2.Models;
namespace servicebusapi2.Models
{
    public class Details
    {
        public string type { get; set; }
        public  NestedDetails vmsIntegration { get; set; }
        public string sequenceNumber { get; set; }
        public string deviceIds { get; set; }

    }
}
