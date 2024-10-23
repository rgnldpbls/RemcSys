using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class GAWADWinners
    {
        [Key]
        public string gw_Id { get; set; }
        public string gw_fileName { get; set; }
        public string gw_fileType { get; set; }
        public byte[] gw_Data { get; set; }
        public DateTime file_Uploaded { get; set; }
        public string? UserId { get; set; }
    }
}
