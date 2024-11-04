using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class FundedResearchEthics
    {
        [Key]
        public string fre_Id {  get; set; }
        public string file_Name { get; set; }
        public string file_Type { get; set; }
        public byte[]? clearanceFile { get; set; }
        public string file_Status { get; set; }
        public string? file_Feedback { get; set; }
        public DateTime file_Uploaded { get; set; }
        public string fra_Id { get; set; }
        public string? urecNo {  get; set; }
        public FundedResearchApplication FundedResearchApplication { get; set; }
        /*public EthicsApplication EthicsApplication {  get; set; }*/
    }
}
