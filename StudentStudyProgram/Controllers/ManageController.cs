using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using StudentStudyProgram.App_Start;
using StudentStudyProgram.Infrastructure;
using StudentStudyProgram.Models;

namespace StudentStudyProgram.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationUserManager _userManager;

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetProfile()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, profile = new {
                userName = user.UserName,
                email = user.Email,
                phone = user.PhoneNumber,
                profilePictureUrl = user.ProfilePictureUrl,
                displayName = user.DisplayName
            } }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateProfile(string userName, string email, string phone)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var user = await UserManager.FindByIdAsync(userId);
                if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı" });

                user.UserName = string.IsNullOrWhiteSpace(userName) ? user.UserName : userName;
                user.Email = string.IsNullOrWhiteSpace(email) ? user.Email : email;
                user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? user.PhoneNumber : phone;

                var result = await UserManager.UpdateAsync(user);
                if (result.Succeeded) return Json(new { success = true, message = "Profil güncellendi" });
                return Json(new { success = false, message = string.Join(" ", result.Errors) });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ChangePassword(string currentPassword, string newPassword)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var result = await UserManager.ChangePasswordAsync(userId, currentPassword, newPassword);
                if (result.Succeeded) return Json(new { success = true, message = "Parola güncellendi" });
                return Json(new { success = false, message = string.Join(" ", result.Errors) });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UploadProfilePicture()
        {
            try
            {
                var file = Request.Files["profilePicture"];
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new { success = false, message = "Lütfen bir resim seçin" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Sadece JPG, PNG, GIF ve WEBP formatları kabul edilir" });
                }

                // Validate file size (max 5MB)
                if (file.ContentLength > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Dosya boyutu maksimum 5MB olabilir" });
                }

                var userId = User.Identity.GetUserId();
                var user = await UserManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });
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
                file.SaveAs(filePath);

                // Update user profile
                user.ProfilePictureUrl = "/Content/Images/ProfilePictures/" + fileName;
                var result = await UserManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new {
                        success = true,
                        message = "Profil resmi güncellendi",
                        pictureUrl = user.ProfilePictureUrl
                    });
                }

                return Json(new { success = false, message = string.Join(" ", result.Errors) });
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Profil resmi yüklenirken hata");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoveProfilePicture()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var user = await UserManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });
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

                user.ProfilePictureUrl = null;
                var result = await UserManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Profil resmi kaldırıldı" });
                }

                return Json(new { success = false, message = string.Join(" ", result.Errors) });
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Profil resmi kaldırılırken hata");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
    }
}



