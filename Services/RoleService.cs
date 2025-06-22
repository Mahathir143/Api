using Api.Data;
using Api.Models.DTOs;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task<PagedResult<RoleDto>> GetRolesAsync(int page, int pageSize, string? search)
        {
            var query = _roleManager.Roles.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name!.Contains(search) ||
                                        r.Description!.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var roles = await query
                .OrderBy(r => r.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var roleDtos = roles.Select(MapToRoleDto).ToList();

            return new PagedResult<RoleDto>
            {
                Items = roleDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<RoleDto?> GetRoleByIdAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            return role == null ? null : MapToRoleDto(role);
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            var role = new ApplicationRole
            {
                Name = createRoleDto.Name,
                Description = createRoleDto.Description,
                IsActive = true,
                SortOrder = createRoleDto.SortOrder
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return MapToRoleDto(role);
        }

        public async Task<RoleDto> UpdateRoleAsync(string id, UpdateRoleDto updateRoleDto)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                throw new ArgumentException("Role not found");

            role.Name = updateRoleDto.Name;
            role.Description = updateRoleDto.Description;
            role.IsActive = updateRoleDto.IsActive;
            role.SortOrder = updateRoleDto.SortOrder;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return MapToRoleDto(role);
        }

        public async Task DeleteRoleAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                throw new ArgumentException("Role not found");

            // Remove related data
            var userRoles = _context.Tbl_UserRoles.Where(ur => ur.RoleId == id);
            _context.Tbl_UserRoles.RemoveRange(userRoles);

            var roleMenus = _context.Tbl_RoleMenus.Where(rm => rm.RoleId == id);
            _context.Tbl_RoleMenus.RemoveRange(roleMenus);

            await _context.SaveChangesAsync();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return roles.Select(MapToRoleDto).ToList();
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            return await _roleManager.RoleExistsAsync(roleName);
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task AssignUserToRoleAsync(string userId, string roleId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var role = await _roleManager.FindByIdAsync(roleId);

            if (user == null || role == null)
                throw new ArgumentException("User or Role not found");

            await _userManager.AddToRoleAsync(user, role.Name!);
        }

        public async Task RemoveUserFromRoleAsync(string userId, string roleId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var role = await _roleManager.FindByIdAsync(roleId);

            if (user == null || role == null)
                throw new ArgumentException("User or Role not found");

            await _userManager.RemoveFromRoleAsync(user, role.Name!);
        }

        private static RoleDto MapToRoleDto(ApplicationRole role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                SortOrder = role.SortOrder
            };
        }
    }
}
