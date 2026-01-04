namespace TimeWindow.api.App.Models
{
    public class CreateGroupRequest
    {
        public int TargetCount { get; set; }
        public int TravelDays { get; set; } = 1;
        public DateOnly? DateStart { get; set; }
        public DateOnly? DateEnd { get; set; }

    }
}
