using System;
using System.Data.Entity;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using StudentStudyProgram.App_Start;
using StudentStudyProgram.Infrastructure;
using StudentStudyProgram.Migrations;
using StudentStudyProgram.Models;

namespace StudentStudyProgram
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var appData = Server.MapPath("~/App_Data");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            // Create Logs directory if it doesn't exist
            var logsPath = Path.Combine(appData, "Logs");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ApplicationDbContext, Configuration>());

            // Ensure admin user exists
            SeedAdmin.EnsureAdmin();
            SeedDemoData.EnsureDemoData();
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            if (exception != null)
            {
                Logger.LogError(exception, "Uygulama hatası");
                
                // Clear the error
                Server.ClearError();
                
                // Redirect to error page or show friendly error
                Response.Redirect("~/Error");
            }
        }
    }
}
