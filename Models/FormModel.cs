namespace RemcSys.Models
{
    public class FormModel
    {
        public string ResearchType { get; set; }
        public string ProjectTitle { get; set; }
        public string ProjectLeader { get; set; }
        public string ProjectMembers { get; set; }
        public string ImplementingInstitution { get; set; }
        public string CollaboratingInstitution { get; set; }
        public string ProjectDuration { get; set; }
        public string TotalProjectCost { get; set; }
        public string Objectives { get; set; }
        public string Scope { get; set; }
        public string Methodology { get; set; }
        public string StudyField {  get; set; }
    }

    public class ViewEvaluationVM
    {
        public string fra_Id { get; set; }
        public string dts_No { get; set; }
        public string research_Title { get; set; }
        public string field_of_Study { get; set; }
        public string application_Status { get; set; }
        public DateTime? evaluation_deadline { get; set; }
    }
}
