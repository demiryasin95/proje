using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentStudyProgram.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Parola gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; }

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Parola gereklidir.")]
        [StringLength(100, ErrorMessage = "Parola en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Parola Doğrulama")]
        [Compare("Password", ErrorMessage = "Parola ve doğrulama parolası eşleşmiyor.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Rol gereklidir.")]
        [Display(Name = "Rol")]
        public string Role { get; set; }
    }

    public class StudentRegistrationViewModel
    {
        [Required(ErrorMessage = "Ad gereklidir.")]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Sınıf gereklidir.")]
        [StringLength(20)]
        [Display(Name = "Sınıf")]
        public string ClassName { get; set; }

        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Cinsiyet")]
        public string Gender { get; set; }

        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; }
    }

    public class TeacherRegistrationViewModel
    {
        [Required(ErrorMessage = "Ad gereklidir.")]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Branş gereklidir.")]
        [StringLength(50)]
        [Display(Name = "Branş")]
        public string Branch { get; set; }

        [Display(Name = "Cinsiyet")]
        public string Gender { get; set; }

        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; }
    }

    public class TimeSlotViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad gereklidir.")]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Başlangıç saati gereklidir.")]
        [Display(Name = "Başlangıç Saati")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş saati gereklidir.")]
        [Display(Name = "Bitiş Saati")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Tür gereklidir.")]
        [Display(Name = "Tür")]
        public string Type { get; set; }

        [Display(Name = "Sıra")]
        public int OrderIndex { get; set; }
    }

    public class ClassroomViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad gereklidir.")]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Tür gereklidir.")]
        [Display(Name = "Tür")]
        public string Type { get; set; }

        [Display(Name = "Kapasite")]
        [Range(1, 100, ErrorMessage = "Kapasite 1-100 arasında olmalıdır.")]
        public int Capacity { get; set; } = 30;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class StudySessionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Öğretmen gereklidir.")]
        [Display(Name = "Öğretmen")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Öğrenci gereklidir.")]
        [Display(Name = "Öğrenci")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Derslik gereklidir.")]
        [Display(Name = "Derslik")]
        public int ClassroomId { get; set; }

        [Required(ErrorMessage = "Saat aralığı gereklidir.")]
        [Display(Name = "Saat Aralığı")]
        public int TimeSlotId { get; set; }

        [Required(ErrorMessage = "Tarih gereklidir.")]
        [Display(Name = "Tarih")]
        [DataType(DataType.Date)]
        public DateTime SessionDate { get; set; }

        [Display(Name = "Yoklama Durumu")]
        public string AttendanceStatus { get; set; } = "Pending";

        public virtual Teacher Teacher { get; set; }
        public virtual Student Student { get; set; }
        public virtual Classroom Classroom { get; set; }
        public virtual TimeSlot TimeSlot { get; set; }
    }

    public class WeeklyCalendarViewModel
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public DateTime WeekStart { get; set; }
        public List<TimeSlot> TimeSlots { get; set; }
        public List<StudySession> StudySessions { get; set; }
        public List<DaySchedule> WeekDays { get; set; }
    }

    public class DaySchedule
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public List<TimeSlotSchedule> TimeSlots { get; set; }
    }

    public class TimeSlotSchedule
    {
        public TimeSlot TimeSlot { get; set; }
        public StudySession StudySession { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class StudentSearchViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class StatisticsViewModel
    {
        public List<TeacherStatistics> TeacherStatistics { get; set; }
        public List<StudentStatistics> StudentStatistics { get; set; }
        public int TotalStudySessions { get; set; }
        public int TotalAttendedSessions { get; set; }
        public int TotalNotAttendedSessions { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class TeacherStatistics
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
        public int NotAttendedSessions { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class StudentStatistics
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
        public int NotAttendedSessions { get; set; }
        public double AttendanceRate { get; set; }
    }
}
