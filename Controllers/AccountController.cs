using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using PerformansTakip.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PerformansTakip.ViewModels;
using PerformansTakip.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using PerformansTakip.Data;
using System.Text;
using System.Security.Cryptography;

namespace PerformansTakip.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const int MAX_LOGIN_ATTEMPTS = 3;
        private const int LOCKOUT_DURATION_MINUTES = 1;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration configuration, ApplicationDbContext context, IMemoryCache cache, ILogger<AccountController> logger)
        {
            _configuration = configuration;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // Test metodu - Tüm adminleri listele
        [HttpGet]
        public IActionResult ListAdmins()
        {
            var admins = _context.Admins.ToList();
            return Json(new { count = admins.Count, admins = admins.Select(a => new { a.Username, a.Email, a.LastLogin }) });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }

            var username = User.Identity.Name;
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == username);

            if (admin == null)
            {
                return NotFound();
            }

            var viewModel = new ProfileViewModel
            {
                Username = admin.Username,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                LastLogin = admin.LastLogin,
                CreatedAt = admin.CreatedAt
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Class");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Admin model)
        {
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

            try
            {
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Username == model.Username);

                if (admin == null)
                {
                    ModelState.AddModelError("Username", "Kullanıcı adı hatalı!");
                    return View(model);
                }

                if (admin.Password != model.Password)
                {
                    ModelState.AddModelError("Password", "Şifre hatalı!");
                    return View(model);
                }

                admin.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim("AdminId", admin.Id.ToString())
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
                _logger.LogError(ex, "Giriş hatası");
                ModelState.AddModelError(string.Empty, "Giriş sırasında bir hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Class");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kullanıcı adı kontrolü
                    if (await _context.Admins.AnyAsync(a => a.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor.");
                        return View(model);
                    }

                    // E-posta kontrolü
                    if (await _context.Admins.AnyAsync(a => a.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                        return View(model);
                    }

                    var admin = new Admin
                    {
                        Username = model.Username,
                        Password = model.Password,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CreatedAt = DateTime.Now
                    };

                    _context.Admins.Add(admin);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Kayıt işlemi başarıyla tamamlandı. Şimdi giriş yapabilirsiniz.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kayıt hatası");
                    ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu: " + ex.Message);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                var username = User.Identity?.Name;
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);

                if (admin == null)
                {
                    return NotFound();
                }

                if (admin.Password != model.CurrentPassword)
                {
                    ModelState.AddModelError("CurrentPassword", "Mevcut şifre hatalı!");
                    return View(model);
                }

                admin.Password = model.NewPassword;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
                return RedirectToAction(nameof(Profile));
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
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == model.Username);

                if (admin != null)
                {
                    var newPassword = GenerateRandomPassword();
                    admin.Password = newPassword;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Yeni şifreniz: {newPassword}";
                    return RedirectToAction(nameof(Login));
                }

                ModelState.AddModelError("Username", "Kullanıcı bulunamadı.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel { Username = username };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == model.Username);

                if (admin != null)
                {
                    admin.Password = model.NewPassword;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Şifreniz başarıyla sıfırlandı.";
                    return RedirectToAction(nameof(Login));
                }

                ModelState.AddModelError("Username", "Kullanıcı bulunamadı.");
            }

            return View(model);
        }

        private string GenerateRandomPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+";
            using var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[16];
            rng.GetBytes(bytes);

            var password = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                password.Append(validChars[bytes[i] % validChars.Length]);
            }

            return password.ToString();
        }
    }
}