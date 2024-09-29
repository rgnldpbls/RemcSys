using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class GeneratedForm
    {
        [Key]
        public string Id { get; set; }
        public string FileName { get; set; }
        public byte[] FileContent { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string fra_Id { get; set; }
        public FundedResearchApplication FundedResearchApplication { get; set; }
    }
}
