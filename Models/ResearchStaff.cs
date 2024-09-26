using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class ResearchStaff
    {
        [Key]
        public int rs_Id { get; set; }
        public string? rs_Name { get; set; }
        public string? rs_Role { get; set; }
        public string? Email { get; set; }
        public string? fra_Id { get; set; }
        public FundedResearchApplication? FundedResearchApplication { get; set; }
    }
}