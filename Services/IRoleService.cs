using Api.Models.DTOs;

namespace Api.Services
{
    public interface IRoleService
    {
        Task<PagedResult<RoleDto>> GetRolesAsync(int page, int pageSize, string? search);
        Task<RoleDto?> GetRoleByIdAsync(string id);
        Task<RoleDto> CreateRoleAsync(CreateRoleDto createRoleDto);
        Task<RoleDto> UpdateRoleAsync(string id, UpdateRoleDto updateRoleDto);
        Task DeleteRoleAsync(string id);
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<bool> RoleExistsAsync(string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task AssignUserToRoleAsync(string userId, string roleId);
        Task RemoveUserFromRoleAsync(string userId, string roleId);
    }
}
