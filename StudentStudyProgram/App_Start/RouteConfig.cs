using System.Web.Mvc;
using System.Web.Routing;

namespace StudentStudyProgram.App_Start
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "ManageIndex",
                url: "Manage/Index",
                defaults: new { controller = "Manage", action = "Index" }
            );

            routes.MapRoute(
                name: "ManageGetProfile",
                url: "Manage/GetProfile",
                defaults: new { controller = "Manage", action = "GetProfile" }
            );

            routes.MapRoute(
                name: "Manage",
                url: "Manage/{action}/{id}",
                defaults: new { controller = "Manage", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "ManageUpdateProfile",
                url: "Manage/UpdateProfile",
                defaults: new { controller = "Manage", action = "UpdateProfile" }
            );

            routes.MapRoute(
                name: "ManageChangePassword",
                url: "Manage/ChangePassword",
                defaults: new { controller = "Manage", action = "ChangePassword" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
            );
        }
    }
}
