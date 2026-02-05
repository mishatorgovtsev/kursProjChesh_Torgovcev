using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessPlatform.Data;
using ChessPlatform.Models;

namespace kursProjChesh_Torgovcev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TournamentsController : ControllerBase
{
    private readonly ChessDbContext _dbContext;

    public TournamentsController(ChessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Создать новый турнир
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentRequest request)
    {
        var tournament = new Tournament
        {
            Title = request.Title,
            Description = request.Description,
            CreatorId = request.CreatorId,
            MaxParticipants = request.MaxParticipants,
            TimeControlMinutes = request.TimeControlMinutes,
            StartsAt = request.StartsAt,
            Status = "Registration",
            CurrentParticipants = 0,
            CreatedAt = DateTime.Now
        };

        _dbContext.Tournaments.Add(tournament);
        await _dbContext.SaveChangesAsync();

        return Ok(new { tournamentId = tournament.Id, message = "Турнир создан" });
    }

    /// <summary>
    /// Присоединиться к турниру
    /// </summary>
    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinTournament(int id, [FromBody] JoinTournamentRequest request)
    {
        var tournament = await _dbContext.Tournaments.FindAsync(id);
        
        if (tournament == null)
            return NotFound("Турнир не найден");

        if (tournament.Status != "Registration")
            return BadRequest("Регистрация закрыта");

        if (tournament.CurrentParticipants >= tournament.MaxParticipants)
            return BadRequest("Все места заняты");

        // Проверяем не зарегистрирован ли уже
        var existing = await _dbContext.TournamentParticipants
            .FirstOrDefaultAsync(tp => tp.TournamentId == id && tp.UserId == request.UserId);
        
        if (existing != null)
            return BadRequest("Вы уже участвуете в этом турнире");

        var participant = new TournamentParticipant
        {
            TournamentId = id,
            UserId = request.UserId,
            RegisteredAt = DateTime.Now,
            SeedNumber = tournament.CurrentParticipants + 1
        };

        _dbContext.TournamentParticipants.Add(participant);
        tournament.CurrentParticipants++;
        
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Вы присоединились к турниру" });
    }

    /// <summary>
    /// Получить список активных турниров
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetTournaments([FromQuery] string status = "Registration")
    {
        var tournaments = await _dbContext.Tournaments
            .Where(t => t.Status == status)
            .OrderBy(t => t.StartsAt)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.MaxParticipants,
                t.CurrentParticipants,
                t.StartsAt,
                t.TimeControlMinutes
            })
            .ToListAsync();

        return Ok(tournaments);
    }

    /// <summary>
    /// Получить детали турнира с участниками
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTournament(int id)
    {
        var tournament = await _dbContext.Tournaments
            .Include(t => t.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null)
            return NotFound("Турнир не найден");

        return Ok(new
        {
            tournament.Id,
            tournament.Title,
            tournament.Description,
            tournament.Status,
            tournament.MaxParticipants,
            tournament.CurrentParticipants,
            tournament.StartsAt,
            tournament.TimeControlMinutes,
            Participants = tournament.Participants.Select(p => new
            {
                p.UserId,
                p.User.Username,
                p.User.Rating,
                p.SeedNumber
            })
        });
    }

    /// <summary>
    /// Запустить турнир (генерация сетки)
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartTournament(int id)
    {
        var tournament = await _dbContext.Tournaments
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null)
            return NotFound("Турнир не найден");

        if (tournament.Status != "Registration")
            return BadRequest("Турнир уже запущен");

        if (tournament.Participants.Count < 2)
            return BadRequest("Недостаточно участников");

        // Закрываем регистрацию
        tournament.Status = "InProgress";
        tournament.TotalRounds = (int)Math.Log2(tournament.MaxParticipants);

        // Создаём сетку (олимпийская система)
        await GenerateBracket(tournament);

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Турнир запущен", rounds = tournament.TotalRounds });
    }

    /// <summary>
    /// Получить турнирную сетку
    /// </summary>
    [HttpGet("{id}/bracket")]
    public async Task<IActionResult> GetBracket(int id)
    {
        var matches = await _dbContext.TournamentMatches
            .Where(tm => tm.TournamentId == id)
            .OrderBy(tm => tm.RoundNumber)
            .ThenBy(tm => tm.MatchNumber)
            .Include(tm => tm.Player1)
            .Include(tm => tm.Player2)
            .Include(tm => tm.Winner)
            .Select(tm => new
            {
                tm.Id,
                tm.RoundNumber,
                tm.MatchNumber,
                Player1 = tm.Player1 != null ? new { tm.Player1.Id, tm.Player1.Username } : null,
                Player2 = tm.Player2 != null ? new { tm.Player2.Id, tm.Player2.Username } : null,
                Winner = tm.Winner != null ? new { tm.Winner.Id, tm.Winner.Username } : null,
                tm.Status,
                tm.IsFinal
            })
            .ToListAsync();

        return Ok(matches);
    }

    // Генерация олимпийской сетки
    private async Task GenerateBracket(Tournament tournament)
    {
        int participantsCount = tournament.Participants.Count;
        int rounds = (int)Math.Log2(tournament.MaxParticipants);
        int matchesInFirstRound = tournament.MaxParticipants / 2;

        // Создаём матчи первого раунда
        var participants = tournament.Participants.OrderBy(p => p.SeedNumber).ToList();
        
        for (int i = 0; i < matchesInFirstRound; i++)
        {
            var match = new TournamentMatch
            {
                TournamentId = tournament.Id,
                RoundNumber = 1,
                MatchNumber = i + 1,
                Player1Id = i * 2 < participants.Count ? participants[i * 2].UserId : null,
                Player2Id = i * 2 + 1 < participants.Count ? participants[i * 2 + 1].UserId : null,
                Status = "Pending"
            };

            _dbContext.TournamentMatches.Add(match);
        }

        await _dbContext.SaveChangesAsync();
        
        // TODO: Создать связи для следующих раундов (NextMatchId)
    }
}

// DTO для запросов
public class CreateTournamentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatorId { get; set; }
    public int MaxParticipants { get; set; } = 16;
    public int TimeControlMinutes { get; set; } = 90;
    public DateTime StartsAt { get; set; }
}

public class JoinTournamentRequest
{
    public int UserId { get; set; }
}