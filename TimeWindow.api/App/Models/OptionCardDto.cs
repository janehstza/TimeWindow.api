namespace TimeWindow.api.App.Models
{
    public class OptionCardDto
    {
        public string OptionId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int Days { get; set; }
        public string Location { get; set; }
    }
}
