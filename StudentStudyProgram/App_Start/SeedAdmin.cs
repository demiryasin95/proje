using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using StudentStudyProgram.Models;
using System;
using System.Linq;

namespace StudentStudyProgram.App_Start
{
    public static class SeedAdmin
    {
        public static void EnsureAdmin()
        {
            using (var context = new ApplicationDbContext())
            {
                var roleStore = new RoleStore<IdentityRole>(context);
                var roleManager = new RoleManager<IdentityRole>(roleStore);

                if (!roleManager.RoleExists("Admin"))
                {
                    roleManager.Create(new IdentityRole("Admin"));
                }
                // Roles for the intended product model
                if (!roleManager.RoleExists("Teacher"))
                {
                    roleManager.Create(new IdentityRole("Teacher"));
                }
                if (!roleManager.RoleExists("Student"))
                {
                    roleManager.Create(new IdentityRole("Student"));
                }

                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);

                var admin = userManager.FindByName("admin");
                if (admin == null)
                {
                    // IMPORTANT: Change this password immediately after first deployment!
                    // For production, use a strong password from environment variable or secure configuration
                    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD") ?? "ChangeMe@2025!";

                    admin = new ApplicationUser { UserName = "admin", Email = "admin@etut.local", EmailConfirmed = true };
                    userManager.Create(admin, adminPassword);
                    userManager.AddToRole(admin.Id, "Admin");

                    // Log warning if using default password
                    if (adminPassword == "ChangeMe@2025!")
                    {
                        System.Diagnostics.Debug.WriteLine("WARNING: Using default admin password. Set ADMIN_INITIAL_PASSWORD environment variable!");
                    }
                }
            }
        }
    }
}
