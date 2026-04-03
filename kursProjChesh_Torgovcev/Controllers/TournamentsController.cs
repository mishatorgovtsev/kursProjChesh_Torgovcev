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
    /// Получить ВСЕ турниры (без фильтра по статусу)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllTournaments()
    {
        var tournaments = await _dbContext.Tournaments
            .OrderByDescending(t => t.CreatedAt)
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
    /// Покинуть турнир (отписаться)
    /// </summary>
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveTournament(int id, [FromBody] JoinTournamentRequest request)
    {
        var tournament = await _dbContext.Tournaments.FindAsync(id);
        if (tournament == null)
            return NotFound("Турнир не найден");

        if (tournament.Status != "Registration")
            return BadRequest("Турнир уже начался");

        var participant = await _dbContext.TournamentParticipants
            .FirstOrDefaultAsync(tp => tp.TournamentId == id && tp.UserId == request.UserId);

        if (participant == null)
            return BadRequest("Вы не являетесь участником");

        _dbContext.TournamentParticipants.Remove(participant);
        tournament.CurrentParticipants = Math.Max(0, tournament.CurrentParticipants - 1);

        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Вы покинули турнир" });
    }

    /// <summary>
    /// Получить список турниров</summary>

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
            tournament.TotalRounds,
            tournament.CreatorId,
            Winner = tournament.WinnerId.HasValue
                ? _dbContext.Users.Where(u => u.Id == tournament.WinnerId)
                    .Select(u => new { u.Id, u.Username }).FirstOrDefault()
                : null,
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

        tournament.Status = "InProgress";
        tournament.TotalRounds = (int)Math.Log2(tournament.MaxParticipants);

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
                tm.NextMatchId,
                tm.GameId,
                Player1 = tm.Player1 != null ? new { tm.Player1.Id, tm.Player1.Username } : null,
                Player2 = tm.Player2 != null ? new { tm.Player2.Id, tm.Player2.Username } : null,
                Winner = tm.Winner != null ? new { tm.Winner.Id, tm.Winner.Username } : null,
                tm.Status,
                tm.IsFinal
            })
            .ToListAsync();

        return Ok(matches);
    }

    /// <summary>
    /// Начать матч — создать игру для двух участников
    /// </summary>
    [HttpPost("{tournamentId}/matches/{matchId}/start")]
    public async Task<IActionResult> StartMatch(int tournamentId, int matchId)
    {
        var match = await _dbContext.TournamentMatches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId);

        if (match == null)
            return NotFound(new { error = "Матч не найден" });

        if (match.Status == "InProgress")
            return Ok(new { gameId = match.GameId, message = "Игра уже создана" });

        if (match.Player1Id == null || match.Player2Id == null)
            return BadRequest(new { error = "Оба игрока должны быть известны" });

        // Создаём игру
        var game = new Game
        {
            WhitePlayerId      = match.Player1Id.Value,
            BlackPlayerId      = match.Player2Id.Value,
            Status             = "Pending",
            CurrentFEN         = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            TimeControlMinutes = match.Tournament.TimeControlMinutes,
            WhiteTimeRemaining = match.Tournament.TimeControlMinutes * 60,
            BlackTimeRemaining = match.Tournament.TimeControlMinutes * 60,
            TournamentId       = tournamentId
        };

        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Привязываем игру к матчу
        match.GameId = game.Id;
        match.Status = "InProgress";
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            gameId      = game.Id,
            whiteId     = match.Player1Id,
            blackId     = match.Player2Id,
            whiteName   = match.Player1!.Username,
            blackName   = match.Player2!.Username,
            timeMin     = match.Tournament.TimeControlMinutes,
            message     = "Игра создана"
        });
    }

    /// <summary>
    /// Завершить матч и продвинуть победителя в следующий раунд</summary>

    [HttpPost("{tournamentId}/matches/{matchId}/complete")]
    public async Task<IActionResult> CompleteMatch(int tournamentId, int matchId, [FromBody] CompleteMatchRequest request)
    {
        var match = await _dbContext.TournamentMatches
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId);

        if (match == null)
            return NotFound("Матч не найден");

        if (match.Status == "Completed")
            return BadRequest("Матч уже завершён");

        // Проверяем что победитель является участником матча
        if (request.WinnerId != match.Player1Id && request.WinnerId != match.Player2Id)
            return BadRequest("Победитель должен быть участником матча");

        match.WinnerId = request.WinnerId;
        match.Status = "Completed";

        // Продвигаем победителя в следующий матч
        if (match.NextMatchId != null)
        {
            var nextMatch = await _dbContext.TournamentMatches.FindAsync(match.NextMatchId);
            if (nextMatch != null)
            {
                if (nextMatch.Player1Id == null)
                    nextMatch.Player1Id = request.WinnerId;
                else
                    nextMatch.Player2Id = request.WinnerId;

                // Оба игрока известны — матч готов к игре
                if (nextMatch.Player1Id != null && nextMatch.Player2Id != null)
                    nextMatch.Status = "Ready";
            }
        }
        else if (match.IsFinal)
        {
            // Финал завершён — закрываем турнир
            match.Tournament.WinnerId = request.WinnerId;
            match.Tournament.Status = "Completed";
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Матч завершён", winnerId = request.WinnerId });
    }

    // Генерация полной олимпийской сетки с NextMatchId
    private async Task GenerateBracket(Tournament tournament)
    {
        var participants = tournament.Participants.OrderBy(p => p.SeedNumber).ToList();
        int totalRounds = (int)Math.Log2(tournament.MaxParticipants);

        // Словарь для быстрого доступа к матчам по (раунд, номер)
        var allMatches = new Dictionary<(int round, int match), TournamentMatch>();

        // Шаг 1: создаём все матчи всех раундов
        for (int round = 1; round <= totalRounds; round++)
        {
            int matchesInRound = (int)Math.Pow(2, totalRounds - round);

            for (int matchNum = 1; matchNum <= matchesInRound; matchNum++)
            {
                bool isFinal = (round == totalRounds);

                var match = new TournamentMatch
                {
                    TournamentId = tournament.Id,
                    RoundNumber = round,
                    MatchNumber = matchNum,
                    Status = "Pending",
                    IsFinal = isFinal,
                    Player1Id = null,
                    Player2Id = null
                };

                allMatches[(round, matchNum)] = match;
                _dbContext.TournamentMatches.Add(match);
            }
        }

        // Сохраняем чтобы EF присвоил Id всем матчам
        await _dbContext.SaveChangesAsync();

        // Шаг 2: расставляем игроков в первом раунде
        int matchesInFirstRound = (int)Math.Pow(2, totalRounds - 1);
        for (int i = 0; i < matchesInFirstRound; i++)
        {
            var match = allMatches[(1, i + 1)];
            match.Player1Id = i * 2 < participants.Count ? participants[i * 2].UserId : (int?)null;
            match.Player2Id = i * 2 + 1 < participants.Count ? participants[i * 2 + 1].UserId : (int?)null;

            // Bye: один игрок без соперника — автоматически проходит
            if (match.Player1Id != null && match.Player2Id == null)
            {
                match.WinnerId = match.Player1Id;
                match.Status = "Completed";
            }
            // Оба игрока известны — матч готов к игре
            else if (match.Player1Id != null && match.Player2Id != null)
            {
                match.Status = "Ready";
            }
        }

        // Шаг 3: проставляем NextMatchId
        // Победитель матча (round, matchNum) идёт в (round+1, ceil(matchNum/2))
        for (int round = 1; round < totalRounds; round++)
        {
            int matchesInRound = (int)Math.Pow(2, totalRounds - round);

            for (int matchNum = 1; matchNum <= matchesInRound; matchNum++)
            {
                int nextMatchNum = (int)Math.Ceiling(matchNum / 2.0);
                var currentMatch = allMatches[(round, matchNum)];
                var nextMatch = allMatches[(round + 1, nextMatchNum)];
                currentMatch.NextMatchId = nextMatch.Id;
            }
        }

        await _dbContext.SaveChangesAsync();
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

public class CompleteMatchRequest
{
    public int WinnerId { get; set; }
}