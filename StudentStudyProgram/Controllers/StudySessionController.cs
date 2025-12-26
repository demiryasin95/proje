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
    public class StudySessionController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        public async Task<JsonResult> GetWeeklyCalendar(int? teacherId, DateTime weekStart)
        {
            try
            {
                var weekEnd = weekStart.AddDays(6);

                var studySessionsQuery = db.StudySessions
                    .Include(s => s.Student)
                    .Include(s => s.Teacher)
                    .Include(s => s.TimeSlot)
                    .Include(s => s.Classroom)
                    .Where(s => s.SessionDate >= weekStart && s.SessionDate <= weekEnd);

                // Enforce role-based visibility:
                // - Student: only their own sessions (ignore teacher filter)
                // - Teacher: only their own sessions unless Admin
                var userId = User.Identity.GetUserId();
                if (User.IsInRole("Student") && !string.IsNullOrEmpty(userId))
                {
                    teacherId = null;
                    studySessionsQuery = studySessionsQuery.Where(s => s.Student.UserId == userId);
                }
                else if (User.IsInRole("Teacher") && !User.IsInRole("Admin") && !string.IsNullOrEmpty(userId))
                {
                    var myTeacherId = await db.Teachers.Where(t => t.UserId == userId).Select(t => (int?)t.Id).FirstOrDefaultAsync();
                    if (myTeacherId.HasValue)
                    {
                        teacherId = myTeacherId.Value;
                        studySessionsQuery = studySessionsQuery.Where(s => s.TeacherId == myTeacherId.Value);
                    }
                }

                if (teacherId.HasValue && teacherId.Value > 0)
                {
                    studySessionsQuery = studySessionsQuery.Where(s => s.TeacherId == teacherId.Value);
                }

                var studySessions = await studySessionsQuery
                    .OrderBy(s => s.SessionDate)
                    .ThenBy(s => s.TimeSlot.StartTime)
                    .ToListAsync();

                var timeSlots = await db.TimeSlots
                    .OrderBy(t => t.StartTime)
                    .ToListAsync();

                // Get teacher availabilities if a teacher is selected
                List<object> teacherAvailabilities = new List<object>();
                if (teacherId.HasValue && teacherId.Value > 0)
                {
                    var availabilities = await db.TeacherAvailabilities
                        .Where(a => a.TeacherId == teacherId.Value)
                        .Select(a => new
                        {
                            dayOfWeek = a.DayOfWeek,
                            timeSlotId = a.TimeSlotId
                        })
                        .ToListAsync();
                    
                    teacherAvailabilities = availabilities.Cast<object>().ToList();
                }

                var result = new
                {
                    success = true,
                    studySessions = studySessions.Select(s => new
                    {
                        id = s.Id,
                        studentId = s.StudentId,
                        // Student UI wants to see "who (teacher) and when" only.
                        studentName = User.IsInRole("Student") ? s.Teacher.FullName : s.Student.FullName,
                        teacherBranch = s.Teacher != null ? s.Teacher.Branch : null,
                        studentPhoto = s.Student.DefaultPhotoPath,
                        date = s.SessionDate.ToString("yyyy-MM-dd"),
                        timeSlotId = s.TimeSlotId,
                        startTime = s.TimeSlot.StartTime.ToString(@"hh\:mm"),
                        endTime = s.TimeSlot.EndTime.ToString(@"hh\:mm"),
                        classroom = s.Classroom.Name,
                        classroomType = s.Classroom.Type,
                        attendanceStatus = s.AttendanceStatus,
                        notes = s.Notes
                    }),
                    timeSlots = timeSlots.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        startTime = t.StartTime.ToString(@"hh\:mm"),
                        endTime = t.EndTime.ToString(@"hh\:mm"),
                        type = t.Type
                    }),
                    teacherAvailabilities = teacherAvailabilities
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Takvim yüklenirken hata oluştu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> AddStudySession(int StudentId, int TeacherId, DateTime Date, int TimeSlotId, int ClassroomId, string notes = "")
        {
            try
            {
                // If non-admin teacher, force TeacherId to current user's teacher record
                if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                {
                    var uid = User.Identity.GetUserId();
                    var myTeacherId = await db.Teachers.Where(t => t.UserId == uid).Select(t => (int?)t.Id).FirstOrDefaultAsync();
                    if (!myTeacherId.HasValue) return Json(new { success = false, message = "Öğretmen hesabınız bir öğretmen kaydına bağlı değil." });
                    TeacherId = myTeacherId.Value;
                }

                // Check teacher availability for the day of week and time slot
                var dayOfWeek = (int)Date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7 in our system
                
                Logger.LogInfo($"Müsaitlik kontrolü: TeacherId={TeacherId}, Date={Date:yyyy-MM-dd}, DayOfWeek={dayOfWeek}, TimeSlotId={TimeSlotId}");
                
                var availabilityCount = await db.TeacherAvailabilities
                    .CountAsync(a => a.TeacherId == TeacherId);
                
                Logger.LogInfo($"Öğretmenin toplam {availabilityCount} müsaitlik kaydı var");
                
                var isTeacherAvailable = await db.TeacherAvailabilities
                    .AnyAsync(a => a.TeacherId == TeacherId &&
                                   a.DayOfWeek == dayOfWeek &&
                                   a.TimeSlotId == TimeSlotId);

                Logger.LogInfo($"Müsaitlik kontrolü sonucu: {isTeacherAvailable}");

                if (!isTeacherAvailable)
                {
                    Logger.LogWarning($"Öğretmen müsait değil! TeacherId={TeacherId}, Day={dayOfWeek}, TimeSlot={TimeSlotId}");
                    return Json(new { success = false, message = "Öğretmen bu gün ve saatte müsait değil!" });
                }

                // Student conflict: cannot have two sessions at same Date+TimeSlot
                var studentBusyEq = await db.StudySessions.AnyAsync(s => s.StudentId == StudentId && s.SessionDate == Date && s.TimeSlotId == TimeSlotId);
                var studentBusyDateOnly = await db.StudySessions.AnyAsync(s => s.StudentId == StudentId && DbFunctions.TruncateTime(s.SessionDate) == Date.Date && s.TimeSlotId == TimeSlotId);
                var studentBusy = studentBusyEq;
                if (studentBusy)
                {
                    return Json(new { success = false, message = "Öğrencinin bu saat diliminde başka bir etüdü var!" });
                }

                // Check if the time slot is already booked for this teacher on this date
                var existingSession = await db.StudySessions
                    .FirstOrDefaultAsync(s => s.TeacherId == TeacherId &&
                                           s.SessionDate == Date &&
                                           s.TimeSlotId == TimeSlotId);

                if (existingSession != null)
                {
                    return Json(new { success = false, message = "Bu saat dilimi zaten dolu!" });
                }

                var studySession = new StudySession
                {
                    StudentId = StudentId,
                    TeacherId = TeacherId,
                    SessionDate = Date,
                    TimeSlotId = TimeSlotId,
                    ClassroomId = ClassroomId,
                    Notes = notes,
                    AttendanceStatus = "Pending",
                    CreatedAt = DateTime.Now
                };

                db.StudySessions.Add(studySession);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Etüt başarıyla eklendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Etüt eklenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> AddStudySessions(AddSessionsRequest request)
        {
            try
            {
                // If non-admin teacher, force TeacherId to current user's teacher record (ignore client-provided)
                if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                {
                    var uid = User.Identity.GetUserId();
                    var myTeacherId = await db.Teachers.Where(t => t.UserId == uid).Select(t => (int?)t.Id).FirstOrDefaultAsync();
                    if (!myTeacherId.HasValue) return Json(new { success = false, message = "Öğretmen hesabınız bir öğretmen kaydına bağlı değil." });
                    if (request == null) request = new AddSessionsRequest();
                    request.TeacherId = myTeacherId.Value;
                }

                if (request == null || request.StudentIds == null || request.StudentIds.Count == 0)
                {
                    return Json(new { success = false, message = "En az bir öğrenci seçilmelidir." });
                }

                // Check teacher availability for the day of week and time slot
                var dayOfWeek = (int)request.Date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7 in our system
                
                var isTeacherAvailable = await db.TeacherAvailabilities
                    .AnyAsync(a => a.TeacherId == request.TeacherId &&
                                   a.DayOfWeek == dayOfWeek &&
                                   a.TimeSlotId == request.TimeSlotId);

                if (!isTeacherAvailable)
                {
                    return Json(new { success = false, message = "Öğretmen bu gün ve saatte müsait değil!" });
                }

                var sessionType = string.IsNullOrWhiteSpace(request.SessionType) ? "Group" : request.SessionType.Trim();
                if (sessionType != "Individual" && sessionType != "Group") sessionType = "Group";

                // Student conflict: a student cannot have two sessions at the same Date+TimeSlot (any teacher)
                // Teacher conflict: allowed only for Group; for Individual, teacher slot must be empty.
                if (sessionType == "Individual")
                {
                    var singleStudentId = request.StudentIds.Distinct().FirstOrDefault();
                    if (request.StudentIds.Distinct().Count() != 1)
                    {
                        return Json(new { success = false, message = "Birebir etüt için sadece 1 öğrenci seçilmelidir." });
                    }

                    var teacherBusy = await db.StudySessions.AnyAsync(s =>
                        s.TeacherId == request.TeacherId &&
                        s.SessionDate == request.Date &&
                        s.TimeSlotId == request.TimeSlotId);
                    
                    if (teacherBusy)
                    {
                        return Json(new { success = false, message = "Birebir etüt için bu saat dilimi öğretmen için dolu!" });
                    }

                    var studentBusy = await db.StudySessions.AnyAsync(s => s.StudentId == singleStudentId && s.SessionDate == request.Date && s.TimeSlotId == request.TimeSlotId);
                    if (studentBusy)
                    {
                        return Json(new { success = false, message = "Öğrencinin bu saat diliminde başka bir etüdü var!" });
                    }
                }

                var classroom = await db.Classrooms.OrderBy(c => c.Name).FirstOrDefaultAsync();
                if (classroom == null)
                {
                    classroom = new Classroom { Name = "Genel", Type = "General", Capacity = 30 };
                    db.Classrooms.Add(classroom);
                    await db.SaveChangesAsync();
                }

                int added = 0;
                int skipped = 0;
                foreach (var studentId in request.StudentIds.Distinct())
                {
                    // In Group mode: skip students that already have a session at the same slot (any teacher)
                    var studentBusyEq = await db.StudySessions.AnyAsync(s => s.StudentId == studentId && s.SessionDate == request.Date && s.TimeSlotId == request.TimeSlotId);
                    var studentBusyDateOnly = await db.StudySessions.AnyAsync(s => s.StudentId == studentId && DbFunctions.TruncateTime(s.SessionDate) == request.Date.Date && s.TimeSlotId == request.TimeSlotId);
                    if (studentBusyEq)
                    {
                        skipped++;
                        continue;
                    }

                    // Avoid exact duplicates for same teacher+student+slot
                    var exists = await db.StudySessions.AnyAsync(s => s.TeacherId == request.TeacherId && s.SessionDate == request.Date && s.TimeSlotId == request.TimeSlotId && s.StudentId == studentId);
                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    var session = new StudySession
                    {
                        StudentId = studentId,
                        TeacherId = request.TeacherId,
                        SessionDate = request.Date,
                        TimeSlotId = request.TimeSlotId,
                        ClassroomId = classroom.Id,
                        Notes = request.Notes ?? string.Empty,
                        AttendanceStatus = "Pending",
                        CreatedAt = DateTime.Now
                    };
                    db.StudySessions.Add(session);
                    added++;

                    if (sessionType == "Individual")
                    {
                        // only one student allowed
                        break;
                    }
                }

                await db.SaveChangesAsync();
                var msg = skipped > 0 ? $"{added} etüt eklendi, {skipped} kayıt çakışma/tekrar nedeniyle atlandı" : $"{added} etüt eklendi";
                return Json(new { success = true, message = msg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Etütler eklenirken hata oluştu: " + ex.Message });
            }
        }

        public class AddSessionsRequest
        {
            public List<int> StudentIds { get; set; }
            public int TeacherId { get; set; }
            public DateTime Date { get; set; }
            public int TimeSlotId { get; set; }
            public string Notes { get; set; }
            public string SessionType { get; set; } // Individual | Group
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> UpdateAttendance(int StudySessionId, string AttendanceStatus)
        {
            try
            {
                var session = await db.StudySessions.FindAsync(StudySessionId);
                if (session == null)
                {
                    return Json(new { success = false, message = "Etüt bulunamadı!" });
                }

                if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                {
                    var uid = User.Identity.GetUserId();
                    var myTeacherId = await db.Teachers.Where(t => t.UserId == uid).Select(t => (int?)t.Id).FirstOrDefaultAsync();
                    if (!myTeacherId.HasValue || session.TeacherId != myTeacherId.Value)
                    {
                        return Json(new { success = false, message = "Bu etüt için yetkiniz yok." });
                    }
                }

                session.AttendanceStatus = AttendanceStatus;
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Yoklama durumu güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Yoklama güncellenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> DeleteStudySession(int StudySessionId)
        {
            try
            {
                var session = await db.StudySessions.FindAsync(StudySessionId);
                if (session == null)
                {
                    return Json(new { success = false, message = "Etüt bulunamadı!" });
                }

                if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                {
                    var uid = User.Identity.GetUserId();
                    var myTeacherId = await db.Teachers.Where(t => t.UserId == uid).Select(t => (int?)t.Id).FirstOrDefaultAsync();
                    if (!myTeacherId.HasValue || session.TeacherId != myTeacherId.Value)
                    {
                        return Json(new { success = false, message = "Bu etüt için yetkiniz yok." });
                    }
                }

                db.StudySessions.Remove(session);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Etüt başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Etüt silinirken hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<JsonResult> GetStudents(string searchTerm = "")
        {
            try
            {
                var query = db.Students.AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    query = query.Where(s =>
                        (s.FirstName + " " + s.LastName).ToLower().Contains(term) ||
                        s.FirstName.ToLower().Contains(term) ||
                        s.LastName.ToLower().Contains(term) ||
                        s.ClassName.ToLower().Contains(term) ||
                        s.PhoneNumber.ToLower().Contains(term)
                    );
                }

                var baseStudents = await query
                    .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
                    .Take(20)
                    .Select(s => new
                    {
                        s.Id,
                        s.FirstName,
                        s.LastName,
                        s.ClassName,
                        s.PhoneNumber,
                        s.PhotoPath,
                        s.Gender
                    })
                    .ToListAsync();

                var students = baseStudents.Select(s => new
                {
                    id = s.Id,
                    fullName = s.FirstName + " " + s.LastName,
                    className = s.ClassName,
                    phoneNumber = s.PhoneNumber,
                    photo = string.IsNullOrEmpty(s.PhotoPath)
                        ? (s.Gender == "Female" ? "/Content/Images/default-female.svg" : "/Content/Images/default-male.svg")
                        : s.PhotoPath
                }).ToList();

                return Json(new { success = true, students = students }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Öğrenciler yüklenirken hata oluştu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<JsonResult> GetClassrooms()
        {
            try
            {
                var baseClassrooms = await db.Classrooms
                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Type
                    })
                    .ToListAsync();

                var classrooms = baseClassrooms.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    type = c.Type,
                    typeDisplayName = c.Type == "Science" ? "Sayısal"
                        : c.Type == "Literature" ? "Sözel"
                        : c.Type == "EqualWeight" ? "Eşit Ağırlık"
                        : c.Type == "Language" ? "Dil" : c.Type
                }).ToList();

                return Json(new { success = true, classrooms = classrooms }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Derslikler yüklenirken hata oluştu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public async Task<JsonResult> MoveSession(int sessionId, DateTime newDate, int newTimeSlotId)
        {
            try
            {
                var session = await db.StudySessions
                    .Include(s => s.Student)
                    .Include(s => s.Teacher)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                {
                    return Json(new { success = false, message = "Etüt bulunamadı!" });
                }

                // Check teacher permission
                if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                {
                    var uid = User.Identity.GetUserId();
                    var myTeacherId = await db.Teachers.Where(t => t.UserId == uid).Select(t => (int?)t.Id).FirstOrDefaultAsync();
                    if (!myTeacherId.HasValue || session.TeacherId != myTeacherId.Value)
                    {
                        return Json(new { success = false, message = "Bu etüt için yetkiniz yok." });
                    }
                }

                // Check teacher availability for the new day of week and time slot
                var dayOfWeek = (int)newDate.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7 in our system
                
                var isTeacherAvailable = await db.TeacherAvailabilities
                    .AnyAsync(a => a.TeacherId == session.TeacherId &&
                                   a.DayOfWeek == dayOfWeek &&
                                   a.TimeSlotId == newTimeSlotId);

                if (!isTeacherAvailable)
                {
                    return Json(new { success = false, message = "Öğretmen bu gün ve saatte müsait değil!" });
                }

                // Check if new slot is available for teacher
                var teacherConflict = await db.StudySessions.AnyAsync(s =>
                    s.Id != sessionId &&
                    s.TeacherId == session.TeacherId &&
                    s.SessionDate == newDate &&
                    s.TimeSlotId == newTimeSlotId);

                if (teacherConflict)
                {
                    return Json(new { success = false, message = "Öğretmenin bu saat diliminde başka bir etüdü var!" });
                }

                // Check if new slot is available for student
                var studentConflict = await db.StudySessions.AnyAsync(s =>
                    s.Id != sessionId &&
                    s.StudentId == session.StudentId &&
                    s.SessionDate == newDate &&
                    s.TimeSlotId == newTimeSlotId);

                if (studentConflict)
                {
                    return Json(new { success = false, message = "Öğrencinin bu saat diliminde başka bir etüdü var!" });
                }

                // Check if target time slot is a lesson slot
                var timeSlot = await db.TimeSlots.FindAsync(newTimeSlotId);
                if (timeSlot == null || timeSlot.Type != "Lesson")
                {
                    return Json(new { success = false, message = "Bu saat dilimi etüt için uygun değil!" });
                }

                // Update session
                session.SessionDate = newDate;
                session.TimeSlotId = newTimeSlotId;
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Etüt başarıyla taşındı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Etüt taşınırken hata oluştu: " + ex.Message });
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
