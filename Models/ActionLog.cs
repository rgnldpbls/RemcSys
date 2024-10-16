using RemcSys.Data;
using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class ActionLog
    {
        [Key]
        public string LogId { get; set; }
        public string? Name {  get; set; }
        public string? ResearchType {  get; set; }
        public string? Action {  get; set; }
        public bool isTeamLeader {  get; set; }
        public bool isChief { get; set; }
        public bool isEvaluator {  get; set; }
        public DateTime Timestamp { get; set; }
        public string FraId { get; set; }
        public FundedResearchApplication fundedResearchApplication { get; set; }

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

        public async Task LogActionAsync(string name, string fraType, string action, bool isTeamLeader, bool isChief, bool isEvaluator, string fraId)
        {
            var logEntry = new ActionLog
            {
                Name = name,
                ResearchType = fraType,
                Action = action,
                isTeamLeader = isTeamLeader,
                isChief = isChief,
                isEvaluator = isEvaluator,
                FraId = fraId
            };

            _context.ActionLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }
    }
}
