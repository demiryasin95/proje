using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentStudyProgram.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        [StringLength(128)]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Sınıf")]
        public string ClassName { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Telefon numarası 10-11 haneli rakamlardan oluşmalıdır")]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Geçersiz e-posta formatı")]
        [Display(Name = "E-posta")]
        public string Email { get; set; }

        [StringLength(10)]
        [Display(Name = "Cinsiyet")]
        public string Gender { get; set; }

        [StringLength(255)]
        [Display(Name = "Fotoğraf")]
        public string PhotoPath { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<StudySession> StudySessions { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string DefaultPhotoPath
        {
            get
            {
                if (!string.IsNullOrEmpty(PhotoPath))
                    return PhotoPath;
                
                return Gender == "Female" 
                    ? "/Content/Images/default-female.png" 
                    : "/Content/Images/default-male.png";
            }
        }
    }

    public class Teacher
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        [StringLength(128)]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Branş")]
        public string Branch { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Telefon numarası 10-11 haneli rakamlardan oluşmalıdır")]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; }

        [StringLength(10)]
        [Display(Name = "Cinsiyet")]
        public string Gender { get; set; }

        [StringLength(255)]
        [Display(Name = "Fotoğraf")]
        public string PhotoPath { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<StudySession> StudySessions { get; set; }
        public virtual ICollection<TeacherAvailability> Availabilities { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string DefaultPhotoPath
        {
            get
            {
                if (!string.IsNullOrEmpty(PhotoPath))
                    return PhotoPath;
                
                return Gender == "Female" 
                    ? "/Content/Images/default-female.png" 
                    : "/Content/Images/default-male.png";
            }
        }
    }

    public class TimeSlot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Başlangıç Saati")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Bitiş Saati")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Tür")]
        public string Type { get; set; }

        [Display(Name = "Sıra")]
        public int OrderIndex { get; set; }

        [Display(Name = "Teneffüs")]
        public bool IsBreak { get; set; } = false;

        [Display(Name = "Öğle Arası")]
        public bool IsLunch { get; set; } = false;

        public virtual ICollection<StudySession> StudySessions { get; set; }
        public virtual ICollection<TeacherAvailability> Availabilities { get; set; }

        [NotMapped]
        public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }

    public class Classroom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Tür")]
        public string Type { get; set; }

        [Display(Name = "Kapasite")]
        public int Capacity { get; set; } = 30;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<StudySession> StudySessions { get; set; }

        [NotMapped]
        public string TypeDisplayName
        {
            get
            {
                switch (Type)
                {
                    case "Science": return "Sayısal";
                    case "Literature": return "Sözel";
                    case "EqualWeight": return "Eşit Ağırlık";
                    case "Language": return "Dil";
                    default: return Type;
                }
            }
        }
    }

    public class StudySession
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Teacher")]
        public int TeacherId { get; set; }
        public virtual Teacher Teacher { get; set; }

        [ForeignKey("Student")]
        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        [ForeignKey("TimeSlot")]
        public int TimeSlotId { get; set; }
        public virtual TimeSlot TimeSlot { get; set; }

        [ForeignKey("Classroom")]
        public int ClassroomId { get; set; }
        public virtual Classroom Classroom { get; set; }

        [Required]
        [Display(Name = "Tarih")]
        public DateTime SessionDate { get; set; }

        [StringLength(20)]
        [Display(Name = "Etüt Tipi")]
        public string SessionType { get; set; } = "Individual"; // Individual | Group

        [StringLength(20)]
        [Display(Name = "Yoklama Durumu")]
        public string AttendanceStatus { get; set; } = "Pending";

        [StringLength(500)]
        [Display(Name = "Notlar")]
        public string Notes { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string AttendanceStatusDisplay
        {
            get
            {
                switch (AttendanceStatus)
                {
                    case "Attended": return "Geldi";
                    case "NotAttended": return "Gelmedi";
                    default: return "Bekleniyor";
                }
            }
        }

        [NotMapped]
        public string AttendanceStatusClass
        {
            get
            {
                switch (AttendanceStatus)
                {
                    case "Attended": return "text-success";
                    case "NotAttended": return "text-danger";
                    default: return "text-primary";
                }
            }
        }
    }

    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Tarih")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(256)]
        public string UserName { get; set; }

        [StringLength(128)]
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }

        [StringLength(100)]
        public string EntityType { get; set; }

        public int? EntityId { get; set; }

        [StringLength(2000)]
        public string Details { get; set; }
    }

    public class TeacherAvailability
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Teacher")]
        public int TeacherId { get; set; }
        public virtual Teacher Teacher { get; set; }

        [Range(1, 7)]
        [Display(Name = "Haftanın Günü")]
        public int DayOfWeek { get; set; }

        [ForeignKey("TimeSlot")]
        public int TimeSlotId { get; set; }
        public virtual TimeSlot TimeSlot { get; set; }

        [Display(Name = "Müsait")]
        public bool IsAvailable { get; set; } = true;

        [NotMapped]
        public string DayName
        {
            get
            {
                switch (DayOfWeek)
                {
                    case 1: return "Pazartesi";
                    case 2: return "Salı";
                    case 3: return "Çarşamba";
                    case 4: return "Perşembe";
                    case 5: return "Cuma";
                    case 6: return "Cumartesi";
                    case 7: return "Pazar";
                    default: return "Bilinmeyen";
                }
            }
        }
    }

    public class StudyNote
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Student")]
        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Başlık")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "İçerik")]
        public string Content { get; set; }

        [StringLength(50)]
        [Display(Name = "Kategori")]
        public string Category { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string ShortContent
        {
            get
            {
                if (string.IsNullOrEmpty(Content))
                    return string.Empty;
                
                // Remove markdown formatting for preview
                var plainText = Content
                    .Replace("#", "")
                    .Replace("*", "")
                    .Replace("_", "")
                    .Replace("`", "");
                
                return plainText.Length > 150 
                    ? plainText.Substring(0, 150) + "..." 
                    : plainText;
            }
        }
    }

    public class PushSubscription
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        [StringLength(128)]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Endpoint")]
        public string Endpoint { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "P256dh Key")]
        public string P256dh { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Auth Key")]
        public string Auth { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class NotificationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(128)]
        public string UserId { get; set; }

        public int? StudySessionId { get; set; }

        [Required]
        [StringLength(50)]
        public string NotificationType { get; set; }

        [Required]
        public DateTime SentAt { get; set; }

        public bool Success { get; set; }

        [StringLength(1000)]
        public string ErrorMessage { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("StudySessionId")]
        public virtual StudySession StudySession { get; set; }
    }

    public class PushNotificationViewModel
    {
        public List<PushSubscription> Subscriptions { get; set; }
        public List<NotificationLog> NotificationLogs { get; set; }
        public int TotalActiveSubscriptions { get; set; }
        public int TotalNotificationsSent { get; set; }
    }
}
