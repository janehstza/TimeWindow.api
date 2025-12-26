namespace TimeWindow.api.App.Models
{
    public class Participant
    {
        public Guid ParticipantId { get; set;  }
        public Guid GroupId { get; set; }
        public string DisplayName { get; set; }
        public bool Submitted { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
