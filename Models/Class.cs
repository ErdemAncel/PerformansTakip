using System.ComponentModel.DataAnnotations;

namespace PerformansTakip.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<Student> Students { get; set; }
    }
} 