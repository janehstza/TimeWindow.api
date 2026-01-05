namespace TimeWindow.api.App.Models
{
    public class Group
    {
        public Guid GroupId { get; set; }
        public string InviteCode { get; set; }
        public int TargetCount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Origin { get; set; }
        public int TravelDays { get; set; }
        public DateOnly DateStart { get; set; }
        public DateOnly DateEnd { get; set; }

    }
}
