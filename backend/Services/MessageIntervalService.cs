namespace servicebusapi2.Services
{

    public class MessageIntervalService
    {
        public int IntervalInMinutes { get; set; } = 1;

        public event Action<int> IntervalChanged;

        public void SetInterval(int newInterval)
        {
            if (newInterval > 0)
            {
                IntervalInMinutes = newInterval;
                IntervalChanged?.Invoke(newInterval);
            }
        }
    }
}
