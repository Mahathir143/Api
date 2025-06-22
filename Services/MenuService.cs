using Api.Data;
using Api.Models;
using Api.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Services
{
    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public MenuService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<List<MenuDto>> GetUserMenusAsync(string userId)
        {
            var cacheKey = $"user_menus_{userId}";

            if (_cache.TryGetValue(cacheKey, out List<MenuDto>? cachedMenus))
            {
                return cachedMenus!;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new List<MenuDto>();

            var userRoles = await _userManager.GetRolesAsync(user);

            var menus = await _context.Tbl_Menus
                .Where(m => m.IsActive)
                .Where(m => _context.Tbl_RoleMenus
                    .Any(rm => userRoles.Contains(rm.Role.Name!) && rm.MenuId == m.Id && rm.CanView))
                .Include(m => m.Children.Where(c => c.IsActive))
                .Where(m => m.ParentId == null)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            var menuDtos = BuildMenuHierarchy(menus, userRoles);

            var cacheExpiration = TimeSpan.FromMinutes(
                _configuration.GetValue<int>("CacheSettings:MenuCacheExpirationMinutes", 30));

            _cache.Set(cacheKey, menuDtos, cacheExpiration);

            return menuDtos;
        }

        public async Task<List<MenuDto>> GetAllMenusAsync()
        {
            var menus = await _context.Tbl_Menus
                .Include(m => m.Children)
                .Where(m => m.ParentId == null)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            return BuildMenuHierarchy(menus);
        }

        public async Task<MenuDto?> GetMenuByIdAsync(int id)
        {
            var menu = await _context.Tbl_Menus
                .Include(m => m.Children)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null)
                return null;

            return MapToMenuDto(menu);
        }

        public async Task<MenuDto> CreateMenuAsync(MenuDto menuDto)
        {
            var menu = new Tbl_Menu
            {
                Title = menuDto.Title,
                Icon = menuDto.Icon,
                Url = menuDto.Url,
                ParentId = menuDto.ParentId,
                SortOrder = menuDto.SortOrder,
                IsActive = menuDto.IsActive,
                Description = menuDto.Description
            };

            _context.Tbl_Menus.Add(menu);
            await _context.SaveChangesAsync();

            // Clear cache
            ClearMenuCache();

            return MapToMenuDto(menu);
        }

        public async Task<MenuDto> UpdateMenuAsync(int id, MenuDto menuDto)
        {
            var menu = await _context.Tbl_Menus.FindAsync(id);
            if (menu == null)
                throw new ArgumentException("Menu not found");

            menu.Title = menuDto.Title;
            menu.Icon = menuDto.Icon;
            menu.Url = menuDto.Url;
            menu.ParentId = menuDto.ParentId;
            menu.SortOrder = menuDto.SortOrder;
            menu.IsActive = menuDto.IsActive;
            menu.Description = menuDto.Description;
            menu.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Clear cache
            ClearMenuCache();

            return MapToMenuDto(menu);
        }

        public async Task DeleteMenuAsync(int id)
        {
            var menu = await _context.Tbl_Menus
                .Include(m => m.Children)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null)
                throw new ArgumentException("Menu not found");

            // Check if menu has children
            if (menu.Children.Any())
            {
                throw new InvalidOperationException("Cannot delete menu with child items");
            }

            // Remove role permissions first
            var roleMenus = _context.Tbl_RoleMenus.Where(rm => rm.MenuId == id);
            _context.Tbl_RoleMenus.RemoveRange(roleMenus);

            _context.Tbl_Menus.Remove(menu);
            await _context.SaveChangesAsync();

            // Clear cache
            ClearMenuCache();
        }

        public async Task ReorderMenusAsync(List<MenuReorderDto> reorderData)
        {
            foreach (var item in reorderData)
            {
                var menu = await _context.Tbl_Menus.FindAsync(item.Id);
                if (menu != null)
                {
                    menu.SortOrder = item.SortOrder;
                    menu.ParentId = item.ParentId;
                    menu.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            // Clear cache
            ClearMenuCache();
        }

        private List<MenuDto> BuildMenuHierarchy(List<Tbl_Menu> menus, IList<string>? userRoles = null)
        {
            var menuDtos = new List<MenuDto>();

            foreach (var menu in menus)
            {
                var menuDto = MapToMenuDto(menu);

                if (menu.Children.Any())
                {
                    var childMenus = menu.Children
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.SortOrder)
                        .ToList();

                    if (userRoles != null)
                    {
                        // Filter children based on user roles
                        childMenus = childMenus
                            .Where(c => _context.Tbl_RoleMenus
                                .Any(rm => userRoles.Contains(rm.Role.Name!) && rm.MenuId == c.Id && rm.CanView))
                            .ToList();
                    }

                    menuDto.Children = BuildMenuHierarchy(childMenus, userRoles);
                }

                menuDtos.Add(menuDto);
            }

            return menuDtos;
        }

        private MenuDto MapToMenuDto(Tbl_Menu menu)
        {
            return new MenuDto
            {
                Id = menu.Id,
                Title = menu.Title,
                Icon = menu.Icon,
                Url = menu.Url,
                ParentId = menu.ParentId,
                SortOrder = menu.SortOrder,
                IsActive = menu.IsActive,
                Description = menu.Description,
                Children = new List<MenuDto>()
            };
        }

        private void ClearMenuCache()
        {
            // In a real application, you might want to use a more sophisticated cache invalidation strategy
            var cacheKeys = new List<string>();

            // Get all user IDs to clear their menu cache
            var userIds = _context.Users.Select(u => u.Id).ToList();
            foreach (var userId in userIds)
            {
                cacheKeys.Add($"user_menus_{userId}");
            }

            foreach (var key in cacheKeys)
            {
                _cache.Remove(key);
            }
        }
    }
}
