using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class UniversityFundedResearch
    {
        [Key]
        public string ufr_Id {  get; set; }
        public string research_Title { get; set; }
        public string team_Leader { get; set; }
        public string teamLead_Email { get; set; }
        public List<string> team_Members { get; set; }
        public string field_of_Study { get; set; }
        public DateTime proceed_Date { get; set; }
    }
}
