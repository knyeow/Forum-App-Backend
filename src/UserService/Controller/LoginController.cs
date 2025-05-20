using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class LoginController : ControllerBase
{
    private readonly UserDbContext _context;

    public LoginController(UserDbContext context)
    {
        _context = context;
    }

    public IActionResult Register()
    {
        return Ok();
    }

}
