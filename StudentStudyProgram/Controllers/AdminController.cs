using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using StudentStudyProgram.Infrastructure;
using StudentStudyProgram.Models;
using StudentStudyProgram.Services;

namespace StudentStudyProgram.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private ApplicationDbContext db;

        public AdminController()
        {
            db = new ApplicationDbContext();
        }

        private UserManager<ApplicationUser> CreateUserManager()
        {
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            
            // Configure password validator
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 12,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };
            
            // Configure user token provider for password reset
            manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(
                new Microsoft.Owin.Security.DataProtection.DpapiDataProtectionProvider().Create("ASP.NET Identity"));
            
            return manager;
        }

        private string ResolvePhotoUrl(string photoPath, string gender)
        {
            var fallback = string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase)
                ? "/Content/Images/default-female.png"
                : "/Content/Images/default-male.png";

            var raw = string.IsNullOrWhiteSpace(photoPath) ? fallback : photoPath;

            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return raw;
            }

            if (raw.StartsWith("~"))
            {
                return Url.Content(raw);
            }

            if (!raw.StartsWith("/"))
            {
                raw = "/" + raw;
            }

            return Url.Content("~" + raw);
        }

        public ActionResult Index()
        {
            try
            {
                Logger.LogInfo($"Admin Index page accessed by user: {User.Identity.Name}");
                return View();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading Admin Index page");
                throw;
            }
        }

        #region Student Management
        public ActionResult Students()
        {
            var list = db.Students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
            return View(list);
        }

        [HttpGet]
        public async Task<JsonResult> GetStudents()
        {
            try
            {
                var students = await db.Students
                    .Include(s => s.User)
                    .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
                    .ToListAsync();

                var result = students.Select(s => new
                {
                    id = s.Id,
                    fullName = s.FirstName + " " + s.LastName,
                    className = s.ClassName,
                    phone = s.PhoneNumber,
                    email = s.Email,
                    gender = s.Gender,
                    photo = ResolvePhotoUrl(s.PhotoPath, s.Gender),
                    userId = s.UserId,
                    userName = !string.IsNullOrEmpty(s.UserId) && s.User != null ? s.User.UserName : null,
                    createdAt = s.CreatedAt,
                    createdAtStr = s.CreatedAt.ToString("yyyy-MM-dd"),
                    createdAtDisplay = s.CreatedAt.ToString("dd.MM.yyyy")
                }).ToList();

                return Json(new { success = true, students = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Öğrenciler yüklenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> AddStudent(Student student, HttpPostedFileBase photo)
        {
            try
            {
                // Clean phone number (remove spaces and non-digits)
                if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
                {
                    student.PhoneNumber = new string(student.PhoneNumber.Where(char.IsDigit).ToArray());
                }
                
                if (ModelState.IsValid)
                {
                    if (photo != null && photo.ContentLength > 0)
                    {
                        // Validate uploaded file
                        var validationResult = FileValidationHelper.ValidateImageFile(photo);
                        if (!validationResult.IsValid)
                        {
                            return Json(new { success = false, message = validationResult.ErrorMessage });
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                        var path = Path.Combine(Server.MapPath("~/Content/Images/Students/"), fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        photo.SaveAs(path);
                        student.PhotoPath = "/Content/Images/Students/" + fileName;
                    }

                    student.CreatedAt = DateTime.Now;
                    db.Students.Add(student);
                    await db.SaveChangesAsync();

                    Logger.LogInfo($"Öğrenci eklendi: {student.FirstName} {student.LastName} (ID: {student.Id})");
                    return Json(new { success = true, message = "Öğrenci başarıyla eklendi!" });
                }

                return Json(new { success = false, message = "Lütfen tüm zorunlu alanları doldurun!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Öğrenci eklenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UpdateStudent(Student student, HttpPostedFileBase photo)
        {
            try
            {
                // Clean phone number (remove spaces and non-digits)
                if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
                {
                    student.PhoneNumber = new string(student.PhoneNumber.Where(char.IsDigit).ToArray());
                }
                
                var existingStudent = await db.Students.FindAsync(student.Id);
                if (existingStudent == null)
                {
                    return Json(new { success = false, message = "Öğrenci bulunamadı!" });
                }

                if (photo != null && photo.ContentLength > 0)
                {
                    // Validate uploaded file
                    var validationResult = FileValidationHelper.ValidateImageFile(photo);
                    if (!validationResult.IsValid)
                    {
                        return Json(new { success = false, message = validationResult.ErrorMessage });
                    }

                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(existingStudent.PhotoPath))
                    {
                        var oldPhotoPath = Server.MapPath("~" + existingStudent.PhotoPath);
                        if (System.IO.File.Exists(oldPhotoPath))
                        {
                            System.IO.File.Delete(oldPhotoPath);
                        }
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images/Students/"), fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    photo.SaveAs(path);
                    existingStudent.PhotoPath = "/Content/Images/Students/" + fileName;
                }

                existingStudent.FirstName = student.FirstName ?? existingStudent.FirstName;
                existingStudent.LastName = student.LastName ?? existingStudent.LastName;
                existingStudent.ClassName = !string.IsNullOrEmpty(student.ClassName) ? student.ClassName : existingStudent.ClassName;
                existingStudent.PhoneNumber = student.PhoneNumber;
                existingStudent.Email = student.Email;
                existingStudent.Gender = student.Gender ?? existingStudent.Gender;

                await db.SaveChangesAsync();

                Logger.LogInfo($"Öğrenci güncellendi: {student.FirstName} {student.LastName} (ID: {student.Id})");
                return Json(new { success = true, message = "Öğrenci başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Öğrenci güncellenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> DeleteStudent(int id, bool force = false)
        {
            try
            {
                var student = await db.Students.FindAsync(id);
                if (student == null)
                {
                    return Json(new { success = false, message = "Öğrenci bulunamadı!" });
                }

                var hasSessions = await db.StudySessions.AnyAsync(s => s.StudentId == id);
                if (hasSessions && !force)
                {
                    return Json(new { success = false, message = "Öğrenciye bağlı etüt kayıtları var. Silmek için onaylayın." });
                }

                if (hasSessions)
                {
                    var sessions = await db.StudySessions.Where(s => s.StudentId == id).ToListAsync();
                    if (sessions.Count > 0)
                    {
                        db.StudySessions.RemoveRange(sessions);
                        await db.SaveChangesAsync();
                    }
                }

                if (!string.IsNullOrEmpty(student.PhotoPath))
                {
                    var photoPath = Server.MapPath("~" + student.PhotoPath);
                    if (System.IO.File.Exists(photoPath))
                    {
                        System.IO.File.Delete(photoPath);
                    }
                }

                db.Students.Remove(student);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Öğrenci başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Öğrenci silinirken hata (ID: {id})");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }
        #endregion

        #region Student Accounts (Identity link)
        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> CreateStudentAccount(int studentId, string username, string password, string email = null)
        {
            try
            {
                if (!User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Yetkisiz işlem." });
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return Json(new { success = false, message = "Kullanıcı adı ve parola zorunludur." });
                }

                // Input validation
                if (username.Length < 3 || username.Length > 50)
                {
                    return Json(new { success = false, message = "Kullanıcı adı 3-50 karakter arasında olmalıdır." });
                }

                if (!string.IsNullOrEmpty(email))
                {
                    var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                    if (!emailRegex.IsMatch(email))
                    {
                        return Json(new { success = false, message = "Geçersiz email formatı." });
                    }
                }

                var student = await db.Students.FindAsync(studentId);
                if (student == null)
                {
                    return Json(new { success = false, message = "Öğrenci bulunamadı." });
                }

                if (!string.IsNullOrEmpty(student.UserId))
                {
                    return Json(new { success = false, message = "Bu öğrenci zaten bir hesaba bağlı." });
                }

                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
                if (!roleManager.RoleExists("Student"))
                {
                    roleManager.Create(new IdentityRole("Student"));
                }

                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

                var existing = await userManager.FindByNameAsync(username);
                if (existing != null)
                {
                    return Json(new { success = false, message = "Bu kullanıcı adı zaten kullanılıyor." });
                }

                var user = new ApplicationUser
                {
                    UserName = username.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                    EmailConfirmed = true
                };

                var createRes = await userManager.CreateAsync(user, password);
                if (!createRes.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(" ", createRes.Errors) });
                }

                await userManager.AddToRoleAsync(user.Id, "Student");

                student.UserId = user.Id;
                
                // Clean phone number before validation
                if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
                {
                    student.PhoneNumber = new string(student.PhoneNumber.Where(char.IsDigit).ToArray());
                }
                
                // Validate student before saving
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(student);
                bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                    student, validationContext, validationResults, true);
                
                if (!isValid)
                {
                    var errors = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
                    return Json(new { success = false, message = "Öğrenci kaydı geçersiz: " + errors });
                }
                
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Öğrenci hesabı oluşturuldu ve bağlandı." });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                // Collect detailed validation errors
                var errorMessages = new System.Text.StringBuilder();
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.AppendLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                    }
                }
                
                Logger.LogError(dbEx, "Öğrenci hesabı oluşturma validation hatası: " + errorMessages.ToString());
                return Json(new { success = false, message = errorMessages.ToString() });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? " Inner: " + ex.InnerException.Message : "";
                Logger.LogError(ex, "Öğrenci hesabı oluşturma hatası" + innerMsg);
                return Json(new { success = false, message = "Hesap oluşturulurken bir hata oluştu. Lütfen bilgileri kontrol edip tekrar deneyin." });
            }
        }
        #endregion

        #region Account Admin Ops (Reset/Unlink)
        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> ResetStudentPassword(int studentId, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 6)
                {
                    return Json(new { success = false, message = "Parola en az 6 karakter olmalıdır." });
                }

                var student = await db.Students.FindAsync(studentId);
                if (student == null) return Json(new { success = false, message = "Öğrenci bulunamadı." });
                if (string.IsNullOrEmpty(student.UserId)) return Json(new { success = false, message = "Bu öğrenciye bağlı hesap yok." });

                var userManager = CreateUserManager();
                var token = await userManager.GeneratePasswordResetTokenAsync(student.UserId);
                var res = await userManager.ResetPasswordAsync(student.UserId, token, newPassword.Trim());
                if (!res.Succeeded) return Json(new { success = false, message = string.Join(" ", res.Errors) });

                await AuditLogService.TryLogAsync(db, User?.Identity?.Name, User.Identity.GetUserId(), "ResetPassword", "Student", student.Id, "Student password reset by admin");
                return Json(new { success = true, message = "Öğrenci parolası sıfırlandı." });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Reset password error");
                return Json(new { success = false, message = "Parola sıfırlanırken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UnlinkStudentAccount(int studentId)
        {
            try
            {
                var student = await db.Students.FindAsync(studentId);
                if (student == null) return Json(new { success = false, message = "Öğrenci bulunamadı." });
                if (string.IsNullOrEmpty(student.UserId)) return Json(new { success = false, message = "Bu öğrenciye bağlı hesap yok." });

                var userId = student.UserId;
                student.UserId = null;
                await db.SaveChangesAsync();

                var userManager = CreateUserManager();
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(100);
                    await userManager.UpdateAsync(user);
                }

                await AuditLogService.TryLogAsync(db, User?.Identity?.Name, User.Identity.GetUserId(), "UnlinkAccount", "Student", student.Id, "Student account unlinked and locked by admin");
                return Json(new { success = true, message = "Öğrenci hesabı bağlantısı kaldırıldı (hesap kilitlendi)." });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unlink account error");
                return Json(new { success = false, message = "Bağlantı kaldırılırken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> ResetTeacherPassword(int teacherId, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 6)
                {
                    return Json(new { success = false, message = "Parola en az 6 karakter olmalıdır." });
                }

                var teacher = await db.Teachers.FindAsync(teacherId);
                if (teacher == null) return Json(new { success = false, message = "Öğretmen bulunamadı." });
                if (string.IsNullOrEmpty(teacher.UserId)) return Json(new { success = false, message = "Bu öğretmene bağlı hesap yok." });

                var userManager = CreateUserManager();
                var token = await userManager.GeneratePasswordResetTokenAsync(teacher.UserId);
                var res = await userManager.ResetPasswordAsync(teacher.UserId, token, newPassword.Trim());
                if (!res.Succeeded) return Json(new { success = false, message = string.Join(" ", res.Errors) });

                await AuditLogService.TryLogAsync(db, User?.Identity?.Name, User.Identity.GetUserId(), "ResetPassword", "Teacher", teacher.Id, "Teacher password reset by admin");
                return Json(new { success = true, message = "Öğretmen parolası sıfırlandı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Parola sıfırlanırken hata: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UnlinkTeacherAccount(int teacherId)
        {
            try
            {
                var teacher = await db.Teachers.FindAsync(teacherId);
                if (teacher == null) return Json(new { success = false, message = "Öğretmen bulunamadı." });
                if (string.IsNullOrEmpty(teacher.UserId)) return Json(new { success = false, message = "Bu öğretmene bağlı hesap yok." });

                var userId = teacher.UserId;
                teacher.UserId = null;
                await db.SaveChangesAsync();

                var userManager = CreateUserManager();
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEndDateUtc = DateTime.UtcNow.AddYears(100);
                    await userManager.UpdateAsync(user);
                }

                await AuditLogService.TryLogAsync(db, User?.Identity?.Name, User.Identity.GetUserId(), "UnlinkAccount", "Teacher", teacher.Id, "Teacher account unlinked and locked by admin");
                return Json(new { success = true, message = "Öğretmen hesabı bağlantısı kaldırıldı (hesap kilitlendi)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bağlantı kaldırılırken hata: " + ex.Message });
            }
        }
        #endregion

        #region Teacher Management
        public ActionResult Teachers()
        {
            var list = db.Teachers.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToList();
            return View(list);
        }

        [HttpGet]
        public async Task<JsonResult> GetTeachers()
        {
            try
            {
                var baseTeachers = await db.Teachers
                    .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
                    .Select(t => new
                    {
                        t.Id,
                        t.FirstName,
                        t.LastName,
                        t.Branch,
                        t.PhoneNumber,
                        t.Gender,
                        t.PhotoPath,
                        t.UserId,
                        t.CreatedAt
                    })
                    .ToListAsync();

                var firstSessionDates = await db.StudySessions
                    .GroupBy(s => s.TeacherId)
                    .Select(g => new { TeacherId = g.Key, FirstDate = g.Min(s => s.SessionDate) })
                    .ToListAsync();
                var firstDateMap = firstSessionDates.ToDictionary(x => x.TeacherId, x => x.FirstDate);

                var teachers = baseTeachers.Select(t => new
                {
                    id = t.Id,
                    fullName = t.FirstName + " " + t.LastName,
                    branch = t.Branch,
                    phone = t.PhoneNumber,
                    gender = t.Gender,
                    photo = ResolvePhotoUrl(t.PhotoPath, t.Gender),
                    userId = t.UserId,
                    createdAt = t.CreatedAt,
                    createdAtStr = (t.CreatedAt != default(DateTime) ? t.CreatedAt.ToString("yyyy-MM-dd")
                                   : (firstDateMap.ContainsKey(t.Id) ? firstDateMap[t.Id].ToString("yyyy-MM-dd") : null)),
                    createdAtDisplay = (t.CreatedAt != default(DateTime) ? t.CreatedAt.ToString("dd.MM.yyyy")
                                       : (firstDateMap.ContainsKey(t.Id) ? firstDateMap[t.Id].ToString("dd.MM.yyyy") : null))
                }).ToList();

                return Json(new { success = true, teachers = teachers }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Öğretmenler yüklenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> AddTeacher(Teacher teacher, HttpPostedFileBase photo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (photo != null && photo.ContentLength > 0)
                    {
                        // Validate uploaded file
                        var validationResult = FileValidationHelper.ValidateImageFile(photo);
                        if (!validationResult.IsValid)
                        {
                            return Json(new { success = false, message = validationResult.ErrorMessage });
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                        var path = Path.Combine(Server.MapPath("~/Content/Images/Teachers/"), fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        photo.SaveAs(path);
                        teacher.PhotoPath = "/Content/Images/Teachers/" + fileName;
                    }

                    teacher.CreatedAt = DateTime.Now;
                    db.Teachers.Add(teacher);
                    await db.SaveChangesAsync();

                    Logger.LogInfo($"Öğretmen eklendi: {teacher.FirstName} {teacher.LastName} (ID: {teacher.Id})");
                    return Json(new { success = true, message = "Öğretmen başarıyla eklendi!" });
                }

                return Json(new { success = false, message = "Lütfen tüm zorunlu alanları doldurun!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Öğretmen eklenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UpdateTeacher(Teacher teacher, HttpPostedFileBase photo)
        {
            try
            {
                var existingTeacher = await db.Teachers.FindAsync(teacher.Id);
                if (existingTeacher == null)
                {
                    return Json(new { success = false, message = "Öğretmen bulunamadı!" });
                }

                if (photo != null && photo.ContentLength > 0)
                {
                    // Validate uploaded file
                    var validationResult = FileValidationHelper.ValidateImageFile(photo);
                    if (!validationResult.IsValid)
                    {
                        return Json(new { success = false, message = validationResult.ErrorMessage });
                    }

                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(existingTeacher.PhotoPath))
                    {
                        var oldPhotoPath = Server.MapPath("~" + existingTeacher.PhotoPath);
                        if (System.IO.File.Exists(oldPhotoPath))
                        {
                            System.IO.File.Delete(oldPhotoPath);
                        }
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images/Teachers/"), fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    photo.SaveAs(path);
                    existingTeacher.PhotoPath = "/Content/Images/Teachers/" + fileName;
                }

                existingTeacher.FirstName = teacher.FirstName ?? existingTeacher.FirstName;
                existingTeacher.LastName = teacher.LastName ?? existingTeacher.LastName;
                existingTeacher.Branch = !string.IsNullOrEmpty(teacher.Branch) ? teacher.Branch : existingTeacher.Branch;
                existingTeacher.PhoneNumber = teacher.PhoneNumber;
                existingTeacher.Gender = teacher.Gender ?? existingTeacher.Gender;

                await db.SaveChangesAsync();

                Logger.LogInfo($"Öğretmen güncellendi: {teacher.FirstName} {teacher.LastName} (ID: {teacher.Id})");
                return Json(new { success = true, message = "Öğretmen başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Öğretmen güncellenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> DeleteTeacher(int id, bool force = false)
        {
            try
            {
                var teacher = await db.Teachers.FindAsync(id);
                if (teacher == null)
                {
                    return Json(new { success = false, message = "Öğretmen bulunamadı!" });
                }

                var hasSessions = await db.StudySessions.AnyAsync(s => s.TeacherId == id);
                var hasAvailabilities = await db.TeacherAvailabilities.AnyAsync(a => a.TeacherId == id);

                if ((hasSessions || hasAvailabilities) && !force)
                {
                    return Json(new { success = false, message = "Öğretmene bağlı etüt/müsaitlik kayıtları var. Silmek için onaylayın." });
                }

                if (hasSessions)
                {
                    var sessions = await db.StudySessions.Where(s => s.TeacherId == id).ToListAsync();
                    if (sessions.Count > 0)
                    {
                        db.StudySessions.RemoveRange(sessions);
                        await db.SaveChangesAsync();
                    }
                }

                if (hasAvailabilities)
                {
                    var avs = await db.TeacherAvailabilities.Where(a => a.TeacherId == id).ToListAsync();
                    if (avs.Count > 0)
                    {
                        db.TeacherAvailabilities.RemoveRange(avs);
                        await db.SaveChangesAsync();
                    }
                }

                if (!string.IsNullOrEmpty(teacher.PhotoPath))
                {
                    var photoPath = Server.MapPath("~" + teacher.PhotoPath);
                    if (System.IO.File.Exists(photoPath))
                    {
                        System.IO.File.Delete(photoPath);
                    }
                }

                db.Teachers.Remove(teacher);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Öğretmen başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Öğretmen silinirken hata (ID: {id})");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }
        #endregion

        #region Teacher Accounts (Identity link)
        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> CreateTeacherAccount(int teacherId, string username, string password, string email = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return Json(new { success = false, message = "Kullanıcı adı ve parola zorunludur." });
                }

                var teacher = await db.Teachers.FindAsync(teacherId);
                if (teacher == null)
                {
                    return Json(new { success = false, message = "Öğretmen bulunamadı." });
                }

                if (!string.IsNullOrEmpty(teacher.UserId))
                {
                    return Json(new { success = false, message = "Bu öğretmen zaten bir hesaba bağlı." });
                }

                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
                if (!roleManager.RoleExists("Teacher"))
                {
                    roleManager.Create(new IdentityRole("Teacher"));
                }

                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
                var existing = await userManager.FindByNameAsync(username);
                if (existing != null)
                {
                    return Json(new { success = false, message = "Bu kullanıcı adı zaten kullanılıyor." });
                }

                var user = new ApplicationUser
                {
                    UserName = username.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                    EmailConfirmed = true
                };

                var createRes = await userManager.CreateAsync(user, password);
                if (!createRes.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(" ", createRes.Errors) });
                }

                await userManager.AddToRoleAsync(user.Id, "Teacher");

                teacher.UserId = user.Id;
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Öğretmen hesabı oluşturuldu ve bağlandı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hesap oluşturulurken hata: " + ex.Message });
            }
        }
        #endregion

        #region Time Slot Management
        public ActionResult TimeSlots()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetTimeSlots()
        {
            try
            {
                var baseSlots = await db.TimeSlots
                    .OrderBy(t => t.StartTime)
                    .ToListAsync();

                var timeSlots = baseSlots.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    startTime = string.Format("{0:D2}:{1:D2}", t.StartTime.Hours, t.StartTime.Minutes),
                    endTime = string.Format("{0:D2}:{1:D2}", t.EndTime.Hours, t.EndTime.Minutes),
                    isBreak = t.IsBreak,
                    isLunch = t.IsLunch
                }).ToList();

                return Json(new { success = true, timeSlots = timeSlots }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ders saatleri yüklenirken hata");
                return Json(new { success = false, message = "Ders saatleri yüklenirken hata oluştu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> AddTimeSlot(TimeSlot timeSlot)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.TimeSlots.Add(timeSlot);
                    await db.SaveChangesAsync();

                    return Json(new { success = true, message = "Ders saati başarıyla eklendi!" });
                }

                return Json(new { success = false, message = "Lütfen tüm zorunlu alanları doldurun!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ders saati eklenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UpdateTimeSlot(TimeSlot timeSlot)
        {
            try
            {
                var existingTimeSlot = await db.TimeSlots.FindAsync(timeSlot.Id);
                if (existingTimeSlot == null)
                {
                    return Json(new { success = false, message = "Ders saati bulunamadı!" });
                }

                existingTimeSlot.Name = timeSlot.Name;
                existingTimeSlot.StartTime = timeSlot.StartTime;
                existingTimeSlot.EndTime = timeSlot.EndTime;
                existingTimeSlot.IsBreak = timeSlot.IsBreak;
                existingTimeSlot.IsLunch = timeSlot.IsLunch;

                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Ders saati başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ders saati güncellenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> DeleteTimeSlot(int id)
        {
            try
            {
                var timeSlot = await db.TimeSlots.FindAsync(id);
                if (timeSlot == null)
                {
                    return Json(new { success = false, message = "Ders saati bulunamadı!" });
                }

                db.TimeSlots.Remove(timeSlot);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Ders saati başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ders saati silinirken hata oluştu: " + ex.Message });
            }
        }
        #endregion

        #region Classroom Management
        public ActionResult Classrooms()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetClassrooms()
        {
            try
            {
                var classrooms = await db.Classrooms
                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        type = c.Type,
                        capacity = c.Capacity
                    })
                    .ToListAsync();

                return Json(new { success = true, classrooms = classrooms }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Derslikler yüklenirken hata oluştu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> AddClassroom(Classroom classroom)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Classrooms.Add(classroom);
                    await db.SaveChangesAsync();

                    return Json(new { success = true, message = "Derslik başarıyla eklendi!" });
                }

                return Json(new { success = false, message = "Lütfen tüm zorunlu alanları doldurun!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Derslik eklenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UpdateClassroom(Classroom classroom)
        {
            try
            {
                var existingClassroom = await db.Classrooms.FindAsync(classroom.Id);
                if (existingClassroom == null)
                {
                    return Json(new { success = false, message = "Derslik bulunamadı!" });
                }

                existingClassroom.Name = classroom.Name;
                existingClassroom.Type = classroom.Type;
                existingClassroom.Capacity = classroom.Capacity;

                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Derslik başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Derslik güncellenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> DeleteClassroom(int id)
        {
            try
            {
                var classroom = await db.Classrooms.FindAsync(id);
                if (classroom == null)
                {
                    return Json(new { success = false, message = "Derslik bulunamadı!" });
                }

                db.Classrooms.Remove(classroom);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Derslik başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Derslik silinirken hata oluştu: " + ex.Message });
            }
        }
        #endregion

        #region Statistics
        public ActionResult Statistics()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetStatistics()
        {
            try
            {
                // Fix N+1 query problem: Use GroupBy to get all stats in one query
                var teacherSessionGroups = await db.StudySessions
                    .GroupBy(s => s.TeacherId)
                    .Select(g => new
                    {
                        TeacherId = g.Key,
                        TotalSessions = g.Count(),
                        AttendedSessions = g.Count(s => s.AttendanceStatus == "Attended"),
                        NotAttendedSessions = g.Count(s => s.AttendanceStatus == "NotAttended")
                    })
                    .ToListAsync();

                var teachers = await db.Teachers.ToListAsync();
                var teacherStats = teachers.Select(t =>
                {
                    var stats = teacherSessionGroups.FirstOrDefault(g => g.TeacherId == t.Id);
                    var total = stats?.TotalSessions ?? 0;
                    var notAttended = stats?.NotAttendedSessions ?? 0;

                    return new
                    {
                        teacherName = t.FirstName + " " + t.LastName,
                        totalSessions = total,
                        attendedSessions = stats?.AttendedSessions ?? 0,
                        absenceRate = total > 0 ? (double)notAttended / total * 100 : 0
                    };
                }).ToList();

                // Fix N+1 query problem for students
                var studentSessionGroups = await db.StudySessions
                    .GroupBy(s => s.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        TotalSessions = g.Count(),
                        AttendedSessions = g.Count(s => s.AttendanceStatus == "Attended"),
                        NotAttendedSessions = g.Count(s => s.AttendanceStatus == "NotAttended")
                    })
                    .ToListAsync();

                var students = await db.Students.ToListAsync();
                var studentStats = students.Select(s =>
                {
                    var stats = studentSessionGroups.FirstOrDefault(g => g.StudentId == s.Id);
                    var total = stats?.TotalSessions ?? 0;
                    var notAttended = stats?.NotAttendedSessions ?? 0;

                    return new
                    {
                        studentName = s.FirstName + " " + s.LastName,
                        totalSessions = total,
                        attendedSessions = stats?.AttendedSessions ?? 0,
                        absenceRate = total > 0 ? (double)notAttended / total * 100 : 0
                    };
                }).ToList();

                var today = DateTime.Today;
                var startOfToday = today.Date;
                var endOfToday = startOfToday.AddDays(1);
                var weekStart = startOfToday.AddDays(-7);
                var weekEnd = endOfToday;
                var firstOfMonth = new DateTime(today.Year, today.Month, 1);
                var nextMonth = firstOfMonth.AddMonths(1);

                var totalSessionsAll = await db.StudySessions.CountAsync();
                var attendedSessionsAll = await db.StudySessions.CountAsync(s => s.AttendanceStatus == "Attended");

                var mostActiveTeacher = await db.StudySessions
                    .GroupBy(s => s.TeacherId)
                    .Select(g => new { TeacherId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefaultAsync();

                string activeTeacherName = null;
                if (mostActiveTeacher != null)
                {
                    activeTeacherName = await db.Teachers
                        .Where(t => t.Id == mostActiveTeacher.TeacherId)
                        .Select(t => t.FirstName + " " + t.LastName)
                        .FirstOrDefaultAsync();
                }

                var mostPopularClassroom = await db.StudySessions
                    .GroupBy(s => s.ClassroomId)
                    .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefaultAsync();

                string popularClassroomName = null;
                if (mostPopularClassroom != null)
                {
                    popularClassroomName = await db.Classrooms
                        .Where(c => c.Id == mostPopularClassroom.ClassroomId)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync();
                }

                // Optimize: Get week stats in one query instead of 3 separate queries
                var weekSessions = await db.StudySessions
                    .Where(s => s.SessionDate >= weekStart && s.SessionDate < weekEnd)
                    .GroupBy(s => s.AttendanceStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var weekActive = weekSessions.FirstOrDefault(w => w.Status == "Pending")?.Count ?? 0;
                var weekCompleted = weekSessions.FirstOrDefault(w => w.Status == "Attended")?.Count ?? 0;
                var weekCancelled = weekSessions.FirstOrDefault(w => w.Status == "NotAttended")?.Count ?? 0;

                // Calculate average session duration
                var averageSessionDuration = await db.StudySessions
                    .Include(s => s.TimeSlot)
                    .Where(s => s.TimeSlot != null)
                    .Select(s => new
                    {
                        DurationMinutes = DbFunctions.DiffMinutes(s.TimeSlot.StartTime, s.TimeSlot.EndTime)
                    })
                    .ToListAsync();

                var avgDuration = averageSessionDuration.Any()
                    ? averageSessionDuration.Average(x => x.DurationMinutes ?? 60)
                    : 60;

                var generalStats = new
                {
                    totalStudents = await db.Students.CountAsync(),
                    totalTeachers = await db.Teachers.CountAsync(),
                    totalSessions = totalSessionsAll,
                    totalClassrooms = await db.Classrooms.CountAsync(),
                    todaySessions = await db.StudySessions.CountAsync(s => s.SessionDate >= startOfToday && s.SessionDate < endOfToday),
                    thisWeekSessions = await db.StudySessions.CountAsync(s => s.SessionDate >= weekStart && s.SessionDate < weekEnd),
                    thisMonthSessions = await db.StudySessions.CountAsync(s => s.SessionDate >= firstOfMonth && s.SessionDate < nextMonth),
                    averageAttendanceRate = totalSessionsAll > 0 ? (double)attendedSessionsAll / totalSessionsAll * 100 : 0,
                    popularClassroom = popularClassroomName,
                    activeTeacher = activeTeacherName,
                    activeSessions = weekActive,
                    completedSessions = weekCompleted,
                    cancelledSessions = weekCancelled,
                    averageSessionDuration = Math.Round(avgDuration)
                };

                var baseRecent = await db.StudySessions
                    .OrderByDescending(s => s.SessionDate)
                    .ThenByDescending(s => s.TimeSlot.StartTime)
                    .Take(5)
                    .Select(s => new
                    {
                        StudentFirst = s.Student.FirstName,
                        StudentLast = s.Student.LastName,
                        TeacherFirst = s.Teacher.FirstName,
                        TeacherLast = s.Teacher.LastName,
                        s.SessionDate,
                        Start = s.TimeSlot.StartTime,
                        End = s.TimeSlot.EndTime,
                        ClassroomName = s.Classroom.Name,
                        s.AttendanceStatus
                    })
                    .ToListAsync();

                var recentSessions = baseRecent.Select(r => new
                {
                    studentName = r.StudentFirst + " " + r.StudentLast,
                    teacherName = r.TeacherFirst + " " + r.TeacherLast,
                    sessionDateStr = r.SessionDate.ToString("yyyy-MM-dd"),
                    timeRange = string.Format("{0:hh\\:mm} - {1:hh\\:mm}", r.Start, r.End),
                    classroom = r.ClassroomName,
                    attendanceStatus = r.AttendanceStatus
                }).ToList();

                var weeklyActivity = new int[7];
                for (int i = 0; i < 7; i++)
                {
                    var dayStart = startOfToday.AddDays(-6 + i);
                    var dayEnd = dayStart.AddDays(1);
                    weeklyActivity[i] = await db.StudySessions.CountAsync(s => s.SessionDate >= dayStart && s.SessionDate < dayEnd);
                }

                return Json(new { success = true, teacherStats, studentStats, generalStats, recentSessions, weeklyActivity }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "İstatistikler yüklenirken hata oluştu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Database Backup
        public ActionResult DatabaseBackup()
        {
            return View();
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult BackupDatabase()
        {
            try
            {
                var backupFolder = Server.MapPath("~/App_Data/Backups/");
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                var backupFileName = $"StudyProgram_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var backupPath = Path.Combine(backupFolder, backupFileName);

                // Note: In a real application, you would connect to SQL Server and create a backup
                // This is a simplified version for demonstration
                System.IO.File.WriteAllText(backupPath, $"Database backup created at {DateTime.Now}");

                return Json(new { success = true, message = "Veritabanı yedekleme işlemi başlatıldı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Yedekleme sırasında hata oluştu: " + ex.Message });
            }
        }
        #endregion

        #region PDF Report Generation
        [HttpGet]
        public async Task<ActionResult> DownloadReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                if (!startDate.HasValue)
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                
                if (!endDate.HasValue)
                    endDate = DateTime.Now;

                // Collect report data
                var reportData = new ReportData
                {
                    StartDate = startDate.Value,
                    EndDate = endDate.Value
                };

                // General statistics
                reportData.TotalStudents = await db.Students.CountAsync();
                reportData.TotalTeachers = await db.Teachers.CountAsync();
                reportData.TotalClassrooms = await db.Classrooms.CountAsync();
                reportData.TotalSessions = await db.StudySessions
                    .CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate);

                reportData.AttendedSessions = await db.StudySessions
                    .CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate && s.AttendanceStatus == "Attended");
                
                reportData.PendingSessions = await db.StudySessions
                    .CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate && s.AttendanceStatus == "Pending");
                
                reportData.CancelledSessions = await db.StudySessions
                    .CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate && s.AttendanceStatus == "NotAttended");

                reportData.AttendanceRate = reportData.TotalSessions > 0
                    ? (double)reportData.AttendedSessions / reportData.TotalSessions * 100
                    : 0;

                // Top 10 Students
                var topStudents = await db.Students
                    .Select(s => new
                    {
                        Student = s,
                        TotalSessions = db.StudySessions.Count(ss => ss.StudentId == s.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate),
                        AttendedSessions = db.StudySessions.Count(ss => ss.StudentId == s.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate && ss.AttendanceStatus == "Attended")
                    })
                    .Where(x => x.TotalSessions > 0)
                    .OrderByDescending(x => x.TotalSessions)
                    .Take(10)
                    .ToListAsync();

                reportData.TopStudents = topStudents.Select(item => new TopStudentItem
                {
                    FullName = item.Student.FirstName + " " + item.Student.LastName,
                    ClassName = item.Student.ClassName,
                    TotalSessions = item.TotalSessions,
                    AttendedSessions = item.AttendedSessions,
                    AttendanceRate = item.TotalSessions > 0 ? (double)item.AttendedSessions / item.TotalSessions * 100 : 0
                }).ToList();

                // Top 10 Teachers
                var topTeachers = await db.Teachers
                    .Select(t => new
                    {
                        Teacher = t,
                        TotalSessions = db.StudySessions.Count(ss => ss.TeacherId == t.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate),
                        CompletedSessions = db.StudySessions.Count(ss => ss.TeacherId == t.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate && ss.AttendanceStatus == "Attended")
                    })
                    .Where(x => x.TotalSessions > 0)
                    .OrderByDescending(x => x.TotalSessions)
                    .Take(10)
                    .ToListAsync();

                reportData.TopTeachers = topTeachers.Select(item => new TopTeacherItem
                {
                    FullName = item.Teacher.FirstName + " " + item.Teacher.LastName,
                    Branch = item.Teacher.Branch,
                    TotalSessions = item.TotalSessions,
                    CompletedSessions = item.CompletedSessions,
                    SuccessRate = item.TotalSessions > 0 ? (double)item.CompletedSessions / item.TotalSessions * 100 : 0
                }).ToList();

                // Generate PDF
                var pdfService = new PdfReportService();
                byte[] pdfBytes = pdfService.GenerateSystemReport(reportData);

                // Return PDF file
                string fileName = $"EtutSistemi_Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PDF rapor oluşturma hatası");
                TempData["ErrorMessage"] = "Rapor oluşturulurken hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region Excel Export (Professional HTML Format)
        [HttpGet]
        public async Task<ActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                if (!startDate.HasValue)
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                
                if (!endDate.HasValue)
                    endDate = DateTime.Now;
                
                // Collect statistics
                var totalStudents = await db.Students.CountAsync();
                var totalTeachers = await db.Teachers.CountAsync();
                var totalClassrooms = await db.Classrooms.CountAsync();
                var totalSessions = await db.StudySessions.CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate);
                var attendedSessions = await db.StudySessions.CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate && s.AttendanceStatus == "Attended");
                var notAttendedSessions = await db.StudySessions.CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate && s.AttendanceStatus == "NotAttended");
                var pendingSessions = await db.StudySessions.CountAsync(s => s.SessionDate >= startDate && s.SessionDate <= endDate && s.AttendanceStatus == "Pending");
                var attendanceRate = totalSessions > 0 ? (double)attendedSessions / totalSessions * 100 : 0;
                
                // Top students
                var topStudents = await db.Students
                    .Select(s => new
                    {
                        Student = s,
                        TotalSessions = db.StudySessions.Count(ss => ss.StudentId == s.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate),
                        AttendedSessions = db.StudySessions.Count(ss => ss.StudentId == s.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate && ss.AttendanceStatus == "Attended")
                    })
                    .Where(x => x.TotalSessions > 0)
                    .OrderByDescending(x => x.TotalSessions)
                    .Take(10)
                    .ToListAsync();
                
                // Top teachers
                var topTeachers = await db.Teachers
                    .Select(t => new
                    {
                        Teacher = t,
                        TotalSessions = db.StudySessions.Count(ss => ss.TeacherId == t.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate),
                        AttendedSessions = db.StudySessions.Count(ss => ss.TeacherId == t.Id && ss.SessionDate >= startDate && ss.SessionDate <= endDate && ss.AttendanceStatus == "Attended")
                    })
                    .Where(x => x.TotalSessions > 0)
                    .OrderByDescending(x => x.TotalSessions)
                    .Take(10)
                    .ToListAsync();
                
                // Build Excel-compatible HTML file
                var html = new System.Text.StringBuilder();
                html.AppendLine("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel' xmlns='http://www.w3.org/TR/REC-html40'>");
                html.AppendLine("<head>");
                html.AppendLine("<meta http-equiv='Content-Type' content='text/html; charset=utf-8'>");
                html.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>Rapor</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Calibri, Arial, sans-serif; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; }");
                html.AppendLine("th { background-color: #9485E4; color: white; padding: 12px; text-align: left; font-weight: bold; border: 1px solid #7B6BC7; }");
                html.AppendLine("td { padding: 10px; border: 1px solid #E6E1FF; }");
                html.AppendLine(".header-cell { background-color: #9485E4; color: white; font-size: 20px; font-weight: bold; padding: 20px; text-align: center; border: 2px solid #7B6BC7; }");
                html.AppendLine(".info-cell { background-color: #F3F1FF; padding: 8px; border: 1px solid #E6E1FF; }");
                html.AppendLine(".label-cell { background-color: #E6E1FF; font-weight: bold; color: #5D4E99; padding: 10px; border: 1px solid #D0CBEF; width: 200px; }");
                html.AppendLine(".value-cell { background-color: #FDFCFF; padding: 10px; border: 1px solid #E6E1FF; font-weight: bold; }");
                html.AppendLine(".section-header { background-color: #7B6BC7; color: white; font-size: 16px; font-weight: bold; padding: 12px; text-align: center; border: 2px solid #6A5BAF; }");
                html.AppendLine(".rank-gold { background-color: #FFD700; color: #000; font-weight: bold; text-align: center; }");
                html.AppendLine(".rank-silver { background-color: #C0C0C0; color: #000; font-weight: bold; text-align: center; }");
                html.AppendLine(".rank-bronze { background-color: #CD7F32; color: #FFF; font-weight: bold; text-align: center; }");
                html.AppendLine(".rank-normal { background-color: #9485E4; color: white; font-weight: bold; text-align: center; }");
                html.AppendLine(".number-cell { text-align: right; font-weight: bold; color: #5D4E99; }");
                html.AppendLine(".percent-cell { background-color: #9485E4; color: white; font-weight: bold; text-align: center; }");
                html.AppendLine(".success-cell { background-color: #10B981; color: white; font-weight: bold; }");
                html.AppendLine(".warning-cell { background-color: #F59E0B; color: white; font-weight: bold; }");
                html.AppendLine(".danger-cell { background-color: #EF4444; color: white; font-weight: bold; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                
                // Header
                html.AppendLine("<table>");
                html.AppendLine("<tr><td colspan='8' class='header-cell'>📊 ETÜT SİSTEMİ RAPORU</td></tr>");
                html.AppendLine($"<tr><td colspan='8' class='info-cell' style='text-align: center;'><strong>Rapor Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm} | <strong>Dönem:</strong> {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}</td></tr>");
                html.AppendLine("<tr><td colspan='8'>&nbsp;</td></tr>");
                
                // Summary Statistics Table
                html.AppendLine("<tr><td colspan='8' class='section-header'>📈 GENEL İSTATİSTİKLER</td></tr>");
                html.AppendLine($"<tr><td colspan='2' class='label-cell'>👨‍🎓 Toplam Öğrenci</td><td colspan='2' class='value-cell'>{totalStudents}</td><td colspan='2' class='label-cell'>👨‍🏫 Toplam Öğretmen</td><td colspan='2' class='value-cell'>{totalTeachers}</td></tr>");
                html.AppendLine($"<tr><td colspan='2' class='label-cell'>🏫 Toplam Derslik</td><td colspan='2' class='value-cell'>{totalClassrooms}</td><td colspan='2' class='label-cell'>📚 Toplam Etüt</td><td colspan='2' class='value-cell'>{totalSessions}</td></tr>");
                html.AppendLine($"<tr><td colspan='2' class='label-cell'>✅ Tamamlanan Etütler</td><td colspan='2' class='value-cell success-cell'>{attendedSessions}</td><td colspan='2' class='label-cell'>⏳ Bekleyen Etütler</td><td colspan='2' class='value-cell warning-cell'>{pendingSessions}</td></tr>");
                html.AppendLine($"<tr><td colspan='2' class='label-cell'>❌ İptal Edilen Etütler</td><td colspan='2' class='value-cell danger-cell'>{notAttendedSessions}</td><td colspan='2' class='label-cell'>📊 Katılım Oranı</td><td colspan='2' class='value-cell percent-cell' style='font-size: 16px;'>{attendanceRate:F1}%</td></tr>");
                html.AppendLine("<tr><td colspan='8'>&nbsp;</td></tr>");
                
                // Top 10 Students
                html.AppendLine("<tr><td colspan='8' class='section-header'>🏆 EN AKTİF 10 ÖĞRENCİ</td></tr>");
                html.AppendLine("<tr><th>🏅 Sıra</th><th>👤 Ad Soyad</th><th>📖 Sınıf</th><th>📚 Toplam Etüt</th><th>✅ Katılım</th><th>❌ Devamsızlık</th><th colspan='2'>📊 Katılım Oranı</th></tr>");
                
                int rank = 1;
                foreach (var item in topStudents)
                {
                    var rate = item.TotalSessions > 0 ? (double)item.AttendedSessions / item.TotalSessions * 100 : 0;
                    var absence = item.TotalSessions - item.AttendedSessions;
                    string rankClass = rank == 1 ? "rank-gold" : rank == 2 ? "rank-silver" : rank == 3 ? "rank-bronze" : "rank-normal";
                    html.AppendLine($"<tr><td class='{rankClass}'>{rank}</td><td><strong>{item.Student.FirstName} {item.Student.LastName}</strong></td><td>{item.Student.ClassName}</td><td class='number-cell'>{item.TotalSessions}</td><td class='number-cell'>{item.AttendedSessions}</td><td class='number-cell'>{absence}</td><td colspan='2' class='percent-cell'>{rate:F1}%</td></tr>");
                    rank++;
                }
                html.AppendLine("<tr><td colspan='8'>&nbsp;</td></tr>");
                
                // Top 10 Teachers
                html.AppendLine("<tr><td colspan='8' class='section-header'>👨‍🏫 EN AKTİF 10 ÖĞRETMEN</td></tr>");
                html.AppendLine("<tr><th>🏅 Sıra</th><th>👤 Ad Soyad</th><th>📚 Branş</th><th>📋 Toplam Etüt</th><th>✅ Tamamlanan</th><th>❌ İptal</th><th colspan='2'>📊 Başarı Oranı</th></tr>");
                
                rank = 1;
                foreach (var item in topTeachers)
                {
                    var rate = item.TotalSessions > 0 ? (double)item.AttendedSessions / item.TotalSessions * 100 : 0;
                    var cancelled = item.TotalSessions - item.AttendedSessions;
                    string rankClass = rank == 1 ? "rank-gold" : rank == 2 ? "rank-silver" : rank == 3 ? "rank-bronze" : "rank-normal";
                    html.AppendLine($"<tr><td class='{rankClass}'>{rank}</td><td><strong>{item.Teacher.FirstName} {item.Teacher.LastName}</strong></td><td>{item.Teacher.Branch}</td><td class='number-cell'>{item.TotalSessions}</td><td class='number-cell'>{item.AttendedSessions}</td><td class='number-cell'>{cancelled}</td><td colspan='2' class='percent-cell'>{rate:F1}%</td></tr>");
                    rank++;
                }
                html.AppendLine("</table>");
                
                html.AppendLine("</body></html>");
                
                var htmlBytes = System.Text.Encoding.UTF8.GetBytes(html.ToString());
                string fileName = $"EtutSistemi_Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                return File(htmlBytes, "application/vnd.ms-excel", fileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Excel export hatası");
                TempData["ErrorMessage"] = "Excel dosyası oluşturulurken hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region Teacher Availability Management
        public ActionResult TeacherAvailability()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetTeacherAvailabilities(int? teacherId)
        {
            try
            {
                if (!teacherId.HasValue)
                {
                    return Json(new { success = true, availabilities = new List<object>() }, JsonRequestBehavior.AllowGet);
                }

                var availabilities = await db.TeacherAvailabilities
                    .Where(a => a.TeacherId == teacherId.Value)
                    .Include(a => a.Teacher)
                    .Include(a => a.TimeSlot)
                    .OrderBy(a => a.DayOfWeek)
                    .ThenBy(a => a.TimeSlot.StartTime)
                    .ToListAsync();

                var result = availabilities.Select(a => new
                {
                    id = a.Id,
                    teacherId = a.TeacherId,
                    teacherName = a.Teacher.FirstName + " " + a.Teacher.LastName,
                    dayOfWeek = a.DayOfWeek,
                    timeSlotId = a.TimeSlotId,
                    timeSlotName = a.TimeSlot.Name,
                    startTime = string.Format("{0:D2}:{1:D2}", a.TimeSlot.StartTime.Hours, a.TimeSlot.StartTime.Minutes),
                    endTime = string.Format("{0:D2}:{1:D2}", a.TimeSlot.EndTime.Hours, a.TimeSlot.EndTime.Minutes)
                }).ToList();

                return Json(new { success = true, availabilities = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Öğretmen müsaitlikleri yüklenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> SetTeacherAvailability(int teacherId, int dayOfWeek, int timeSlotId, bool isAvailable)
        {
            try
            {
                var existing = await db.TeacherAvailabilities
                    .FirstOrDefaultAsync(a => a.TeacherId == teacherId && a.DayOfWeek == dayOfWeek && a.TimeSlotId == timeSlotId);

                if (isAvailable)
                {
                    // Add availability if not exists
                    if (existing == null)
                    {
                        db.TeacherAvailabilities.Add(new TeacherAvailability
                        {
                            TeacherId = teacherId,
                            DayOfWeek = dayOfWeek,
                            TimeSlotId = timeSlotId
                        });
                        await db.SaveChangesAsync();
                        Logger.LogInfo($"Öğretmen müsaitlik eklendi: Teacher={teacherId}, Day={dayOfWeek}, TimeSlot={timeSlotId}");
                    }
                }
                else
                {
                    // Remove availability if exists
                    if (existing != null)
                    {
                        db.TeacherAvailabilities.Remove(existing);
                        await db.SaveChangesAsync();
                        Logger.LogInfo($"Öğretmen müsaitlik kaldırıldı: Teacher={teacherId}, Day={dayOfWeek}, TimeSlot={timeSlotId}");
                    }
                }

                return Json(new { success = true, message = "Müsaitlik durumu güncellendi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Müsaitlik güncellenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> BulkSetTeacherAvailability(int teacherId, string slotsJson)
        {
            try
            {
                Logger.LogInfo($"BulkSetTeacherAvailability başladı: TeacherId={teacherId}");
                Logger.LogInfo($"SlotsJson: {slotsJson}");
                
                // Parse slots from JSON
                var slots = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AvailabilitySlot>>(slotsJson);
                Logger.LogInfo($"Decode edilen slot sayısı: {slots?.Count ?? 0}");
                
                // Remove all existing availabilities for this teacher
                var existing = await db.TeacherAvailabilities
                    .Where(a => a.TeacherId == teacherId)
                    .ToListAsync();
                
                Logger.LogInfo($"Silinen mevcut kayıt sayısı: {existing.Count}");
                db.TeacherAvailabilities.RemoveRange(existing);

                // Add new availabilities
                int addedCount = 0;
                if (slots != null && slots.Any())
                {
                    foreach (var slot in slots)
                    {
                        db.TeacherAvailabilities.Add(new TeacherAvailability
                        {
                            TeacherId = teacherId,
                            DayOfWeek = slot.DayOfWeek,
                            TimeSlotId = slot.TimeSlotId
                        });
                        addedCount++;
                        Logger.LogInfo($"Eklenen müsaitlik: TeacherId={teacherId}, Day={slot.DayOfWeek}, TimeSlot={slot.TimeSlotId}");
                    }
                }

                await db.SaveChangesAsync();
                Logger.LogInfo($"SaveChanges başarılı! Toplam {addedCount} kayıt eklendi.");
                
                // Verify what was saved
                var savedCount = await db.TeacherAvailabilities.CountAsync(a => a.TeacherId == teacherId);
                Logger.LogInfo($"Veritabanında kayıtlı müsaitlik sayısı: {savedCount}");

                return Json(new { success = true, message = $"{addedCount} müsaitlik başarıyla kaydedildi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Toplu müsaitlik güncellenirken hata");
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu: " + ex.Message });
            }
        }
        #endregion

        #region User Management
        public ActionResult Users()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetUsers()
        {
            try
            {
                var userManager = CreateUserManager();
                var allUsers = await db.Users.ToListAsync();
                
                var students = await db.Students.Where(s => s.UserId != null).Select(s => s.UserId).ToListAsync();
                var teachers = await db.Teachers.Where(t => t.UserId != null).Select(t => t.UserId).ToListAsync();

                var users = allUsers.Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    email = u.Email,
                    emailConfirmed = u.EmailConfirmed,
                    profilePictureUrl = u.ProfilePictureUrl,
                    displayName = u.DisplayName,
                    locked = u.LockoutEnabled && u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc > DateTime.UtcNow,
                    roles = u.Roles.Select(r => db.Roles.FirstOrDefault(role => role.Id == r.RoleId)?.Name).ToList(),
                    linkedTo = students.Contains(u.Id) ? "Student"
                              : teachers.Contains(u.Id) ? "Teacher"
                              : null
                }).ToList();

                return Json(new { success = true, users = users }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Kullanıcılar yüklenirken hata");
                return Json(new { success = false, message = "Kullanıcılar yüklenirken hata oluştu." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UpdateUser(string userId, string email, string password, string[] roles, bool locked)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { success = false, message = "Kullanıcı ID'si gerekli." });
                }

                var userManager = CreateUserManager();
                var user = await userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                // Update email
                if (!string.IsNullOrWhiteSpace(email) && user.Email != email)
                {
                    user.Email = email;
                }

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user.Id);
                    var result = await userManager.ResetPasswordAsync(user.Id, token, password);
                    if (!result.Succeeded)
                    {
                        return Json(new { success = false, message = "Şifre güncellenemedi: " + string.Join(", ", result.Errors) });
                    }
                }

                // Update lockout status
                user.LockoutEnabled = true;
                user.LockoutEndDateUtc = locked ? (DateTime?)DateTime.UtcNow.AddYears(100) : null;

                await userManager.UpdateAsync(user);

                // Update roles
                if (roles != null)
                {
                    var currentRoles = await userManager.GetRolesAsync(userId);
                    await userManager.RemoveFromRolesAsync(userId, currentRoles.ToArray());
                    
                    foreach (var role in roles)
                    {
                        if (!string.IsNullOrWhiteSpace(role))
                        {
                            await userManager.AddToRoleAsync(userId, role);
                        }
                    }
                }

                Logger.LogInfo($"Kullanıcı güncellendi: {user.UserName} (ID: {userId})");
                return Json(new { success = true, message = "Kullanıcı başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Kullanıcı güncellenirken hata");
                return Json(new { success = false, message = "Kullanıcı güncellenirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UpdateUserProfilePicture(string userId, HttpPostedFileBase profilePicture)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { success = false, message = "Kullanıcı ID'si gerekli." });
                }

                var userManager = CreateUserManager();
                var user = await userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                if (profilePicture != null && profilePicture.ContentLength > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return Json(new { success = false, message = "Sadece JPG, PNG, GIF ve WEBP formatları kabul edilir" });
                    }

                    // Validate file size (max 5MB)
                    if (profilePicture.ContentLength > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "Dosya boyutu maksimum 5MB olabilir" });
                    }

                    // Create upload directory
                    var uploadDir = Server.MapPath("~/Content/Images/ProfilePictures/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        var oldPath = Server.MapPath("~" + user.ProfilePictureUrl);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Save new file
                    var fileName = userId + "_" + Guid.NewGuid().ToString("N") + fileExtension;
                    var filePath = Path.Combine(uploadDir, fileName);
                    profilePicture.SaveAs(filePath);

                    user.ProfilePictureUrl = "/Content/Images/ProfilePictures/" + fileName;
                    await userManager.UpdateAsync(user);

                    return Json(new {
                        success = true,
                        message = "Profil resmi güncellendi",
                        pictureUrl = user.ProfilePictureUrl
                    });
                }

                return Json(new { success = false, message = "Lütfen bir resim seçin" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Profil resmi güncellenirken hata");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> DeleteUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { success = false, message = "Kullanıcı ID'si gerekli." });
                }

                // Check if user is linked to student or teacher
                var linkedStudent = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                var linkedTeacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (linkedStudent != null || linkedTeacher != null)
                {
                    return Json(new { success = false, message = "Bu kullanıcı bir öğrenci/öğretmene bağlı. Önce bağlantıyı kaldırın." });
                }

                var userManager = CreateUserManager();
                var user = await userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                // Don't allow deleting current admin
                var currentUserId = User.Identity.GetUserId();
                if (userId == currentUserId)
                {
                    return Json(new { success = false, message = "Kendi hesabınızı silemezsiniz!" });
                }

                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }

                await AuditLogService.TryLogAsync(db, User?.Identity?.Name, currentUserId, "DeleteUser", "User", 0, $"User {user.UserName} deleted by admin");
                return Json(new { success = true, message = "Kullanıcı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Kullanıcı silinirken hata");
                return Json(new { success = false, message = "Kullanıcı silinirken hata oluştu: " + ex.Message });
            }
        }
        #endregion

        #region Dashboard API Endpoints
        [HttpGet]
        public async Task<JsonResult> GetTodaySessions()
        {
            try
            {
                var today = DateTime.Today;
                var count = await db.StudySessions.CountAsync(s => DbFunctions.TruncateTime(s.SessionDate) == today);
                return Json(new { success = true, count = count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting today's sessions count");
                return Json(new { success = false, count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetRecentSessions()
        {
            try
            {
                Logger.LogInfo("GetRecentSessions called");
                
                var sessionsQuery = db.StudySessions
                    .OrderByDescending(s => s.SessionDate)
                    .Take(10);
                
                var sessionsData = await sessionsQuery
                    .Select(s => new
                    {
                        StudentFirstName = s.Student != null ? s.Student.FirstName : "",
                        StudentLastName = s.Student != null ? s.Student.LastName : "",
                        TeacherFirstName = s.Teacher != null ? s.Teacher.FirstName : "",
                        TeacherLastName = s.Teacher != null ? s.Teacher.LastName : "",
                        ClassroomName = s.Classroom != null ? s.Classroom.Name : "",
                        SessionDate = s.SessionDate,
                        TimeSlotStart = s.TimeSlot != null ? s.TimeSlot.StartTime : (TimeSpan?)null
                    })
                    .ToListAsync();

                Logger.LogInfo($"GetRecentSessions found {sessionsData.Count} sessions");

                var result = sessionsData.Select(s => new
                {
                    studentName = !string.IsNullOrEmpty(s.StudentFirstName) ? $"{s.StudentFirstName} {s.StudentLastName}" : "N/A",
                    teacherName = !string.IsNullOrEmpty(s.TeacherFirstName) ? $"{s.TeacherFirstName} {s.TeacherLastName}" : "N/A",
                    classroomName = !string.IsNullOrEmpty(s.ClassroomName) ? s.ClassroomName : "N/A",
                    date = s.SessionDate.ToString("dd.MM.yyyy") + " " + (s.TimeSlotStart.HasValue ? s.TimeSlotStart.Value.ToString(@"hh\:mm") : "")
                }).ToList();

                return Json(new { success = true, sessions = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting recent sessions: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Logger.LogError(ex.InnerException, "Inner exception: " + ex.InnerException.Message);
                }
                return Json(new { success = false, message = ex.Message, sessions = new List<object>() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetWeeklySummary()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
                var monthStart = new DateTime(today.Year, today.Month, 1);

                var thisWeek = await db.StudySessions.CountAsync(s => s.SessionDate >= weekStart && s.SessionDate < tomorrow);
                var thisMonth = await db.StudySessions.CountAsync(s => s.SessionDate >= monthStart && s.SessionDate < tomorrow);
                var total = await db.StudySessions.CountAsync();

                // Get popular classroom - simplified approach
                var classroomName = "-";
                var classrooms = await db.StudySessions
                    .Select(s => s.ClassroomId)
                    .ToListAsync();
                
                if (classrooms.Any())
                {
                    var mostPopularClassroomId = classrooms
                        .GroupBy(x => x)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault();
                    
                    var classroom = await db.Classrooms.FindAsync(mostPopularClassroomId);
                    classroomName = classroom?.Name ?? "-";
                }

                // Get most active teacher - simplified approach
                var teacherName = "-";
                var teachers = await db.StudySessions
                    .Select(s => s.TeacherId)
                    .ToListAsync();
                
                if (teachers.Any())
                {
                    var mostActiveTeacherId = teachers
                        .GroupBy(x => x)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault();
                    
                    var teacher = await db.Teachers.FindAsync(mostActiveTeacherId);
                    teacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "-";
                }

                // Calculate attendance rate
                var attendedCount = await db.StudySessions.CountAsync(s => s.AttendanceStatus == "Attended");
                var attendanceRate = total > 0 ? Math.Round((double)attendedCount / total * 100, 1) : 0;

                // Calculate average duration
                var averageDuration = "45 dk";

                return Json(new
                {
                    success = true,
                    thisWeek = thisWeek,
                    thisMonth = thisMonth,
                    total = total,
                    attendanceRate = attendanceRate,
                    popularClassroom = classroomName,
                    activeTeacher = teacherName,
                    averageDuration = averageDuration
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting weekly summary: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> ExportToExcel()
        {
            try
            {
                TempData["Error"] = "Excel export özelliği henüz eklenmedi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error exporting to Excel");
                TempData["Error"] = "Excel dışa aktarma sırasında hata oluştu.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<ActionResult> GeneratePDFReport(string startDate, string endDate)
        {
            try
            {
                TempData["Error"] = "PDF rapor özelliği henüz eklenmedi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating PDF report");
                TempData["Error"] = "PDF rapor oluşturma sırasında hata oluştu.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetChartData()
        {
            try
            {
                var today = DateTime.Today;
                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek + 1);
                var nextWeekStart = thisWeekStart.AddDays(7);
                var lastWeekStart = thisWeekStart.AddDays(-7);
                var twoWeeksAgoStart = thisWeekStart.AddDays(-14);

                Logger.LogInfo($"GetChartData - Today: {today:yyyy-MM-dd}, DayOfWeek: {today.DayOfWeek}, thisWeekStart: {thisWeekStart:yyyy-MM-dd}");

                // Weekly data for doughnut chart
                var thisWeek = await db.StudySessions.CountAsync(s => s.SessionDate >= thisWeekStart && s.SessionDate < nextWeekStart);
                var lastWeek = await db.StudySessions.CountAsync(s => s.SessionDate >= lastWeekStart && s.SessionDate < thisWeekStart);
                var twoWeeksAgo = await db.StudySessions.CountAsync(s => s.SessionDate >= twoWeeksAgoStart && s.SessionDate < lastWeekStart);

                // Daily data for line chart (this week, Mon-Sun)
                // Pre-compute all day boundaries
                var dayBoundaries = new DateTime[8];
                for (int i = 0; i <= 7; i++)
                {
                    dayBoundaries[i] = thisWeekStart.AddDays(i);
                }
                
                var dailyData = new int[7];
                var dayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                for (int i = 0; i < 7; i++)
                {
                    var dayStart = dayBoundaries[i];
                    var dayEnd = dayBoundaries[i + 1];
                    dailyData[i] = await db.StudySessions.CountAsync(s => s.SessionDate >= dayStart && s.SessionDate < dayEnd);
                    Logger.LogInfo($"GetChartData - {dayNames[i]} ({dayStart:yyyy-MM-dd}): {dailyData[i]} sessions");
                }

                return Json(new
                {
                    success = true,
                    weeklyData = new
                    {
                        thisWeek = thisWeek > 0 ? thisWeek : 1,
                        lastWeek = lastWeek > 0 ? lastWeek : 1,
                        twoWeeksAgo = twoWeeksAgo > 0 ? twoWeeksAgo : 1
                    },
                    dailyData = dailyData
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting chart data");
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Helper class for availability slots
    public class AvailabilitySlot
    {
        public int DayOfWeek { get; set; }
        public int TimeSlotId { get; set; }
    }
}
