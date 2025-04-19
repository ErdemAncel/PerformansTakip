using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PerformansTakip.Models;
using Microsoft.EntityFrameworkCore;
using PerformansTakip.Data;
using System.Linq;

namespace PerformansTakip.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Toplam sınıf sayısı
            ViewBag.TotalClasses = await _context.Classes.CountAsync();
            
            // Toplam öğrenci sayısı
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            
            // Ortalama performans puanı
            var avgPerformance = await _context.Students
                .Where(s => s.PerformanceScore > 0)
                .AverageAsync(s => s.PerformanceScore);
            ViewBag.AveragePerformance = avgPerformance > 0 ? Math.Round(avgPerformance, 1) : 0;
            
            // Sınıf bazında öğrenci dağılımı
            var classDistribution = await _context.Classes
                .Select(c => new
                {
                    ClassName = c.Name,
                    StudentCount = c.StudentCount
                })
                .OrderBy(c => c.ClassName)
                .ToListAsync();
            ViewBag.ClassDistribution = classDistribution;
            
            // Son güncellemeleri al
            var recentUpdates = await _context.Students
                .Include(s => s.Class)
                .OrderByDescending(s => s.LastUpdated)
                .Take(5)
                .Select(s => new
                {
                    StudentName = $"{s.FirstName} {s.LastName}",
                    ClassName = s.Class.Name,
                    UpdateType = s.LastUpdateType,
                    UpdateDate = s.LastUpdated
                })
                .ToListAsync();
            ViewBag.RecentUpdates = recentUpdates;
            
            // Kıyafet ve ödev durumu istatistikleri
            var uniformStats = await _context.Students
                .GroupBy(s => s.UniformStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.UniformStats = uniformStats;
            
            var homeworkStats = await _context.Students
                .GroupBy(s => s.HomeworkStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.HomeworkStats = homeworkStats;
            
            // Performans dağılımı
            var performanceDistribution = await _context.Students
                .Where(s => s.PerformanceScore > 0)
                .GroupBy(s => s.PerformanceScore / 10 * 10)
                .Select(g => new { Range = $"{g.Key}-{g.Key + 9}", Count = g.Count() })
                .OrderBy(g => g.Range)
                .ToListAsync();
            ViewBag.PerformanceDistribution = performanceDistribution;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ana sayfa verileri yüklenirken hata oluştu");
            return View();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
