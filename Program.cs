using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PerformansTakip.Models;
using System.IO;
using PerformansTakip.Services;

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
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureDeleted(); // Veritabanını tamamen sil
    db.Database.EnsureCreated(); // Yeni veritabanı oluştur

    // Admin kullanıcısını kesin olarak oluştur
    if (!db.Admins.Any())
    {
        var admin = new Admin
        {
            Username = "admin",
            Password = "admin1234",
            Email = "ancellerdem1234@gmail.com",
            LastLogin = null
        };
        db.Admins.Add(admin);
        db.SaveChanges();
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
