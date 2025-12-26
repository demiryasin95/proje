using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace StudentStudyProgram.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string ProfilePictureUrl { get; set; }
        public string DisplayName { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<StudySession> StudySessions { get; set; }
        public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<StudyNote> StudyNotes { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>()
                .HasOptional(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<Teacher>()
                .HasOptional(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<StudySession>()
                .HasRequired(s => s.Teacher)
                .WithMany(t => t.StudySessions)
                .HasForeignKey(s => s.TeacherId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<StudySession>()
                .HasRequired(s => s.Student)
                .WithMany(s => s.StudySessions)
                .HasForeignKey(s => s.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<StudySession>()
                .HasRequired(s => s.TimeSlot)
                .WithMany(t => t.StudySessions)
                .HasForeignKey(s => s.TimeSlotId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<StudySession>()
                .HasRequired(s => s.Classroom)
                .WithMany(c => c.StudySessions)
                .HasForeignKey(s => s.ClassroomId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeacherAvailability>()
                .HasRequired(ta => ta.Teacher)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(ta => ta.TeacherId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeacherAvailability>()
                .HasRequired(ta => ta.TimeSlot)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(ta => ta.TimeSlotId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<StudyNote>()
                .HasRequired(sn => sn.Student)
                .WithMany()
                .HasForeignKey(sn => sn.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PushSubscription>()
                .HasOptional(ps => ps.User)
                .WithMany()
                .HasForeignKey(ps => ps.UserId);
        }
    }
}