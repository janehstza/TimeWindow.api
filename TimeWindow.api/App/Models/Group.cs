namespace TimeWindow.api.App.Models
{
    public class Group
    {
        public Guid GroupId { get; set; }
        public string InviteCode { get; set; }
        public int TargetCount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
