using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBuildingBlocks.Logger;
using MyBuildingBlocks.Models.User;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly UserDbContext _context;

    private readonly ILoggerService _loggerService;

    private readonly IPasswordHasher<User> _passwordHasher;

    public LoginController(UserDbContext context, ILoggerService loggerService, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _loggerService = loggerService;
        _passwordHasher = passwordHasher;
        
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody]RegisterRequest request)
    {
        
        foreach (var objectfield in request.GetType().GetProperties())
        {
            if (string.IsNullOrWhiteSpace(objectfield.GetValue(request)?.ToString()))
            {
                return BadRequest(new { message = $"{objectfield.Name} is required" });
            }
        }


        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest("This email is already registered.");
        }
        else if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest("This Username is already registered");

        var user = new User
        {
            Email = request.Email,
            Username = request.Username
        };

        //password hashing
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var profile = new UserProfile
        {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        user.Profile = profile;


        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _loggerService.LogError($"Error during registration: {e.Message}");
            return StatusCode(500, "An error occurred while processing your request.");
        }

        _loggerService.LogInfo
        ($"Yeni bir kullanıcı kayıt yaptı, email:{user.Email}, userName:{user.Username}");

        return Ok("Succesfully Registered");
    }

}
