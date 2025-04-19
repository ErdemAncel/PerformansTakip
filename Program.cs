using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PerformansTakip.Models;
using System.IO;
using PerformansTakip.Services;
using PerformansTakip.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "PerformansTakip.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// Configure SQLite database
var dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");
if (!Directory.Exists(dataFolder))
{
    Directory.CreateDirectory(dataFolder);
}

var dbPath = Path.Combine(dataFolder, "PerformansTakip.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
    // Detaylı hata mesajlarını etkinleştir
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
});

builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Veritabanı bağlantısını kontrol et
        if (db.Database.CanConnect())
        {
            logger.LogInformation("Veritabanına başarıyla bağlanıldı.");
            
            // Bekleyen migration var mı kontrol et
            var pendingMigrations = db.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                logger.LogInformation($"Bekleyen {pendingMigrations.Count()} migration bulundu. Uygulanıyor...");
                db.Database.Migrate();
            }
            
            // Admin kullanıcısı var mı kontrol et
            if (!db.Admins.Any())
            {
                logger.LogInformation("Admin kullanıcısı bulunamadı. Varsayılan admin oluşturuluyor...");
                var admin = new Admin
                {
                    Username = "admin",
                    Password = "admin1234",
                    Email = "admin@performanstakip.com",
                    FirstName = "Admin",
                    LastName = "User",
                    CreatedAt = DateTime.Now
                };
                db.Admins.Add(admin);
                db.SaveChanges();
                logger.LogInformation("Varsayılan admin kullanıcısı oluşturuldu.");
            }
            else
            {
                var adminCount = db.Admins.Count();
                logger.LogInformation($"Sistemde {adminCount} admin kullanıcısı bulunuyor.");
            }
        }
        else
        {
            logger.LogWarning("Veritabanına bağlanılamadı. Yeni veritabanı oluşturuluyor...");
            db.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Veritabanı işlemleri sırasında bir hata oluştu.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
