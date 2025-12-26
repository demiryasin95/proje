using System;
using System.Web.Helpers;
using System.Web.Mvc;

namespace StudentStudyProgram.Infrastructure
{
    /// <summary>
    /// Validates Anti-Forgery token from request headers for AJAX/JSON posts.
    /// Expects the form token in header: RequestVerificationToken (or X-RequestVerificationToken).
    /// </summary>
    public class ValidateHeaderAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            try
            {
                var request = filterContext?.HttpContext?.Request;
                if (request == null) return;

                // Skip if not an authenticated action? Keep strict; caller can omit attribute.
                var cookieToken = request.Cookies?[AntiForgeryConfig.CookieName]?.Value;
                var formToken = request.Headers["RequestVerificationToken"] ?? request.Headers["X-RequestVerificationToken"];

                AntiForgery.Validate(cookieToken, formToken);
            }
            catch
            {
                // Block request
                filterContext.Result = new HttpStatusCodeResult(403);
            }
        }
    }
}


