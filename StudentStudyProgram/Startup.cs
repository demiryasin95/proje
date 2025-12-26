using Microsoft.Owin;
using Owin;
using StudentStudyProgram.App_Start;

[assembly: OwinStartup(typeof(StudentStudyProgram.Startup))]

namespace StudentStudyProgram
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            // SignalR Configuration for Real-time Notifications
            app.MapSignalR();

            // Web API will be configured after installing Microsoft.AspNet.WebApi.WebHost package
            // var config = new HttpConfiguration();
            // WebApiConfig.Register(config);
            // app.UseWebApi(config);

            StudentStudyProgram.App_Start.SeedAdmin.EnsureAdmin();
            StudentStudyProgram.App_Start.SeedDemoData.EnsureDemoData();
        }
    }
}
