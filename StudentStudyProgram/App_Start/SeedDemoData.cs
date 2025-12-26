using System;
using System.Collections.Generic;
using System.Linq;
using StudentStudyProgram.Models;

namespace StudentStudyProgram.App_Start
{
    public static class SeedDemoData
    {
        private static readonly object SeedLock = new object();

        public static void EnsureDemoData()
        {
            lock (SeedLock)
            {
                try
                {
                    using (var db = new ApplicationDbContext())
                    {
                        var random = new Random();

                        SeedTimeSlots(db);
                        SeedClassrooms(db);
                        SeedTeachers(db, random);
                        SeedStudents(db, random);
                        SeedTeacherAvailability(db, random);
                        SeedStudySessions(db, random);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SeedDemoData error: " + ex.Message);
                }
            }
        }

        private static void SeedTimeSlots(ApplicationDbContext db)
        {
            if (db.TimeSlots.Any())
            {
                return;
            }

            var slots = new[]
            {
                new TimeSlot { Name = "1. Ders", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(8, 45, 0), Type = "Lesson", OrderIndex = 1 },
                new TimeSlot { Name = "2. Ders", StartTime = new TimeSpan(8, 45, 0), EndTime = new TimeSpan(9, 30, 0), Type = "Lesson", OrderIndex = 2 },
                new TimeSlot { Name = "Teneffus", StartTime = new TimeSpan(9, 30, 0), EndTime = new TimeSpan(9, 40, 0), Type = "Break", OrderIndex = 3, IsBreak = true },
                new TimeSlot { Name = "3. Ders", StartTime = new TimeSpan(9, 40, 0), EndTime = new TimeSpan(10, 25, 0), Type = "Lesson", OrderIndex = 4 },
                new TimeSlot { Name = "4. Ders", StartTime = new TimeSpan(10, 25, 0), EndTime = new TimeSpan(11, 10, 0), Type = "Lesson", OrderIndex = 5 },
                new TimeSlot { Name = "Teneffus", StartTime = new TimeSpan(11, 10, 0), EndTime = new TimeSpan(11, 20, 0), Type = "Break", OrderIndex = 6, IsBreak = true },
                new TimeSlot { Name = "5. Ders", StartTime = new TimeSpan(11, 20, 0), EndTime = new TimeSpan(12, 5, 0), Type = "Lesson", OrderIndex = 7 },
                new TimeSlot { Name = "6. Ders", StartTime = new TimeSpan(12, 5, 0), EndTime = new TimeSpan(12, 50, 0), Type = "Lesson", OrderIndex = 8 },
                new TimeSlot { Name = "Ogle Arasi", StartTime = new TimeSpan(12, 50, 0), EndTime = new TimeSpan(13, 30, 0), Type = "Lunch", OrderIndex = 9, IsLunch = true },
                new TimeSlot { Name = "7. Ders", StartTime = new TimeSpan(13, 30, 0), EndTime = new TimeSpan(14, 15, 0), Type = "Lesson", OrderIndex = 10 },
                new TimeSlot { Name = "8. Ders", StartTime = new TimeSpan(14, 15, 0), EndTime = new TimeSpan(15, 0, 0), Type = "Lesson", OrderIndex = 11 },
                new TimeSlot { Name = "Teneffus", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(15, 10, 0), Type = "Break", OrderIndex = 12, IsBreak = true },
                new TimeSlot { Name = "9. Ders", StartTime = new TimeSpan(15, 10, 0), EndTime = new TimeSpan(15, 55, 0), Type = "Lesson", OrderIndex = 13 },
                new TimeSlot { Name = "10. Ders", StartTime = new TimeSpan(15, 55, 0), EndTime = new TimeSpan(16, 40, 0), Type = "Lesson", OrderIndex = 14 }
            };

            db.TimeSlots.AddRange(slots);
            db.SaveChanges();
        }

        private static void SeedClassrooms(ApplicationDbContext db)
        {
            var baseClassrooms = new[]
            {
                new Classroom { Name = "101", Type = "Science", Capacity = 30 },
                new Classroom { Name = "102", Type = "Literature", Capacity = 30 },
                new Classroom { Name = "103", Type = "EqualWeight", Capacity = 30 },
                new Classroom { Name = "104", Type = "Language", Capacity = 25 },
                new Classroom { Name = "201", Type = "Science", Capacity = 30 },
                new Classroom { Name = "202", Type = "Literature", Capacity = 30 },
                new Classroom { Name = "203", Type = "EqualWeight", Capacity = 30 },
                new Classroom { Name = "204", Type = "Language", Capacity = 25 }
            };

            if (!db.Classrooms.Any())
            {
                db.Classrooms.AddRange(baseClassrooms);
                db.SaveChanges();
                return;
            }

            var existing = new HashSet<string>(
                db.Classrooms.Select(c => c.Name).ToList(),
                StringComparer.OrdinalIgnoreCase);
            var toAdd = baseClassrooms.Where(c => !existing.Contains(c.Name)).ToList();
            if (toAdd.Count > 0)
            {
                db.Classrooms.AddRange(toAdd);
                db.SaveChanges();
            }
        }

        private static void SeedTeachers(ApplicationDbContext db, Random random)
        {
            const int minTeachers = 8;
            var existingCount = db.Teachers.Count();
            var toCreate = minTeachers - existingCount;
            if (toCreate <= 0)
            {
                return;
            }

            var firstNames = new[]
            {
                "Ahmet", "Mehmet", "Ayse", "Fatma", "Ali", "Zeynep", "Merve", "Can",
                "Deniz", "Emre", "Ece", "Selin", "Kerem", "Omer", "Seda"
            };
            var lastNames = new[]
            {
                "Yilmaz", "Demir", "Kaya", "Aydin", "Sahin", "Kilic", "Aslan", "Arslan",
                "Guler", "Koc", "Cetin", "Ozdemir", "Polat", "Kaplan"
            };
            var branches = new[]
            {
                "Matematik", "Fizik", "Kimya", "Biyoloji", "Tarih",
                "Cografya", "Edebiyat", "Ingilizce", "Geometri", "Fen"
            };
            var genders = new[] { "Male", "Female" };

            var existingNames = new HashSet<string>(
                db.Teachers.Select(t => t.FirstName + " " + t.LastName).ToList(),
                StringComparer.OrdinalIgnoreCase);

            int attempts = 0;
            while (toCreate > 0 && attempts < 500)
            {
                attempts++;
                var first = firstNames[random.Next(firstNames.Length)];
                var last = lastNames[random.Next(lastNames.Length)];
                var full = first + " " + last;
                if (existingNames.Contains(full))
                {
                    continue;
                }

                var teacher = new Teacher
                {
                    FirstName = first,
                    LastName = last,
                    Branch = branches[random.Next(branches.Length)],
                    PhoneNumber = BuildPhone(random),
                    Gender = genders[random.Next(genders.Length)],
                    CreatedAt = DateTime.Now.AddDays(-random.Next(0, 365))
                };

                db.Teachers.Add(teacher);
                existingNames.Add(full);
                toCreate--;
            }

            if (db.ChangeTracker.HasChanges())
            {
                db.SaveChanges();
            }
        }

        private static void SeedStudents(ApplicationDbContext db, Random random)
        {
            const int minStudents = 30;
            var existingCount = db.Students.Count();
            var toCreate = minStudents - existingCount;
            if (toCreate <= 0)
            {
                return;
            }

            var firstNames = new[]
            {
                "Ali", "Veli", "Ayse", "Elif", "Murat", "Berk", "Efe", "Ekin",
                "Cem", "Ceyda", "Sena", "Duru", "Mete", "Buse", "Dilan", "Kaan",
                "Yusuf", "Sude", "Onur", "Gizem"
            };
            var lastNames = new[]
            {
                "Yilmaz", "Demir", "Kaya", "Aydin", "Sahin", "Kilic", "Aslan", "Arslan",
                "Guler", "Koc", "Cetin", "Ozdemir", "Polat", "Kaplan", "Erdem", "Acar"
            };
            var classNames = new[] { "9A", "9B", "10A", "10B", "11A", "11B", "12A", "12B" };
            var genders = new[] { "Male", "Female" };

            var existingNames = new HashSet<string>(
                db.Students.Select(s => s.FirstName + " " + s.LastName).ToList(),
                StringComparer.OrdinalIgnoreCase);

            int attempts = 0;
            while (toCreate > 0 && attempts < 2000)
            {
                attempts++;
                var first = firstNames[random.Next(firstNames.Length)];
                var last = lastNames[random.Next(lastNames.Length)];
                var full = first + " " + last;
                if (existingNames.Contains(full))
                {
                    continue;
                }

                var email = (first + "." + last + random.Next(10, 99).ToString()).ToLowerInvariant() + "@example.com";

                var student = new Student
                {
                    FirstName = first,
                    LastName = last,
                    ClassName = classNames[random.Next(classNames.Length)],
                    PhoneNumber = BuildPhone(random),
                    Email = email,
                    Gender = genders[random.Next(genders.Length)],
                    CreatedAt = DateTime.Now.AddDays(-random.Next(0, 365))
                };

                db.Students.Add(student);
                existingNames.Add(full);
                toCreate--;
            }

            if (db.ChangeTracker.HasChanges())
            {
                db.SaveChanges();
            }
        }

        private static void SeedTeacherAvailability(ApplicationDbContext db, Random random)
        {
            var timeSlots = db.TimeSlots.Where(t => !t.IsBreak && !t.IsLunch).ToList();
            if (timeSlots.Count == 0)
            {
                return;
            }

            var teachers = db.Teachers.ToList();
            if (teachers.Count == 0)
            {
                return;
            }

            var existingTeacherIds = new HashSet<int>(
                db.TeacherAvailabilities.Select(a => a.TeacherId).Distinct().ToList());

            foreach (var teacher in teachers)
            {
                if (existingTeacherIds.Contains(teacher.Id))
                {
                    continue;
                }

                int addedForTeacher = 0;
                for (int day = 1; day <= 5; day++)
                {
                    foreach (var slot in timeSlots)
                    {
                        if (random.NextDouble() < 0.65)
                        {
                            db.TeacherAvailabilities.Add(new TeacherAvailability
                            {
                                TeacherId = teacher.Id,
                                DayOfWeek = day,
                                TimeSlotId = slot.Id,
                                IsAvailable = true
                            });
                            addedForTeacher++;
                        }
                    }
                }

                if (addedForTeacher == 0)
                {
                    foreach (var slot in timeSlots)
                    {
                        if (random.NextDouble() < 0.5)
                        {
                            db.TeacherAvailabilities.Add(new TeacherAvailability
                            {
                                TeacherId = teacher.Id,
                                DayOfWeek = 1,
                                TimeSlotId = slot.Id,
                                IsAvailable = true
                            });
                            addedForTeacher++;
                        }
                    }

                    if (addedForTeacher == 0 && timeSlots.Count > 0)
                    {
                        db.TeacherAvailabilities.Add(new TeacherAvailability
                        {
                            TeacherId = teacher.Id,
                            DayOfWeek = 1,
                            TimeSlotId = timeSlots[0].Id,
                            IsAvailable = true
                        });
                    }
                }
            }

            if (db.ChangeTracker.HasChanges())
            {
                db.SaveChanges();
            }
        }

        private static void SeedStudySessions(ApplicationDbContext db, Random random)
        {
            const int targetSessions = 80;
            var existingCount = db.StudySessions.Count();
            var toCreate = targetSessions - existingCount;
            if (toCreate <= 0)
            {
                return;
            }

            var teachers = db.Teachers.ToList();
            var students = db.Students.ToList();
            var timeSlots = db.TimeSlots.Where(t => !t.IsBreak && !t.IsLunch).ToList();
            var classrooms = db.Classrooms.ToList();
            if (teachers.Count == 0 || students.Count == 0 || timeSlots.Count == 0 || classrooms.Count == 0)
            {
                return;
            }

            var availabilitySet = new HashSet<string>(
                db.TeacherAvailabilities.Select(a => a.TeacherId + "|" + a.DayOfWeek + "|" + a.TimeSlotId).ToList());

            var existingSessions = db.StudySessions
                .Select(s => new { s.TeacherId, s.StudentId, s.TimeSlotId, s.ClassroomId, s.SessionDate })
                .ToList();

            var teacherSlots = new HashSet<string>();
            var studentSlots = new HashSet<string>();
            var roomSlots = new HashSet<string>();

            foreach (var s in existingSessions)
            {
                var dateKey = s.SessionDate.Date.ToString("yyyy-MM-dd");
                teacherSlots.Add(s.TeacherId + "|" + dateKey + "|" + s.TimeSlotId);
                studentSlots.Add(s.StudentId + "|" + dateKey + "|" + s.TimeSlotId);
                roomSlots.Add(s.ClassroomId + "|" + dateKey + "|" + s.TimeSlotId);
            }

            var statuses = new[] { "Attended", "Pending", "NotAttended" };
            var sessionTypes = new[] { "Individual", "Group" };
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today.AddDays(7);

            int attempts = 0;
            int maxAttempts = toCreate * 30;
            while (toCreate > 0 && attempts < maxAttempts)
            {
                attempts++;

                var date = RandomWeekday(random, startDate, endDate);
                int dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0)
                {
                    dayOfWeek = 7;
                }

                var teacher = teachers[random.Next(teachers.Count)];
                var student = students[random.Next(students.Count)];
                var timeSlot = timeSlots[random.Next(timeSlots.Count)];
                var classroom = classrooms[random.Next(classrooms.Count)];

                if (availabilitySet.Count > 0)
                {
                    var availabilityKey = teacher.Id + "|" + dayOfWeek + "|" + timeSlot.Id;
                    if (!availabilitySet.Contains(availabilityKey))
                    {
                        continue;
                    }
                }

                var dateKey = date.ToString("yyyy-MM-dd");
                var teacherKey = teacher.Id + "|" + dateKey + "|" + timeSlot.Id;
                var studentKey = student.Id + "|" + dateKey + "|" + timeSlot.Id;
                var roomKey = classroom.Id + "|" + dateKey + "|" + timeSlot.Id;

                if (teacherSlots.Contains(teacherKey) || studentSlots.Contains(studentKey) || roomSlots.Contains(roomKey))
                {
                    continue;
                }

                var session = new StudySession
                {
                    TeacherId = teacher.Id,
                    StudentId = student.Id,
                    TimeSlotId = timeSlot.Id,
                    ClassroomId = classroom.Id,
                    SessionDate = date.Date,
                    SessionType = sessionTypes[random.Next(sessionTypes.Length)],
                    AttendanceStatus = statuses[random.Next(statuses.Length)],
                    Notes = "Demo session",
                    CreatedAt = DateTime.Now.AddDays(-random.Next(0, 30)),
                    UpdatedAt = DateTime.Now
                };

                db.StudySessions.Add(session);
                teacherSlots.Add(teacherKey);
                studentSlots.Add(studentKey);
                roomSlots.Add(roomKey);
                toCreate--;
            }

            if (db.ChangeTracker.HasChanges())
            {
                db.SaveChanges();
            }
        }

        private static string BuildPhone(Random random)
        {
            return "5" + random.Next(0, 1000000000).ToString("D9");
        }

        private static DateTime RandomWeekday(Random random, DateTime start, DateTime end)
        {
            var startDate = start.Date;
            var endDate = end.Date;
            if (endDate < startDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }

            var range = (endDate - startDate).Days;
            if (range < 0)
            {
                range = 0;
            }

            for (int i = 0; i < 20; i++)
            {
                var date = startDate.AddDays(random.Next(0, range + 1));
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    return date;
                }
            }

            var fallback = startDate;
            while (fallback.DayOfWeek == DayOfWeek.Saturday || fallback.DayOfWeek == DayOfWeek.Sunday)
            {
                fallback = fallback.AddDays(1);
                if (fallback > endDate)
                {
                    return startDate;
                }
            }

            return fallback;
        }
    }
}
