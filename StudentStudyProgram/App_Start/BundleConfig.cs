using System.Web;
using System.Web.Optimization;

namespace StudentStudyProgram.App_Start
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      ));

            // Enhanced features bundle
            bundles.Add(new ScriptBundle("~/bundles/enhanced-features").Include(
                      "~/Scripts/toast-notifications.js",
                      "~/Scripts/widgets-and-bulk.js",
                      "~/Scripts/signalr-notifications.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/site.css",
                      "~/Content/melody-theme.css",
                      "~/Content/modern-calendar.css",
                      "~/Content/calendar.css",
                      "~/Content/toast-notifications.css",
                      "~/Content/widgets-and-bulk.css"));
            
            // Disable bundling and minification for development (to see changes immediately)
            BundleTable.EnableOptimizations = false;
        }
    }
}
