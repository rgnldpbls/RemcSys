using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class FundedResearchApplication
    {
        [Key]
        public string? fra_Id { get; set; }
        public string? fra_Type { get; set; }
        public string? research_Title { get; set; }
        public string? applicant_Name { get; set; }
        public string? applicant_Email { get; set; }
        public string? college { get; set; }
        public string? branch { get; set; }
        public string? field_of_Study { get; set; }
        public string? application_Status { get; set; }
        public DateTime? submission_Date { get; set; }
        public string? dts_No { get; set; }
        public List<ResearchStaff>? ResearchStaffs { get; set; }
        public List<FileRequirement>? FileRequirements { get; set; }
        public string? UserId { get; set; }
    }
}
