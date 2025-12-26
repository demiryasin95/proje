using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StudentStudyProgram.Models;
using Microsoft.AspNet.Identity;
using StudentStudyProgram.Infrastructure;

namespace StudentStudyProgram.Controllers
{
    [Authorize]
    public class NotesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Notes
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetNotes(string category = "")
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var currentUser = db.Users.Find(userId);

                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı!" }, JsonRequestBehavior.AllowGet);
                }

                // Get student ID for current user
                var student = db.Students.FirstOrDefault(s => s.UserId == userId);
                
                if (student == null && !User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Öğrenci kaydı bulunamadı!" }, JsonRequestBehavior.AllowGet);
                }

                var notesQuery = db.StudyNotes
                    .Include(n => n.Student)
                    .AsQueryable();

                // Students can only see their own notes
                if (!User.IsInRole("Admin"))
                {
                    notesQuery = notesQuery.Where(n => n.StudentId == student.Id);
                }

                // Filter by category if provided
                if (!string.IsNullOrEmpty(category))
                {
                    notesQuery = notesQuery.Where(n => n.Category == category);
                }

                var rawNotes = await notesQuery
                    .OrderByDescending(n => n.UpdatedAt)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Content,
                        n.Category,
                        n.CreatedAt,
                        n.UpdatedAt,
                        StudentFirstName = n.Student.FirstName,
                        StudentLastName = n.Student.LastName
                    })
                    .ToListAsync();

                var notes = rawNotes.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    content = n.Content,
                    shortContent = n.Content != null && n.Content.Length > 100 ? n.Content.Substring(0, 100) + "..." : n.Content,
                    category = n.Category,
                    createdAt = n.CreatedAt,
                    updatedAt = n.UpdatedAt,
                    studentName = n.StudentFirstName + " " + n.StudentLastName
                }).ToList();

                return Json(new { success = true, notes = notes }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get notes error");
                return Json(new { success = false, message = "Notlar yüklenirken bir hata oluştu. Lütfen tekrar deneyin." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult CreateNote(string title, string content, string category)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var student = db.Students.FirstOrDefault(s => s.UserId == userId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Öğrenci kaydı bulunamadı!" });
                }

                var note = new StudyNote
                {
                    StudentId = student.Id,
                    Title = title,
                    Content = content,
                    Category = category ?? "Genel",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                db.StudyNotes.Add(note);
                db.SaveChanges();

                return Json(new { success = true, message = "Not başarıyla oluşturuldu!", noteId = note.Id });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Create note error");
                return Json(new { success = false, message = "Not oluşturulurken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult UpdateNote(int id, string title, string content, string category)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var student = db.Students.FirstOrDefault(s => s.UserId == userId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Öğrenci kaydı bulunamadı!" });
                }

                var note = db.StudyNotes.Find(id);

                if (note == null)
                {
                    return Json(new { success = false, message = "Not bulunamadı!" });
                }

                // Students can only update their own notes
                if (note.StudentId != student.Id && !User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Bu notu düzenleme yetkiniz yok!" });
                }

                note.Title = title;
                note.Content = content;
                note.Category = category ?? note.Category;
                note.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                return Json(new { success = true, message = "Not başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Update note error");
                return Json(new { success = false, message = "Not güncellenirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult DeleteNote(int id)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var student = db.Students.FirstOrDefault(s => s.UserId == userId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Öğrenci kaydı bulunamadı!" });
                }

                var note = db.StudyNotes.Find(id);

                if (note == null)
                {
                    return Json(new { success = false, message = "Not bulunamadı!" });
                }

                // Students can only delete their own notes
                if (note.StudentId != student.Id && !User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Bu notu silme yetkiniz yok!" });
                }

                db.StudyNotes.Remove(note);
                db.SaveChanges();

                return Json(new { success = true, message = "Not başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Delete note error");
                return Json(new { success = false, message = "Not silinirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpGet]
        public JsonResult GetCategories()
        {
            try
            {
                // Get all unique teacher branches as categories
                var categories = db.Teachers
                    .Where(t => !string.IsNullOrEmpty(t.Branch))
                    .Select(t => t.Branch)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();

                // If no teacher branches exist, return default categories
                if (!categories.Any())
                {
                    categories = new List<string>
                    {
                        "Matematik",
                        "Türkçe",
                        "İngilizce",
                        "Fen Bilgisi",
                        "Sosyal Bilgiler",
                        "Fizik",
                        "Kimya",
                        "Biyoloji",
                        "Tarih",
                        "Coğrafya",
                        "Edebiyat",
                        "Geometri",
                        "Diğer"
                    };
                }

                return Json(new { success = true, categories = categories }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get categories error");
                return Json(new { success = false, message = "Kategoriler yüklenirken bir hata oluştu. Lütfen tekrar deneyin." }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
