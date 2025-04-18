using System.ComponentModel.DataAnnotations;

namespace PerformansTakip.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }
    }
} 