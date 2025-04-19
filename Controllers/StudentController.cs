using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PerformansTakip.Data;
using PerformansTakip.Models;

namespace PerformansTakip.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Class)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Sınıf listesini ViewBag'e ekle
            ViewBag.Classes = await _context.Classes
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Section)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            return View(students);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Student student)
        {
            if (student == null)
            {
                return Json(new { success = false, message = "Öğrenci bilgileri boş olamaz." });
            }

            try
            {
                student.LastUpdated = DateTime.Now;
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Sınıfın öğrenci sayısını güncelle
                var classItem = await _context.Classes.FindAsync(student.ClassId);
                if (classItem != null)
                {
                    classItem.StudentCount = await _context.Students.CountAsync(s => s.ClassId == student.ClassId);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return Json(new { success = false, message = "Öğrenci bulunamadı." });
            }

            try
            {
                var classId = student.ClassId;
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                // Sınıfın öğrenci sayısını güncelle
                var classItem = await _context.Classes.FindAsync(classId);
                if (classItem != null)
                {
                    classItem.StudentCount = await _context.Students.CountAsync(s => s.ClassId == classId);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUniform(int id, bool status)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return Json(new { success = false, message = "Öğrenci bulunamadı." });
            }

            try
            {
                student.UniformStatus = status;
                student.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHomework(int id, bool status)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return Json(new { success = false, message = "Öğrenci bulunamadı." });
            }

            try
            {
                student.HomeworkStatus = status;
                student.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePerformance(int id, int score)
        {
            if (score < 0 || score > 100)
            {
                return Json(new { success = false, message = "Performans puanı 0-100 arasında olmalıdır." });
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return Json(new { success = false, message = "Öğrenci bulunamadı." });
            }

            try
            {
                student.PerformanceScore = score;
                student.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
} 