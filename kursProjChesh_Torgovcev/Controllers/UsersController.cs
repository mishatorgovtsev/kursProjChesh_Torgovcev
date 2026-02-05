using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessPlatform.Data;
using ChessPlatform.Models;
using System.Security.Cryptography;
using System.Text;

namespace kursProjChesh_Torgovcev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ChessDbContext _dbContext;

    public UsersController(ChessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Проверка существования username
        if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest("Имя пользователя уже занято");

        // Проверка существования email
        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("Email уже используется");

        // Хеширование пароля
        var passwordHash = HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            Rating = 1200,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new { userId = user.Id, message = "Регистрация успешна" });
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
            return Unauthorized("Неверное имя пользователя или пароль");

        if (!VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Неверное имя пользователя или пароль");

        if (!user.IsActive)
            return Unauthorized("Аккаунт заблокирован");

        // Обновляем время последнего входа
        user.LastOnlineAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        return Ok(new 
        { 
            userId = user.Id,
            username = user.Username,
            email = user.Email,
            rating = user.Rating,
            message = "Вход выполнен успешно"
        });
    }

    /// <summary>
    /// Получить профиль пользователя
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        
        if (user == null)
            return NotFound("Пользователь не найден");

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Rating,
            user.GamesPlayed,
            user.Wins,
            user.Losses,
            user.Draws,
            user.CreatedAt,
            user.LastOnlineAt
        });
    }

    /// <summary>
    /// Получить таблицу лидеров по рейтингу
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 10)
    {
        var leaders = await _dbContext.Users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.Rating)
            .Take(top)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Rating,
                u.Wins,
                u.GamesPlayed
            })
            .ToListAsync();

        return Ok(leaders);
    }

    // Вспомогательные методы для хеширования паролей
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }
}

// DTO для запросов
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}