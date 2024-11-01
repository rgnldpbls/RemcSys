using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class Criteria
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public double Weight { get; set; }
        public List<SubCategory> subCategory { get; set; }
    }

    public class SubCategory
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaxScore {  get; set; }
        public string? Description { get; set; }
        public int CriteriaId {  get; set; }
        public Criteria criteria { get; set; }
    }
}
