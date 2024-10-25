namespace RemcSys.Models
{
    public class CalendarEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime Start {  get; set; }
        public DateTime? End { get; set; }
        public string Visibility { get; set; }
        public string UserId { get; set; }
    }
}
