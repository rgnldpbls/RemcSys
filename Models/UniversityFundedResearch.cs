using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class UniversityFundedResearch
    {
        [Key]
        public string ufrw_Id {  get; set; }
        public string research_Title { get; set; }
        public string team_Leader { get; set; }
        public string teamLead_Email { get; set; }
        public List<string> team_Members { get; set; }
        public string college { get; set; }
        public string branch { get; set; }
        public string field_of_Study { get; set; }
        public string research_Status { get; set; }
        public DateTime start_Date { get; set; }
        public DateTime end_Date { get; set; }
        public int? projectDuration { get; set; }
        public string? dts_No { get; set; }
        public int project_Duration { get; set; }
        public double? total_project_Cost { get; set; }
        public string fra_Id { get; set; }
        public FundedResearchApplication FundedResearchApplication { get; set; }
        public string UserId { get; set; }
        public bool isArchive { get; set; }
    }
}
