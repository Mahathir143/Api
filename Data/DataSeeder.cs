using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager, roleManager);
            await SeedMenusAsync(context);
            await SeedRoleMenusAsync(context, roleManager);
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            var roles = new[]
            {
                new ApplicationRole { Name = "Admin", Description = "Administrator with full access", SortOrder = 1 },
                new ApplicationRole { Name = "Moderator", Description = "Moderator with limited access", SortOrder = 2 },
                new ApplicationRole { Name = "User", Description = "Regular user", SortOrder = 3 }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name!))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            if (!userManager.Users.Any())
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!@#");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        private static async Task SeedMenusAsync(ApplicationDbContext context)
        {
            if (!context.Tbl_Menus.Any())
            {
                var menus = new List<Tbl_Menu>
                {
                    // Main menus (no explicit Id values)
                    new() { Title = "Dashboard", Icon = "fas fa-tachometer-alt", Url = "/dashboard", SortOrder = 1 },
                    new() { Title = "User Management", Icon = "fas fa-users", Url = "#", SortOrder = 2 },
                    new() { Title = "System", Icon = "fas fa-cogs", Url = "#", SortOrder = 3 },
                    new() { Title = "Security", Icon = "fas fa-shield-alt", Url = "#", SortOrder = 4 }
                };

                context.Tbl_Menus.AddRange(menus);
                await context.SaveChangesAsync();

                // Now add submenus with ParentId references
                var userManagementMenu = await context.Tbl_Menus.FirstAsync(m => m.Title == "User Management");
                var systemMenu = await context.Tbl_Menus.FirstAsync(m => m.Title == "System");
                var securityMenu = await context.Tbl_Menus.FirstAsync(m => m.Title == "Security");

                var subMenus = new List<Tbl_Menu>
                {
                    // User Management submenu
                    new() { Title = "Users", Icon = "fas fa-user", Url = "/users", ParentId = userManagementMenu.Id, SortOrder = 1 },
                    new() { Title = "Roles", Icon = "fas fa-user-tag", Url = "/roles", ParentId = userManagementMenu.Id, SortOrder = 2 },
                    
                    // System submenu
                    new() { Title = "Menu Management", Icon = "fas fa-sitemap", Url = "/menus", ParentId = systemMenu.Id, SortOrder = 1 },
                    new() { Title = "Settings", Icon = "fas fa-cog", Url = "/settings", ParentId = systemMenu.Id, SortOrder = 2 },
                    new() { Title = "Audit Logs", Icon = "fas fa-history", Url = "/audit-logs", ParentId = systemMenu.Id, SortOrder = 3 },
                    
                    // Security submenu
                    new() { Title = "2FA Setup", Icon = "fas fa-mobile-alt", Url = "/2fa-setup", ParentId = securityMenu.Id, SortOrder = 1 },
                    new() { Title = "Login Attempts", Icon = "fas fa-sign-in-alt", Url = "/login-attempts", ParentId = securityMenu.Id, SortOrder = 2 }
                };

                context.Tbl_Menus.AddRange(subMenus);
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
                    var allMenus = await context.Tbl_Menus.ToListAsync();
                    var roleMenus = new List<Tbl_RoleMenu>();

                    // Admin has access to all menus
                    foreach (var menu in allMenus)
                    {
                        roleMenus.Add(new Tbl_RoleMenu
                        {
                            RoleId = adminRole.Id,
                            MenuId = menu.Id,
                            CanView = true,
                            CanEdit = true,
                            CanDelete = true
                        });
                    }

                    // User has limited access
                    var dashboardMenu = allMenus.First(m => m.Title == "Dashboard");
                    var securityMenu = allMenus.First(m => m.Title == "Security");
                    var twoFactorMenu = allMenus.First(m => m.Title == "2FA Setup");

                    var userMenuIds = new[] { dashboardMenu.Id, securityMenu.Id, twoFactorMenu.Id };
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