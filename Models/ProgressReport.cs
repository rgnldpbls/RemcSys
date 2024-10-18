using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class ProgressReport
    {
        [Key]
        public string pr_Id { get; set; }
        public string file_Name { get; set; }
        public string file_Type { get; set; }
        public byte[] data { get; set; }
        public string file_Status { get; set; }
        public string? document_Type { get; set; }
        public string? file_Feedback { get; set; }
        public DateTime file_Uploaded { get; set; }
        public string fr_Id { get; set; }
        public FundedResearch FundedResearch { get; set; }
    }
}
