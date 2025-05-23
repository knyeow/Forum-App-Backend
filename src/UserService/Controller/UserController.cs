using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBuildingBlocks.Logger;
using MyBuildingBlocks.Models.User;
using MyBuildingBlocks.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserService.Utilities;
using UserService.Models;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILoggerService _loggerService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserController(UserDbContext context, ILoggerService loggerService, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _loggerService = loggerService;
        _passwordHasher = passwordHasher;
    }

    // ---------------- Admin Methods ----------------

    // DELETE: api/User/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            _loggerService.LogInfo($"DeleteUser: User with ID {id} not found.");
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _loggerService.LogInfo($"DeleteUser: User with ID {id} deleted.");
        return NoContent();
    }

    // GET: api/User
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
    var users = await _context.Users
        .Include(u => u.Profile)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            Username = u.Username,
            Role = u.Role,
            IsActive = u.IsActive,
            EmailConfirmed = u.EmailConfirmed,
            CreatedAt = u.CreatedAt,
            Profile = u.Profile == null ? null : new UserProfileDto
            {
                Id = u.Profile.Id,
                FirstName = u.Profile.FirstName,
                LastName = u.Profile.LastName,
                ProfilePictureUrl = u.Profile.ProfilePictureUrl,
                LastLoginDate = u.Profile.LastLoginDate
            }
        }).ToListAsync();

         return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/admin-user")]
    public async Task<IActionResult> AdminUpdateUser(Guid id, [FromBody] UserPatch updatedUser)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");


        if (!string.IsNullOrWhiteSpace(updatedUser.Username) && updatedUser.Username != user.Username)
        {
            if (Utilities.ContainsSpecialCharacters(updatedUser.Username))
            {
                return BadRequest("Username can't contain special characters");
            }

            var usernameExists = await _context.Users.AnyAsync(u => u.Username == updatedUser.Username);
            if (usernameExists)
                return BadRequest("Username already in use.");

            user.Username = updatedUser.Username;
        }

        if (!string.IsNullOrWhiteSpace(updatedUser.Role))
            user.Role = updatedUser.Role;
           
        await _context.SaveChangesAsync();

        _loggerService.LogInfo($"AdminUpdateUserProfile: Admin updated profile for user ID {id}.");
        return NoContent();
    }


    // PATCH: api/User/{id}/admin-profile
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/admin-profile")]
    public async Task<IActionResult> AdminUpdateUserProfile(Guid id, [FromBody] UserProfilePatch updatedProfile)
    {
        // Find the user and their profile
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);
        if (profile == null)
            return NotFound("User profile not found.");

        // Update profile fields if provided
        if (!string.IsNullOrWhiteSpace(updatedProfile.FirstName))
            profile.FirstName = updatedProfile.FirstName;

        if (!string.IsNullOrWhiteSpace(updatedProfile.LastName))
            profile.LastName = updatedProfile.LastName;

        if (!string.IsNullOrWhiteSpace(updatedProfile.ProfilePictureUrl))
            profile.ProfilePictureUrl = updatedProfile.ProfilePictureUrl;

        await _context.SaveChangesAsync();

        _loggerService.LogInfo($"AdminUpdateUserProfile: Admin updated profile for user ID {id}.");
        return NoContent();
    }

    // ---------------- User Personal Methods ----------------

    // PATCH: api/User/{id}/username
    [Authorize(Roles = "User")]
    [HttpPatch("username/{newUsername}")]
    public async Task<IActionResult> ChangeUsername(string newUsername)
    {
        if (Utilities.ContainsSpecialCharacters(newUsername))
        {
            return BadRequest("Username can't contain special characters");
        }

        var oldUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == newUsername);

        if (oldUser != null)
        {
            return BadRequest("Username already in use");
        }

        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound();

        var oldUsername = user.Username;
        user.Username = newUsername;
        await _context.SaveChangesAsync();

        _loggerService.LogInfo($"ChangeUsername: User with ID {userId} changed username. {oldUsername} -> {newUsername} ");
        return NoContent();
    }

        // PATCH: api/User/password
    [Authorize(Roles = "User")]
    [HttpPatch("password/{newPassword}")]
    public async Task<IActionResult> ChangePassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || !Utilities.IsPasswordSixDigit(newPassword))
        {
            return BadRequest("Password must be at least 6 characters.");
        }

        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound();

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _context.SaveChangesAsync();

        _loggerService.LogInfo($"ChangePassword: User with ID {userId} changed password.");
        return NoContent();
    }

    // PATCH: api/User/profile
    [Authorize(Roles = "User")]
    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfilePatch updatedProfile)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));
        if (profile == null)
            return NotFound("User profile not found.");

        // Only update allowed fields
        if (!string.IsNullOrWhiteSpace(updatedProfile.FirstName))
            profile.FirstName = updatedProfile.FirstName;

        if (!string.IsNullOrWhiteSpace(updatedProfile.LastName))
            profile.LastName = updatedProfile.LastName;

        if (!string.IsNullOrWhiteSpace(updatedProfile.ProfilePictureUrl))
            profile.ProfilePictureUrl = updatedProfile.ProfilePictureUrl;


        await _context.SaveChangesAsync();

        _loggerService.LogInfo($"UpdateUserProfile: User with ID {userId} updated their profile.");
        return NoContent();
    }


}