using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class GenerateReport
    {
        [Key]
        public string gr_Id { get; set; }
        public string gr_fileName { get; set; }
        public string gr_fileType { get; set; }
        public byte[] gr_Data { get; set; }
        public DateTime gr_startDate {  get; set; }
        public DateTime gr_endDate { get; set; }
        public string gr_typeofReport { get; set; }
        public DateTime generateDate { get; set; }
        public string? UserId { get; set; }
        public bool isArchived { get; set; }
    }
}
