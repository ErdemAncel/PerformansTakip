using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PerformansTakip.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace PerformansTakip.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClassController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Sınıfları kontrol et ve yoksa ekle
            if (!await _context.Classes.AnyAsync())
            {
                await SeedClasses();
            }

            var classes = await _context.Classes
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Section)
                .ToListAsync();

            return View(classes);
        }

        private async Task SeedClasses()
        {
            var grades = new[] { 9, 10, 11, 12 };
            var sections = new[] { 'A', 'B', 'C', 'D', 'E' };

            foreach (var grade in grades)
            {
                foreach (var section in sections)
                {
                    var className = $"{grade}-{section}";
                    var classEntity = new Class
                    {
                        Name = className,
                        Grade = grade,
                        Section = section.ToString(),
                        StudentCount = 0
                    };

                    _context.Classes.Add(classEntity);
                }
            }

            await _context.SaveChangesAsync();
        }

        public IActionResult Students(int id, string trackingType)
        {
            var students = _context.Students
                .Where(s => s.ClassId == id)
                .ToList();

            ViewBag.TrackingType = trackingType;
            ViewBag.ClassName = _context.Classes.Find(id)?.Name;
            ViewBag.ClassId = id;

            return View(students);
        }

        [HttpPost]
        public IActionResult UpdateUniform(int studentId, bool status)
        {
            var student = _context.Students.Find(studentId);
            if (student != null)
            {
                student.UniformStatus = status;
                student.LastUpdated = DateTime.Now;
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UpdateHomework(int studentId, bool status)
        {
            var student = _context.Students.Find(studentId);
            if (student != null)
            {
                student.HomeworkStatus = status;
                student.LastUpdated = DateTime.Now;
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UpdatePerformance(int studentId, int score)
        {
            var student = _context.Students.Find(studentId);
            if (student != null)
            {
                student.PerformanceScore = score;
                student.LastUpdated = DateTime.Now;
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
} 