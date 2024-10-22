using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class GenerateGAWADNominees
    {
        [Key]
        public string gn_Id { get; set; }
        public string gn_fileName { get; set; }
        public string gn_fileType { get; set; }
        public byte[] gn_Data { get; set; }
        public string gn_type { get; set; }
        public DateTime generateDate { get; set; }
        public string? UserId { get; set; }
        public bool isArchived { get; set; }
    }
}
