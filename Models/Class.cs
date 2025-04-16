using System.ComponentModel.DataAnnotations;

namespace PerformansTakip.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Sınıf adı zorunludur")]
        [Display(Name = "Sınıf Adı")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Sınıf seviyesi zorunludur")]
        [Display(Name = "Sınıf Seviyesi")]
        public int Grade { get; set; }

        [Required(ErrorMessage = "Şube zorunludur")]
        [Display(Name = "Şube")]
        public string Section { get; set; }

        [Display(Name = "Öğrenci Sayısı")]
        public int StudentCount { get; set; }

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
} 