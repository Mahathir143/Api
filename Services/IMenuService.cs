using Api.Models.DTOs;

namespace Api.Services
{
    public interface IMenuService
    {
        Task<List<MenuDto>> GetUserMenusAsync(string userId);
        Task<List<MenuDto>> GetAllMenusAsync();
        Task<MenuDto?> GetMenuByIdAsync(int id);
        Task<MenuDto> CreateMenuAsync(MenuDto menuDto);
        Task<MenuDto> UpdateMenuAsync(int id, MenuDto menuDto);
        Task DeleteMenuAsync(int id);
        Task ReorderMenusAsync(List<MenuReorderDto> reorderData);
    }
}
