using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class FundedResearchApplication
    {
        [Key]
        public string fra_Id { get; set; }
        public string fra_Type { get; set; }
        public string research_Title { get; set; }
        public string applicant_Name { get; set; }
        public string applicant_Email { get; set; }
        public List<string> team_Members { get; set; }
        public string college { get; set; }
        public string branch { get; set; }
        public string field_of_Study { get; set; }
        public string application_Status { get; set; }
        public DateTime submission_Date { get; set; }
        public string? dts_No { get; set; }
        public int? project_Duration {  get; set; }
        public double? total_project_Cost { get; set; }
        public ICollection<GeneratedForm> GeneratedForms { get; set; }
        public ICollection<FileRequirement> FileRequirements { get; set; }
        public ICollection<Evaluation> Evaluations { get; set; } 
        public ICollection<ActionLog> ActionLogs { get; set; }
        public FundedResearchEthics FundedResearchEthics { get; set; }
        public UniversityFundedResearch? UniversityFundedResearch { get; set; }
        public ExternallyFundedResearch? ExternallyFundedResearch { get; set; }
        public UniversityFundedResearchLoad? UniversityFundedResearchLoad { get; set; }
        public string UserId { get; set; }
        public bool isArchive { get; set; }
    }
}
