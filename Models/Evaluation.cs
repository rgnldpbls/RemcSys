using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class Evaluation
    {
        [Key]
        public string evaluation_Id {  get; set; }
        public string evaluation_Status { get; set; }
        public string evaluator_Name { get; set; }
        public string? evaluation_Grade { get; set; }
        public DateTime assigned_Date { get; set; }
        public DateTime? evaluation_Date { get; set; }
        public int evaluator_Id { get; set; }
        public string fra_Id { get; set; }
        public FundedResearchApplication? fundedResearchApplication { get; set; }
        public Evaluator? evaluator { get; set; }
    }
}
