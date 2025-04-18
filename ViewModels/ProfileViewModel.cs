using System.ComponentModel.DataAnnotations;

namespace PerformansTakip.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Display(Name = "E-posta")]
        public string Email { get; set; }

        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Display(Name = "Son Giriş Tarihi")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Kayıt Tarihi")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";
    }
} 