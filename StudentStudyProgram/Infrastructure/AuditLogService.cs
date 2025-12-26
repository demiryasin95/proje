using System;
using System.Threading.Tasks;
using StudentStudyProgram.Models;

namespace StudentStudyProgram.Infrastructure
{
    internal static class AuditLogService
    {
        public static async Task TryLogAsync(ApplicationDbContext db, string userName, string userId, string action, string entityType = null, int? entityId = null, string details = null)
        {
            try
            {
                db.AuditLogs.Add(new AuditLog
                {
                    CreatedAt = DateTime.Now,
                    UserName = userName,
                    UserId = userId,
                    Action = action ?? "Unknown",
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details
                });
                await db.SaveChangesAsync();
            }
            catch
            {
                // Never fail the main operation because of audit logging.
            }
        }
    }
}


