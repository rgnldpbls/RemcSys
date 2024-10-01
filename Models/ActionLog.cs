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
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }

        public ActionLog()
        {
            LogId = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }
    }

    public class ActionLoggerService
    {
        private readonly RemcDBContext _context;
        
        public ActionLoggerService(RemcDBContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(string userId, string fraId, string description)
        {
            var logEntry = new ActionLog
            {
                UserId = userId,
                FraId = fraId,
                Description = description
            };

            _context.ActionLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }
    }
}
