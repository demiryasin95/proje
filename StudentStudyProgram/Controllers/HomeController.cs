using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using StudentStudyProgram.Models;

namespace StudentStudyProgram.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            // If Teacher: preselect their own teacherId for UI (dropdown can be hidden)
            if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
            {
                var uid = User.Identity.GetUserId();
                if (!string.IsNullOrEmpty(uid))
                {
                    var myTeacher = db.Teachers.FirstOrDefault(t => t.UserId == uid);
                    if (myTeacher != null)
                    {
                        ViewBag.MyTeacherId = myTeacher.Id;
                        ViewBag.MyTeacherName = myTeacher.FullName;
                    }
                }
            }

            var teachers = db.Teachers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToList();
            ViewBag.Teachers = teachers.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = string.IsNullOrEmpty(t.Branch) ? t.FullName : (t.FullName + " (" + t.Branch + ")")
            }).ToList();
            if (!db.Classrooms.Any())
            {
                db.Classrooms.AddRange(new[]
                {
                    new Classroom { Name = "101", Type = "Science", Capacity = 30 },
                    new Classroom { Name = "102", Type = "Literature", Capacity = 30 },
                    new Classroom { Name = "103", Type = "EqualWeight", Capacity = 30 },
                    new Classroom { Name = "104", Type = "Language", Capacity = 25 }
                });
                db.SaveChanges();
            }
            ViewBag.Classrooms = db.Classrooms.OrderBy(c => c.Name).ToList();
            
            var model = new WeeklyCalendarViewModel
            {
                WeekStart = GetWeekStart(DateTime.Now),
                TimeSlots = db.TimeSlots.OrderBy(t => t.OrderIndex).ToList()
            };
            
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var difference = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return date.AddDays(-difference).Date;
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
