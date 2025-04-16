using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerformansTakip.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Öğrenci adı zorunludur")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Öğrenci soyadı zorunludur")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Sınıf seçimi zorunludur")]
        [Display(Name = "Sınıf")]
        public int ClassId { get; set; }

        [Display(Name = "Kıyafet Durumu")]
        public bool UniformStatus { get; set; }

        [Display(Name = "Ödev Durumu")]
        public bool HomeworkStatus { get; set; }

        [Display(Name = "Performans Puanı")]
        [Range(0, 100, ErrorMessage = "Performans puanı 0-100 arasında olmalıdır")]
        public int PerformanceScore { get; set; }

        [Display(Name = "Son Güncelleme")]
        public DateTime LastUpdated { get; set; }

        // Navigation property
        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }
    }
} 