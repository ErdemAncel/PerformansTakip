using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using PerformansTakip.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;

namespace PerformansTakip.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const int MAX_LOGIN_ATTEMPTS = 3;
        private const int LOCKOUT_DURATION_MINUTES = 1;

        public AccountController(IConfiguration configuration, ApplicationDbContext context, IMemoryCache cache)
        {
            _configuration = configuration;
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Class");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Admin model)
        {
            // Boş alan kontrolü
            if (string.IsNullOrEmpty(model.Username))
            {
                ModelState.AddModelError("Username", "Kullanıcı adı boş bırakılamaz!");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Şifre boş bırakılamaz!");
                return View(model);
            }

            var adminUsername = _configuration["AdminCredentials:Username"];
            var adminPassword = _configuration["AdminCredentials:Password"];

            // Kullanıcı adı kontrolü
            if (model.Username != adminUsername)
            {
                ModelState.AddModelError("Username", "Kullanıcı adı hatalı!");
                return View(model);
            }

            // Şifre kontrolü
            if (model.Password != adminPassword)
            {
                ModelState.AddModelError("Password", "Şifre hatalı!");
                return View(model);
            }

            try
            {
                // Admin kaydını veritabanında kontrol et veya oluştur
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == model.Username);
                if (admin == null)
                {
                    admin = new Admin
                    {
                        Username = model.Username,
                        Password = model.Password,
                        Email = "admin@performanstakip.com",
                        LastLogin = DateTime.Now
                    };
                    _context.Admins.Add(admin);
                }
                else
                {
                    admin.LastLogin = DateTime.Now;
                }
                await _context.SaveChangesAsync();

                // Kimlik doğrulama bilgilerini oluştur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Class");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Giriş sırasında bir hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var adminUsername = _configuration["AdminCredentials:Username"];
                    var currentPassword = _configuration["AdminCredentials:Password"];

                    if (model.CurrentPassword == currentPassword)
                    {
                        _configuration["AdminCredentials:Password"] = model.NewPassword;
                        
                        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == adminUsername);
                        if (admin != null)
                        {
                            admin.Password = model.NewPassword;
                            await _context.SaveChangesAsync();
                        }

                        TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
                        return RedirectToAction("Login");
                    }

                    ModelState.AddModelError(string.Empty, "Mevcut şifre yanlış");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Şifre değiştirme sırasında bir hata oluştu: " + ex.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var adminUsername = _configuration["AdminCredentials:Username"];
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == adminUsername);

                    if (admin != null && admin.Email == model.Email)
                    {
                        var newPassword = GenerateRandomPassword();
                        _configuration["AdminCredentials:Password"] = newPassword;
                        admin.Password = newPassword;
                        await _context.SaveChangesAsync();

                        // Burada e-posta gönderme işlemi yapılabilir
                        TempData["SuccessMessage"] = "Yeni şifreniz e-posta adresinize gönderildi.";
                        return RedirectToAction("Login");
                    }

                    ModelState.AddModelError(string.Empty, "E-posta adresi bulunamadı");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Şifre sıfırlama sırasında bir hata oluştu: " + ex.Message);
                }
            }

            return View(model);
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur")]
        [Display(Name = "Mevcut Şifre")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur")]
        [Display(Name = "Yeni Şifre")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı zorunludur")]
        [Display(Name = "Yeni Şifre Tekrar")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmPassword { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string Email { get; set; }
    }
} 