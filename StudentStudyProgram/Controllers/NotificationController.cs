using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using StudentStudyProgram.Models;
using Microsoft.AspNet.Identity;
using StudentStudyProgram.Infrastructure;
using WebPush;
using Newtonsoft.Json;
using ModelPushSubscription = StudentStudyProgram.Models.PushSubscription;

namespace StudentStudyProgram.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Notification - Push Notifications Management Page
        [Authorize(Roles = "Admin,Teacher")]
        public ActionResult Index()
        {
            // Get all users with push subscriptions
            var subscriptions = db.PushSubscriptions
                .Include(s => s.User)
                .Where(s => s.IsActive)
                .ToList();

            // Get notification logs
            var notificationLogs = db.NotificationLogs
                .OrderByDescending(n => n.SentAt)
                .Take(50)
                .ToList();

            var viewModel = new PushNotificationViewModel
            {
                Subscriptions = subscriptions,
                NotificationLogs = notificationLogs,
                TotalActiveSubscriptions = subscriptions.Count,
                TotalNotificationsSent = db.NotificationLogs.Count()
            };

            return View(viewModel);
        }

        // Test page
        public ActionResult Test()
        {
            return View();
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult Subscribe(string endpoint, string p256dh, string auth)
        {
            try
            {
                var userId = User.Identity.GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı!" });
                }

                // Check if subscription already exists
                var existingSubscription = db.PushSubscriptions
                    .FirstOrDefault(s => s.UserId == userId && s.Endpoint == endpoint);

                if (existingSubscription != null)
                {
                    // Update existing subscription
                    existingSubscription.P256dh = p256dh;
                    existingSubscription.Auth = auth;
                    existingSubscription.IsActive = true;
                }
                else
                {
                    // Create new subscription
                    var subscription = new ModelPushSubscription
                    {
                        UserId = userId,
                        Endpoint = endpoint,
                        P256dh = p256dh,
                        Auth = auth,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    db.PushSubscriptions.Add(subscription);
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Bildirim aboneliği başarıyla oluşturuldu!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Push subscription error");
                return Json(new { success = false, message = "Abonelik oluşturulurken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        // Delete subscription
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult DeleteSubscription(int id)
        {
            try
            {
                var subscription = db.PushSubscriptions.Find(id);
                
                if (subscription == null)
                {
                    return Json(new { success = false, message = "Abonelik bulunamadı!" });
                }

                db.PushSubscriptions.Remove(subscription);
                db.SaveChanges();

                Logger.LogInfo($"Push subscription deleted: ID {id}");
                return Json(new { success = true, message = "Abonelik başarıyla silindi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Delete subscription error");
                return Json(new { success = false, message = "Abonelik silinirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult Unsubscribe(string endpoint)
        {
            try
            {
                var userId = User.Identity.GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı!" });
                }

                var subscription = db.PushSubscriptions
                    .FirstOrDefault(s => s.UserId == userId && s.Endpoint == endpoint);

                if (subscription != null)
                {
                    subscription.IsActive = false;
                    db.SaveChanges();
                }

                return Json(new { success = true, message = "Bildirim aboneliği iptal edildi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unsubscribe error");
                return Json(new { success = false, message = "Abonelik iptal edilirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        [HttpGet]
        public JsonResult GetVapidPublicKey()
        {
            try
            {
                // Read from Web.config
                var publicKey = System.Configuration.ConfigurationManager.AppSettings["VapidPublicKey"];
                
                if (string.IsNullOrEmpty(publicKey))
                {
                    return Json(new { success = false, message = "VAPID Public Key Web.config'de tanımlanmamış!" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, publicKey = publicKey }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get VAPID key error");
                return Json(new { success = false, message = "VAPID key alınırken bir hata oluştu." }, JsonRequestBehavior.AllowGet);
            }
        }

        // Send push notification to a specific user
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult SendNotification(string userId, string title, string body, string url = null)
        {
            try
            {
                var subscription = db.PushSubscriptions
                    .FirstOrDefault(s => s.UserId == userId && s.IsActive);

                if (subscription == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bildirimlere abone değil!" });
                }

                // Send the push notification
                SendPushNotification(subscription, title, body, url);

                return Json(new { success = true, message = "Bildirim gönderildi!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Send notification error");
                return Json(new { success = false, message = "Bildirim gönderilirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        // Send push notification to multiple users
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateHeaderAntiForgeryToken]
        public JsonResult SendBulkNotification(List<string> userIds, string title, string body, string url = null)
        {
            try
            {
                var subscriptions = db.PushSubscriptions
                    .Where(s => userIds.Contains(s.UserId) && s.IsActive)
                    .ToList();

                if (!subscriptions.Any())
                {
                    return Json(new { success = false, message = "Hiçbir kullanıcı bildirimlere abone değil!" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        SendPushNotification(subscription, title, body, url);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Failed to send notification to user {subscription.UserId}");
                        failCount++;
                    }
                }

                return Json(new {
                    success = true,
                    message = $"{successCount} bildirim gönderildi, {failCount} başarısız."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Bulk notification error");
                return Json(new { success = false, message = "Toplu bildirim gönderilirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        // Helper method to send push notification
        private void SendPushNotification(ModelPushSubscription subscription, string title, string body, string url = null)
        {
            try
            {
                // Get VAPID keys from config
                var publicKey = System.Configuration.ConfigurationManager.AppSettings["VapidPublicKey"];
                // Try to get private key from environment variable first, then fallback to config
                var privateKey = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY")
                    ?? System.Configuration.ConfigurationManager.AppSettings["VapidPrivateKey"];
                var subject = System.Configuration.ConfigurationManager.AppSettings["VapidSubject"];

                if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
                {
                    throw new Exception("VAPID keys are not configured. Set VAPID_PRIVATE_KEY environment variable.");
                }

                // Create the push subscription object
                var pushSubscription = new WebPush.PushSubscription(
                    subscription.Endpoint,
                    subscription.P256dh,
                    subscription.Auth
                );

                // Create the VAPID details
                var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

                // Create the payload
                var payload = JsonConvert.SerializeObject(new
                {
                    title = title,
                    body = body,
                    icon = "/Content/Images/icon-192.png",
                    badge = "/Content/Images/badge-72.png",
                    url = url ?? "/"
                });

                // Send the notification
                var webPushClient = new WebPushClient();
                webPushClient.SendNotification(pushSubscription, payload, vapidDetails);

                Logger.LogInfo($"Push notification sent to user {subscription.UserId}: {title}");
            }
            catch (WebPushException ex)
            {
                Logger.LogError(ex, $"WebPush error for user {subscription.UserId}");
                
                // If subscription is no longer valid, deactivate it
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    subscription.IsActive = false;
                    db.SaveChanges();
                    Logger.LogInfo($"Deactivated invalid subscription for user {subscription.UserId}");
                }
                
                throw;
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
