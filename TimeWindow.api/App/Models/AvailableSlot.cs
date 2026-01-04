namespace TimeWindow.api.App.Models
{
    public class AvailableSlot
    {
        public Guid GroupId { get; set; }
        public string DisplayName { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
    }
}
