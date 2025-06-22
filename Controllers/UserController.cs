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
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;

        public UserController(IUserService userService, IAuditService auditService)
        {
            _userService = userService;
            _auditService = auditService;
        }

        [HttpGet]
        [Authorize(Policy = "ModeratorOrAdmin")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            try
            {
                var result = await _userService.GetUsersAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "ModeratorOrAdmin")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userService.CreateUserAsync(createUserDto);

                await _auditService.LogAsync(
                    currentUserId,
                    "Create User",
                    "User",
                    user.Id,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Created user: {user.Email}"
                );

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Users can only update their own profile unless they're admin/moderator
                if (currentUserId != id && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
                {
                    return Forbid();
                }

                var user = await _userService.UpdateUserAsync(id, updateUserDto);

                await _auditService.LogAsync(
                    currentUserId,
                    "Update User",
                    "User",
                    id,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Updated user: {user.Email}"
                );

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Prevent self-deletion
                if (currentUserId == id)
                {
                    return BadRequest(new { message = "Cannot delete your own account" });
                }

                await _userService.DeleteUserAsync(id);

                await _auditService.LogAsync(
                    currentUserId,
                    "Delete User",
                    "User",
                    id,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Deleted user with ID: {id}"
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/roles")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AssignRoles(string id, [FromBody] AssignRolesDto assignRolesDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _userService.AssignRolesAsync(id, assignRolesDto.RoleNames);

                await _auditService.LogAsync(
                    currentUserId,
                    "Assign Roles",
                    "User",
                    id,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Assigned roles: {string.Join(", ", assignRolesDto.RoleNames)}"
                );

                return Ok(new { message = "Roles assigned successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userService.GetUserByIdAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userService.UpdateProfileAsync(userId, updateProfileDto);

                await _auditService.LogAsync(
                    userId,
                    "Update Profile",
                    "User",
                    userId,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    "User profile updated"
                );

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _userService.ChangePasswordAsync(userId, changePasswordDto);

                await _auditService.LogAsync(
                    userId,
                    "Change Password",
                    "User",
                    userId,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    "User password changed"
                );

                return Ok(new { message = "Password changed successfully" });
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

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
