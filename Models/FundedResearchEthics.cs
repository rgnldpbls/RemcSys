using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class FundedResearchEthics
    {
        [Key]
        public string fre_Id {  get; set; }
        public string fra_Id { get; set; }
        public string? urec_No {  get; set; }
        public int? ethicClearance_Id { get; set; }
        public int? completionCertificate_Id {  get; set; } 
        public FundedResearchApplication FundedResearchApplication { get; set; }
    }
}
