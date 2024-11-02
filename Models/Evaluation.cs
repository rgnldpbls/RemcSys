using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class Evaluation
    {
        [Key]
        public string evaluation_Id {  get; set; }
        public string evaluation_Status { get; set; }
        public string evaluator_Name { get; set; }
        public double? evaluation_Grade { get; set; }
        public DateTime assigned_Date { get; set; }
        public DateTime evaluation_Deadline { get; set; }
        public DateTime? evaluation_Date { get; set; }
        public int evaluator_Id { get; set; }
        public string fra_Id { get; set; }
        public FundedResearchApplication? fundedResearchApplication { get; set; }
        public Evaluator? evaluator { get; set; }
        public bool reminded_ThreeDaysBefore { get; set; }
        public bool reminded_OneDayBefore { get; set; }
        public bool reminded_Today { get; set; }
        public bool reminded_OneDayOverdue { get; set; }
        public bool reminded_ThreeDaysOverdue { get; set; }
        public bool reminded_SevenDaysOverdue { get; set; }
    }
}
