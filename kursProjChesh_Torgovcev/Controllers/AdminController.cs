using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessPlatform.Data;
using ChessPlatform.Models;
using System.Security.Cryptography;
using System.Text;

namespace kursProjChesh_Torgovcev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ChessDbContext _dbContext;
    private readonly IConfiguration _config;

    public AdminController(ChessDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config   = config;
    }

    // ── Проверка токена ───────────────────────────────────────────────────────
    private bool IsAuthorized()
    {
        var token = Request.Headers["X-Admin-Token"].FirstOrDefault();
        return token == _config["Admin:Token"];
    }

    private IActionResult Unauthorized401() =>
        StatusCode(401, new { error = "Нет доступа" });

    // ── Авторизация ───────────────────────────────────────────────────────────

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var expectedUser = _config["Admin:Username"];
        var expectedPass = _config["Admin:Password"];

        if (request.Username != expectedUser || request.Password != expectedPass)
            return Unauthorized(new { error = "Неверные данные" });

        return Ok(new
        {
            token   = _config["Admin:Token"],
            message = "Вход выполнен"
        });
    }

    // ═══════════════════════════════════════════════════
    //  ПОЛЬЗОВАТЕЛИ
    // ═══════════════════════════════════════════════════

    /// Полный список пользователей
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string sort = "rating",
        [FromQuery] string order = "desc")
    {
        if (!IsAuthorized()) return Unauthorized401();

        var query = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

        query = (sort, order) switch
        {
            ("rating",      "asc")  => query.OrderBy(u => u.Rating),
            ("rating",      _)      => query.OrderByDescending(u => u.Rating),
            ("username",    "asc")  => query.OrderBy(u => u.Username),
            ("username",    _)      => query.OrderByDescending(u => u.Username),
            ("games",       "asc")  => query.OrderBy(u => u.GamesPlayed),
            ("games",       _)      => query.OrderByDescending(u => u.GamesPlayed),
            ("created",     "asc")  => query.OrderBy(u => u.CreatedAt),
            ("created",     _)      => query.OrderByDescending(u => u.CreatedAt),
            _                       => query.OrderByDescending(u => u.Rating)
        };

        var users = await query.Select(u => new
        {
            u.Id, u.Username, u.Email, u.Rating,
            u.GamesPlayed, u.Wins, u.Losses, u.Draws,
            u.CreatedAt, u.LastOnlineAt, u.IsActive
        }).ToListAsync();

        return Ok(users);
    }

    /// Добавить пользователя
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
    {
        if (!IsAuthorized()) return Unauthorized401();

        if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { error = "Имя пользователя уже занято" });

        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { error = "Email уже используется" });

        var user = new User
        {
            Username     = request.Username,
            Email        = request.Email,
            PasswordHash = HashPassword(request.Password),
            Rating       = request.Rating > 0 ? request.Rating : 1200,
            CreatedAt    = DateTime.Now,
            IsActive     = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new { userId = user.Id, message = "Пользователь создан" });
    }

    /// Изменить рейтинг
    [HttpPatch("users/{id}/rating")]
    public async Task<IActionResult> UpdateRating(int id, [FromBody] UpdateRatingRequest request)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "Пользователь не найден" });

        user.Rating = request.Rating;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Рейтинг обновлён", rating = user.Rating });
    }

    /// Сбросить рейтинг до 1200
    [HttpPost("users/{id}/reset-rating")]
    public async Task<IActionResult> ResetRating(int id)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "Пользователь не найден" });

        user.Rating = 1200;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Рейтинг сброшен до 1200" });
    }

    /// Заблокировать / разблокировать
    [HttpPatch("users/{id}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequest request)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "Пользователь не найден" });

        user.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = user.IsActive ? "Пользователь разблокирован" : "Пользователь заблокирован" });
    }

    /// Удалить пользователя
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "Пользователь не найден" });

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Пользователь удалён" });
    }

    // ═══════════════════════════════════════════════════
    //  ИГРЫ
    // ═══════════════════════════════════════════════════

    /// Список всех игр с фильтрацией
    [HttpGet("games")]
    public async Task<IActionResult> GetGames(
        [FromQuery] int? userId   = null,
        [FromQuery] string? status = null,
        [FromQuery] string sort   = "created",
        [FromQuery] string order  = "desc")
    {
        if (!IsAuthorized()) return Unauthorized401();

        var query = _dbContext.Games
            .Include(g => g.WhitePlayer)
            .Include(g => g.BlackPlayer)
            .Include(g => g.Winner)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(g => g.WhitePlayerId == userId || g.BlackPlayerId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(g => g.Status == status);

        query = (sort, order) switch
        {
            ("created", "asc")  => query.OrderBy(g => g.CreatedAt),
            ("created", _)      => query.OrderByDescending(g => g.CreatedAt),
            ("ended",   "asc")  => query.OrderBy(g => g.EndedAt),
            ("ended",   _)      => query.OrderByDescending(g => g.EndedAt),
            _                   => query.OrderByDescending(g => g.CreatedAt)
        };

        var games = await query.Select(g => new
        {
            g.Id,
            g.Status,
            g.Result,
            g.CreatedAt,
            g.EndedAt,
            g.TimeControlMinutes,
            WhitePlayer = new { g.WhitePlayer.Id, g.WhitePlayer.Username },
            BlackPlayer = new { g.BlackPlayer.Id, g.BlackPlayer.Username },
            Winner      = g.Winner != null ? new { g.Winner.Id, g.Winner.Username } : null
        }).ToListAsync();

        return Ok(games);
    }

    /// Создать игру (от имени двух игроков)
    [HttpPost("games")]
    public async Task<IActionResult> CreateGame([FromBody] AdminCreateGameRequest request)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var white = await _dbContext.Users.FindAsync(request.WhitePlayerId);
        var black = await _dbContext.Users.FindAsync(request.BlackPlayerId);

        if (white == null || black == null)
            return BadRequest(new { error = "Один из игроков не найден" });

        var game = new Game
        {
            WhitePlayerId      = request.WhitePlayerId,
            BlackPlayerId      = request.BlackPlayerId,
            Status             = "Pending",
            CurrentFEN         = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            TimeControlMinutes = request.TimeControlMinutes > 0 ? request.TimeControlMinutes : 10,
            WhiteTimeRemaining = request.TimeControlMinutes * 60,
            BlackTimeRemaining = request.TimeControlMinutes * 60,
            CreatedAt          = DateTime.Now
        };

        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        return Ok(new { gameId = game.Id, message = "Игра создана" });
    }

    /// Принудительно завершить игру
    [HttpPost("games/{id}/finish")]
    public async Task<IActionResult> ForceFinishGame(int id, [FromBody] AdminFinishGameRequest request)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var game = await _dbContext.Games
            .Include(g => g.WhitePlayer)
            .Include(g => g.BlackPlayer)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (game == null) return NotFound(new { error = "Игра не найдена" });
        if (game.Status == "Completed")
            return BadRequest(new { error = "Игра уже завершена" });

        game.Status  = "Completed";
        game.EndedAt = DateTime.Now;
        game.Result  = request.Result; // "white", "black", "draw"

        if (request.Result == "white")      game.WinnerId = game.WhitePlayerId;
        else if (request.Result == "black") game.WinnerId = game.BlackPlayerId;

        // Обновляем статистику
        var white = game.WhitePlayer;
        var black = game.BlackPlayer;

        white.GamesPlayed++;
        black.GamesPlayed++;

        if (request.Result == "white")      { white.Wins++;   black.Losses++; }
        else if (request.Result == "black") { black.Wins++;   white.Losses++; }
        else                                { white.Draws++;  black.Draws++;  }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Игра завершена" });
    }

    /// Удалить игру
    [HttpDelete("games/{id}")]
    public async Task<IActionResult> DeleteGame(int id)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var game = await _dbContext.Games.FindAsync(id);
        if (game == null) return NotFound(new { error = "Игра не найдена" });

        _dbContext.Games.Remove(game);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Игра удалена" });
    }

    // ═══════════════════════════════════════════════════
    //  ТУРНИРЫ
    // ═══════════════════════════════════════════════════

    /// Список всех турниров
    [HttpGet("tournaments")]
    public async Task<IActionResult> GetTournaments(
        [FromQuery] string? status = null,
        [FromQuery] string sort   = "created",
        [FromQuery] string order  = "desc")
    {
        if (!IsAuthorized()) return Unauthorized401();

        var query = _dbContext.Tournaments
            .Include(t => t.Creator)
            .Include(t => t.Winner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        query = (sort, order) switch
        {
            ("created",  "asc") => query.OrderBy(t => t.CreatedAt),
            ("created",  _)     => query.OrderByDescending(t => t.CreatedAt),
            ("starts",   "asc") => query.OrderBy(t => t.StartsAt),
            ("starts",   _)     => query.OrderByDescending(t => t.StartsAt),
            ("title",    "asc") => query.OrderBy(t => t.Title),
            ("title",    _)     => query.OrderByDescending(t => t.Title),
            _                   => query.OrderByDescending(t => t.CreatedAt)
        };

        var list = await query.Select(t => new
        {
            t.Id, t.Title, t.Description, t.Status,
            t.MaxParticipants, t.CurrentParticipants,
            t.TimeControlMinutes, t.StartsAt, t.CreatedAt,
            Creator = new { t.Creator.Id, t.Creator.Username },
            Winner  = t.Winner != null ? new { t.Winner.Id, t.Winner.Username } : null
        }).ToListAsync();

        return Ok(list);
    }

    /// Сменить статус турнира
    [HttpPatch("tournaments/{id}/status")]
    public async Task<IActionResult> SetTournamentStatus(int id, [FromBody] SetStatusRequest request)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var t = await _dbContext.Tournaments.FindAsync(id);
        if (t == null) return NotFound(new { error = "Турнир не найден" });

        t.Status = request.Status;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Статус обновлён", status = t.Status });
    }

    /// Удалить турнир
    [HttpDelete("tournaments/{id}")]
    public async Task<IActionResult> DeleteTournament(int id)
    {
        if (!IsAuthorized()) return Unauthorized401();

        var t = await _dbContext.Tournaments.FindAsync(id);
        if (t == null) return NotFound(new { error = "Турнир не найден" });

        _dbContext.Tournaments.Remove(t);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Турнир удалён" });
    }

    /// Статистика для дашборда
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        if (!IsAuthorized()) return Unauthorized401();

        return Ok(new
        {
            totalUsers       = await _dbContext.Users.CountAsync(),
            activeUsers      = await _dbContext.Users.CountAsync(u => u.IsActive),
            totalGames       = await _dbContext.Games.CountAsync(),
            activeGames      = await _dbContext.Games.CountAsync(g => g.Status == "Pending" || g.Status == "InProgress"),
            completedGames   = await _dbContext.Games.CountAsync(g => g.Status == "Completed"),
            totalTournaments = await _dbContext.Tournaments.CountAsync(),
            activeTournaments= await _dbContext.Tournaments.CountAsync(t => t.Status == "InProgress"),
        });
    }

    // ── Хеш пароля (такой же как в UsersController) ──────────────────────────
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────
public class AdminLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AdminCreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int    Rating   { get; set; } = 1200;
}

public class UpdateRatingRequest
{
    public int Rating { get; set; }
}

public class SetActiveRequest
{
    public bool IsActive { get; set; }
}

public class AdminCreateGameRequest
{
    public int WhitePlayerId      { get; set; }
    public int BlackPlayerId      { get; set; }
    public int TimeControlMinutes { get; set; } = 10;
}

public class AdminFinishGameRequest
{
    public string Result { get; set; } = "draw"; // "white", "black", "draw"
}

public class SetStatusRequest
{
    public string Status { get; set; } = string.Empty;
}