using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PerformansTakip.Models;

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

        public IActionResult Index()
        {
            var classes = _context.Classes.ToList();
            return View(classes);
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