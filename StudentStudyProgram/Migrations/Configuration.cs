namespace StudentStudyProgram.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using StudentStudyProgram.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<StudentStudyProgram.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "StudentStudyProgram.Models.ApplicationDbContext";
        }

        protected override void Seed(StudentStudyProgram.Models.ApplicationDbContext context)
        {
            // Seed temporarily disabled to allow migration
            // TODO: Re-enable after debugging
            /*
            // Seed Time Slots
            if (!context.TimeSlots.Any())
            {
                context.TimeSlots.AddRange(new[]
                {
                    new TimeSlot { Name = "1. Ders", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(8, 45, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 1 },
                    new TimeSlot { Name = "2. Ders", StartTime = new TimeSpan(8, 45, 0), EndTime = new TimeSpan(9, 30, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 2 },
                    new TimeSlot { Name = "Teneffüs", StartTime = new TimeSpan(9, 30, 0), EndTime = new TimeSpan(9, 40, 0), IsBreak = true, IsLunch = false, Type = "Break", OrderIndex = 3 },
                    new TimeSlot { Name = "3. Ders", StartTime = new TimeSpan(9, 40, 0), EndTime = new TimeSpan(10, 25, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 4 },
                    new TimeSlot { Name = "4. Ders", StartTime = new TimeSpan(10, 25, 0), EndTime = new TimeSpan(11, 10, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 5 },
                    new TimeSlot { Name = "Teneffüs", StartTime = new TimeSpan(11, 10, 0), EndTime = new TimeSpan(11, 20, 0), IsBreak = true, IsLunch = false, Type = "Break", OrderIndex = 6 },
                    new TimeSlot { Name = "5. Ders", StartTime = new TimeSpan(11, 20, 0), EndTime = new TimeSpan(12, 5, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 7 },
                    new TimeSlot { Name = "6. Ders", StartTime = new TimeSpan(12, 5, 0), EndTime = new TimeSpan(12, 50, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 8 },
                    new TimeSlot { Name = "Öğle Arası", StartTime = new TimeSpan(12, 50, 0), EndTime = new TimeSpan(13, 30, 0), IsBreak = false, IsLunch = true, Type = "Lunch", OrderIndex = 9 },
                    new TimeSlot { Name = "7. Ders", StartTime = new TimeSpan(13, 30, 0), EndTime = new TimeSpan(14, 15, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 10 },
                    new TimeSlot { Name = "8. Ders", StartTime = new TimeSpan(14, 15, 0), EndTime = new TimeSpan(15, 0, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 11 },
                    new TimeSlot { Name = "Teneffüs", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(15, 10, 0), IsBreak = true, IsLunch = false, Type = "Break", OrderIndex = 12 },
                    new TimeSlot { Name = "9. Ders", StartTime = new TimeSpan(15, 10, 0), EndTime = new TimeSpan(15, 55, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 13 },
                    new TimeSlot { Name = "10. Ders", StartTime = new TimeSpan(15, 55, 0), EndTime = new TimeSpan(16, 40, 0), IsBreak = false, IsLunch = false, Type = "Lesson", OrderIndex = 14 }
                });
                context.SaveChanges();
            }

            // Seed Classrooms
            if (!context.Classrooms.Any())
            {
                context.Classrooms.AddRange(new[]
                {
                    new Classroom { Name = "101", Type = "Science", Capacity = 30 },
                    new Classroom { Name = "102", Type = "Literature", Capacity = 30 },
                    new Classroom { Name = "103", Type = "EqualWeight", Capacity = 30 },
                    new Classroom { Name = "104", Type = "Language", Capacity = 25 },
                    new Classroom { Name = "201", Type = "Science", Capacity = 30 },
                    new Classroom { Name = "202", Type = "Literature", Capacity = 30 },
                    new Classroom { Name = "203", Type = "EqualWeight", Capacity = 30 },
                    new Classroom { Name = "204", Type = "Language", Capacity = 25 }
                });
                context.SaveChanges();
            }

            // Seed Teachers
            if (!context.Teachers.Any())
            {
                context.Teachers.AddRange(new[]
                {
                    new Teacher { FirstName = "Ahmet", LastName = "Yılmaz", Branch = "Matematik", PhoneNumber = "0532 111 22 33", Gender = "Male", CreatedAt = DateTime.Now },
                    new Teacher { FirstName = "Ayşe", LastName = "Demir", Branch = "Türkçe", PhoneNumber = "0533 222 33 44", Gender = "Female", CreatedAt = DateTime.Now },
                    new Teacher { FirstName = "Mehmet", LastName = "Kaya", Branch = "Fizik", PhoneNumber = "0534 333 44 55", Gender = "Male", CreatedAt = DateTime.Now },
                    new Teacher { FirstName = "Fatma", LastName = "Şahin", Branch = "Kimya", PhoneNumber = "0535 444 55 66", Gender = "Female", CreatedAt = DateTime.Now },
                    new Teacher { FirstName = "Ali", LastName = "Öztürk", Branch = "Biyoloji", PhoneNumber = "0536 555 66 77", Gender = "Male", CreatedAt = DateTime.Now },
                    new Teacher { FirstName = "Zeynep", LastName = "Arslan", Branch = "Tarih", PhoneNumber = "0537 666 77 88", Gender = "Female", CreatedAt = DateTime.Now }
                });
                context.SaveChanges();
            }

            // Seed Students
            if (!context.Students.Any())
            {
                context.Students.AddRange(new[]
                {
                    new Student { FirstName = "Ali", LastName = "Veli", ClassName = "9", PhoneNumber = "0541 111 11 11", Gender = "Male", CreatedAt = DateTime.Now },
                    new Student { FirstName = "Ayşe", LastName = "Güneş", ClassName = "10", PhoneNumber = "0542 222 22 22", Gender = "Female", CreatedAt = DateTime.Now },
                    new Student { FirstName = "Mehmet", LastName = "Can", ClassName = "11", PhoneNumber = "0543 333 33 33", Gender = "Male", CreatedAt = DateTime.Now },
                    new Student { FirstName = "Fatma", LastName = "Yıldız", ClassName = "12", PhoneNumber = "0544 444 44 44", Gender = "Female", CreatedAt = DateTime.Now },
                    new Student { FirstName = "Ahmet", LastName = "Demir", ClassName = "9", PhoneNumber = "0545 555 55 55", Gender = "Male", CreatedAt = DateTime.Now },
                    new Student { FirstName = "Zeynep", LastName = "Kaya", ClassName = "10", PhoneNumber = "0546 666 66 66", Gender = "Female", CreatedAt = DateTime.Now },
                    new Student { FirstName = "Mustafa", LastName = "Şahin", ClassName = "11", PhoneNumber = "0547 777 77 77", Gender = "Male", CreatedAt = DateTime.Now },
                    new Student { FirstName = "Elif", LastName = "Öztürk", ClassName = "12", PhoneNumber = "0548 888 88 88", Gender = "Female", CreatedAt = DateTime.Now }
                });
                context.SaveChanges();
            }

            // Seed some sample study sessions for the current week
            if (!context.StudySessions.Any())
            {
                var today = DateTime.Today;
                var monday = today.AddDays(-(int)today.DayOfWeek + 1);
                
                var teachers = context.Teachers.ToList();
                var students = context.Students.ToList();
                var timeSlots = context.TimeSlots.Where(t => !t.IsBreak && !t.IsLunch).ToList();
                var classrooms = context.Classrooms.ToList();
                
                var random = new Random();
                
                // Create sample sessions for the week
                if (teachers.Count > 0 && students.Count > 0 && timeSlots.Count > 0 && classrooms.Count > 0)
                {
                    for (int day = 0; day < 5; day++)
                    {
                        var currentDate = monday.AddDays(day);
                    
                    // Create 2-3 sessions per day
                        for (int i = 0; i < 3; i++)
                        {
                            var teacher = teachers[random.Next(teachers.Count)];
                            var student = students[random.Next(students.Count)];
                            var timeSlot = timeSlots[random.Next(timeSlots.Count)];
                            var classroom = classrooms[random.Next(classrooms.Count)];
                        
                        var statuses = new[] { "Attended", "NotAttended", "Pending" };
                        var status = statuses[random.Next(statuses.Length)];
                        
                        context.StudySessions.Add(new StudySession
                        {
                            StudentId = student.Id,
                            TeacherId = teacher.Id,
                            SessionDate = currentDate,
                            TimeSlotId = timeSlot.Id,
                            ClassroomId = classroom.Id,
                            AttendanceStatus = status,
                            Notes = "Örnek etüt notu",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            }

            context.SaveChanges();
            */
        }
    }
}
