using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager);
            await SeedMenusAsync(context);
            await SeedConfigurationsAsync(context);
            await SeedRoleMenusAsync(context, roleManager);
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            var roles = new[]
            {
                new ApplicationRole { Name = "Admin", Description = "System Administrator", SortOrder = 1 },
                new ApplicationRole { Name = "Moderator", Description = "Content Moderator", SortOrder = 2 },
                new ApplicationRole { Name = "User", Description = "Regular User", SortOrder = 3 }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name!))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@secureauth.com",
                Email = "admin@secureauth.com",
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true
            };

            if (await userManager.FindByEmailAsync(adminUser.Email) == null)
            {
                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var testUser = new ApplicationUser
            {
                UserName = "mahathir143@example.com",
                Email = "mahathir143@example.com",
                FirstName = "Mahathir",
                LastName = "Test User",
                EmailConfirmed = true,
                IsActive = true
            };

            if (await userManager.FindByEmailAsync(testUser.Email) == null)
            {
                var result = await userManager.CreateAsync(testUser, "Test@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
            }
        }

        private static async Task SeedMenusAsync(ApplicationDbContext context)
        {
            if (!context.Tbl_Menus.Any())
            {
                var menus = new List<Tbl_Menu>
                {
                    new() { Id = 1, Title = "Dashboard", Icon = "fas fa-tachometer-alt", Url = "/dashboard", SortOrder = 1 },
                    new() { Id = 2, Title = "User Management", Icon = "fas fa-users", Url = "#", SortOrder = 2 },
                    new() { Id = 3, Title = "System", Icon = "fas fa-cogs", Url = "#", SortOrder = 3 },
                    new() { Id = 4, Title = "Security", Icon = "fas fa-shield-alt", Url = "#", SortOrder = 4 },
                    
                    // User Management submenu
                    new() { Id = 21, Title = "Users", Icon = "fas fa-user", Url = "/users", ParentId = 2, SortOrder = 1 },
                    new() { Id = 22, Title = "Roles", Icon = "fas fa-user-tag", Url = "/roles", ParentId = 2, SortOrder = 2 },
                    
                    // System submenu
                    new() { Id = 31, Title = "Menu Management", Icon = "fas fa-sitemap", Url = "/menus", ParentId = 3, SortOrder = 1 },
                    new() { Id = 32, Title = "Settings", Icon = "fas fa-cog", Url = "/settings", ParentId = 3, SortOrder = 2 },
                    new() { Id = 33, Title = "Audit Logs", Icon = "fas fa-history", Url = "/audit-logs", ParentId = 3, SortOrder = 3 },
                    
                    // Security submenu
                    new() { Id = 41, Title = "2FA Setup", Icon = "fas fa-mobile-alt", Url = "/2fa-setup", ParentId = 4, SortOrder = 1 },
                    new() { Id = 42, Title = "Login Attempts", Icon = "fas fa-sign-in-alt", Url = "/login-attempts", ParentId = 4, SortOrder = 2 }
                };

                context.Tbl_Menus.AddRange(menus);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedConfigurationsAsync(ApplicationDbContext context)
        {
            if (!context.Tbl_Configurations.Any())
            {
                var configs = new List<Tbl_Configuration>
                {
                    new() { Key = "RecaptchaSiteKey", Value = "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI", Category = "Security", IsClientVisible = true, Description = "Google reCAPTCHA Site Key" },
                    new() { Key = "ApplicationName", Value = "SecureAuth PWA", Category = "General", IsClientVisible = true, Description = "Application Name" },
                    new() { Key = "CompanyName", Value = "Your Company Name", Category = "General", IsClientVisible = true, Description = "Company Name" },
                    new() { Key = "Version", Value = "1.0.0", Category = "General", IsClientVisible = true, Description = "Application Version" },
                    new() { Key = "SessionTimeoutHours", Value = "2", Category = "Security", IsClientVisible = true, Description = "Session timeout in hours" },
                    new() { Key = "MaxLoginAttempts", Value = "3", Category = "Security", IsClientVisible = true, Description = "Maximum login attempts before lockout" },
                    new() { Key = "LockoutDurationMinutes", Value = "5", Category = "Security", IsClientVisible = true, Description = "Account lockout duration in minutes" }
                };

                context.Tbl_Configurations.AddRange(configs);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedRoleMenusAsync(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager)
        {
            if (!context.Tbl_RoleMenus.Any())
            {
                var adminRole = await roleManager.FindByNameAsync("Admin");
                var userRole = await roleManager.FindByNameAsync("User");

                if (adminRole != null && userRole != null)
                {
                    var menuIds = context.Tbl_Menus.Select(m => m.Id).ToList();
                    var roleMenus = new List<Tbl_RoleMenu>();

                    // Admin has access to all menus
                    foreach (var menuId in menuIds)
                    {
                        roleMenus.Add(new Tbl_RoleMenu
                        {
                            RoleId = adminRole.Id,
                            MenuId = menuId,
                            CanView = true,
                            CanEdit = true,
                            CanDelete = true
                        });
                    }

                    // User has limited access
                    var userMenuIds = new[] { 1, 4, 41 }; // Dashboard, Security, 2FA Setup
                    foreach (var menuId in userMenuIds)
                    {
                        roleMenus.Add(new Tbl_RoleMenu
                        {
                            RoleId = userRole.Id,
                            MenuId = menuId,
                            CanView = true,
                            CanEdit = false,
                            CanDelete = false
                        });
                    }

                    context.Tbl_RoleMenus.AddRange(roleMenus);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
