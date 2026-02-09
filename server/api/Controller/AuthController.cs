using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using api.dto;
using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api;

[ApiController]
[Route("auth")]
public class AuthController(ChatDbContext ctx, JwtService jwt) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] LoginRequest req)
    {
        // Validation -> handled by GlobalExceptionHandler (400)
        if (string.IsNullOrWhiteSpace(req.Username))
            throw new ValidationException("Username is required");

        if (string.IsNullOrWhiteSpace(req.Password))
            throw new ValidationException("Password is required");

        var taken = await ctx.Users.AnyAsync(u => u.Nickname == req.Username, HttpContext.RequestAborted);
        if (taken)
            throw new ValidationException("Name is already taken");

        var salt = Guid.NewGuid().ToString();
        var hash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(req.Password + salt))
        );

        var u = new User
        {
            Id = Guid.NewGuid().ToString(),
            Nickname = req.Username,
            Salt = salt,
            Hash = hash,
            Role = "User"
        };

        ctx.Users.Add(u);
        await ctx.SaveChangesAsync(HttpContext.RequestAborted);

        var token = jwt.GenerateToken(u.Id, u.Role);
        return Ok(new LoginResponse(token));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        // Validation -> handled by GlobalExceptionHandler (400)
        if (string.IsNullOrWhiteSpace(req.Username))
            throw new ValidationException("Username is required");

        if (string.IsNullOrWhiteSpace(req.Password))
            throw new ValidationException("Password is required");

        var user = await ctx.Users.FirstOrDefaultAsync(
            u => u.Nickname == req.Username,
            HttpContext.RequestAborted
        );

        // Invalid credentials -> handled by GlobalExceptionHandler (401)
        if (user is null)
            throw new UnauthorizedAccessException("Not valid credentials");

        var hash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(req.Password + user.Salt))
        );

        if (hash != user.Hash)
            throw new UnauthorizedAccessException("Not valid credentials");

        var token = jwt.GenerateToken(user.Id, user.Role);
        return Ok(new LoginResponse(token));
    }
}
