using Api.Models.DTOs;
using Api.Services;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;
        private readonly IAuditService _auditService;

        public MenuController(IMenuService menuService, IAuditService auditService)
        {
            _menuService = menuService;
            _auditService = auditService;
        }

        [HttpGet("user-menus")]
        public async Task<IActionResult> GetUserMenus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var menus = await _menuService.GetUserMenusAsync(userId);
                return Ok(menus);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllMenus()
        {
            try
            {
                var menus = await _menuService.GetAllMenusAsync();
                return Ok(menus);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetMenuById(int id)
        {
            try
            {
                var menu = await _menuService.GetMenuByIdAsync(id);
                if (menu == null)
                    return NotFound();

                return Ok(menu);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateMenu([FromBody] MenuDto menuDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var menu = await _menuService.CreateMenuAsync(menuDto);

                await _auditService.LogAsync(
                    userId,
                    "Create Menu",
                    "Menu",
                    menu.Id.ToString(),
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Created menu: {menu.Title}"
                );

                return CreatedAtAction(nameof(GetMenuById), new { id = menu.Id }, menu);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateMenu(int id, [FromBody] MenuDto menuDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var menu = await _menuService.UpdateMenuAsync(id, menuDto);

                await _auditService.LogAsync(
                    userId,
                    "Update Menu",
                    "Menu",
                    id.ToString(),
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Updated menu: {menu.Title}"
                );

                return Ok(menu);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _menuService.DeleteMenuAsync(id);

                await _auditService.LogAsync(
                    userId,
                    "Delete Menu",
                    "Menu",
                    id.ToString(),
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Deleted menu with ID: {id}"
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reorder")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ReorderMenus([FromBody] List<MenuReorderDto> reorderData)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _menuService.ReorderMenusAsync(reorderData);

                await _auditService.LogAsync(
                    userId,
                    "Reorder Menus",
                    "Menu",
                    null,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    "Menu order updated"
                );

                return Ok(new { message = "Menus reordered successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string GetClientIPAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

            if (Request.Headers.ContainsKey("X-Real-IP"))
                return Request.Headers["X-Real-IP"].ToString();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
