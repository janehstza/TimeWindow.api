namespace TimeWindow.api.App.Models
{
    public record CommonTimeRangeDto(DateOnly StartDate, DateOnly EndDate, int Days);
}
