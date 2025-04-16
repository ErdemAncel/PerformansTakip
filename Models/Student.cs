using System.ComponentModel.DataAnnotations;

namespace PerformansTakip.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public int ClassId { get; set; }
        public virtual Class Class { get; set; }

        // Performance tracking properties
        public bool UniformStatus { get; set; }
        public bool HomeworkStatus { get; set; }
        public int PerformanceScore { get; set; }
        public DateTime LastUpdated { get; set; }

        // Additional tracking properties
        public string Notes { get; set; }
    }
} 