namespace RemcSys.Models
{
    public class Settings
    {
        public string Id { get; set; }
        public bool isMaintenance { get; set; }
        public bool isUFRApplication { get; set; }
        public bool isEFRApplication { get; set; }
        public bool isUFRLApplication { get; set; }
        public int evaluatorNum { get; set; }
        public int daysEvaluation { get; set; }
    }
}
