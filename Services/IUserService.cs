using Api.Models.DTOs;

namespace Api.Services
{
    public interface IUserService
    {
        Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search);
        Task<UserDto?> GetUserByIdAsync(string id);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> UpdateUserAsync(string id, UpdateUserDto updateUserDto);
        Task DeleteUserAsync(string id);
        Task AssignRolesAsync(string userId, List<string> roleNames);
        Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileDto updateProfileDto);
        Task ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    }
}
