using RemcSys.Data;
using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class ActionLog
    {
        [Key]
        public string LogId { get; set; }
        public string UserId { get; set; }
        public string FraId { get; set; }
        public string? ProjLead {  get; set; } //EvaluatorNotif
        public string? FraType {  get; set; } // ChiefNotif
        public FundedResearchApplication fundedResearchApplication { get; set; }
        public string Description { get; set; }
        public string? Action {  get; set; }
        public DateTime Timestamp { get; set; }

        public ActionLog()
        {
            LogId = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }

        public static string GetTimeAgo(DateTime Timestamp)
        {
            var timeSpan = DateTime.Now - Timestamp;
            if (timeSpan.TotalMinutes < 1)
                return $"{timeSpan.Seconds} seconds ago";

            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.Minutes}m ago";

            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.Hours}hr ago";

            if (timeSpan.TotalDays < 7)
                return $"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")} ago";

            if (timeSpan.TotalDays < 30)
                return $"{timeSpan.Days / 7} week{(timeSpan.Days / 7 > 1 ? "s" : "")} ago";

            if (timeSpan.TotalDays < 365)
                return $"{timeSpan.Days / 30} month{(timeSpan.Days / 30 > 1 ? "s" : "")} ago";

            return $"{timeSpan.Days / 365} year{(timeSpan.Days / 365 > 1 ? "s" : "")} ago";
        }
    }

    public class ActionLoggerService
    {
        private readonly RemcDBContext _context;
        
        public ActionLoggerService(RemcDBContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(string userId, string fraId, string projLead, string fraType, string description, string action)
        {
            var logEntry = new ActionLog
            {
                UserId = userId,
                FraId = fraId,
                ProjLead = projLead,
                FraType = fraType,
                Description = description,
                Action = action
            };

            _context.ActionLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }
    }
}
