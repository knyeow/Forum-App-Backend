using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBuildingBlocks.Logger;
using MyBuildingBlocks.Models.User;
using System.ComponentModel.DataAnnotations;
using Azure.Core;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using MyBuildingBlocks.JWT;
using UserService.Utilities;



[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly UserDbContext _context;

    private readonly JwtTokenService _jwtTokenService;
    private readonly ILoggerService _loggerService;

    private readonly IPasswordHasher<User> _passwordHasher;

    public LoginController(UserDbContext context,JwtTokenService jwtTokenService ,ILoggerService loggerService, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _loggerService = loggerService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost]
    public async Task<IActionResult> UserLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = $"Enter a password" });

        User user;
        var isEmailEntered = false;

        if (!string.IsNullOrWhiteSpace(request.EmailOrUsername))
        {
            if (Utilities.IsValidEmail(request.EmailOrUsername))
            {
                isEmailEntered = true;

                user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == request.EmailOrUsername);
            }
            else if (Utilities.ContainsSpecialCharacters(request.EmailOrUsername))
            {
                return BadRequest(new { message = "Usernames can not contain special characters." });
            }
            else
            {
                isEmailEntered = false;

                user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Username == request.EmailOrUsername);
            }


            if (user == null)
            {
                var problemField = isEmailEntered ? "Email" : "Username";
                var message = $"Invalid {problemField}";
                return Unauthorized(new { message });
            }
            
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (passwordVerificationResult == PasswordVerificationResult.Success)
            {
                // TODO: Return success response (e.g., JWT token or user info)
                var userProfile = _context.UserProfiles.SingleOrDefault(up => up.UserId == user.Id);
                if (userProfile != null)
                {
                    userProfile.LastLoginDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                else
                    _loggerService.LogError($"ProfileNotFound: User don't have profile id: {user.Id}");


                _loggerService.LogInfo($"{user.Username} logged in");
                var token = _jwtTokenService.GenerateToken(user);
                return Ok(new { token });
            }
            else
            {
                return Unauthorized(new { message = $"Invalid password." });
            }

        }
        else
            return BadRequest(new { message = $"Enter a Username or Email" });
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {

        foreach (var objectfield in request.GetType().GetProperties())
        {
            if (string.IsNullOrWhiteSpace(objectfield.GetValue(request)?.ToString()))
            {
                return BadRequest(new { message = $"{objectfield.Name} is required" });
            }
        }

        if (!Utilities.IsValidEmail(request.Email))
            return BadRequest("Please enter a valid email");

        if (Utilities.ContainsSpecialCharacters(request.Username))
            return BadRequest("Username can not contain special characters");

        if (!Utilities.IsPasswordSixDigit(request.Password))
            return BadRequest("Password must contain at least 6 digit");

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
