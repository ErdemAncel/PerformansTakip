using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PerformansTakip.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using PerformansTakip.Data;

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

        [HttpGet]
        public async Task<IActionResult> Students(int id, string trackingType = "uniform")
        {
            try
            {
                // Sınıfı ve öğrencilerini yükle
                var classItem = await _context.Classes
                    .Include(c => c.Students)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (classItem == null)
                {
                    return NotFound("Sınıf bulunamadı.");
                }

                // ViewBag değerlerini ayarla
                ViewBag.ClassName = classItem.Name;
                ViewBag.TrackingType = trackingType;
                ViewBag.ClassId = id;

                // Öğrencileri sıralı şekilde getir
                var students = classItem.Students
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList();

                return View(students);
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yapılabilir
                return View(new List<Student>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUniform(int studentId, bool status)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return Json(new { success = false, message = "Öğrenci bulunamadı" });
            }

            student.UniformStatus = status;
            student.LastUpdated = DateTime.Now;
            student.LastUpdateType = "Kıyafet Güncellemesi";
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateHomework(int studentId, bool status)
        {
            var student = _context.Students.Find(studentId);
            if (student != null)
            {
                student.HomeworkStatus = status;
                student.LastUpdated = DateTime.Now;
                student.LastUpdateType = "Ödev Güncellemesi";
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
                student.LastUpdateType = "Performans Güncellemesi";
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> AddStudent([FromBody] Student student)
        {
            if (student == null)
            {
                return Json(new { success = false, message = "Öğrenci bilgileri boş olamaz." });
            }

            if (student.ClassId <= 0)
            {
                return Json(new { success = false, message = "Geçerli bir sınıf seçilmelidir." });
            }

            try
            {
                // Sınıfın var olup olmadığını kontrol et
                var classExists = await _context.Classes.AnyAsync(c => c.Id == student.ClassId);
                if (!classExists)
                {
                    return Json(new { success = false, message = "Seçilen sınıf bulunamadı." });
                }

                // Öğrenci bilgilerini ayarla
                student.LastUpdated = DateTime.Now;
                student.UniformStatus = false;
                student.HomeworkStatus = false;
                student.PerformanceScore = 0;

                // Öğrenciyi ekle
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Sınıfın öğrenci sayısını güncelle
                var classItem = await _context.Classes.FindAsync(student.ClassId);
                if (classItem != null)
                {
                    classItem.StudentCount = await _context.Students.CountAsync(s => s.ClassId == student.ClassId);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Öğrenci başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddMultipleStudents([FromBody] AddMultipleStudentsRequest request)
        {
            try
            {
                // Önce sınıfı bul
                var classItem = await _context.Classes.FindAsync(request.classId);
                if (classItem == null)
                {
                    return Json(new { success = false, message = "Sınıf bulunamadı. ClassId: " + request.classId });
                }

                // Öğrenci isimlerini kontrol et
                if (request.studentNames == null || !request.studentNames.Any())
                {
                    return Json(new { success = false, message = "En az bir öğrenci bilgisi girmelisiniz." });
                }

                foreach (var fullName in request.studentNames)
                {
                    var nameParts = fullName.Split(' ');
                    if (nameParts.Length >= 2)
                    {
                        var student = new Student
                        {
                            FirstName = nameParts[0],
                            LastName = string.Join(" ", nameParts.Skip(1)),
                            ClassId = request.classId,
                            LastUpdated = DateTime.Now,
                            UniformStatus = false,
                            HomeworkStatus = false,
                            PerformanceScore = 0
                        };

                        _context.Students.Add(student);
                    }
                }

                await _context.SaveChangesAsync();

                // Sınıfın öğrenci sayısını güncelle
                classItem.StudentCount = await _context.Students.CountAsync(s => s.ClassId == request.classId);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Öğrenciler başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        public class AddMultipleStudentsRequest
        {
            public int classId { get; set; }
            public List<string> studentNames { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllStudents([FromForm] int classId)
        {
            try
            {
                // Önce sınıfı bul
                var classItem = await _context.Classes.FindAsync(classId);
                if (classItem == null)
                {
                    return Json(new { success = false, message = "Sınıf bulunamadı. ClassId: " + classId });
                }

                // Sınıftaki tüm öğrencileri bul ve sil
                var students = await _context.Students
                    .Where(s => s.ClassId == classId)
                    .ToListAsync();

                if (students.Any())
                {
                    _context.Students.RemoveRange(students);
                    await _context.SaveChangesAsync();
                }

                // Sınıfın öğrenci sayısını sıfırla
                classItem.StudentCount = 0;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tüm öğrenciler başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudent(int studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return Json(new { success = false, message = "Öğrenci bulunamadı." });
                }

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

                return Json(new { success = true, message = "Öğrenci başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
} 