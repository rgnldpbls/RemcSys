using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class Evaluator
    {
        [Key]
        public int evaluator_Id { get; set; } // Primary Key
        public string evaluator_Name { get; set; } // Additional Attri - can be retrieve in User table
        public string evaluator_Email { get; set; } // Additional Attri - can be retrieve in User table
        public List<string> field_of_Interest {  get; set; } // Additional Attri - must asked to evaluator what his/her field of interest
        // Field of Interests are:
        // a. Science
        // b. Social Science
        // c. Business
        // d. Accountancy and Finance,
        // e. Computer Science and Information System Technology
        // f. Education
        // g. Engineering, Architecture, Design, and Built Environment
        // h. Humanities, Language, and Communication
        // i. Public Administration, Political Science, and Law
        public string UserId {  get; set; } // Foreign Key
        public string? UserType { get; set; }
        public List<string>? center {  get; set; }
        public ICollection<Evaluation> Evaluations { get; set; }
    }
}
