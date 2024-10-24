﻿using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class FundedResearch
    {
        [Key]
        public string fr_Id { get; set; }
        public string fr_Type { get; set; }
        public string research_Title { get; set; }
        public string team_Leader { get; set; }
        public string teamLead_Email { get; set; }
        public List<string> team_Members { get; set; }
        public string college { get; set; }
        public string branch { get; set; }
        public string field_of_Study { get; set; }
        public string status { get; set; }
        public DateTime start_Date { get; set; }
        public DateTime end_Date { get; set; }
        public string dts_No { get; set; }
        public int project_Duration { get; set; }
        public double? total_project_Cost { get; set; }
        public bool isExtension1 {  get; set; }
        public bool isExtension2 { get; set; }
        public string fra_Id { get; set; }
        public FundedResearchApplication FundedResearchApplication { get; set; }
        public string UserId { get; set; }
        public bool isArchive { get; set; }
        public ICollection<ProgressReport> ProgressReports { get; set; }
    }
}
