using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class Guidelines
    {
        [Key]
        public string Id { get; set; }
        public string file_Name { get; set; }
        public string file_Type { get; set; }
        public byte[] data { get; set; }
        public string document_Type { get; set; }
        public DateTime file_Uploaded { get; set; }
    }
}
